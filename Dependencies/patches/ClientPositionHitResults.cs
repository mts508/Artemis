using System.Collections.Generic;
using UnityEngine;

public class ClientPositionHitResults
{
	public ClientPositionHitResults(
		List<ClientEffectStartData> effectsToStart,
		List<ClientBarrierStartData> barriersToStart,
		List<int> effectsToRemove,
		List<int> barriersToRemove,
		List<ServerClientUtils.SequenceEndData> sequencesToEnd,
		List<ClientMovementResults> reactionsOnPosHit)
	{
		m_effectsToStart = effectsToStart;
		m_barriersToStart = barriersToStart;
		m_effectsToRemove = effectsToRemove;
		m_barriersToRemove = barriersToRemove;
		m_sequencesToEnd = sequencesToEnd;
		m_reactionsOnPosHit = reactionsOnPosHit;
		ExecutedHit = false;
	}
	
	public void SerializeToStream(ref IBitStream stream)
	{
		AbilityResultsUtils.SerializeEffectsToStartToStream(ref stream, m_effectsToStart);
		AbilityResultsUtils.SerializeBarriersToStartToStream(ref stream, m_barriersToStart);
		AbilityResultsUtils.SerializeIntListToStream(ref stream, m_effectsToRemove);
		AbilityResultsUtils.SerializeIntListToStream(ref stream, m_barriersToRemove);
		AbilityResultsUtils.SerializeSequenceEndDataListToStream(ref stream, m_sequencesToEnd);
		AbilityResultsUtils.SerializeClientMovementResultsListToStream(ref stream, m_reactionsOnPosHit);
	}
}
