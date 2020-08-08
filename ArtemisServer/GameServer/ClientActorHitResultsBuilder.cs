using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtemisServer.GameServer
{
    class ClientActorHitResultsBuilder
    {
        private bool m_hasDamage = false;
        private bool m_hasHealing = false;
        private bool m_hasTechPointGain = false;
        private bool m_hasTechPointLoss = false;
        private bool m_hasTechPointGainOnCaster = false;
        private bool m_hasKnockback = false;
        private ActorData m_knockbackSourceActor = null;

        private int m_finalDamage = 0;
        private int m_finalHealing = 0;
        private int m_finalTechPointsGain = 0;
        private int m_finalTechPointsLoss = 0;
        private int m_finalTechPointGainOnCaster = 0;

        private bool m_damageBoosted = false;
        private bool m_damageReduced = false;
        private bool m_isPartOfHealOverTime = false;
        private bool m_updateCasterLastKnownPos = false;
        private bool m_updateTargetLastKnownPos = false;
        private bool m_triggerCasterVisOnHitVisualOnly = false;
        private bool m_updateEffectHolderLastKnownPos = false;
        private ActorData m_effectHolderActor = null;
        private bool m_updateOtherLastKnownPos = false;
        private List<ActorData> m_otherActorsToUpdateVisibility = new List<ActorData>();
        private bool m_targetInCoverWrtDamage = false;
        private Vector3 m_damageHitOrigin = Vector3.zero;
        private bool m_canBeReactedTo = true;
        private bool m_isCharacterSpecificAbility = true; // TODO some CTF flag?

        private List<ClientEffectStartData> m_effectsToStart = new List<ClientEffectStartData>();
        private List<int> m_effectsToRemove = new List<int>();
        private List<ClientBarrierStartData> m_barriersToAdd = new List<ClientBarrierStartData>();
        private List<int> m_barriersToRemove = new List<int>();
        private List<ServerClientUtils.SequenceEndData> m_sequencesToEnd = new List<ServerClientUtils.SequenceEndData>();
        private List<ClientReactionResults> m_reactions = new List<ClientReactionResults>();
        private List<int> m_powerupsToRemove = new List<int>();
        private List<ClientPowerupStealData> m_powerupsToSteal = new List<ClientPowerupStealData>();
        private List<ClientMovementResults> m_directPowerupHits = new List<ClientMovementResults>();
        private List<ClientGameModeEvent> m_gameModeEvents = new List<ClientGameModeEvent>();
        private List<int> m_overconIds = new List<int>();

        public ClientActorHitResultsBuilder SetDamage(int finalDamage, Vector3 origin, bool targetInCoverWrtDamage, bool boosted, bool reduced)
        {
            m_hasDamage = true;
            m_finalDamage = finalDamage;
            m_targetInCoverWrtDamage = targetInCoverWrtDamage;
            m_damageBoosted = boosted;
            m_damageReduced = reduced;
            m_damageHitOrigin = origin;
            return this;
        }

        public ClientActorHitResultsBuilder SetRevealCaster(bool updateCasterLastKnownPos = true)
        {
            m_updateCasterLastKnownPos = updateCasterLastKnownPos;
            return this;
        }

        public ClientActorHitResultsBuilder SetRevealTarget(bool updateTargetLastKnownPos = true)
        {
            m_updateTargetLastKnownPos = updateTargetLastKnownPos;
            return this;
        }

        public ClientActorHitResultsBuilder SetCanBeReactedTo(bool canBeReactedTo = true)
        {
            m_canBeReactedTo = canBeReactedTo;
            return this;
        }

        public ClientActorHitResults Build()
        {
            return new ClientActorHitResults(
                m_hasDamage,
                m_hasHealing,
                m_hasTechPointGain,
                m_hasTechPointLoss,
                m_hasTechPointGainOnCaster,
                m_hasKnockback,
                m_knockbackSourceActor,
                m_finalDamage,
                m_finalHealing,
                m_finalTechPointsGain,
                m_finalTechPointsLoss,
                m_finalTechPointGainOnCaster,
                m_damageBoosted,
                m_damageReduced,
                m_isPartOfHealOverTime,
                m_updateCasterLastKnownPos,
                m_updateTargetLastKnownPos,
                m_triggerCasterVisOnHitVisualOnly,
                m_updateEffectHolderLastKnownPos,
                m_effectHolderActor,
                m_updateOtherLastKnownPos,
                m_otherActorsToUpdateVisibility,
                m_targetInCoverWrtDamage,
                m_damageHitOrigin,
                m_canBeReactedTo,
                m_isCharacterSpecificAbility,
                m_effectsToStart,
                m_effectsToRemove,
                m_barriersToAdd,
                m_barriersToRemove,
                m_sequencesToEnd,
                m_reactions,
                m_powerupsToRemove,
                m_powerupsToSteal,
                m_directPowerupHits,
                m_gameModeEvents,
                m_overconIds);
        }
    }
}
