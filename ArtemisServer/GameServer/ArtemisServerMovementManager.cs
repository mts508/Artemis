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

            List<GridPos> posList = new List<GridPos>();
            BoardSquarePathInfo pathNode = path;
            while (pathNode.next != null)
            {
                posList.Add(pathNode.next.square.GetGridPosition());
                pathNode.m_unskippable = true;  // so that aestetic path is not optimized (see CreateRunAndVaultAesteticPath)
                pathNode = pathNode.next;
            }

            actor.TeamSensitiveData_authority.MovementLine.m_positions.AddRange(posList);
            actor.TeamSensitiveData_authority.MoveFromBoardSquare = boardSquare;
            actor.MoveFromBoardSquare = boardSquare;

            UpdatePlayerMovement(actor);
            actorTurnSM.CallRpcTurnMessage((int)TurnMessage.MOVEMENT_ACCEPTED, 0);
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
                actorTurnSM.OnCmdSetSquareCallback = delegate (int x, int y, bool setWaypoint)
                {
                    CmdSetSquare(actorTurnSM, x, y, setWaypoint);
                };
            }
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public static ArtemisServerMovementManager Get()
        {
            return instance;
        }


    }
}
