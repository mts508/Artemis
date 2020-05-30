using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ActorTurnSM : NetworkBehaviour
{
	public delegate void OnCmdChase(ActorTurnSM actorTurnSm, int selectedSquareX, int selectedSquareY);
	public OnCmdChase OnCmdChaseCallback = null;

	[Command]
	// was empty
	private void CmdChase(int selectedSquareX, int selectedSquareY)
	{
		OnCmdChaseCallback?.Invoke(this, selectedSquareX, selectedSquareY);
	}

	public delegate void OnCmdGUITurnMessage(ActorTurnSM actorTurnSm, int msgEnum, int extraData);
	public OnCmdGUITurnMessage OnCmdGUITurnMessageCallback = null;

	[Command]
	// was empty
	private void CmdGUITurnMessage(int msgEnum, int extraData)
	{
		OnCmdGUITurnMessageCallback?.Invoke(this, msgEnum, extraData);
	}

	public delegate void OnCmdRequestCancelAction(ActorTurnSM actorTurnSm, int action, bool hasIncomingRequest);
	public OnCmdRequestCancelAction OnCmdRequestCancelActionCallback = null;

	[Command]
	// was empty
	private void CmdRequestCancelAction(int action, bool hasIncomingRequest)
	{
		OnCmdRequestCancelActionCallback?.Invoke(this, action, hasIncomingRequest);
	}

	public delegate void OnCmdSetSquare(ActorTurnSM actorTurnSm, int x, int y, bool setWaypoint);
	public OnCmdSetSquare OnCmdSetSquareCallback = null;

	[Command]
	// was empty
	private void CmdSetSquare(int x, int y, bool setWaypoint)
	{
		OnCmdSetSquareCallback?.Invoke(this, x, y, setWaypoint);
	}
}
