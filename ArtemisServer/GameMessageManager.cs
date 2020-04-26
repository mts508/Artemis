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
            AddPlayerMessage addPlayerMessage = message.ReadMessage<AddPlayerMessage>();
        }

        private static void HandleLoginRequest(NetworkMessage message)
        {
            GameManager.LoginRequest loginRequest = message.ReadMessage<GameManager.LoginRequest>();

            Player player = GameFlow.Get().GetPlayerFromConnectionId(message.conn.connectionId);
            if (player.m_connectionId != message.conn.connectionId)
            {
                player.m_connectionId = message.conn.connectionId;
                player.m_accountId = Convert.ToInt64(loginRequest.AccountId);
                player.m_valid = true;
                player.m_id = 0; // 0 because i dont know what it does

                GameFlow.Get().playerDetails[player] = new PlayerDetails(PlayerGameAccountType.Human)
                {
                    m_accountId = player.m_accountId,
                    m_disconnected = false,
                    m_handle = "Connecting Player",
                    m_idleTurns = 0,
                    m_team = Team.Invalid
                };
            }
        }

        private static void HandleAssetsLoadedNotification(NetworkMessage message)
        {
            GameManager.AssetsLoadedNotification loginRequest = message.ReadMessage<GameManager.AssetsLoadedNotification>();
        }

        private static void HandleAssetsLoadinProgressUpdate(NetworkMessage message)
        {
            GameManager.AssetsLoadingProgress loginRequest = message.ReadMessage<GameManager.AssetsLoadingProgress>();
        }


    }
}
