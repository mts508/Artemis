using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

namespace ArtemisServer
{
    public static class GameMessageManager
    {
        public static void RegisterAllHandlers()
        {
            // AddPlayer
            NetworkServer.RegisterHandler((short)MsgType.AddPlayer, new NetworkMessageDelegate(HandleAddPlayer));

            // LoginRequest
            NetworkServer.RegisterHandler((short)MyMsgType.LoginRequest, new NetworkMessageDelegate(HandleLoginRequest));

            // AssetsLoadedNotification
            NetworkServer.RegisterHandler((short)MyMsgType.AssetsLoadedNotification, new NetworkMessageDelegate(HandleAssetsLoadedNotification));

            // AssetsLoadingProgress
            NetworkServer.RegisterHandler((short)MyMsgType.ClientAssetsLoadingProgressUpdate, new NetworkMessageDelegate(HandleAssetsLoadinProgressUpdate));

        }

        private static void HandleAddPlayer(NetworkMessage message)
        {
            Log.Info("ADDPLAYER");
            AddPlayerMessage addPlayerMessage = message.ReadMessage<AddPlayerMessage>();
        }

        private static void HandleLoginRequest(NetworkMessage message)
        {
            Log.Info("LOGIN REQUEST");
            GameManager.LoginRequest loginRequest = message.ReadMessage<GameManager.LoginRequest>();

            Player player = GameFlow.Get().GetPlayerFromConnectionId(message.conn.connectionId);
            if (player.m_connectionId != message.conn.connectionId)
            {
                player = new Player(message.conn, Convert.ToInt64(loginRequest.AccountId));  // PATCH internal -> public Player::Player

                GameFlow.Get().playerDetails[player] = new PlayerDetails(PlayerGameAccountType.Human)
                {
                    m_accountId = player.m_accountId,
                    m_disconnected = false,
                    m_handle = "Connecting Player",
                    m_idleTurns = 0,
                    m_team = Team.Invalid
                };
            }

            GameManager.LoginResponse loginResponse = new GameManager.LoginResponse()
            {
                Reconnecting = false,
                Success = true,
                LastReceivedMsgSeqNum = message.conn.lastMessageIncomingSeqNum
            };

            message.conn.Send((short)MyMsgType.LoginResponse, loginResponse);
        }

        private static void HandleAssetsLoadedNotification(NetworkMessage message)
        {
            Log.Info("ASSETSLOADED");
            GameManager.AssetsLoadedNotification loadedNotification = message.ReadMessage<GameManager.AssetsLoadedNotification>();

            message.conn.Send(56, new GameManager.ReconnectReplayStatus { WithinReconnectReplay = true });
            message.conn.Send(56, new GameManager.SpawningObjectsNotification
            {
                PlayerId = loadedNotification.PlayerId,
                SpawnableObjectCount = 7 // NetObjects.Count
            });
            message.conn.Send(56, new GameManager.ReconnectReplayStatus { WithinReconnectReplay = false });
            Artemis.ArtemisServer.Get().ClientLoaded(message.conn, loadedNotification.PlayerId);
            
            // TODO wait for everybody to load, not just first player
            Artemis.ArtemisServer.Get().Launch();
        }

        private static void HandleAssetsLoadinProgressUpdate(NetworkMessage message)
        {
            GameManager.AssetsLoadingProgress loadingProgress = message.ReadMessage<GameManager.AssetsLoadingProgress>();
            Log.Info("LOADINGPROGRESS -> " + loadingProgress.TotalLoadingProgress);
            message.conn.SendByChannel(62, loadingProgress, message.channelId);
        }
    }
}
