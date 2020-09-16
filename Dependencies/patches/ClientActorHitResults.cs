using System.Collections.Generic;
using UnityEngine;

public class ClientActorHitResults
{
	public int Damage => m_finalDamage;

	public int Healing => m_finalHealing;

	public int TechPoints => m_finalTechPointsGain - m_finalTechPointsLoss;

	public int TechPointsOnCaster => m_finalTechPointGainOnCaster;
	
	public ClientActorHitResults(
		bool hasDamage,
		bool hasHealing,
		bool hasTechPointGain,
		bool hasTechPointLoss,
		bool hasTechPointGainOnCaster,
		bool hasKnockback,
		ActorData knockbackSourceActor,
		int finalDamage,
		int finalHealing,
		int finalTechPointsGain,
		int finalTechPointsLoss,
		int finalTechPointGainOnCaster,
		bool damageBoosted,
		bool damageReduced,
		bool isPartOfHealOverTime,
		bool updateCasterLastKnownPos,
		bool updateTargetLastKnownPos,
		bool triggerCasterVisOnHitVisualOnly,
		bool updateEffectHolderLastKnownPos,
		ActorData effectHolderActor,
		bool updateOtherLastKnownPos,
		List<ActorData> otherActorsToUpdateVisibility,
		bool targetInCoverWrtDamage,
		Vector3 damageHitOrigin,
		bool canBeReactedTo,
		bool isCharacterSpecificAbility,
		List<ClientEffectStartData> effectsToStart,
		List<int> effectsToRemove,
		List<ClientBarrierStartData> barriersToAdd,
		List<int> barriersToRemove,
		List<ServerClientUtils.SequenceEndData> sequencesToEnd,
		List<ClientReactionResults> reactions,
		List<int> powerupsToRemove,
		List<ClientPowerupStealData> powerupsToSteal,
		List<ClientMovementResults> directPowerupHits,
		List<ClientGameModeEvent> gameModeEvents,
		List<int> overconIds)
	{
		m_hasDamage = hasDamage;
		m_hasHealing = hasHealing;
		m_hasTechPointGain = hasTechPointGain;
		m_hasTechPointLoss = hasTechPointLoss;
		m_hasTechPointGainOnCaster = hasTechPointGainOnCaster;
		m_hasKnockback = hasKnockback;
		m_knockbackSourceActor = knockbackSourceActor;
		m_finalDamage = finalDamage;
		m_finalHealing = finalHealing;
		m_finalTechPointsGain = finalTechPointsGain;
		m_finalTechPointsLoss = finalTechPointsLoss;
		m_finalTechPointGainOnCaster = finalTechPointGainOnCaster;
		m_damageBoosted = damageBoosted;
		m_damageReduced = damageReduced;
		m_isPartOfHealOverTime = isPartOfHealOverTime;
		m_updateCasterLastKnownPos = updateCasterLastKnownPos;
		m_updateTargetLastKnownPos = updateTargetLastKnownPos;
		m_triggerCasterVisOnHitVisualOnly = triggerCasterVisOnHitVisualOnly;
		m_updateEffectHolderLastKnownPos = updateEffectHolderLastKnownPos;
		m_effectHolderActor = effectHolderActor;
		m_updateOtherLastKnownPos = updateOtherLastKnownPos;
		m_otherActorsToUpdateVisibility = otherActorsToUpdateVisibility;
		m_targetInCoverWrtDamage = targetInCoverWrtDamage;
		m_damageHitOrigin = damageHitOrigin;
		m_canBeReactedTo = canBeReactedTo;
		m_isCharacterSpecificAbility = isCharacterSpecificAbility;
		m_effectsToStart = effectsToStart;
		m_effectsToRemove = effectsToRemove;
		m_barriersToAdd = barriersToAdd;
		m_barriersToRemove = barriersToRemove;
		m_sequencesToEnd = sequencesToEnd;
		m_reactions = reactions;
		m_powerupsToRemove = powerupsToRemove;
		m_powerupsToSteal = powerupsToSteal;
		m_directPowerupHits = directPowerupHits;
		m_gameModeEvents = gameModeEvents;
		m_overconIds = overconIds;
	}
	
