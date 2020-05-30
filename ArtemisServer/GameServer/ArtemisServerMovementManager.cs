using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtemisServer.GameServer
{
    class ArtemisServerMovementManager : MonoBehaviour
    {
        private static ArtemisServerMovementManager instance;

        internal void UpdatePlayerMovement(ActorData actor, bool send = true)
        {
            ActorTurnSM turnSm = actor.gameObject.GetComponent<ActorTurnSM>();
            ActorController actorController = actor.gameObject.GetComponent<ActorController>();
            ActorMovement actorMovement = actor.GetActorMovement();

            float movementCost = 0;
            float cost = 0;
            GridPos prevPos = actor.InitialMoveStartSquare.GetGridPosition();

            LineData.LineInstance movementLine = actor.TeamSensitiveData_authority.MovementLine;
            if (movementLine != null)
            {
                foreach (var curPos in movementLine.m_positions)
                {
                    cost = actorMovement.BuildPathTo(Board.Get().GetSquare(prevPos), Board.Get().GetSquare(curPos)).next?.moveCost ?? 0f;  // TODO optimize this atrocity
                    movementCost += cost;
                    prevPos = curPos;
                }
            }

            bool cannotExceedMaxMovement = GameplayData.Get()?.m_movementMaximumType == GameplayData.MovementMaximumType.CannotExceedMax;

            List<ActorTargeting.AbilityRequestData> abilityRequest = actor.TeamSensitiveData_authority.GetAbilityRequestData();
            bool abilitySet = !abilityRequest.IsNullOrEmpty() && abilityRequest[0].m_actionType != AbilityData.ActionType.INVALID_ACTION;

            foreach (var a in abilityRequest)
            {
                Log.Info($"Ability target: {a.m_actionType} {a.m_targets}");
            }

            actor.m_postAbilityHorizontalMovement = actorMovement.GetAdjustedMovementFromBuffAndDebuff(4, true);  // TODO Get default movement ranges
            actor.m_maxHorizontalMovement = actorMovement.GetAdjustedMovementFromBuffAndDebuff(8, false);
            // TODO check AbilityData.GetQueuedAbilitiesAllowMovement/Sprinting etc

            actor.RemainingHorizontalMovement = (abilitySet ? actor.m_postAbilityHorizontalMovement : actor.m_maxHorizontalMovement) - movementCost;
            actor.RemainingMovementWithQueuedAbility = actor.m_postAbilityHorizontalMovement - movementCost;
            actor.QueuedMovementAllowsAbility = abilitySet || 
                (cannotExceedMaxMovement
                    ? movementCost <= actor.m_postAbilityHorizontalMovement
                    : movementCost - cost < actor.m_postAbilityHorizontalMovement);

            Log.Info($"UpdatePlayerMovement: Basic: {actor.m_postAbilityHorizontalMovement}/{actor.m_maxHorizontalMovement}, " +
                $"Remaining: {actor.RemainingMovementWithQueuedAbility}/{actor.RemainingHorizontalMovement}, " +
                $"Movement cost: {movementCost}, Ability set: {abilitySet}, Ability allowed: {actor.QueuedMovementAllowsAbility}");

            if (send)
            {
                actorController.CallRpcUpdateRemainingMovement(actor.RemainingHorizontalMovement, actor.RemainingMovementWithQueuedAbility);
            }
        }

        private void CmdSetSquare(ActorTurnSM actorTurnSM, int x, int y, bool setWaypoint)
        {
            ActorData actor = actorTurnSM.gameObject.GetComponent<ActorData>();

            if (!GameFlowData.Get().IsInDecisionState())
            {
                Log.Info($"Recieved CmdSetSquare not in desicion state! {actor.DisplayName} [{x}, {y}] (setWaypoint = {setWaypoint})");
                actorTurnSM.CallRpcTurnMessage((int)TurnMessage.MOVEMENT_REJECTED, 0);
                return;
            }

            Log.Info($"CmdSetSquare {actor.DisplayName} [{x}, {y}] (setWaypoint = {setWaypoint})");

            BoardSquare boardSquare = Board.Get().GetSquare(x, y);
            ActorMovement actorMovement = actor.GetActorMovement();

            if (!setWaypoint)
            {
                actor.TeamSensitiveData_authority.MovementLine?.m_positions.Clear();
                actor.MoveFromBoardSquare = actor.InitialMoveStartSquare;
                UpdatePlayerMovement(actor, false);
            }

            actorMovement.UpdateSquaresCanMoveTo();

            if (!actor.CanMoveToBoardSquare(boardSquare))
            {
                boardSquare = actorMovement.GetClosestMoveableSquareTo(boardSquare, false);
            }
            if (actor.TeamSensitiveData_authority.MovementLine == null)
            {
                actor.TeamSensitiveData_authority.MovementLine = new LineData.LineInstance();
            }
            if (actor.TeamSensitiveData_authority.MovementLine.m_positions.Count == 0)
            {
                actor.TeamSensitiveData_authority.MovementLine.m_positions.Add(actor.InitialMoveStartSquare.GetGridPosition());
            }

            BoardSquarePathInfo path = actorMovement.BuildPathTo(actor.TeamSensitiveData_authority.MoveFromBoardSquare, boardSquare);

            if (path == null)  // TODO check cost
            {
                Log.Info($"CmdSetSquare: Movement rejected");
                UpdatePlayerMovement(actor); // TODO updating because we cancelled movement - perhaps we should not cancel in this case
                actorTurnSM.CallRpcTurnMessage((int)TurnMessage.MOVEMENT_REJECTED, 0);
                return;
            }

            //List<GridPos> posList = path.ToGridPosPath();
            List<GridPos> posList = new List<GridPos>();
            for (var pathNode = path; pathNode.next != null; pathNode = pathNode.next)
            {
                posList.Add(pathNode.next.square.GetGridPosition()); // TODO why doesnt path.ToGridPosPath() work?
            }

            actor.TeamSensitiveData_authority.MovementLine.m_positions.AddRange(posList);
            actor.TeamSensitiveData_authority.MoveFromBoardSquare = boardSquare;
            actor.MoveFromBoardSquare = boardSquare;

            UpdatePlayerMovement(actor);
            actorTurnSM.CallRpcTurnMessage((int)TurnMessage.MOVEMENT_ACCEPTED, 0);
        }

        public void ResolveMovement()
        {
            Dictionary<int, BoardSquarePathInfo> paths = new Dictionary<int, BoardSquarePathInfo>();
            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                paths.Add(actor.ActorIndex, ResolveMovement(actor));
            }

            // TODO merge paths (clashes, etc.)

            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                BoardSquarePathInfo start = paths[actor.ActorIndex];
                BoardSquarePathInfo end = start;
                while (end.next != null) end = end.next;

                ActorTeamSensitiveData atsd = actor.TeamSensitiveData_authority;

                // TODO GetPathEndpoint everywhere

                // TODO movement camera bounds
                actor.MoveFromBoardSquare = end.square;
                actor.InitialMoveStartSquare = end.square;

                atsd.CallRpcMovement(
                     GameEventManager.EventType.Invalid,
                     GridPosProp.FromGridPos(start.square.GetGridPosition()),
                     GridPosProp.FromGridPos(end.square.GetGridPosition()),
                     MovementUtils.SerializePath(start),
                     ActorData.MovementType.Normal,
                     false,
                     false);

                atsd.MovementLine?.m_positions.Clear();
            }
            Log.Info("Movement resolved");
        }

        private BoardSquarePathInfo ResolveMovement(ActorData actor)
        {
            ActorTurnSM turnSm = actor.gameObject.GetComponent<ActorTurnSM>();
            ActorController actorController = actor.gameObject.GetComponent<ActorController>();
            AbilityData abilityData = actor.gameObject.GetComponent<AbilityData>();
            ActorTeamSensitiveData atsd = actor.TeamSensitiveData_authority;
            ActorMovement actorMovement = actor.GetActorMovement();

            BoardSquare start = actor.InitialMoveStartSquare;
            BoardSquare end = actor.MoveFromBoardSquare;

            GridPosProp startPosProp = GridPosProp.FromGridPos(start.GetGridPosition());
            GridPosProp endPosProp = GridPosProp.FromGridPos(end.GetGridPosition());

            BoardSquarePathInfo path;
            if (atsd.MovementLine != null)
            {
                // TODO refactor this atrocity
                path = actorMovement.BuildPathTo(start, start);
                BoardSquarePathInfo node = path;
                foreach (var curPos in atsd.MovementLine.m_positions)
                {
                    node.next = actorMovement.BuildPathTo(node.square, Board.Get().GetSquare(curPos)).next;
                    if (node.next == null)
                    {
                        continue;
                    }
                    node.next.moveCost += node.moveCost;
                    node.next.prev = node;
                    node = node.next;
                }
            }
            else
            {
                path = actorMovement.BuildPathTo(start, end);
            }

            if (path == null)
            {
                path = actorMovement.BuildPathTo(start, start);
            }

            for (var pathNode = path; pathNode.next != null; pathNode = pathNode.next)
            {
                pathNode.m_unskippable = true;  // so that aestetic path is not optimized (see CreateRunAndVaultAesteticPath)
            }

            var path2 = path;
            while (path2.next != null)
            {
                Log.Info($"FINAL PATH {path2.square.GetGridPosition()}");
                path2 = path2.next;
            }
            Log.Info($"FINAL PATH {path2.square.GetGridPosition()}");

            return path;
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }

            foreach (var player in GameFlowData.Get().GetPlayers())
            {
                ActorTurnSM actorTurnSM = player.GetComponent<ActorTurnSM>();
                actorTurnSM.OnCmdSetSquareCallback += CmdSetSquare;
            }
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            foreach (var player in GameFlowData.Get().GetPlayers())
            {
                ActorTurnSM actorTurnSM = player.GetComponent<ActorTurnSM>();
                actorTurnSM.OnCmdSetSquareCallback -= CmdSetSquare;
            }
        }

        public static ArtemisServerMovementManager Get()
        {
            return instance;
        }


    }
}
