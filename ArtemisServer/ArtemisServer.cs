using ArtemisServer;
using ArtemisServer.BridgeServer;
using ArtemisServer.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.SceneManagement;

namespace Artemis
{
    public class ArtemisServer
    {
        private static ArtemisServer instance;
        private static GameObject highlightUtilsPrefab;
        bool IsMapLoaded;
        LobbyGameInfo GameInfo;
        LobbyTeamInfo TeamInfo;
        public static String Address = "127.0.0.1";
        public static int Port = 6061;

        public void Start()
        {
            instance = this;

            Log.Info("Starting Server...");
            UIFrontendLoadingScreen.Get().StartDisplayError("Starting Server...");
            NetworkServer.useWebSockets = true;
            NetworkServer.Listen(Port);

            // Regiser Network Handlers
            GameMessageManager.RegisterAllHandlers();

            // Load map bundle
            AssetBundle MapsBundle = AssetBundle.LoadFromFile(Path.Combine(Application.dataPath, @"Bundles\scenes\maps.bundle"));

            WebsocketManager.Init();

            // to keep highlight utils for now
            ClientGamePrefabInstantiator prefabInstantiator = ClientGamePrefabInstantiator.Get();
            foreach(var prefab in prefabInstantiator.m_prefabs)
            {
                Log.Info($"client prefab: {prefab.name}");
                if (prefab.name == "HighlightUtilsSingleton")
                {
                    highlightUtilsPrefab = prefab;
                    break;
                }
            }
            GameObject.Destroy(prefabInstantiator);
        }

        public void Reset()
        {
            IsMapLoaded = false;
        }

        public static void StartGame()
        {
            SceneManager.sceneLoaded += instance.OnSceneLoaded;
            instance.LoadMap();
        }

        public void AddCharacterActor(LobbyPlayerInfo playerInfo, int playerIndex)
        {
            string characterResourcePath = playerInfo.CharacterType.ToString();
            if (playerInfo.CharacterType == CharacterType.PunchingDummy) // PunchingDummy has a _ in its resource name...
                characterResourcePath = "Punching_Dummy";

            Log.Info($"Add Character {characterResourcePath} for player {playerInfo.Handle}");

            GameObject prefab = Resources.Load<GameObject>(characterResourcePath);
            GameObject character = GameObject.Instantiate(prefab);

            ActorData actorData = character.GetComponent<ActorData>();
            PlayerData playerData = character.GetComponent<PlayerData>();
            playerData.PlayerIndex = playerIndex;
            
            
            actorData.SetTeam(playerInfo.TeamId);
            actorData.UpdateDisplayName(playerInfo.Handle);
            actorData.PlayerIndex = playerIndex;

            GameFlowData.Get().AddPlayer(character);
            GameFlowData.Get().AddActor(actorData);
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

        private void LoadMap()
        {
            string map = GameInfo.GameConfig.Map.ToString();
            UIFrontendLoadingScreen.Get().StartDisplayError("Loading " + map);
            SceneManager.LoadScene(map, LoadSceneMode.Single);
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.Info("Loaded scene map: " + SceneManager.GetActiveScene().name);
            UIFrontendLoadingScreen.Get().StartDisplayError("Map loaded");
            IsMapLoaded = true;

            GameObject.Instantiate(Artemis.ArtemisServer.highlightUtilsPrefab);

            foreach (GameObject sceneObject in scene.GetRootGameObjects())
            {
                if (sceneObject.GetComponent<NetworkIdentity>() != null && !sceneObject.activeSelf)
                {
                    Log.Info($"Activating scene object '{sceneObject.name}'");
                    sceneObject.SetActive(true);
                }
            }


            var board = Board.Get();

            GameManager.Get().SetTeamInfo(TeamInfo);
            GameManager.Get().SetGameInfo(GameInfo);

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
            instance.GameInfo = gameInfo;
            Log.Info("Setting Game Info");
        }
        public static void SetTeamInfo(LobbyTeamInfo teamInfo)
        {
            instance.TeamInfo = teamInfo;
            Log.Info("Setting Team Info");
        }
    }
}
