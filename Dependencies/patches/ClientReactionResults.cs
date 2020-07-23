using System.Collections.Generic;
using UnityEngine;

public class ClientReactionResults
{
	public void SerializeToStream(ref IBitStream stream)
	{
		AbilityResultsUtils.SerializeSequenceStartDataListToStream(ref stream, m_seqStartDataList);
		m_effectResults.SerializeToStream(ref stream);
		byte extraFlags = GetExtraFlags();
		stream.Serialize(ref extraFlags);
	}
}
