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
            yield return null;
            GameFlowData.Get().Networkm_gameState = GameState.StartingGame;
            yield return null;
            GameFlowData.Get().Networkm_gameState = GameState.Deployment;
            yield return new WaitForSeconds(7);
            Log.Info("Done preparing for game");
        }

        private IEnumerator EndGame()
        {
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
            Log.Info("TurnLoop");
            yield return TurnDecision();
        }

        private IEnumerator TurnDecision()
        {
            GameFlowData.Get().Networkm_gameState = GameState.BothTeams_Decision;
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

            while(true)
            {
                Log.Info("TurnDecision");
                yield return new WaitForSeconds(21);
            }
        }
    }
}
