using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArtemisServer.GameServer.Targeters
{
    class TargeterResolver
    {
        protected AbilityUtil_Targeter m_targeter;
        protected AbilityTarget m_target;
        protected ActorData m_caster;
        protected Ability m_ability;

        public TargeterResolver(AbilityUtil_Targeter targeter, AbilityTarget target, ActorData caster, Ability ability)
        {
            m_targeter = targeter;
            m_target = target;
            m_caster = caster;
            m_ability = ability;
        }

        public virtual AbilityUtil_Targeter Targeter { get => m_targeter; }

        public virtual void Prepare()
        {
            Targeter.UpdateTargeting(m_target, m_caster);
            Targeter.SetLastUpdateCursorState(m_target);
        }

        public Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> Resolve(ActorData caster, Ability ability, int targeterIndex)
        {
            Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> currentTargetedActors = new Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>>();

            List<AbilityUtil_Targeter.ActorTarget> actorsInRange = Targeter.GetActorsInRange();
            Log.Info($"{actorsInRange.Count} actors in range");
            foreach (AbilityUtil_Targeter.ActorTarget actorTarget in actorsInRange)
            {
                Dictionary<AbilityTooltipSymbol, int> targetSymbols = new Dictionary<AbilityTooltipSymbol, int>();
                ActorTargeting.GetNameplateNumbersForTargeter(caster, actorTarget.m_actor, ability, targeterIndex, targetSymbols);
                Log.Info($"{targetSymbols.Count} nameplate numbers for {actorTarget.m_actor.DisplayName}");
                Utils.Add(ref currentTargetedActors, new Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>>() {{ actorTarget.m_actor, targetSymbols }});
            }

            return currentTargetedActors;
        }
    }
}
