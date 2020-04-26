using ArtemisServer;
using ArtemisServer.BridgeServer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.SceneManagement;

namespace Artemis
{
    public class ArtemisServer
    {
        bool IsMapLoaded;
        static LobbyGameInfo GameInfo;
        static LobbyTeamInfo TeamInfo;
        public static String Address = "127.0.0.1";
        public static int Port = 6061;

        public void Start()
        {
            Log.Info("Starting Server...");
            NetworkServer.useWebSockets = true;
            NetworkServer.Listen(Port);

            // Regiser Network Handlers
            GameMessageManager.RegisterAllHandlers();

            // Load map bundle
            AssetBundle MapsBundle = AssetBundle.LoadFromFile(@"C:\Artemis\Win64\AtlasReactor_Data\Bundles\scenes\maps.bundle");

            WebsocketManager.Init();
            // Load current map
            //SceneManager.sceneLoaded += this.OnSceneLoaded;
            //LoadMap();
        }

        public void Reset()
        {
            IsMapLoaded = false;
        }

        public void AddCharacterActor(LobbyPlayerInfo playerInfo, int playerIndex)
        {
            GameObject character = Resources.Load<GameObject>(playerInfo.CharacterType.ToString());

            GameObject.Instantiate(character);

            ActorData actorData = character.GetComponent<ActorData>();
            PlayerData playerData = character.GetComponent<PlayerData>();
            playerData.PlayerIndex = playerIndex;
            
            
            actorData.SetTeam(playerInfo.TeamId);
            actorData.UpdateDisplayName(playerInfo.Handle);
            actorData.PlayerIndex = playerIndex;

            GameFlowData.Get().AddPlayer(character);
            GameFlowData.Get().AddActor(actorData);

            

            Log.Info("AddCharacterActor");
            DumpGameObject(character);
        }

        public void DumpSceneObjects()
        {
            return; // #
            Scene scene = SceneManager.GetActiveScene();
            Log.Info("DumpSceneObjects");
            foreach (GameObject gameObject in scene.GetRootGameObjects())
            {
                DumpGameObject(gameObject);
            }
        }
        public void DumpGameObject(GameObject gameObject, int deep = 0)
        {
            
            string indentation = "+ ";
            for (int i = 0; i < deep; i++) { indentation += "  "; }
            //Log.Info(indentation + "DumpGameObject, deep=" + deep);
            Log.Info(indentation + gameObject.name + " " + gameObject.GetType());

            foreach (var component in gameObject.GetComponents<MonoBehaviour>())
            {
                DumpComponent(component, deep+1);
            }

            foreach (var obj in gameObject.GetComponents<GameObject>())
            {
                DumpGameObject(obj, deep + 1);
            }
        }
        private void DumpComponent(MonoBehaviour component, int deep)
        {
            if (component == null) return;

            string indentation = "- ";
            for (int i = 0; i < deep; i++) { indentation += "  "; }
            //Log.Info(indentation + "DumpComponent, deep=" + deep);
            Log.Info(indentation + component.name + " " + component.GetType());
        }

        public void LoadMap() {
            SceneManager.LoadScene(GameInfo.GameConfig.Map.ToString(), LoadSceneMode.Single);
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.Info("Active scene: " + SceneManager.GetActiveScene().name);

            VisualsLoader visualsLoader = GameObject.FindObjectOfType<VisualsLoader>();
            if (visualsLoader != null)
                GameObject.Destroy(visualsLoader);

            IsMapLoaded = true;

            Log.Info("OnSceneLoaded -> " + SceneManager.GetActiveScene().name);

            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<GameManager>();
            GameObject.Instantiate(gameManagerObj);

            GameManager.Get().SetTeamInfo(TeamInfo);
            GameManager.Get().SetGameInfo(GameInfo);

            Log.Info(GameManager.Get().TeamInfo.ToJson());
            int id_player = 0;

            List<LobbyPlayerInfo> playerInfoList = GameManager.Get().TeamInfo.TeamPlayerInfo;

            for (int i=0; i<playerInfoList.Count; i++)
            {
                LobbyPlayerInfo playerInfo = playerInfoList[i];
                AddCharacterActor(playerInfo, i);
            }

            DumpSceneObjects();
        }

        public static void SetGameInfo(LobbyGameInfo gameInfo)
        {
            GameInfo = gameInfo;
            Log.Info("Setting Game Info");
        }
        public static void SetTeamInfo(LobbyTeamInfo teamInfo)
        {
            TeamInfo = teamInfo;
            Log.Info("Setting Team Info");
        }
    }
}
