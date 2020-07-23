using System.Collections.Generic;
using UnityEngine;

public class ClientPositionHitResults
{
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
