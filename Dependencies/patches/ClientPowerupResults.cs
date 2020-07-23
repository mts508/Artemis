using System.Collections.Generic;
using UnityEngine;

public class ClientPowerupResults
{
	public void SerializeToStream(ref IBitStream stream)
	{
		AbilityResultsUtils.SerializeSequenceStartDataListToStream(ref stream, m_seqStartDataList);
		m_powerupAbilityResults.SerializeToStream(ref stream);
	}
}
