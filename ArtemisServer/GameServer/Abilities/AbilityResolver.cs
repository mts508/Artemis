using ArtemisServer.GameServer.Targeters;
using System;
using System.Collections.Generic;
using Theatrics;
using UnityEngine;

namespace ArtemisServer.GameServer.Abilities
{
    class AbilityResolver
    {
        protected ActorData m_caster;
        protected Ability m_ability;
        protected AbilityPriority m_priority;
        protected ActorTargeting.AbilityRequestData m_abilityRequestData;

        public Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> TargetedActors;
        public List<ClientResolutionAction> Actions;
        public List<ActorAnimation> Animations;

        protected TargeterResolver CurrentTargeterResolver { get; private set; }

        protected AbilityData.ActionType ActionType { get => m_abilityRequestData.m_actionType; }

        public AbilityResolver(ActorData actor, Ability ability, AbilityPriority priority, ActorTargeting.AbilityRequestData abilityRequestData)
        {
            m_caster = actor;
            m_ability = ability;
            m_priority = priority;
            m_abilityRequestData = abilityRequestData;
        }

        protected TargeterResolver GetTargeterResolver(Ability ability, int index, AbilityTarget target, ActorData caster)
        {
            AbilityUtil_Targeter targeter = ability.Targeters[index];

            if (targeter == null)
            {
                return null;
            }

            return GetTargeterResolver(ability, index, targeter, target, caster);
        }

        protected virtual TargeterResolver GetTargeterResolver(Ability ability, int index, AbilityUtil_Targeter targeter, AbilityTarget target, ActorData caster)
        {
            return new TargeterResolver(targeter, target, caster, ability);
        }

        public void Resolve()
        {
            TargetedActors = new Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>>();
            Actions = new List<ClientResolutionAction>();
            Animations = new List<ActorAnimation>();

            List<TargeterResolver> targeterResolvers = new List<TargeterResolver>();

            for (int i = 0; i < m_abilityRequestData.m_targets.Count; ++i)
            {
                CurrentTargeterResolver = GetTargeterResolver(m_ability, i, m_abilityRequestData.m_targets[i], m_caster);
                targeterResolvers.Add(CurrentTargeterResolver);
                CurrentTargeterResolver.Prepare();
            }

            // Based on ActorTargeting.CalculateTargetedActors
           
            Log.Info($"{m_ability.Targeters.Count}/{m_ability.GetExpectedNumberOfTargeters()} targeters");
            for (int i = 0; i < m_ability.Targeters.Count && i < m_ability.GetExpectedNumberOfTargeters(); i++)
            {
                CurrentTargeterResolver = targeterResolvers[i];
                Log.Info($"targeter {i} : {CurrentTargeterResolver.Targeter}");
                if (CurrentTargeterResolver != null)
                {
                    var targets = CurrentTargeterResolver.Resolve(m_caster, m_ability, i);
                    Utils.Add(ref TargetedActors, targets);
                }

                SequenceSource SeqSource = new SequenceSource(null, null, ArtemisServerResolutionManager.Get().NextSeqSourceRootID, true); // TODO
                SeqSource.SetWaitForClientEnable(true);

                Dictionary<ActorData, ClientActorHitResults> actorToHitResults = new Dictionary<ActorData, ClientActorHitResults>();
                foreach (var targetedActor in TargetedActors)
                {
                    foreach (var symbol in targetedActor.Value)
                    {
                        ClientActorHitResults hitResults;
                        switch (symbol.Key)
                        {
                            case AbilityTooltipSymbol.Damage:
                                hitResults = new ClientActorHitResultsBuilder()
                                    .SetDamage(symbol.Value, Vector3.zero, false, false)  // TODO
                                    .Build();
                                Log.Info($"HitResults: damage: {symbol.Value}");
                                break;
                            default:
                                hitResults = new ClientActorHitResultsBuilder().Build();
                                break;
                        }
                        actorToHitResults.Add(targetedActor.Key, hitResults);
                    }
                }
                Actions.Add(MakeResolutionAction(actorToHitResults, SeqSource));
                MakeAnimations(SeqSource);
            }
            CurrentTargeterResolver = null;
        }

