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
            int x = 18;
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
                //Log.Info($"MoveFromBoardSquare: {actor.TeamSensitiveData_authority.MoveFromBoardSquare}");
                ArtemisServerMovementManager.Get().UpdatePlayerMovement(actor);
                actor.GetActorMovement().UpdateSquaresCanMoveTo();
                turnSm.CallRpcTurnMessage((int)TurnMessage.TURN_START, 0);
            }
            //BarrierManager.Get().CallRpcUpdateBarriers();

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
            GameFlowData.Get().gameState = GameState.BothTeams_Resolve;
            yield return new WaitForSeconds(1);

            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                var turnSm = actor.gameObject.GetComponent<ActorTurnSM>();
                CancelAbility(actor, false);  // for now, cannot resolve it anyway
                turnSm.CallRpcTurnMessage((int)TurnMessage.CLIENTS_RESOLVED_ABILITIES, 0);
            }
        }

        private IEnumerator MovementResolution()
        {
            ArtemisServerMovementManager.Get().ResolveMovement();
            yield return new WaitForSeconds(6); // TODO ActorMovement.CalculateMoveTimeout() -- do we need some server version of ProcessMovement?


            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                var turnSm = actor.gameObject.GetComponent<ActorTurnSM>();

                //ArtemisServerMovementManager.Get().UpdatePlayerMovement(actor, false);
                turnSm.CallRpcTurnMessage((int)TurnMessage.MOVEMENT_RESOLVED, 0);
                //actor.GetActorMovement().UpdateSquaresCanMoveTo();
            }

            GameFlowData.Get().gameState = GameState.EndingTurn;
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

            AbilityData abilityData = actor.gameObject.GetComponent<AbilityData>();
            ActorTurnSM turnSm = actor.gameObject.GetComponent<ActorTurnSM>();

            abilityData.Networkm_selectedActionForTargeting = actionType;
            turnSm.ClearAbilityTargets();
            actor.TeamSensitiveData_authority.SetToggledAction(actionType, true);
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
                return;
            }

            Log.Info($"OnCastAbility {actor.DisplayName} {actionType} ({targets.Count} targets)");

            if (!actor.QueuedMovementAllowsAbility)
            {
                Log.Info($"OnCastAbility {actor.DisplayName} {actionType} rejected");
                turnSm.CallRpcTurnMessage((int)TurnMessage.ABILITY_REQUEST_REJECTED, 0);
                return;
            }

            // TODO AbilityData.ValidateAbilityOnTarget
            turnSm.CallRpcTurnMessage((int)TurnMessage.ABILITY_REQUEST_ACCEPTED, 0);

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
                }
            }
            else
            {
                abilityRequest = new List<ActorTargeting.AbilityRequestData>(actor.TeamSensitiveData_authority.GetAbilityRequestData());
            }

            abilityRequest.Add(new ActorTargeting.AbilityRequestData(actionType, targets));
            actor.TeamSensitiveData_authority.SetAbilityRequestData(abilityRequest);

            ArtemisServerMovementManager.Get().UpdatePlayerMovement(actor);
        }

        public void CancelAbility(ActorData actor, bool sendMessage = true)
        {
            ActorTurnSM turnSm = actor.gameObject.GetComponent<ActorTurnSM>();

            turnSm.ClearAbilityTargets();
            actor.TeamSensitiveData_authority.SetAbilityRequestData(new List<ActorTargeting.AbilityRequestData>());
            ArtemisServerMovementManager.Get().UpdatePlayerMovement(actor);
            if (sendMessage)
            {
                turnSm.CallRpcTurnMessage((int)TurnMessage.CANCEL_BUTTON_CLICKED, 0);
            }
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
            foreach (var player in GameFlowData.Get().GetPlayers())
            {
                ActorTurnSM actorTurnSM = player.GetComponent<ActorTurnSM>();
                actorTurnSM.OnCmdGUITurnMessageCallback -= CmdGUITurnMessage;
                ActorController actorController = player.GetComponent<ActorController>();
                actorController.OnCmdSelectAbilityRequestCallback -= CmdSelectAbilityRequest;
            }
        }

        public static ArtemisServerGameManager Get()
        {
            return instance;
        }
    }
}
