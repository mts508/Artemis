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
public bool \u0018() -> public bool IsVisibleToClient()
internal bool \u000E() -> public bool IsDead()
internal -> public void SetHitPoints(int value)
internal -> public void SetAbsorbPoints(int value)
internal -> public get_HitPoints
public BoardSquare \u000E() -> public BoardSquare GetTravelBoardSquare() 
public Team \u000E() -> public Team GetTeam()
internal ActorCover \u000E() -> internal ActorCover GetActorCover()
internal ActorVFX \u000E() -> internal ActorVFX GetActorVFX()
internal TimeBank \u000E() -> internal TimeBank GetTimeBank()
internal FogOfWar \u000E() -> internal GetFogOfWar()
public Animator \u000E() -> public Animator GetModelAnimator
public Vector3 \u0015() -> public Vector3 GetTravelBoardSquareWorldPositionForLos()
public Vector3 \u0016() -> public Vector3 GetTravelBoardSquareWorldPosition()
public Vector3 \u000E(BoardSquare \u001D) -> public Vector3 GetSquareWorldPositionForLoS(BoardSquare square)
public Vector3 \u0012(BoardSquare \u001D) -> public Vector3 GetSquareWorldPosition(BoardSquare square)
public List<Team> \u000E() -> public List<Team> GetOtherTeams()
public List<Team> \u0012() -> public List<Team> GetTeams()
public List<Team> \u0015() -> public List<Team> GetEnemyTeams()

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
