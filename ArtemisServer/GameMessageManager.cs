using ArtemisServer.GameServer;
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


            NetworkServer.RegisterHandler((short)MyMsgType.CastAbility, new NetworkMessageDelegate(HandleCastAbility));
            NetworkServer.RegisterHandler((short)MyMsgType.ClientResolutionPhaseCompleted, new NetworkMessageDelegate(HandleClientResolutionPhaseCompleted));
        }

        private static void HandleAddPlayer(NetworkMessage message)
        {
            Log.Info("ADDPLAYER");
            AddPlayerMessage addPlayerMessage = message.ReadMessage<AddPlayerMessage>();
        }

        private static void HandleLoginRequest(NetworkMessage message)
        {
            // TODO client will send login request again in case of connection timeout
            Log.Info("LOGIN REQUEST");
            GameManager.LoginRequest loginRequest = message.ReadMessage<GameManager.LoginRequest>();

            Player player = GameFlow.Get().GetPlayerFromConnectionId(message.conn.connectionId);
            if (player.m_connectionId != message.conn.connectionId)
            {
                Log.Info($"New player AccountID:{loginRequest.AccountId} PlayerId:{loginRequest.PlayerId} SessionToken:{loginRequest.SessionToken}");
                player = new Player(message.conn, Convert.ToInt64(loginRequest.AccountId));  // PATCH internal -> public Player::Player

                GameFlow.Get().playerDetails[player] = new PlayerDetails(PlayerGameAccountType.Human)
                {
                    m_accountId = player.m_accountId,
                    m_disconnected = false,
                    m_handle = "Connecting Player",
                    m_idleTurns = 0,
                    m_team = Team.Invalid
                };
                Log.Info($"Registered AccountID:{player.m_accountId}");
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

        private static void HandleCastAbility(NetworkMessage message)
        {
            CastAbility castAbility = message.ReadMessage<CastAbility>();
            Log.Info($"CASTABILITY -> caster: {castAbility.CasterIndex}, action: {castAbility.ActionType}, {castAbility.Targets.Count} targets");
            ArtemisServerGameManager gameManager = ArtemisServerGameManager.Get();
            if (gameManager == null)
            {
                Log.Info($"CASTABILITY ignored");
            }
            else
            {
                gameManager.OnCastAbility(message.conn, castAbility.CasterIndex, castAbility.ActionType, castAbility.Targets);
            }
        }

        private static void HandleClientResolutionPhaseCompleted(NetworkMessage message)
        {
            ClientResolutionPhaseCompleted msg = message.ReadMessage<ClientResolutionPhaseCompleted>();
            Log.Info($"CLIENTRESOLUTIONPHASECOMPLETED -> actor: {msg.ActorIndex}, phase: {msg.AbilityPhase}, failsafe: {msg.AsFailsafe}, resend: {msg.AsResend}");
            ArtemisServerResolutionManager resolutionManager = ArtemisServerResolutionManager.Get();
            if (resolutionManager == null)
            {
                Log.Info($"CLIENTRESOLUTIONPHASECOMPLETED ignored");
            }
            else
            {
                resolutionManager.OnClientResolutionPhaseCompleted(message.conn, msg);
            }
        }

        public class CastAbility : MessageBase
        {
            public int CasterIndex;
            public int ActionType;
            public List<AbilityTarget> Targets;

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(CasterIndex);
                writer.Write(ActionType);
                AbilityTarget.SerializeAbilityTargetList(Targets, writer);
            }

            public override void Deserialize(NetworkReader reader)
            {
                CasterIndex = reader.ReadInt32();
                ActionType = reader.ReadInt32();
                Targets = AbilityTarget.DeSerializeAbilityTargetList(reader);
            }
        }

        public class ClientResolutionPhaseCompleted : MessageBase
        {
            public AbilityPriority AbilityPhase;
            public int ActorIndex;
            public bool AsFailsafe;
            public bool AsResend;

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write((sbyte)AbilityPhase);
                writer.Write(ActorIndex);
                writer.Write(AsFailsafe);
                writer.Write(AsResend);
            }

            public override void Deserialize(NetworkReader reader)
            {
                AbilityPhase = (AbilityPriority)reader.ReadSByte();
                ActorIndex = reader.ReadInt32();
                AsFailsafe = reader.ReadBoolean();
                AsResend = reader.ReadBoolean();
            }
        }
    }
}
