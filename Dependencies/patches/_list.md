TODO add older patches

## AbilityTarget.cs
internal -> public [De]serializeAbilityTargetList

## ActorAnimation
internal -> public ActorAnimation(Turn \u001D)
private short \u000E -> public short animationIndex
private Vector3 \u0012 -> public Vector3 targetPos
private bool \u0015 -> public bool _0015
private int \u0016 -> public int tauntNumber
private bool \u0013 -> public bool _0013
private bool \u0018 -> public bool _0018
private bool \u0009 -> public bool reveal
private AbilityData.ActionType \u0019 -> public AbilityData.ActionType actionType
internal int \u0017 -> public int actorIndex
internal bool \u0008 -> public bool cinematicCamera
internal sbyte \u000A -> public sbyte playOrderIndex
internal sbyte \u0006 -> public sbyte groupIndex
internal Bounds \u0020 -> public Bounds bounds
private List<byte> \u000C -> public List<byte> _000C_X
private List<byte> \u0014 -> public List<byte> _0014_Z
internal/private SequenceSource SeqSource -> public SequenceSource SeqSource
internal/private SequenceSource ParentAbilitySeqSource -> public SequenceSource ParentAbilitySeqSource
internal ActorData \u000D\u000E -> public ActorData Actor
internal/private Dictionary<ActorData, int> HitActorsToDeltaHP -> public Dictionary<ActorData, int> HitActorsToDeltaHP

## ActorData.cs
public static bool \u001A -> _001A // unused, always false
public bool \u0018() -> public bool IsVisibleToClient()
internal bool \u000E() -> public bool IsDead()
internal -> public void SetHitPoints(int value)
internal -> public void SetAbsorbPoints(int value)
internal -> public get_HitPoints
internal -> public get_TechPoints
public int \u000E() -> public int GetLastVisibleTurnToClient()
public Vector3 \u000E() -> public Vector3 GetClientLastKnownPos()
public Vector3 \u0012() -> public Vector3 GetServerLastKnownPos()
internal ActorModelData \u0012() -> internal ActorModelData GetFaceActorModelData()
internal NPCBrain \u000E() -> internal NPCBrain GetEnabledNPCBrain()
public BoardSquare \u000E() -> public BoardSquare GetTravelBoardSquare() 
public Team \u000E() -> public Team GetTeam()
internal ActorCover \u000E() -> internal ActorCover GetActorCover()
internal ActorVFX \u000E() -> internal ActorVFX GetActorVFX()
internal TimeBank \u000E() -> internal TimeBank GetTimeBank()
internal FogOfWar \u000E() -> internal GetFogOfWar()
internal ActorAdditionalVisionProviders \u000E() -> internal ActorAdditionalVisionProviders GetActorAdditionalVisionProviders()
public string \u000E() -> public string GetDisplayNameForLog()
public Sprite \u000E() -> public Sprite GetAliveHUDIcon()
public Sprite \u0012() -> public Sprite GetDeadHUDIcon()
public Sprite \u0015() -> public Sprite GetScreenIndicatorIcon()
public Sprite \u0016() -> public Sprite GetScreenIndicatorBWIcon()
public string \u0012() -> public string GetObjectName()
public float \u000E() -> public float GetPostAbilityHorizontalMovementChange()
public int \u0012() -> public int GetMaxHitPoints()
public float \u0012() -> public float GetHitPointShareOfMax()
public int \u0015() -> public int GetPassiveHpRegen()
public int \u0016() -> public int GetMaxTechPoints()
public int \u0013() -> public int GetPassiveEnergyRegen()
public float \u0015() -> public float GetSightRange()
public ActorTeamSensitiveData \u000E() -> public ActorTeamSensitiveData GetTeamSensitiveData()
public bool \u000E(BoardSquare \u001D) -> public bool ShouldRevealRespawnSquareToEnemy(BoardSquare square) // no-op return false
internal bool \u000E(ActorData \u001D) -> internal bool IsLineOfSightVisibleException(ActorData actor)
internal bool \u0012() -> internal bool IsModelAnimatorDisabled()
public bool \u0015() -> public bool IsPickingRespawnSquare()
public bool \u0016() -> public bool IsHiddenInBrush()
public int \u0018() -> public int GetTravelBoardSquareBrushRegion()
public bool \u0013() -> public bool ShouldShowNameplate()
public bool \u0012(ActorData \u001D) -> public bool IsVisibleForChase(ActorData observer)
public bool \u0015(ActorData \u001D) -> public bool CanBeSeen(ActorData observer)
public Vector3 \u000E(float \u001D) -> public Vector3 GetNameplatePosition(float offsetInPixels)
public int \u0009() -> public int GetHitPointsToDisplay()
public int \u000E(int \u001D) -> public int GetHitPointsToDisplayWithDelta(int deltaHP)
public int \u0019() -> public int GetEnergyToDisplay()
public int \u0011() -> public int GetPendingHoTTotalToDisplay()
public int \u001A() -> public int GetPendingHoTThisTurnToDisplay()
public string \u0015() -> public string GetHitPointsToDisplayDebugString()
public int \u0004() -> public int GetAbsorbToDisplay()
public bool \u0019() -> public bool GetIsHumanControlled()
public bool \u0011() -> public bool IsBotMasqueradingAsHuman()
public long \u000E() -> public long GetActualAccountId()
public long \u0012() -> public long GetAccountId()
public GridPos \u000E() -> public GridPos GetGridPosWithIncrementedHeight()
public Vector3 \u0013() -> public Vector3 GetFacingDirectionAfterMovement()
public string \u0016() -> public string GetTeamColorName()
public string \u0013() -> public string GetEnemyTeamColorName()
public Color \u000E() -> public Color GetTeamColor()
public Color \u0012() -> public Color GetEnemyTeamColor()
public Color \u000E(Team \u001D) -> public Color GetColorForTeam(Team observingTeam)
public float \u0016() -> public float GetRotationTimeRemaining()
public Rigidbody \u000E(string \u001D) -> public Rigidbody GetRigidBody(string boneName)
public Rigidbody \u000E() -> public Rigidbody GetHipJointRigidBody()
public Vector3 \u0018() -> public Vector3 GetHipJointRigidBodyPosition()
public Vector3 \u000E(string \u001D) -> public Vector3 GetBonePosition(string boneName)
public Quaternion \u000E(string \u001D) -> public Quaternion GetBoneRotation(string boneName)
public bool \u0012(BoardSquare \u001D) -> public bool CanChase(BoardSquare square)
internal void \u000E(int \u001D, int \u000E) -> internal void no_op(int _001D, int _000E) // unused no-op
public string \u0018() -> public string GetDebugName()
public string \u0012(string \u001D) -> public string GetColoredDebugName(string color)
public string \u0009() -> public string GetPointsDebugString()
public string \u0019() -> public string GetActorTurnSMDebugString()
public Animator \u000E() -> public Animator GetModelAnimator
public Vector3 \u0015() -> public Vector3 GetTravelBoardSquareWorldPositionForLos()
public Vector3 \u0016() -> public Vector3 GetTravelBoardSquareWorldPosition()
public Vector3 \u000E(BoardSquare \u001D) -> public Vector3 GetSquareWorldPositionForLoS(BoardSquare square)
public Vector3 \u0012(BoardSquare \u001D) -> public Vector3 GetSquareWorldPosition(BoardSquare square)
public List<Team> \u000E() -> public List<Team> GetOtherTeams()
public List<Team> \u0012() -> public List<Team> GetTeams()
public List<Team> \u0015() -> public List<Team> GetEnemyTeams()
public bool \u000E(PlayerData \u001D, bool \u000E = true) -> public bool IsRevealed(PlayerData observer, bool includePendingStatus = true)
public bool \u000E(PlayerData \u001D, bool \u000E = true, bool \u0012 = false) -> public bool IsHidden(PlayerData observer, bool includePendingStatus = true, bool forceViewingTeam = false)
public bool \u000E(ActorData \u001D, bool \u000E = false) -> public bool IsActorVisibleToActor(ActorData observer, bool forceViewingTeam = false)
public bool \u0009() -> public bool IsVisibleToEnemyTeam()
internal -> public void SetTechPoints(int value, bool combatText = false, ActorData caster = null, string sourceName = null)
TeleportType.\u001D -> TeleportType.Unused
<>f__mg$cache0 -> __f__mg_cache0
<>f__mg$cache1 -> __f__mg_cache1
<>f__mg$cache2 -> __f__mg_cache2

