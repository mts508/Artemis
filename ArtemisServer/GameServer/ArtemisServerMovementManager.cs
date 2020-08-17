using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtemisServer.GameServer
{
    class ArtemisServerMovementManager : MonoBehaviour
    {
        private static ArtemisServerMovementManager instance;

        private const float CLASH_THRESHOLD = 0.7f;
        private const float RESOLUTION_STEP = 0.25f;

        private float GetActorMovementCost(ActorData actor, out float lastStepCost)
        {
            float movementCost = 0;
            lastStepCost = 0;
            ActorMovement actorMovement = actor.GetActorMovement();
            LineData.LineInstance movementLine = actor.TeamSensitiveData_authority.MovementLine;
            if (movementLine != null)
            {
                GridPos prevPos = actor.InitialMoveStartSquare.GetGridPosition();
                foreach (var curPos in movementLine.m_positions)
                {
                    lastStepCost = actorMovement.BuildPathTo(Board.Get().GetSquare(prevPos), Board.Get().GetSquare(curPos)).next?.moveCost ?? 0f;  // TODO optimize this atrocity
                    movementCost += lastStepCost;
                    prevPos = curPos;
                }
            }
            return movementCost;
        }

        internal void UpdatePlayerRemainingMovement(ActorData actor, bool send = true)
        {
            ActorTurnSM turnSm = actor.gameObject.GetComponent<ActorTurnSM>();
            ActorController actorController = actor.gameObject.GetComponent<ActorController>();
            ActorMovement actorMovement = actor.GetActorMovement();

            float movementCost = GetActorMovementCost(actor, out float lastStepCost);

            bool cannotExceedMaxMovement = GameplayData.Get()?.m_movementMaximumType == GameplayData.MovementMaximumType.CannotExceedMax;

            List<ActorTargeting.AbilityRequestData> abilityRequest = actor.TeamSensitiveData_authority.GetAbilityRequestData();
            bool abilitySet = !abilityRequest.IsNullOrEmpty() && abilityRequest[0].m_actionType != AbilityData.ActionType.INVALID_ACTION;
            actor.RemainingHorizontalMovement = actorMovement.CalculateMaxHorizontalMovement() - movementCost;
            actor.RemainingMovementWithQueuedAbility = abilitySet ? actor.RemainingHorizontalMovement : actorMovement.CalculateMaxHorizontalMovement(true) - movementCost;
            actor.QueuedMovementAllowsAbility = abilitySet ||
                (cannotExceedMaxMovement
                    ? actor.RemainingMovementWithQueuedAbility >= 0
                    : actor.RemainingMovementWithQueuedAbility + lastStepCost > 0);

            Log.Info($"UpdatePlayerMovement:  Basic: {actor.m_postAbilityHorizontalMovement}/{actor.m_maxHorizontalMovement}, +", 
                $"Remaining: {actor.RemainingMovementWithQueuedAbility}/{actor.RemainingHorizontalMovement}, " +
                $"Movement cost: {movementCost}, Ability set: {abilitySet}, Ability allowed: {actor.QueuedMovementAllowsAbility}, " +
                $"Movement max type: {GameplayData.Get()?.m_movementMaximumType}");

            actorMovement.UpdateSquaresCanMoveTo();
            if (send)
            {
                actorController.CallRpcUpdateRemainingMovement(actor.RemainingHorizontalMovement, actor.RemainingMovementWithQueuedAbility);
            }
        }

        public void CmdSetSquare(ActorTurnSM actorTurnSM, int x, int y, bool setWaypoint)
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
                ClearMovementRequest(actor, false);
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
                UpdatePlayerRemainingMovement(actor); // TODO updating because we cancelled movement - perhaps we should not cancel in this case
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

            UpdatePlayerRemainingMovement(actor);
            actorTurnSM.CallRpcTurnMessage((int)TurnMessage.MOVEMENT_ACCEPTED, 0);
        }

        public void ClearMovementRequest(ActorData actor, bool sendUpdateToClient)
        {
            actor.TeamSensitiveData_authority.MovementLine?.m_positions.Clear();
            actor.MoveFromBoardSquare = actor.InitialMoveStartSquare;
            UpdatePlayerRemainingMovement(actor, sendUpdateToClient);
        }

        public void ResolveMovement()
        {
            Dictionary<int, BoardSquarePathInfo> paths = new Dictionary<int, BoardSquarePathInfo>();
            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                paths.Add(actor.ActorIndex, ResolveMovement(actor));
            }

            Dictionary<int, BoardSquarePathInfo> nodes = new Dictionary<int, BoardSquarePathInfo>(paths);
            bool finished = false;
            for (float time = 0; !finished; time += RESOLUTION_STEP)
            {
                if (!ResolveSubstep(nodes, time, out finished))
                {
                    // TODO optimize
                    time = -RESOLUTION_STEP;
                    nodes = new Dictionary<int, BoardSquarePathInfo>(paths);
                    Log.Info("Restarting movement resolution loop");
                }
            }

            var movementActions = ArtemisServerBarrierManager.Get().OnMovement(paths);
            ArtemisServerResolutionManager.Get().SendMovementActions(movementActions);

            // TODO ClientMovementManager.MsgServerMovementStarting

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

        private bool ResolveSubstep(Dictionary<int, BoardSquarePathInfo> nodes, float time, out bool finished)
        {
            // Advancing
            finished = true;
            foreach (var node in new Dictionary<int, BoardSquarePathInfo>(nodes))
            {
                if (node.Value.next != null)
                {
                    finished = false;
                    if (node.Value.next.moveCost < time)
                    {
                        nodes[node.Key] = node.Value.next;
                    }
                }
            }

            // Grouping by square
            var nodesBySquare = new Dictionary<GridPos, List<KeyValuePair<int, BoardSquarePathInfo>>>();
            foreach (var node in nodes)
            {
                GridPos square = node.Value.square.GetGridPosition();
                if (!nodesBySquare.ContainsKey(square))
                {
                    nodesBySquare[square] = new List<KeyValuePair<int, BoardSquarePathInfo>> { node };
                }
                else
                {
                    nodesBySquare[square].Add(node);
                }
            }

            // Detecting clashes
            foreach (var node in nodesBySquare)
            {
                GridPos pos = node.Key;
                List<KeyValuePair<int, BoardSquarePathInfo>> pathInfos = node.Value;
                if (pathInfos.Count > 1)
                {
                    pathInfos.Sort(delegate (KeyValuePair<int, BoardSquarePathInfo> a, KeyValuePair<int, BoardSquarePathInfo> b)
                    {
                        return a.Value.moveCost.CompareTo(b.Value.moveCost);
                    });
                    for (int i = 0; i < pathInfos.Count - 1; ++i)
                    {
                        for (int j = i + 1; j < pathInfos.Count; ++j)
                        {
                            BoardSquarePathInfo a = pathInfos[i].Value;
                            BoardSquarePathInfo b = pathInfos[j].Value;
                            bool mutualClash = b.moveCost - a.moveCost < CLASH_THRESHOLD;
                            if (mutualClash)
                            {
                                a.m_moverClashesHere = true;
                                b.m_moverClashesHere = true;
                            }

                            // if both stop
                            if (a.next == null && b.next == null)
                            {
                                if (mutualClash)
                                {
                                    var occupiedSquares = new HashSet<GridPos>(nodesBySquare.Keys);
                                    ActorData aActor = Utils.GetActorByIndex(pathInfos[i].Key);
                                    a.next = BackOff(aActor, a, occupiedSquares); // TODO choose winner randomly? a and b can have idential backoff, a has an advantage currently
                                    ActorData bActor = Utils.GetActorByIndex(pathInfos[j].Key);
                                    b.next = BackOff(bActor, b, occupiedSquares);
                                }
                                else
                                {
                                    if (b.prev != null)
                                    {
                                        b.prev.next = null;
                                        b.prev.m_moverBumpedFromClash = true;
                                        return false;
                                    }
                                    else
                                    {
                                        Log.Error($"Failed to resolve movement for player {pathInfos[i].Key} -- but they did not move!");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        private BoardSquarePathInfo BackOff(ActorData actor, BoardSquarePathInfo pathEnd, HashSet<GridPos> occupiedSquares)
        {
            if (actor == null)
            {
                Log.Error($"Backoff failed because actor is null!");
                return null;
            }
            Log.Info($"Calculating backoff for {actor.DisplayName}");

            BoardSquare dest = pathEnd.prev?.square ?? pathEnd.square;
            if (occupiedSquares.Contains(dest.GetGridPosition()))
            {
                bool diagMovementAllowed = GameplayData.Get().m_diagonalMovement != GameplayData.DiagonalMovement.Disabled;
                List<BoardSquare> neighbours = new List<BoardSquare>(8);
                Queue<BoardSquare> candidates = new Queue<BoardSquare>();
                candidates.Enqueue(pathEnd.square);
                while (candidates.Count > 0)
                {
                    BoardSquare s = candidates.Dequeue();
                    if (!occupiedSquares.Contains(s.GetGridPosition()))
                    {
                        dest = s;
                        break;
                    }

                    neighbours.Clear();
                    if (!diagMovementAllowed)
                    {
                        Board.Get().GetStraightAdjacentSquares(s.x, s.y, ref neighbours);
                    }
                    else
                    {
                        Board.Get().GetAllAdjacentSquares(s.x, s.y, ref neighbours);
                    }
                    neighbours.Sort(delegate (BoardSquare a, BoardSquare b)
                    {
                        return dest.HorizontalDistanceInWorldTo(a).CompareTo(dest.HorizontalDistanceInWorldTo(b));
                    });
                    foreach (var n in neighbours)
                    {
                        if (n.IsBoardHeight())
                        {
                            candidates.Enqueue(n);
                        }
                    }
                }
            }

            if (occupiedSquares.Contains(dest.GetGridPosition()))
            {
                Log.Error($"Backoff failed to find a free square for {actor.DisplayName}!");
            }
            occupiedSquares.Add(dest.GetGridPosition());

            BoardSquarePathInfo result = actor.GetActorMovement().BuildPathTo_IgnoreBarriers(pathEnd.square, dest);
            result.heuristicCost += pathEnd.heuristicCost;  // not actually correct but shouldn't matter
            result.moveCost += pathEnd.moveCost;
            result.m_moverBumpedFromClash = true;
            return result;
        }

        private BoardSquarePathInfo ResolveMovement(ActorData actor)
        {
            ActorTeamSensitiveData atsd = actor.TeamSensitiveData_authority;
            ActorMovement actorMovement = actor.GetActorMovement();
            BoardSquare start = actor.InitialMoveStartSquare;
            BoardSquare end = actor.MoveFromBoardSquare;

            BoardSquarePathInfo path;
            if (atsd.MovementLine != null)
            {
                path = BuildPathAlongMovementLine(actor);
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

        private static BoardSquarePathInfo BuildPathAlongMovementLine(ActorData actor)
        {
            ActorTeamSensitiveData atsd = actor.TeamSensitiveData_authority;
            ActorMovement actorMovement = actor.GetActorMovement();
            BoardSquare start = actor.InitialMoveStartSquare;
            BoardSquare end = actor.MoveFromBoardSquare;
            // TODO refactor this atrocity
            BoardSquarePathInfo path = actorMovement.BuildPathTo(start, start);
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

            return path;
        }

        public void UpdateTurn()
        {

        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }

            if (GameFlowData.Get() != null)
            {
                foreach (var player in GameFlowData.Get().GetPlayers())
                {
                    ActorTurnSM actorTurnSM = player.GetComponent<ActorTurnSM>();
                    actorTurnSM.OnCmdSetSquareCallback += CmdSetSquare;
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            if (GameFlowData.Get() != null)
            {
                foreach (var player in GameFlowData.Get().GetPlayers())
                {
                    ActorTurnSM actorTurnSM = player.GetComponent<ActorTurnSM>();
                    actorTurnSM.OnCmdSetSquareCallback -= CmdSetSquare;
                }
            }
        }

        public static ArtemisServerMovementManager Get()
        {
            return instance;
        }
    }
}
