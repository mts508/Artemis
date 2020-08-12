using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtemisServer.GameServer
{
    class ArtemisServerGameManager : MonoBehaviour
    {
        private static ArtemisServerGameManager instance = null;

        public void StartGame()
        {
            Log.Info("ArtemisServerGameManager.StartGame");
            Init();
            StartCoroutine(GameLoop());
        }

        private void Init()
        {
            PlaceCharacters();
        }

        private IEnumerator GameLoop()
        {
            yield return PrepareForGame();
            yield return TurnLoop();
            yield return EndGame();
        }

        private IEnumerator PrepareForGame()
        {
            Log.Info("Preparing for game");

            //GameFlowData.Get().enabled = true;
            GameFlowData.Get().gameState = GameState.Deployment;
            yield return new WaitForSeconds(5);
            GameFlow.Get().CallRpcSetMatchTime(0);
            GameFlowData.Get().Networkm_currentTurn = 0;
            Log.Info("Done preparing for game");
        }

        private IEnumerator EndGame()
        {
            GameFlowData.Get().gameState = GameState.EndingGame;
            yield break;
        }

        private void PlaceCharacters()
        {
            // TODO
            Log.Info("Placing characters");
            int x = 16;
            int y = 9;
            foreach (var player in GameFlowData.Get().GetPlayers())
            {
                //UnityUtils.DumpGameObject(player);

                ActorData actorData = player.GetComponent<ActorData>();
                var atsd = actorData.TeamSensitiveData_authority;
                if (atsd == null) continue;

                BoardSquare start = Board.Get().GetSquare(x++, y);
                GridPosProp startProp = GridPosProp.FromGridPos(start.GetGridPosition());

                atsd.CallRpcMovement(GameEventManager.EventType.Invalid,
                    startProp, startProp,
                    null, ActorData.MovementType.Teleport, false, false);

                actorData.ServerLastKnownPosSquare = start;
                actorData.InitialMoveStartSquare = start;
                actorData.MoveFromBoardSquare = start;
                Log.Info($"Placing {actorData.DisplayName} at {startProp.m_x}, {startProp.m_y}");  // PATCH internal -> public ActorData.DisplayName
            }
            Log.Info("Done placing characters");
        }

        private IEnumerator TurnLoop()
        {
            while(true)
            {
                Log.Info("TurnLoop");
                yield return TurnDecision();
                yield return new WaitForSeconds(1);
                yield return ActionResolution();
                yield return MovementResolution();
                yield return EndTurn();
            }
        }

        private IEnumerator TurnDecision()
        {
            GameFlowData.Get().gameState = GameState.BothTeams_Decision;
            // TODO timebanks
            GameFlowData.Get().Networkm_willEnterTimebankMode = false;
            GameFlowData.Get().Networkm_timeRemainingInDecisionOverflow = 0;

            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                var turnSm = actor.gameObject.GetComponent<ActorTurnSM>();
                ArtemisServerGameManager.Get().ClearAbilityRequests(actor);
                actor.AppearAtBoardSquare(actor.TeamSensitiveData_authority.MoveFromBoardSquare);
                ArtemisServerMovementManager.Get().ClearMovementRequest(actor, true);
                turnSm.CallRpcTurnMessage((int)TurnMessage.TURN_START, 0);
            }
            ArtemisServerMovementManager.Get().UpdateTurn();
            ArtemisServerBarrierManager.Get().UpdateTurn();
            SharedEffectBarrierManager.Get().UpdateTurn();

            Log.Info("TurnDecision");

            while (GameFlowData.Get().GetTimeRemainingInDecision() > 0)
            {
                Log.Info($"Time remaining: {GameFlowData.Get().GetTimeRemainingInDecision()}");

                GameFlowData.Get().CallRpcUpdateTimeRemaining(GameFlowData.Get().GetTimeRemainingInDecision());
                yield return new WaitForSeconds(2);
            }
        }

        private IEnumerator ActionResolution()
        {
            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                var turnSm = actor.gameObject.GetComponent<ActorTurnSM>();
                turnSm.CallRpcTurnMessage((int)TurnMessage.BEGIN_RESOLVE, 0);
            }
            yield return 0;
            Artemis.ArtemisServer.Get().SharedActionBuffer.Networkm_actionPhase = ActionBufferPhase.Abilities;
            GameFlowData.Get().gameState = GameState.BothTeams_Resolve;
            // TODO update ATSDs on a separate tick
            yield return new WaitForSeconds(1);

            bool hasNextPhase = true;
            while(hasNextPhase)
            {
                hasNextPhase = ArtemisServerResolutionManager.Get().ResolveNextPhase();
                yield return ArtemisServerResolutionManager.Get().WaitForTheatrics();
            }
            yield return new WaitForSeconds(1);
            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                var turnSm = actor.gameObject.GetComponent<ActorTurnSM>();
                turnSm.CallRpcTurnMessage((int)TurnMessage.CLIENTS_RESOLVED_ABILITIES, 0);
            }
        }

        private IEnumerator MovementResolution()
        {
            Artemis.ArtemisServer.Get().SharedActionBuffer.Networkm_actionPhase = ActionBufferPhase.Movement;

            ArtemisServerMovementManager.Get().ResolveMovement();
            yield return new WaitForSeconds(6); // TODO ActorMovement.CalculateMoveTimeout() -- do we need some server version of ProcessMovement?

            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                var turnSm = actor.gameObject.GetComponent<ActorTurnSM>();

                //ArtemisServerMovementManager.Get().UpdatePlayerMovement(actor, false);
                turnSm.CallRpcTurnMessage((int)TurnMessage.MOVEMENT_RESOLVED, 0);
                //actor.GetActorMovement().UpdateSquaresCanMoveTo();
            }

            // TODO repeat all of the above for movement_chase
            yield return null;
            Artemis.ArtemisServer.Get().SharedActionBuffer.Networkm_actionPhase = ActionBufferPhase.MovementChase;
            // ....
            yield return null;

            Artemis.ArtemisServer.Get().SharedActionBuffer.Networkm_actionPhase = ActionBufferPhase.MovementWait;
            yield return null;
            Artemis.ArtemisServer.Get().SharedActionBuffer.Networkm_actionPhase = ActionBufferPhase.Done;
        }

        private IEnumerator EndTurn()
        {
            GameFlowData.Get().gameState = GameState.EndingTurn;
            ArtemisServerResolutionManager.Get().ApplyTargets();

            // Update statuses
            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                var actorStatus = actor.GetActorStatus();
                for (StatusType status = 0; status < StatusType.NUM; status++)
                {
                    if (actorStatus.HasStatus(status))
                    {
                        int duration = Math.Max(0, actorStatus.GetDurationOfStatus(status) - 1);
                        actorStatus.UpdateStatusDuration(status, duration);
                        if (duration == 0)
                        {
                            do
                            {
                                actorStatus.RemoveStatus(status);
                            } while (actorStatus.HasStatus(status));
                            Log.Info($"{actor.DisplayName}'s {status} status has expired.");
                        }
                    }
                }
            }
            yield return null;
        }

        private void CmdGUITurnMessage(ActorTurnSM actorTurnSM, int msgEnum, int extraData)
        {
            ActorData actor = actorTurnSM.gameObject.GetComponent<ActorData>();
            TurnMessage msg = (TurnMessage)msgEnum;

            if (!GameFlowData.Get().IsInDecisionState())
            {
                Log.Info($"Recieved CmdGuiTurnMessage not in desicion state! {actor.DisplayName} {msg} ({extraData})");
                return;
            }

            Log.Info($"CmdGuiTurnMessage {actor.DisplayName} {msg} ({extraData})");
            if (msg == TurnMessage.CANCEL_BUTTON_CLICKED)
            {
                // TODO distinguish CANCEL button and ability cancelling
                // actor.TeamSensitiveData_authority.SetToggledAction(actionType, false);
                // if (DONE) make undone
                // else if (targeting action) set toggled action(false)
                actorTurnSM.CallRpcTurnMessage((int)TurnMessage.CANCEL_BUTTON_CLICKED, 0);
            }
            else if (msg == TurnMessage.DONE_BUTTON_CLICKED)
            {
                actorTurnSM.CallRpcTurnMessage((int)TurnMessage.DONE_BUTTON_CLICKED, 0);
            }
            // TODO: Timebanks. Notice that client sends CANCEL msg when selecting ability after confirmed
            // (but we still should have a fallback if it doesn't) but doesn't send one when updating movement.
        }

        private void CmdSelectAbilityRequest(ActorController actorController, int actionTypeInt)
        {
            ActorData actor = actorController.gameObject.GetComponent<ActorData>();
            AbilityData.ActionType actionType = (AbilityData.ActionType)actionTypeInt;

            if (!GameFlowData.Get().IsInDecisionState())
            {
                Log.Info($"Recieved CmdSelectAbilityRequest not in desicion state! {actor.DisplayName} {actionType}");
                return;
            }

            Log.Info($"CmdSelectAbilityRequest {actor.DisplayName} {actionType}");

            if (!actor.QueuedMovementAllowsAbility &&
                actor.GetAbilityData().GetAbilityOfActionType(actionType).GetMovementAdjustment() != Ability.MovementAdjustment.FullMovement)
            {
                Log.Info($"CmdSelectAbilityRequest - Clearing movement for {actor.DisplayName}");
                ArtemisServerMovementManager.Get().ClearMovementRequest(actor, true);
            }

            AbilityData abilityData = actor.gameObject.GetComponent<AbilityData>();
            abilityData.Networkm_selectedActionForTargeting = actionType;
            SetAbilityRequest(actor, actionType, null);
        }

        private void CmdRequestCancelAction(ActorTurnSM actorTurnSM, int actionTypeInt, bool hasIncomingRequest)
        {
            ActorData actor = actorTurnSM.gameObject.GetComponent<ActorData>();
            AbilityData.ActionType actionType = (AbilityData.ActionType)actionTypeInt;

            if (!GameFlowData.Get().IsInDecisionState())
            {
                Log.Info($"Recieved CmdRequestCancelAction not in desicion state! {actor.DisplayName} {actionType} ({hasIncomingRequest})");
                return;
            }

            Log.Info($"CmdRequestCancelAction {actor.DisplayName} {actionType} ({hasIncomingRequest})");
            ClearAbilityRequest(actor, actionType);
            ArtemisServerMovementManager.Get().UpdatePlayerRemainingMovement(actor, !hasIncomingRequest);
        }

        internal void OnCastAbility(NetworkConnection conn, int casterIndex, int actionTypeInt, List<AbilityTarget> targets)
        {
            Player player = GameFlow.Get().GetPlayerFromConnectionId(conn.connectionId);
            ActorData actor = GameFlowData.Get().FindActorByActorIndex(casterIndex);
            ActorTurnSM turnSm = actor.gameObject.GetComponent<ActorTurnSM>();
            AbilityData.ActionType actionType = (AbilityData.ActionType)actionTypeInt;

            if (actor.gameObject.GetComponent<PlayerData>().m_player.m_connectionId != conn.connectionId)
            {
                Log.Error($"Illegal OnCastAbility: {actor.DisplayName} does not belong to player {player.m_accountId}!");
                turnSm.CallRpcTurnMessage((int)TurnMessage.ABILITY_REQUEST_REJECTED, 0);
                ClearAbilityRequest(actor, actionType);
                return;
            }

            Log.Info($"OnCastAbility {actor.DisplayName} {actionType} ({targets.Count} targets)");

            // TODO AbilityData.ValidateAbilityOnTarget
            turnSm.CallRpcTurnMessage((int)TurnMessage.ABILITY_REQUEST_ACCEPTED, 0);
            SetAbilityRequest(actor, actionType, targets);

            ArtemisServerMovementManager.Get().UpdatePlayerRemainingMovement(actor);
        }

        private void SetAbilityRequest(ActorData actor, AbilityData.ActionType actionType, List<AbilityTarget> targets)
        {
            List<ActorTargeting.AbilityRequestData> abilityRequest;
            Ability ability = actor.GetAbilityData().GetAbilityOfActionType(actionType);
            if (!ability.IsFreeAction())
            {
                abilityRequest = new List<ActorTargeting.AbilityRequestData>();
                // If this ability isn't free, remove previous non-free ability request, if any
                foreach (var prevAbilityRequest in actor.TeamSensitiveData_authority.GetAbilityRequestData())
                {
                    if (actor.GetAbilityData().GetAbilityOfActionType(prevAbilityRequest.m_actionType).IsFreeAction())
                    {
                        abilityRequest.Add(prevAbilityRequest);
                    }
                    else
                    {
                        actor.TeamSensitiveData_authority.SetToggledAction(actionType, false); // seems unused
                        actor.TeamSensitiveData_authority.SetQueuedAction(actionType, false);
                    }
                }
            }
            else
            {
                abilityRequest = new List<ActorTargeting.AbilityRequestData>(actor.TeamSensitiveData_authority.GetAbilityRequestData());
            }
            actor.TeamSensitiveData_authority.SetToggledAction(actionType, true); // seems unused
            actor.TeamSensitiveData_authority.SetQueuedAction(actionType, true);

            if (targets != null)
            {
                abilityRequest.Add(new ActorTargeting.AbilityRequestData(actionType, targets));
            }
            actor.TeamSensitiveData_authority.SetAbilityRequestData(abilityRequest);
        }

        private void ClearAbilityRequest(ActorData actor, AbilityData.ActionType actionType)
        {
            var abilityRequest = new List<ActorTargeting.AbilityRequestData>();
            foreach (var req in actor.TeamSensitiveData_authority.GetAbilityRequestData())
            {
                if (req.m_actionType != actionType)
                {
                    abilityRequest.Add(req);
                }
            }
            actor.TeamSensitiveData_authority.SetToggledAction(actionType, false); // seems unused
            actor.TeamSensitiveData_authority.SetQueuedAction(actionType, false);
            actor.TeamSensitiveData_authority.SetAbilityRequestData(abilityRequest);
        }

        private void ClearAbilityRequests(ActorData actor)
        {
            for (AbilityData.ActionType actionType = 0; actionType < AbilityData.ActionType.NUM_ACTIONS; actionType++)
            {
                actor.TeamSensitiveData_authority.SetToggledAction(actionType, false); // seems unused
            }
            actor.TeamSensitiveData_authority.UnqueueActions();
            actor.TeamSensitiveData_authority.SetAbilityRequestData(new List<ActorTargeting.AbilityRequestData>());
            //actorTurnSM.ClearAbilityTargets();
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
                actorTurnSM.OnCmdGUITurnMessageCallback += CmdGUITurnMessage;
                actorTurnSM.OnCmdRequestCancelActionCallback += CmdRequestCancelAction;
                ActorController actorController = player.GetComponent<ActorController>();
                actorController.OnCmdSelectAbilityRequestCallback += CmdSelectAbilityRequest;
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
                    if (actorTurnSM != null)
                    {
                        actorTurnSM.OnCmdGUITurnMessageCallback -= CmdGUITurnMessage;
                    }
                    ActorController actorController = player.GetComponent<ActorController>();
                    if (actorController != null)
                    {
                        actorController.OnCmdSelectAbilityRequestCallback -= CmdSelectAbilityRequest;
                    }
                }
            }
        }

        public static ArtemisServerGameManager Get()
        {
            return instance;
        }
    }
}