## Barrier.cs
internal -> public static BarrierSerializeInfo BarrierToSerializeInfo(Barrier barrier)
internal -> public static Barrier CreateBarrierFromSerializeInfo(BarrierSerializeInfo info)

## BarrierManager.cs
private -> public List<Barrier> m_barriers = new List<Barrier>();

## CameraManager.cs
in Update
	if (!this.GetFlyThroughCamera().enabled) -> if (!this.GetFlyThroughCamera()?.enabled ?? false)

## CharacterResourceLink.cs
internal -> public CharacterResourceLink.ActorDataPrefab

## ClientAbilityResults.cs
public static bool \u001D -> WarningEnabled
public static bool \u000E -> DebugEnabled

## GameFlowData.cs
internal -> public GameState gameState
internal -> public FindActorByActorIndex

## FogOfWarEffect.cs
Removed error message in CheckMinHeight (Design scene file {0} is missing LOSHighlights, Fog of War will not render!)

## MovementUtils.cs
All internal methods -> public

## NetworkReaderAdapter.cs
internal -> public class NetworkReaderAdapter
internal -> public NetworkReaderAdapter(NetworkReader stream)

## NetworkWriterAdapter.cs
internal -> public class NetworkWriterAdapter
internal -> public NetworkWriterAdapter(NetworkReader stream)

## Phase.cs
internal -> public class Phase
internal -> public Phase(Turn \u001D)
internal List<ActorAnimation> \u000E -> public List<ActorAnimation> Animations 
private Dictionary<int, int> \u0015 -> public Dictionary<int, int> ActorIndexToKnockback
private List<int> \u0016 -> public List<int> Participants
private Turn \u000F -> public Turn Turn;
internal/private -> public AbilityPriority Index
internal/private Dictionary<int, int> \u001C -> public Dictionary<int, int> ActorIndexToDeltaHP

## SequenceSource.cs
internal -> public SequenceSource(...)
internal -> public void SetWaitForClientEnable(bool value)

## TheatricsManager.cs
internal -> public static TheatricsManager Get()
private -> public Turn m_turn
private -> public AbilityPriority m_phaseToUpdate
private -> public int m_turnToUpdate
private -> public HashSet<long> m_playerConnectionIdsInUpdatePhase
private -> public float m_phaseStartTime
internal -> public void PlayPhase(AbilityPriority phaseIndex)
internal static bool \u000E -> internal static bool DebugLog
internal void \u000E(string \u001D) -> internal void ServerLog(string msg)

## Turn.cs
internal -> public class Turn
internal -> public Turn()
internal List<Phase> \u000E -> public List<Phase> Phases 
internal/private -> public int TurnID
internal static bool \u0011 -> IsEvasionOrKnockback // renamed for ease of patching -- but code patching fails anyway
