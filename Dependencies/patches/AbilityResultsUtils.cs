using System.Collections.Generic;
using UnityEngine;

public static class AbilityResultsUtils
{
	public static void SerializeIntListToStream(ref IBitStream stream, List<int> list)
	{
		sbyte num = (sbyte)list.Count;
		stream.Serialize(ref num);
		for (int i = 0; i < num; i++)
		{
			int id = list[i];
			stream.Serialize(ref id);
		}
	}

	public static void SerializeEffectsToStartToStream(ref IBitStream stream, List<ClientEffectStartData> effectStartList)
	{
		sbyte effectStartNum = (sbyte)effectStartList.Count;
		stream.Serialize(ref effectStartNum);
		for (int i = 0; i < effectStartNum; i++)
		{
			ClientEffectStartData effectStart = effectStartList[i];

			uint effectGUID = (uint)effectStart.m_effectGUID;
			stream.Serialize(ref effectGUID);
			List<ServerClientUtils.SequenceStartData> seqStartList = effectStart.m_sequenceStartDataList;
			sbyte seqStartNum = (sbyte)seqStartList.Count;
			stream.Serialize(ref seqStartNum);
			for (int j = 0; j < seqStartNum; j++)
			{
				seqStartList[j].SequenceStartData_SerializeToStream(ref stream);
			}
			sbyte casterActorIndex = (sbyte)(effectStart.m_caster?.ActorIndex ?? ActorData.s_invalidActorIndex);
			stream.Serialize(ref casterActorIndex);
			sbyte targetActorIndex = (sbyte)(effectStart.m_effectTarget?.ActorIndex ?? ActorData.s_invalidActorIndex);
			stream.Serialize(ref targetActorIndex);
			if (targetActorIndex != ActorData.s_invalidActorIndex)
			{
				sbyte statusNum = (sbyte)effectStart.m_statuses.Count;
				stream.Serialize(ref statusNum);
				for (int j = 0; j < statusNum; j++)
				{
					byte statusType = (byte)effectStart.m_statuses[j];
					stream.Serialize(ref statusType);
				}
			}
			if (targetActorIndex != ActorData.s_invalidActorIndex)
			{
				sbyte statusOnTurnStartNum = (sbyte)effectStart.m_statusesOnTurnStart.Count;
				stream.Serialize(ref statusOnTurnStartNum);
				for (int j = 0; j < statusOnTurnStartNum; j++)
				{
					byte statusType = (byte)effectStart.m_statusesOnTurnStart[j];
					stream.Serialize(ref statusType);
				}
			}
			bool hasAbsorb = effectStart.m_absorb != 0;
			bool hasExpectedHoT = effectStart.m_expectedHoT != 0;
			byte bitField = ServerClientUtils.CreateBitfieldFromBools(
				effectStart.m_isBuff,
				effectStart.m_isDebuff,
				effectStart.m_hasMovementDebuff,
				hasAbsorb,
				hasExpectedHoT,
				false, false, false);
			stream.Serialize(ref bitField);
			if (hasAbsorb)
			{
				short absorb = (short)effectStart.m_absorb;
				stream.Serialize(ref absorb);
			}
			if (hasExpectedHoT)
			{
				short expectedHoT = (short)effectStart.m_expectedHoT;
				stream.Serialize(ref expectedHoT);
			}
		}
	}

	public static void SerializeBarriersToStartToStream(ref IBitStream stream, List<ClientBarrierStartData> barrierStartList)
	{
		sbyte barrierStartNum = (sbyte)barrierStartList.Count;
		stream.Serialize(ref barrierStartNum);

		for (int i = 0; i < barrierStartNum; i++)
		{
			ClientBarrierStartData clientBarrierStartData = barrierStartList[i];
			BarrierSerializeInfo info = clientBarrierStartData.m_barrierGameplayInfo;
			BarrierSerializeInfo.SerializeBarrierInfo(stream, ref info);
			sbyte seqenceStartNum = (sbyte)clientBarrierStartData.m_sequenceStartDataList.Count;
			stream.Serialize(ref seqenceStartNum);
			for (int j = 0; j < seqenceStartNum; j++)
			{
				clientBarrierStartData.m_sequenceStartDataList[j].SequenceStartData_SerializeToStream(ref stream);
			}
		}
	}

