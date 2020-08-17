using System.Collections.Generic;
using UnityEngine;

public class ClientMovementResults
{
	public void SerializeToStream(ref IBitStream stream)
	{
		sbyte triggeringMoverActorIndex = (sbyte)m_triggeringMover.ActorIndex;
		stream.Serialize(ref triggeringMoverActorIndex);
		MovementUtils.SerializeLightweightPath(m_triggeringPath, stream);
		AbilityResultsUtils.SerializeSequenceStartDataListToStream(ref stream, m_seqStartDataList);

		if (m_effectResults != null)
		{
			sbyte responseType = (sbyte)MovementResults_GameplayResponseType.Effect;
			stream.Serialize(ref responseType);
			m_effectResults.SerializeToStream(ref stream);
		}
		else if (m_barrierResults != null)
		{
			sbyte responseType = (sbyte)MovementResults_GameplayResponseType.Barrier;
			stream.Serialize(ref responseType);
			m_barrierResults.SerializeToStream(ref stream);
		}
		else if (m_powerupResults != null)
		{
			sbyte responseType = (sbyte)MovementResults_GameplayResponseType.Powerup;
			stream.Serialize(ref responseType);
			m_powerupResults.SerializeToStream(ref stream);
		}
		else if (m_gameModeResults != null)
		{
			sbyte responseType = (sbyte)MovementResults_GameplayResponseType.GameMode;
			stream.Serialize(ref responseType);
			m_gameModeResults.SerializeToStream(ref stream);
		}
	}

	public void GetHitResults(out Dictionary<ActorData, ClientActorHitResults> actorHitResList, out Dictionary<Vector3, ClientPositionHitResults> posHitResList)
	{
		actorHitResList = null;
		posHitResList = null;
		// TODO are these mutually exclusive?
		if (m_effectResults != null)
		{
			actorHitResList = m_effectResults.GetActorHitResults();
			posHitResList = m_effectResults.GetPosHitResults();
			return;
		}
		if (m_barrierResults != null)
		{
			actorHitResList = m_barrierResults.GetActorHitResults();
			posHitResList = m_barrierResults.GetPosHitResults();
			return;
		}
		if (m_powerupResults != null)
		{
			actorHitResList = m_powerupResults.GetActorHitResults();
			posHitResList = m_powerupResults.GetPosHitResults();
			return;
		}
		if (m_gameModeResults != null)
		{
			actorHitResList = m_gameModeResults.GetActorHitResults();
			posHitResList = m_gameModeResults.GetPosHitResults();
			return;
		}
	}

	public ActorData GetCaster()
	{
		// TODO are these mutually exclusive?
		return m_effectResults?.GetCaster() ?? m_barrierResults?.GetCaster() ?? m_powerupResults?.GetCaster() ?? m_gameModeResults?.GetCaster();
	}
}
