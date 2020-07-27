using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public partial class FogOfWar : MonoBehaviour
{
	public bool IsVisible(BoardSquare square)
	{
		// server shortcut added
		if (NetworkServer.active)
		{
			return true;
		}
		
		if (!NetworkServer.active && m_owner != GameFlowData.Get().activeOwnedActorData)
		{
			Log.Warning("Calling FogOfWar::IsVisible(BoardSquare square) on a client for not-the-client actor.");
		}
		if (square == null)
		{
			return false;
		}
		return m_visibleSquares.ContainsKey(square);
	}
}