	public static void SerializeSequenceEndDataListToStream(ref IBitStream stream, List<ServerClientUtils.SequenceEndData> list)
	{
		sbyte num = (sbyte)list.Count;
		stream.Serialize(ref num);
		for (int i = 0; i < num; i++)
		{
			list[i].SequenceEndData_SerializeToStream(ref stream);
		}
	}

	public static void SerializeClientReactionResultsToStream(ref IBitStream stream, List<ClientReactionResults> list)
	{
		sbyte num = (sbyte)list.Count;
		stream.Serialize(ref num);
		for (int i = 0; i < num; i++)
		{
			list[i].SerializeToStream(ref stream);
		}
	}

	public static void SerializeSequenceStartDataListToStream(ref IBitStream stream, List<ServerClientUtils.SequenceStartData> list)
	{
		sbyte seqStartNum = (sbyte)list.Count;
		stream.Serialize(ref seqStartNum);
		for (int i = 0; i < seqStartNum; i++)
		{
			list[i].SequenceStartData_SerializeToStream(ref stream);
		}
	}

	public static void SerializeActorHitResultsDictionaryToStream(ref IBitStream stream, Dictionary<ActorData, ClientActorHitResults> dictionary)
	{
		sbyte hitResultNum = (sbyte)dictionary.Count;
		stream.Serialize(ref hitResultNum);
		foreach (var e in dictionary)
		{
			sbyte actorIndex = (sbyte)e.Key.ActorIndex;
			stream.Serialize(ref actorIndex);
			e.Value.SerializeToStream(ref stream);
		}
	}

	public static void SerializePositionHitResultsDictionaryToStream(ref IBitStream stream, Dictionary<Vector3, ClientPositionHitResults> dictionary)
	{
		sbyte hitResultNum = (sbyte)dictionary.Count;
		stream.Serialize(ref hitResultNum);
		foreach (var e in dictionary)
		{
			Vector3 pos = e.Key;
			stream.Serialize(ref pos);
			e.Value.SerializeToStream(ref stream);
		}
	}

	public static void SerializeClientMovementResultsListToStream(ref IBitStream stream, List<ClientMovementResults> list)
	{
		sbyte num = (sbyte)list.Count;
		stream.Serialize(ref num);
		for (int i = 0; i < num; i++)
		{
			list[i].SerializeToStream(ref stream);
		}
	}

	public static void SerializePowerupsToStealToStream(ref IBitStream stream, List<ClientPowerupStealData> list)
	{
		sbyte num = (sbyte)list.Count;
		stream.Serialize(ref num);
		for (int i = 0; i < num; i++)
		{
			int powerupGUID = list[i].m_powerupGuid;
			stream.Serialize(ref powerupGUID);
			list[i].m_powerupResults.SerializeToStream(ref stream);
		}
	}

	public static void SerializeClientGameModeEventListToStream(ref IBitStream stream, List<ClientGameModeEvent> list)
	{
		sbyte num = (sbyte)list.Count;
		stream.Serialize(ref num);
		for (int i = 0; i < num; i++)
		{
			SerializeClientGameModeEventToStream(ref stream, list[i]);
		}
	}

	public static void SerializeClientGameModeEventToStream(ref IBitStream stream, ClientGameModeEvent gameModeEvent)
	{
		sbyte eventTypeId = (sbyte)gameModeEvent.m_eventType;
		byte objectGUID = gameModeEvent.m_objectGuid;
		sbyte primaryActorIndex = (sbyte)(gameModeEvent.m_primaryActor?.ActorIndex ?? ActorData.s_invalidActorIndex);
		sbyte secondaryActorIndex = (sbyte)(gameModeEvent.m_secondaryActor?.ActorIndex ?? ActorData.s_invalidActorIndex);
		sbyte x = (sbyte)(gameModeEvent.m_square?.x ?? -1);
		sbyte y = (sbyte)(gameModeEvent.m_square?.y ?? -1);
		int eventGUID = gameModeEvent.m_eventGuid;
		stream.Serialize(ref eventTypeId);
		stream.Serialize(ref objectGUID);
		stream.Serialize(ref primaryActorIndex);
		stream.Serialize(ref secondaryActorIndex);
		stream.Serialize(ref x);
		stream.Serialize(ref y);
		stream.Serialize(ref eventGUID);
	}
}
