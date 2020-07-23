using System.Collections.Generic;
using UnityEngine;

public class ClientEffectResults
{
	public void SerializeToStream(ref IBitStream stream)
	{
		uint effectGUID = (uint)m_effectGUID;
		sbyte casterActorIndex = (sbyte)m_effectCaster.ActorIndex;
		sbyte sourceAbilityActionType = (sbyte)m_sourceAbilityActionType;
		stream.Serialize(ref effectGUID);
		stream.Serialize(ref casterActorIndex);
		stream.Serialize(ref sourceAbilityActionType);
		AbilityResultsUtils.SerializeSequenceStartDataListToStream(ref stream, m_seqStartDataList);
		AbilityResultsUtils.SerializeActorHitResultsDictionaryToStream(ref stream, m_actorToHitResults);
		AbilityResultsUtils.SerializePositionHitResultsDictionaryToStream(ref stream, m_posToHitResults);
	}
}
