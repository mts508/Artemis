using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebSocketSharp;
using Newtonsoft.Json;

namespace ArtemisServer.BridgeServer
{
    public class WebsocketManager
    {
        private static WebsocketManager Instance;
        private static WebSocketSharp.WebSocket ws;

        public enum BridgeMessageType
        {
            InitialConfig,
            SetLobbyGameInfo,
            SetTeamInfo,
            Start,
            Stop,
            GameStatusChange
        }
        private WebsocketManager()
        {
            ws = new WebSocketSharp.WebSocket("ws://127.0.0.1:6060/BridgeServer");
            ws.OnMessage += Ws_OnMessage;
            ws.OnError += Ws_OnError;
            ws.OnOpen += Ws_OnOpen;
            ws.Connect();
        }

        private void Ws_OnOpen(object sender, EventArgs e)
        {
            Log.Info("Successfully connected to lobby's bridge server");
            MemoryStream stream = new MemoryStream();
            stream.WriteByte((byte)BridgeMessageType.InitialConfig);
            string addressAndPort = Artemis.ArtemisServer.Address + ":" + Artemis.ArtemisServer.Port;
            
            stream.Write(GetByteArray(addressAndPort), 0,addressAndPort.Length);

            byte[] buffer = stream.ToArray();
            ws.Send(buffer);
        }

        private void Ws_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Log.Info("ws error!!!!!!!");
            Log.Info(e.Message);
        }

        public static void Init()
        {
            Log.Info("init ws bridge");
            Instance = new WebsocketManager();
        }

        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            MemoryStream stream = new MemoryStream(e.RawData);
            BridgeMessageType messageType;
            string data;

            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                messageType = (BridgeMessageType)reader.Read();
                data = reader.ReadToEnd();
            }
            
            switch (messageType)
            {
                case BridgeMessageType.SetLobbyGameInfo:
                    LobbyGameInfo gameInfo = JsonConvert.DeserializeObject<LobbyGameInfo>(data);
                    Artemis.ArtemisServer.SetGameInfo(gameInfo);
                    break;
                case BridgeMessageType.SetTeamInfo:
                    LobbyTeamInfo teamInfo = JsonConvert.DeserializeObject<LobbyTeamInfo>(data);
                    Artemis.ArtemisServer.SetTeamInfo(teamInfo);
                    break;
                default:
                    Log.Error("Received unhandled ws message type: " + messageType.ToString());
                    break;
            }
        }

        private byte[] GetByteArray(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
    }
}
