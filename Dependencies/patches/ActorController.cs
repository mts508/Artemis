using System.Collections.Generic;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

public class ActorController : NetworkBehaviour
{
	public delegate void OnCmdDebugTeleportRequest(ActorController actorController, int x, int y);
	public OnCmdDebugTeleportRequest OnCmdDebugTeleportRequestCallback = null;
	
	[Command]
	// was empty
	private void CmdDebugTeleportRequest(int x, int y)
	{
		OnCmdDebugTeleportRequestCallback?.Invoke(this, x, y);
	}

	public delegate void OnCmdPickedRespawnRequest(ActorController actorController, int x, int y);
	public OnCmdPickedRespawnRequest OnCmdPickedRespawnRequestCallback = null;

	[Command]
	// was empty
	private void CmdPickedRespawnRequest(int x, int y)
	{
		OnCmdPickedRespawnRequestCallback?.Invoke(this, x, y);
	}

	public delegate void OnCmdSendMinimapPing(ActorController actorController, int teamIndex, Vector3 worldPosition, PingType pingType);
	public OnCmdSendMinimapPing OnCmdSendMinimapPingCallback = null;

	[Command]
	// was empty
	internal void CmdSendMinimapPing(int teamIndex, Vector3 worldPosition, PingType pingType)
	{
		OnCmdSendMinimapPingCallback?.Invoke(this, teamIndex, worldPosition, pingType);
	}

	public delegate void OnCmdSendAbilityPing(ActorController actorController, int teamIndex, LocalizationArg_AbilityPing localizedPing);
	public OnCmdSendAbilityPing OnCmdSendAbilityPingCallback = null;

	[Command]
	// was empty
	internal void CmdSendAbilityPing(int teamIndex, LocalizationArg_AbilityPing localizedPing)
	{
		OnCmdSendAbilityPingCallback?.Invoke(this, teamIndex, localizedPing);
	}

	public delegate void OnCmdSelectAbilityRequest(ActorController actorController, int actionTypeInt);
	public OnCmdSelectAbilityRequest OnCmdSelectAbilityRequestCallback = null;

	[Command]
	// was empty
	protected void CmdSelectAbilityRequest(int actionTypeInt)
	{
		OnCmdSelectAbilityRequestCallback?.Invoke(this, actionTypeInt);
	}

	public delegate void OnCmdQueueSimpleActionRequest(ActorController actorController, int actionTypeInt);
	public OnCmdQueueSimpleActionRequest OnCmdQueueSimpleActionRequestCallback = null;

	[Command]
	// was empty
	protected void CmdQueueSimpleActionRequest(int actionTypeInt)
	{
		OnCmdQueueSimpleActionRequestCallback?.Invoke(this, actionTypeInt);
	}

	public delegate void OnCmdCustomGamePause(ActorController actorController, bool desiredPause, int requestActorIndex);
	public OnCmdCustomGamePause OnCmdCustomGamePauseCallback = null;

	[Command]
	private void CmdCustomGamePause(bool desiredPause, int requestActorIndex)
	{
		//HandleCustomGamePauseOnServer(desiredPause, requestActorIndex); // empty method
		OnCmdCustomGamePauseCallback?.Invoke(this, desiredPause, requestActorIndex);
	}

	public OnCmdCustomGamePause OnCmdCustomGamePauseOnServerCallback = null;

	private void HandleCustomGamePauseOnServer(bool desiredPause, int requestActorIndex)
	{
		OnCmdCustomGamePauseOnServerCallback?.Invoke(this, desiredPause, requestActorIndex);
	}
}