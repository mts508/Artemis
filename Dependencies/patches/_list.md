TODO add older patches

## AbilityTarget.cs
internal -> public [De]serializeAbilityTargetList

## ActorData.cs
public bool \u0018() -> public bool IsVisibleToClient()
internal bool \u000E() -> public bool IsDead()
internal -> public void SetHitPoints(int value)
internal -> public void SetAbsorbPoints(int value)
internal -> public get_HitPoints

## CameraManager.cs
in Update
	if (!this.GetFlyThroughCamera().enabled) -> if (!this.GetFlyThroughCamera()?.enabled ?? false)

## CharacterResourceLink.cs
internal -> public CharacterResourceLink.ActorDataPrefab

## GameFlowData.cs
internal -> public GameState gameState
internal -> public FindActorByActorIndex

## FogOfWarEffect.cs
Removed error message in CheckMinHeight (Design scene file {0} is missing LOSHighlights, Fog of War will not render!)

## MovementUtils.cs
All internal methods -> public