	public void SerializeToStream(ref IBitStream stream)
	{
		byte bitField1 = ServerClientUtils.CreateBitfieldFromBools(
			m_hasDamage,
			m_hasHealing,
			m_hasTechPointGain,
			m_hasTechPointLoss,
			m_hasTechPointGainOnCaster,
			m_hasKnockback,
			m_targetInCoverWrtDamage,
			m_canBeReactedTo);
		stream.Serialize(ref bitField1);

		byte bitField2 = ServerClientUtils.CreateBitfieldFromBools(
			m_damageBoosted,
			m_damageReduced,
			m_updateCasterLastKnownPos,
			m_updateTargetLastKnownPos,
			m_updateEffectHolderLastKnownPos,
			m_updateOtherLastKnownPos,
			m_isPartOfHealOverTime,
			m_triggerCasterVisOnHitVisualOnly);
		stream.Serialize(ref bitField2);

		if (m_hasDamage)
		{
			short finalDamage = (short)m_finalDamage;
			stream.Serialize(ref finalDamage);
		}
		if (m_hasHealing)
		{
			short finalHealing = (short)m_finalHealing;
			stream.Serialize(ref finalHealing);
		}
		if (m_hasTechPointGain)
		{
			short finalTechPointsGain = (short)m_finalTechPointsGain;
			stream.Serialize(ref finalTechPointsGain);
		}
		if (m_hasTechPointLoss)
		{
			short finalTechPointsLoss = (short)m_finalTechPointsLoss;
			stream.Serialize(ref finalTechPointsLoss);
		}
		if (m_hasTechPointGainOnCaster)
		{
			short finalTechPointGainOnCaster = (short)m_finalTechPointGainOnCaster;
			stream.Serialize(ref finalTechPointGainOnCaster);
		}
		if (m_hasKnockback)
		{
			short actorIndex = (short)(m_knockbackSourceActor?.ActorIndex ?? ActorData.s_invalidActorIndex);
			stream.Serialize(ref actorIndex);
		}
		if (m_hasDamage && m_targetInCoverWrtDamage || m_hasKnockback)
		{
			float damageHitOriginX = m_damageHitOrigin.x;
			float damageHitOriginZ = m_damageHitOrigin.z;
			stream.Serialize(ref damageHitOriginX);
			stream.Serialize(ref damageHitOriginZ);
		}
		if (m_updateEffectHolderLastKnownPos)
		{
			short effectHolderActor = (short)(m_effectHolderActor?.ActorIndex ?? ActorData.s_invalidActorIndex);
			stream.Serialize(ref effectHolderActor);
		}
		if (m_updateOtherLastKnownPos)
		{
			byte otherActorsToUpdateVisibilityNum = (byte)(m_otherActorsToUpdateVisibility?.Count ?? 0);
			stream.Serialize(ref otherActorsToUpdateVisibilityNum);
			for (int i = 0; i < otherActorsToUpdateVisibilityNum; i++)
			{
				short actorIndex = (short)m_otherActorsToUpdateVisibility[i].ActorIndex;
				stream.Serialize(ref actorIndex);
			}
		}

		bool hasEffectsToStart = !m_effectsToStart.IsNullOrEmpty();
		bool hasEffectsToRemove = !m_effectsToRemove.IsNullOrEmpty();
		bool hasBarriersToAdd = !m_barriersToAdd.IsNullOrEmpty();
		bool hasBarriersToRemove = !m_barriersToRemove.IsNullOrEmpty();
		bool hasSequencesToEnd = !m_sequencesToEnd.IsNullOrEmpty();
		bool hasReactions = !m_reactions.IsNullOrEmpty();
		bool hasPowerupsToRemove = !m_powerupsToRemove.IsNullOrEmpty();
		bool hasPowerupsToSteal = !m_powerupsToSteal.IsNullOrEmpty();
		bool hasDirectPowerupHits = !m_directPowerupHits.IsNullOrEmpty();
		bool hasGameModeEvents = !m_gameModeEvents.IsNullOrEmpty();
		bool isCharacterSpecificAbility = m_isCharacterSpecificAbility;
		bool hasOverconIds = !m_overconIds.IsNullOrEmpty();
		byte bitField3 = ServerClientUtils.CreateBitfieldFromBools(
			hasEffectsToStart,
			hasEffectsToRemove,
			hasBarriersToRemove,
			hasSequencesToEnd,
			hasReactions,
			hasPowerupsToRemove,
			hasPowerupsToSteal,
			hasDirectPowerupHits);
		byte bitField4 = ServerClientUtils.CreateBitfieldFromBools(
			hasGameModeEvents,
			isCharacterSpecificAbility,
			hasBarriersToAdd,
			hasOverconIds,
			false, false, false, false);
		stream.Serialize(ref bitField3);
		stream.Serialize(ref bitField4);

		if (hasEffectsToStart)
		{
			AbilityResultsUtils.SerializeEffectsToStartToStream(ref stream, m_effectsToStart);
		}
		if (hasEffectsToRemove)
		{
			AbilityResultsUtils.SerializeIntListToStream(ref stream, m_effectsToRemove);
		}
		if (hasBarriersToAdd)
		{
			AbilityResultsUtils.SerializeBarriersToStartToStream(ref stream, m_barriersToAdd);
		}
		if (hasBarriersToRemove)
		{
			AbilityResultsUtils.SerializeIntListToStream(ref stream, m_barriersToRemove);
		}
		if (hasSequencesToEnd)
		{
			AbilityResultsUtils.SerializeSequenceEndDataListToStream(ref stream, m_sequencesToEnd);
		}
		if (hasReactions)
		{
			AbilityResultsUtils.SerializeClientReactionResultsToStream(ref stream, m_reactions);
		}
		if (hasPowerupsToRemove)
		{
			AbilityResultsUtils.SerializeIntListToStream(ref stream, m_powerupsToRemove);
		}
		if (hasPowerupsToSteal)
		{
			AbilityResultsUtils.SerializePowerupsToStealToStream(ref stream, m_powerupsToSteal);
		}
		if (hasDirectPowerupHits)
		{
			AbilityResultsUtils.SerializeClientMovementResultsListToStream(ref stream, m_directPowerupHits);
		}
		if (hasGameModeEvents)
		{
			AbilityResultsUtils.SerializeClientGameModeEventListToStream(ref stream, m_gameModeEvents);
		}
		if (hasOverconIds)
		{
			AbilityResultsUtils.SerializeIntListToStream(ref stream, m_overconIds);
		}
		m_isCharacterSpecificAbility = isCharacterSpecificAbility;
	}
}
