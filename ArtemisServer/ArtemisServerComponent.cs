using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ArtemisServer
{
    public class ArtemisServerComponent : MonoBehaviour
    {
        public object PrefabUtility { get; private set; }
        public static Dictionary<string, GameObject> ResourceNetworkObjects = new Dictionary<string, GameObject>();

        protected virtual void Awake()
        {
            Log.Info("Awaking server component on scene " + SceneManager.GetActiveScene().name);
            SceneManager.sceneLoaded += SceneLoader.Get().OnSceneLoaded;

            //UnityUtils.DumpAllGameObjects();
            
            NetworkIdentity[] objects = Resources.FindObjectsOfTypeAll<NetworkIdentity>();
            foreach (NetworkIdentity netid in objects)
            {
                GameObject obj = netid.gameObject;
                ResourceNetworkObjects.Add(obj.name, obj);
                //UnityUtils.DumpGameObject(obj);
            }

            GameObject gameSceneSingletons = ResourceNetworkObjects["GameSceneSingletons"]; // TODO: Use GameObject.Instantiate()
            GameObject applicationSingletonsNetId = ResourceNetworkObjects["ApplicationSingletonsNetId"];

            //base.StartCoroutine(AssetBundleManager.Get().LoadSceneAsync("DevEnvironmentSingletons", "frontend", LoadSceneMode.Single));
        }

        public GameObject GetNetworkPrefabByName(string name)
        {
            return ResourceNetworkObjects[name];
        }
    }
}