        private void MakeAnimations(SequenceSource SeqSource)
        {
            Vector3 targetPos = m_ability.Targeter.LastUpdateFreePos;  // just testing
            ActorAnimation anim = new ActorAnimation(ArtemisServerResolutionManager.Get().Turn)
            {
                animationIndex = (short)(ActionType + 1),
                actionType = ActionType,
                targetPos = targetPos, // TODO
                actorIndex = m_caster.ActorIndex,
                cinematicCamera = false, // TODO taunts
                tauntNumber = -1,
                reveal = true,
                playOrderIndex = (sbyte)Animations.Count, // TODO sort animations?
                groupIndex = (sbyte)Animations.Count, // TODO what is it?
                bounds = new Bounds(m_caster.CurrentBoardSquare.GetWorldPosition(), new Vector3(10, 3, 10)), // TODO
                HitActorsToDeltaHP = MakeAnimActorToDeltaHP(),
                SeqSource = SeqSource
            };
            Make_000C_X_0014_Z(out anim._000C_X, out anim._0014_Z);

            Animations.Add(anim);
        }

        protected virtual void Make_000C_X_0014_Z(out List<byte> x, out List<byte> y)
        {
            x = new List<byte>();
            y = new List<byte>();
        }

        protected ClientResolutionAction MakeResolutionAction(
            Dictionary<ActorData, ClientActorHitResults> actorToHitResults,
            SequenceSource seqSource)
        {
            List<ServerClientUtils.SequenceStartData> seqStartDataList = MakeSequenceStartList(seqSource);
            Dictionary<Vector3, ClientPositionHitResults> posToHitResults = MakePosToHitResultsList(actorToHitResults, seqStartDataList);
            ClientAbilityResults abilityResults = new ClientAbilityResults(m_caster.ActorIndex, (int)ActionType, seqStartDataList, actorToHitResults, posToHitResults);
            return new ClientResolutionAction(ResolutionActionType.AbilityCast, abilityResults, null, null);
        }

        protected virtual Dictionary<Vector3, ClientPositionHitResults> MakePosToHitResultsList(
             Dictionary<ActorData, ClientActorHitResults> actorToHitResults,
             List<ServerClientUtils.SequenceStartData> seqStartDataList)
        {
            return new Dictionary<Vector3, ClientPositionHitResults>();
            // TODO
        }

        protected virtual List<ServerClientUtils.SequenceStartData> MakeSequenceStartList(SequenceSource seqSource)
        {
            return new List<ServerClientUtils.SequenceStartData>()
            {
                MakeSequenceStart(seqSource)
            };
        }

        protected virtual ServerClientUtils.SequenceStartData MakeSequenceStart(SequenceSource seqSource)
        {
            List<AbilityUtil_Targeter.ActorTarget> actorTargets = m_ability.Targeter.GetActorsInRange();
            ActorData[] targetActorArray = new ActorData[actorTargets.Count];
            for (int i = 0; i < actorTargets.Count; ++i)
            {
                targetActorArray[i] = actorTargets[i].m_actor;
            }
            ServerClientUtils.SequenceStartData result = new ServerClientUtils.SequenceStartData(
                m_ability.m_sequencePrefab,
                m_caster.CurrentBoardSquare,
                targetActorArray,
                m_caster,
                seqSource,
                MakeExtraParams());
            Log.Info($"SequenceStartData: prefab: {result.GetSequencePrefabId()}, pos: {result.GetTargetPos()}, actors: {result.GetTargetActorsString()}");
            return result;
        }

        protected virtual Sequence.IExtraSequenceParams[] MakeExtraParams()
        {
            return null;
        }

        protected virtual Dictionary<ActorData, int> MakeAnimActorToDeltaHP()
        {
            Dictionary<int, int> actorIndexToDeltaHP = Utils.GetActorIndexToDeltaHP(TargetedActors);
            Dictionary<ActorData, int> actorToDeltaHP = new Dictionary<ActorData, int>();
            foreach (var actorIndexAndDeltaHP in actorIndexToDeltaHP)
            {
                ActorData actor = GameFlowData.Get().FindActorByActorIndex(actorIndexAndDeltaHP.Key);
                if (actor != null)
                {
                    actorToDeltaHP.Add(actor, Math.Sign(actorIndexAndDeltaHP.Value));
                }
            }
            return actorToDeltaHP;
        }
    }
}
