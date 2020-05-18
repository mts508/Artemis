using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArtemisServer
{
    static class UnityUtils
    {
        public static void DumpGameObject(GameObject gameObject)
        {
            Log.Info("-" + gameObject.name +  " (" + gameObject.GetType().Name + ")");
            MonoBehaviour[] componentList = gameObject.GetComponents<MonoBehaviour>();
            if (componentList != null)
            {
                foreach (MonoBehaviour component in componentList)
                {
                    if (component == null)
                    {
                        Log.Info("-    null?");
                    }
                    else
                    {
                        Log.Info(String.Format("-    (" + component.GetType() + ")"));
                    }
                }
            }
        }

        public static void DumpSceneObjects()
        {
            Log.Info("Listing root GameObjects in scene" + SceneManager.GetActiveScene().name);

            foreach (GameObject sceneObj in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                DumpGameObject(sceneObj);
            }
        }

        public static void DumpAllGameObjects()
        {
            foreach (GameObject gameObject in GameObject.FindObjectsOfType<GameObject>())
            {
                DumpGameObject(gameObject);
            }
        }
    }
}
