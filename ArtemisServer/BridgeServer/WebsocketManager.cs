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
        private static string BridgeServerAddress = "ws://127.0.0.1:6060/BridgeServer";

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
            UIFrontendLoadingScreen.Get().StartDisplayError("trying to connect to bridge server ", "at address: "+BridgeServerAddress);
            ws = new WebSocketSharp.WebSocket(BridgeServerAddress);
            ws.OnMessage += Ws_OnMessage;
            ws.OnError += Ws_OnError;
            ws.OnOpen += Ws_OnOpen;
            ws.Connect();
        }

        private void Ws_OnOpen(object sender, EventArgs e)
        {
            Log.Info("Successfully connected to lobby's bridge server");
            UIFrontendLoadingScreen.Get().StartDisplayError("connected to bridge server");
            MemoryStream stream = new MemoryStream();
            stream.WriteByte((byte)BridgeMessageType.InitialConfig);
            string addressAndPort = Artemis.ArtemisServer.Address + ":" + Artemis.ArtemisServer.Port;
            
            stream.Write(GetByteArray(addressAndPort), 0,addressAndPort.Length);

            byte[] buffer = stream.ToArray();
            ws.Send(buffer);
        }

        private void Ws_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Log.Info("--- Websocket Error ---");
            Log.Info(e.Exception.Source);
            Log.Info(e.Message);
            Log.Info(e.Exception.StackTrace);
        }

        public static void Init()
        {
            Log.Info("Init WebsocketManager");
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
                case BridgeMessageType.Start:
                    Artemis.ArtemisServer.StartGame();
                    ws.Send(new byte[] { (byte)BridgeMessageType.Start }); // tell the lobby server that we started successfully
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
