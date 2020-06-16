using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtemisServer.GameServer
{
    class ArtemisServerResolutionManager : MonoBehaviour
    {
        private static ArtemisServerResolutionManager instance;

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        public void ResolveAbilities()
        {
            foreach (AbilityPriority priority in Enum.GetValues(typeof(AbilityPriority))) // TODO remove invalid priorities
            {
                Log.Info($"Resolving {priority} abilities");
                foreach (ActorData actor in GameFlowData.Get().GetActors())
                {
                    GameFlowData.Get().activeOwnedActorData = actor;
                    ResolveAbilities(actor, priority);
                }
            }
            GameFlowData.Get().activeOwnedActorData = null;
            Log.Info("Abilities resolved");
        }

        public void ResolveAbilities(ActorData actor, AbilityPriority priority)
        {
            ActorTurnSM turnSm = actor.gameObject.GetComponent<ActorTurnSM>();
            ActorController actorController = actor.gameObject.GetComponent<ActorController>();
            AbilityData abilityData = actor.gameObject.GetComponent<AbilityData>();
            ActorTeamSensitiveData atsd = actor.TeamSensitiveData_authority;
            ActorMovement actorMovement = actor.GetActorMovement();

            // I didn't find any code that calculates what an ability hits aside from UpdateTargeting which is
            // used to draw targeters on the client. In order for it to work on the server we need to
            // * set actor as active owned actor data -- calculations rely on this
            // * AppearAtBoardSquare to set actor's current board square
            // * patch TargeterUtils so that RemoveActorsInvisibleToClient isn't called on the server
            // * ..?
            // TODO targeters can use visibility to client inside (ActorData.IsVisibleToClient)
            // TODO SoldierDashAndOverwatch.m_hitPhase
            foreach (ActorTargeting.AbilityRequestData ard in actor.TeamSensitiveData_authority.GetAbilityRequestData())
            {
                Ability ability = abilityData.GetAbilityOfActionType(ard.m_actionType);

                if (ability.m_runPriority != priority)
                {
                    continue;
                }
                Log.Info($"Resolving {ability.m_abilityName} for {actor.DisplayName}");

                for (int i = 0; i < ard.m_targets.Count; ++i)
                {
                    ability.Targeters[i].UpdateTargeting(ard.m_targets[i], actor);
                }

                Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> currentTargetedActors = CalculateTargetedActors(actor, ability);

                foreach (ActorData target in currentTargetedActors.Keys)
                {
                    foreach (AbilityTooltipSymbol symbol in currentTargetedActors[target].Keys)
                    {
                        int value = currentTargetedActors[target][symbol];
                        Log.Info($"target {target.DisplayName} {symbol} {value}");
                        switch (symbol)
                        {
                            case AbilityTooltipSymbol.Damage:
                                target.SetHitPoints(target.HitPoints - value);
                                break;
                        }
                    }
                }
            }
        }

        // Based on ActorTargeting.CalculateTargetedActors
        public Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> CalculateTargetedActors(ActorData instigator, Ability abilityOfActionType)
        {
            Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> m_currentTargetedActors = new Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>>();
            int num = 0;
            Log.Info($"{abilityOfActionType.Targeters.Count}/{abilityOfActionType.GetExpectedNumberOfTargeters()} targeters");
            while (num < abilityOfActionType.Targeters.Count && num < abilityOfActionType.GetExpectedNumberOfTargeters())
            {
                AbilityUtil_Targeter abilityUtil_Targeter = abilityOfActionType.Targeters[num];
                Log.Info($"targeter {num} : {abilityUtil_Targeter}");
                if (abilityUtil_Targeter != null)
                {
                    List<AbilityUtil_Targeter.ActorTarget> actorsInRange = abilityUtil_Targeter.GetActorsInRange();
                    Log.Info($"{actorsInRange.Count} actors in range");
                    foreach (AbilityUtil_Targeter.ActorTarget actorTarget in actorsInRange)
                    {
                        Dictionary<AbilityTooltipSymbol, int> dictionary = new Dictionary<AbilityTooltipSymbol, int>();
                        ActorTargeting.GetNameplateNumbersForTargeter(instigator, actorTarget.m_actor, abilityOfActionType, num, dictionary);
                        Log.Info($"{dictionary.Count} nameplate numbers for {actorTarget.m_actor.DisplayName}");
                        foreach (KeyValuePair<AbilityTooltipSymbol, int> keyValuePair in dictionary)
                        {
                            AbilityTooltipSymbol key = keyValuePair.Key;
                            if (!m_currentTargetedActors.ContainsKey(actorTarget.m_actor))
                            {
                                m_currentTargetedActors[actorTarget.m_actor] = new Dictionary<AbilityTooltipSymbol, int>();
                            }
                            if (!m_currentTargetedActors[actorTarget.m_actor].ContainsKey(key))
                            {
                                m_currentTargetedActors[actorTarget.m_actor][key] = 0;
                            }
                            m_currentTargetedActors[actorTarget.m_actor][key] += keyValuePair.Value;
                        }
                    }
                }
                num++;
            }
            return m_currentTargetedActors;
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public static ArtemisServerResolutionManager Get()
        {
            return instance;
        }
    }
}
