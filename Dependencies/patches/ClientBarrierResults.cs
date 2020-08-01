using System.Collections.Generic;
using UnityEngine;

public class ClientBarrierResults
{
	public void SerializeToStream(ref IBitStream stream)
	{
		int barrierGUID = m_barrierGUID;
		sbyte casterIndex = (sbyte)m_barrierCaster.ActorIndex;
		stream.Serialize(ref barrierGUID);
		stream.Serialize(ref casterIndex);
		AbilityResultsUtils.SerializeActorHitResultsDictionaryToStream(ref stream, m_actorToHitResults);
		AbilityResultsUtils.SerializePositionHitResultsDictionaryToStream(ref stream, m_posToHitResults);
	}
}
