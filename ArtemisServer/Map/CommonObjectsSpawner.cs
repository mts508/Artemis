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
        public static void Spawn()
        {
            GameObject highlightUtilsObj = new GameObject("HighlightUtils");
            highlightUtilsObj.AddComponent<HighlightUtils>();
            GameObject.Instantiate(highlightUtilsObj);
        }
    }
}
