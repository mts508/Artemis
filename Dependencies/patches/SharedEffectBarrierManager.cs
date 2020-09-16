using System.Collections.Generic;
using UnityEngine.Networking;

public class SharedEffectBarrierManager : NetworkBehaviour
{
	private List<int> m_endedEffectTurnRanges;
	private List<int> m_endedBarrierTurnRanges;
	private static SharedEffectBarrierManager s_instance;

	// modified
	private void Awake()
	{
		s_instance = this;
		m_endedEffectGuidsSync = new List<int>();
		m_endedBarrierGuidsSync = new List<int>();
		m_endedEffectTurnRanges = new List<int>(new int[m_numTurnsInMemory]);
		m_endedBarrierTurnRanges = new List<int>(new int[m_numTurnsInMemory]);
	}

	private void OnDestroy()
	{
		if (s_instance == this)
		{
			s_instance = null;
		}
	}

	public static SharedEffectBarrierManager Get()
	{
		return s_instance;
	}

	public void EndEffect(int effectGuid)
	{
		m_endedEffectGuidsSync.Add(effectGuid);
		SetDirtyBit(DirtyBit.EndedEffects);
	}

	public void EndBarrier(int barrierGuid)
	{
		m_endedBarrierGuidsSync.Add(barrierGuid);
		SetDirtyBit(DirtyBit.EndedBarriers);
	}

	public void UpdateTurn()
	{
		ShiftRange(m_endedBarrierGuidsSync, m_endedBarrierTurnRanges);
		ShiftRange(m_endedEffectGuidsSync, m_endedEffectTurnRanges);
	}

	private static void ShiftRange(List<int> guids, List<int> ranges)
	{
		int shift = ranges[0];
		for (int i = 0; i < ranges.Count - 1; ++i)
		{
			ranges[i] = ranges[i + 1] - shift;
		}
		guids.RemoveRange(0, shift);
		ranges[ranges.Count - 1] = guids.Count;
	}
}
