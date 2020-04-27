using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtemisServer.Map
{
    static class CommonObjectsSpawner
    {
        private static GameObject Instantiate(string name, Type componentType)
        {
            GameObject obj = new GameObject(name);
            obj.AddComponent(componentType);
            return GameObject.Instantiate(obj);
        }

        public static void Spawn()
        {
            Instantiate("TeamStatusDisplay", typeof(TeamStatusDisplay));
            Instantiate("GameFlow", typeof(GameFlow));
            Instantiate("GameFlowData", typeof(GameFlowData));
            Instantiate("GameplayData", typeof(GameplayData));
        }
    }
}
