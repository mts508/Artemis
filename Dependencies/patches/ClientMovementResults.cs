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
}
