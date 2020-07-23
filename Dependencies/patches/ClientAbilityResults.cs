using System.Collections.Generic;
using UnityEngine;

public class ClientAbilityResults
{
	public void SerializeToStream(ref IBitStream stream)
	{
		sbyte casterActorIndex = (sbyte)GetCaster().ActorIndex;
		sbyte abilityAction = (sbyte)GetSourceActionType();
		stream.Serialize(ref casterActorIndex);
		stream.Serialize(ref abilityAction);
		AbilityResultsUtils.SerializeSequenceStartDataListToStream(ref stream, m_seqStartDataList);
		AbilityResultsUtils.SerializeActorHitResultsDictionaryToStream(ref stream, m_actorToHitResults);
		AbilityResultsUtils.SerializePositionHitResultsDictionaryToStream(ref stream, m_posToHitResults);
	}
}
