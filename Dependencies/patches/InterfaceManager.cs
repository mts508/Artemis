using System;
using UnityEngine;
using UnityEngine.Networking;

public partial class InterfaceManager : NetworkBehaviour
{
	public void OnTurnTick()
	{
		UISounds.GetUISounds().Play("ui_notification_turn_start");
		UILoadingScreenPanel.Get()?.SetVisible(false);  // was no `?`
		if (UIFrontendLoadingScreen.Get().gameObject.activeSelf)
		{
			UIFrontendLoadingScreen.Get().StartDisplayFadeOut();
		}
	}
}
