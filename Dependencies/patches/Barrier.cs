using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Barrier
{
	public GameplayResponseForActor m_onEnemyMovedThrough;
	public GameplayResponseForActor m_onAllyMovedThrough;
	public bool m_endOnCasterDeath;
	
	private void InitBarrier(
		int guid,
		string name,
		Vector3 center,
		Vector3 facingDir,
		float width,
		bool bidirectional,
		BlockingRules blocksVision,
		BlockingRules blocksAbilities,
		BlockingRules blocksMovement,
		BlockingRules blocksMovementOnCrossover,
		BlockingRules blocksPositionTargeting,
		bool considerAsCover,
		int maxDuration,
		ActorData owner,
		List<GameObject> barrierSequencePrefabs,
		bool playSequences,
		GameplayResponseForActor onEnemyMovedThrough,
		GameplayResponseForActor onAllyMovedThrough,
		int maxHits,
		bool endOnCasterDeath,
		SequenceSource parentSequenceSource,
		Team barrierTeam)
	{
		m_guid = guid;
		m_name = name;
		m_center = center;
		m_facingDir = facingDir;
		m_bidirectional = bidirectional;
		Vector3 a = Vector3.Cross(facingDir, Vector3.up);
		a.Normalize();
		float d = width * Board.Get().squareSize;
		m_endpoint1 = center + a * d / 2f;
		m_endpoint2 = center - a * d / 2f;
		BlocksVision = blocksVision;
		BlocksAbilities = blocksAbilities;
		BlocksMovement = blocksMovement;
		BlocksMovementOnCrossover = blocksMovementOnCrossover;
		BlocksPositionTargeting = blocksPositionTargeting;
		m_considerAsCover = considerAsCover;
		m_owner = owner;
		m_team = m_owner?.GetTeam() ?? barrierTeam;
		m_time = new EffectDuration();
		m_time.duration = maxDuration;
		m_barrierSequencePrefabs = barrierSequencePrefabs;
		m_playSequences = playSequences && m_barrierSequencePrefabs != null;
		m_barrierSequences = new List<Sequence>();
		if (m_playSequences)
		{
			BarrierSequenceSource = new SequenceSource(null, null, false, parentSequenceSource);
		}
		m_maxHits = maxHits;
		
		// added
		m_onEnemyMovedThrough = onEnemyMovedThrough;
		m_onAllyMovedThrough = onAllyMovedThrough;
		m_endOnCasterDeath = endOnCasterDeath;
	}
}
