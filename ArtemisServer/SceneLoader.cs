using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArtemisServer
{
    public class SceneLoader
    {
        private static SceneLoader s_instance = null;
        private SceneLoader()
        {
            s_instance = this;
        }

        public static SceneLoader Get()
        {
            if (s_instance == null) s_instance = new SceneLoader();
            return s_instance;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.Info($"SceneLoader - Loaded scene {scene.name} in mode {mode.ToString()}");
            UnityUtils.DumpSceneObjects();
        }
    }
}
