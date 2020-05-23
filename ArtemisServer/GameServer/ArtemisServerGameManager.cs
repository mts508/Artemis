using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtemisServer.GameServer
{
    class ArtemisServerGameManager : MonoBehaviour
    {
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
            int x = 6;
            int y = 5;
            foreach (var player in GameFlowData.Get().GetPlayers())
            {
                //UnityUtils.DumpGameObject(player);

                ActorData actorData = player.GetComponent<ActorData>();
                var atsd = actorData.TeamSensitiveData_authority;
                if (atsd == null) continue;

                BoardSquare start = Board.Get().GetSquareBoardAtPosition(x++, y);
                GridPosProp startProp = GridPosProp.FromGridPos(start.GetGridPosition());

                atsd.CallRpcMovement(GameEventManager.EventType.Invalid,
                    startProp, startProp,
                    null, ActorData.MovementType.Teleport, false, false);

                actorData.ServerLastKnownPosSquare = start;
                actorData.InitialMoveStartSquare = start;
                atsd.MoveFromBoardSquare = start;
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

            foreach (var actor in GameFlowData.Get().GetActors())
            {
                var turnSm = actor.gameObject.GetComponent<ActorTurnSM>();
                turnSm.CallRpcTurnMessage((int)TurnMessage.TURN_START, 0);
                //actor.MoveFromBoardSquare = actor.TeamSensitiveData_authority.MoveFromBoardSquare;
                //UpdatePlayerMovement(player);
            }
            //BarrierManager.Get().CallRpcUpdateBarriers();

            Log.Info("TurnDecision");

            while (GameFlowData.Get().GetTimeRemainingInDecision() > 0)
            {
                Log.Info($"Time remaining: {GameFlowData.Get().GetTimeRemainingInDecision()}");

                GameFlowData.Get().CallRpcUpdateTimeRemaining(GameFlowData.Get().GetTimeRemainingInDecision());
                yield return new WaitForSeconds(5);
            }
        }

        private IEnumerator ActionResolution()
        {
            GameFlowData.Get().gameState = GameState.BothTeams_Resolve;
            yield return new WaitForSeconds(1);
        }

        private IEnumerator MovementResolution()
        {
            yield return new WaitForSeconds(1);
            GameFlowData.Get().gameState = GameState.EndingTurn;
        }
    }
}
