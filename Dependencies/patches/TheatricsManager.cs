using System;
using System.Collections.Generic;
using Theatrics;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public partial class TheatricsManager : NetworkBehaviour, IGameEventListener
{
	private void Update()
	{
		if (NetworkClient.active)
		{
			this.UpdateClient();
		}
	}
}
