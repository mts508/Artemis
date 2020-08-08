using ArtemisServer;
using ArtemisServer.BridgeServer;
using ArtemisServer.GameServer;
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
        bool IsMapLoaded;
        LobbyGameInfo GameInfo;
        LobbyTeamInfo TeamInfo;
        public static String Address = "127.0.0.1";
        public static int Port = 6061;
        ArtemisServerComponent artemisServerComponent;
        public SharedActionBuffer SharedActionBuffer;

        public void Start()
        {
            instance = this;

            // TODO Look into state usage on server side
            // Creating Teardown because GameFlowData checks if it isn't the current state, and null == null
            AppState_GameTeardown.Create();

            Log.Info("Starting Server...");
            UIFrontendLoadingScreen.Get().StartDisplayError("Starting Server...");
            NetworkServer.useWebSockets = true;
            NetworkServer.Listen(Port);

            // Regiser Network Handlers
            GameMessageManager.RegisterAllHandlers();

            // Load map bundle
            AssetBundle MapsBundle = AssetBundle.LoadFromFile(Path.Combine(Application.dataPath, @"Bundles\scenes\maps.bundle"));

            foreach (Scene sn in SceneManager.GetAllScenes()) {
                Log.Info(sn.name);
            }

            GameObject artemisServerObject = new GameObject("ArtemisServerComponent");
            artemisServerComponent = artemisServerObject.AddComponent<ArtemisServerComponent>();
            GameObject.DontDestroyOnLoad(artemisServerObject);

            WebsocketManager.Init();
            ClientGamePrefabInstantiatorFix();
        }

        private void ClientGamePrefabInstantiatorFix()
        {
            // to keep highlight utils for now
            GameObject highlightUtilsPrefab = null;
            ClientGamePrefabInstantiator prefabInstantiator = ClientGamePrefabInstantiator.Get();
            foreach (var prefab in prefabInstantiator.m_prefabs)
            {
                Log.Info($"client prefab: {prefab.name}");
                if (prefab.name == "HighlightUtilsSingleton")
                {
                    highlightUtilsPrefab = prefab;
                }
                else
                {
                    GameObject.Destroy(prefab);
                }
            }
            if (highlightUtilsPrefab != null)
            {
                prefabInstantiator.m_prefabs = new GameObject[] { highlightUtilsPrefab };
            }
            else
            {
                prefabInstantiator.m_prefabs = null;  // just in case
            }
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

        public void AddCharacterActor(LobbyPlayerInfo playerInfo)
        {
            CharacterResourceLink resourceLink = GameWideData.Get().GetCharacterResourceLink(playerInfo.CharacterType);

            Log.Info($"Add Character {resourceLink.GetDisplayName()} for player {playerInfo.Handle}");

            GameObject atsdObject = SpawnObject("ActorTeamSensitiveData_Friendly", false);
            GameObject character = GameObject.Instantiate(resourceLink.ActorDataPrefab);

            ActorData actorData = character.GetComponent<ActorData>();
            ActorTeamSensitiveData atsd = atsdObject.GetComponent<ActorTeamSensitiveData>();
            actorData.SetupAbilityMods(playerInfo.CharacterInfo.CharacterMods); //#
            actorData.PlayerIndex = playerInfo.PlayerId;
            actorData.ActorIndex = playerInfo.PlayerId;
            atsd.SetActorIndex(actorData.ActorIndex); // PATCH private -> public ActorTeamSensitiveData.SetActorIndex
            PlayerData playerData = character.GetComponent<PlayerData>();
            playerData.PlayerIndex = playerInfo.PlayerId;
            
            actorData.SetTeam(playerInfo.TeamId);
            actorData.UpdateDisplayName(playerInfo.Handle);
            actorData.SetClientFriendlyTeamSensitiveData(atsd);
            NetworkServer.Spawn(atsdObject);
            NetworkServer.Spawn(character);
            // For some reason, when you spawn atsd first, you see enemy characters, and when you spawn character first, you don't
            // They get lost because enemies are not YET registered in GameFlowData actors when TeamSensitiveDataMatchmaker.SetTeamSensitiveDataForUnhandledActors is called
            // TODO add hostile atsds and connect friendly/hostile ones to respective clients (Patch in ATSD.OnCheck/RebuildObservers) 
        }

        private void LoadMap()
        {
            string map = GameInfo.GameConfig.Map.ToString();
            UIFrontendLoadingScreen.Get().StartDisplayError("Loading " + map);
            SceneManager.LoadScene(map, LoadSceneMode.Single);
        }

        private GameObject SpawnObject(string name, bool network = true)
        {
            Log.Info($"Spawning {name}");
            GameObject prefab = artemisServerComponent.GetNetworkPrefabByName(name);

            if (prefab == null)
            {
                Log.Error($"Not found: {name}");
                return null;
            }

            Log.Info($"Prefab {name}");
            UnityUtils.DumpGameObject(prefab);

            GameObject obj = GameObject.Instantiate(prefab);
            Log.Info($"Instantiated {name}");
            if (network)
            {
                NetworkServer.Spawn(obj);
                Log.Info($"Network spawned {name}");
            }
            return obj;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.Info($"Loaded scene map ({mode}): {scene.name}");
            UIFrontendLoadingScreen.Get().StartDisplayError($"{scene.name} loaded");

            // Disable VisualsLoader so we dont go to the enviroment scene
            if (VisualsLoader.Get() != null)
            {
                VisualsLoader.Get().enabled = false;  // Breaks client UI (at least) if not disabled
            }

            // Avoid creating characters two times because OnSceneLoaded() gets called two times because VisualsLoader changes the current scene...
            if (this.IsMapLoaded)
            {
                Log.Error("Exiting on scene loaded, already loaded");
                return;
            }
            IsMapLoaded = true;
            InitializeGame(scene);
        }

        private void InitializeGame(Scene scene)
        {
            GameManager.Get().SetTeamInfo(TeamInfo);
            GameManager.Get().SetGameInfo(GameInfo);

            SpawnObject("ApplicationSingletonsNetId");
            var gameSceneSingletons = SpawnObject("GameSceneSingletons");
            gameSceneSingletons.SetActive(true);
            //var cameraMan = gameSceneSingletons.GetComponent<CameraManager>();
            //if (cameraMan != null)
            //{
            //    GameObject.Destroy(cameraMan);
            //}
            //else
            //{
            //    Log.Info("CameraManager is null");
            //}
            var SharedEffectBarrierManager = SpawnObject("SharedEffectBarrierManager");
            var SharedActionBufferObject = SpawnObject("SharedActionBuffer");
            SharedActionBuffer = SharedActionBufferObject.GetComponent<SharedActionBuffer>();
            SharedActionBuffer.Networkm_actionPhase = ActionBufferPhase.Done;

            foreach (GameObject sceneObject in scene.GetRootGameObjects())
            {
                if (sceneObject.GetComponent<NetworkIdentity>() != null && !sceneObject.activeSelf)
                {
                    Log.Info($"Activating scene object '{sceneObject.name}'");
                    sceneObject.SetActive(true);
                    NetworkServer.Spawn(sceneObject);
                }
            }

            GameFlowData.Get().gameState = GameState.SpawningPlayers;

            bool destroyVisualsLoader = false;
            if (destroyVisualsLoader)
            {
                GameObject visualsLoader = GameObject.Find("VisualsLoader");
                if (visualsLoader != null)
                {
                    Log.Info("Trying to destroy VisualsLoader");
                    GameObject.Destroy(visualsLoader);
                }
            }

            Log.Info("Board is " + Board.Get());

            List<LobbyPlayerInfo> playerInfoList = GameManager.Get().TeamInfo.TeamPlayerInfo;
            IsMapLoaded = true;
            for (int i = 0; i < playerInfoList.Count; i++)
            {
                LobbyPlayerInfo playerInfo = playerInfoList[i];
                AddCharacterActor(playerInfo);
            }

            GameFlowData.Get().Networkm_currentTurn = 0;
            GameFlowData.Get().gameState = GameState.StartingGame;

            // Show what objects are present in the current scene
            UnityUtils.DumpSceneObjects();

            WebsocketManager.ReportGameReady(); // Not sure where exactly it is supposed to happen
        }

        public static ArtemisServer Get() { return instance; }

        public void ClientLoaded(NetworkConnection connection, int playerIndex)
        {
            Player player = GameFlow.Get().GetPlayerFromConnectionId(connection.connectionId);
            foreach (ActorData playerActor in GameFlowData.Get().GetAllActorsForPlayer(playerIndex))
            {
                GameObject character = playerActor.gameObject;
                character.GetComponent<PlayerData>().m_player = player;  // PATCH internal -> public PlayerData::m_player
                GameFlow.Get().playerDetails[player]?.m_gameObjects.Add(character);
                Log.Info($"Registered player with account id {player.m_accountId} as player {playerIndex} ({character.name})");
                NetworkServer.AddPlayerForConnection(connection, character, 0);
            }
        }

        public void Launch()
        {
            foreach (NetworkIdentity networkIdentity in NetworkServer.objects.Values)
            {
                Log.Info($"Network identity: '{networkIdentity.name}' [{networkIdentity.connectionToClient?.connectionId}] {networkIdentity.observers.Count} observers");
            }

            artemisServerComponent.gameObject.AddComponent<ArtemisServerMovementManager>();
            artemisServerComponent.gameObject.AddComponent<ArtemisServerResolutionManager>();
            artemisServerComponent.gameObject.AddComponent<ArtemisServerBarrierManager>();
            ArtemisServerGameManager gm = artemisServerComponent.gameObject.AddComponent<ArtemisServerGameManager>();
            gm.StartGame();
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
