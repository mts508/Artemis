using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class TargeterUtils
{
	public unsafe static void RemoveActorsInvisibleToClient(ref List<ActorData> actors)
	{
		// This method looks useless on the server since IsVisibleToClient relies on LocalPlayerData
		// (designed to use on client only apparently)
		if (NetworkServer.active)
		{
			return;
		}
		for (int i = actors.Count - 1; i >= 0; i--)
		{
			if (!actors[i].IsVisibleToClient())
			{
				actors.RemoveAt(i);
			}
		}
	}
}
