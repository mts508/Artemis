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
        public List<Barrier> Barriers;

        protected TargeterResolver CurrentTargeterResolver { get; private set; }

        protected AbilityData.ActionType ActionType { get => m_abilityRequestData.m_actionType; }

        public AbilityResolver(ActorData actor, Ability ability, AbilityPriority priority, ActorTargeting.AbilityRequestData abilityRequestData)
        {
            m_caster = actor;
            m_ability = ability;
            m_priority = priority;
            m_abilityRequestData = abilityRequestData;
        }

        public void Resolve()
        {
            TargetedActors = new Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>>();
            Actions = new List<ClientResolutionAction>();
            Animations = new List<ActorAnimation>();
            Barriers = new List<Barrier>();
            ResolveImpl();
        }

        protected virtual void ResolveImpl()
        {
            List<TargeterResolver> targeterResolvers = new List<TargeterResolver>();

            for (int i = 0; i < m_abilityRequestData.m_targets.Count; ++i)
            {
                CurrentTargeterResolver = MakeTargeterResolver(m_ability, i, m_abilityRequestData.m_targets[i], m_caster);
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
                    Utils.Add(TargetedActors, targets);
                }

                Dictionary<ActorData, ClientActorHitResults> actorToHitResults = new Dictionary<ActorData, ClientActorHitResults>();
                foreach (var targetedActor in TargetedActors)
                {
                    CurrentTargeterResolver.Targeter.GetActorContextVars().TryGetValue(targetedActor.Key, out var hitContext);
                    foreach (var symbol in targetedActor.Value)
                    {
                        ClientActorHitResults hitResults;
                        switch (symbol.Key)
                        {
                            // NOTE: If you add something here (or in descendants), make sure that ArtemisServerResolutionManager.ApplyActions can process it
                            case AbilityTooltipSymbol.Damage:
                                CurrentTargeterResolver.Targeter.IsActorInTargetRange(targetedActor.Key, out bool inCover);
                                hitResults = new ClientActorHitResultsBuilder()
                                    .SetDamage(symbol.Value, Vector3.zero, inCover, false, false)  // TODO
                                    .SetRevealCaster()  // TODO
                                    .SetRevealTarget()  // TODO
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

                SequenceSource seqSource = MakeSequenceSource();
                MakeBarriers(seqSource);
                Actions.Add(MakeResolutionAction(actorToHitResults, seqSource));
                MakeAnimations(seqSource);
            }
            CurrentTargeterResolver = null;
        }

        protected TargeterResolver MakeTargeterResolver(Ability ability, int index, AbilityTarget target, ActorData caster)
        {
            AbilityUtil_Targeter targeter = ability.Targeters[index];

            if (targeter == null)
            {
                return null;
            }

            return MakeTargeterResolver(ability, index, targeter, target, caster);
        }

        protected virtual TargeterResolver MakeTargeterResolver(Ability ability, int index, AbilityUtil_Targeter targeter, AbilityTarget target, ActorData caster)
        {
            return new TargeterResolver(targeter, target, caster, ability);
        }

        protected virtual SequenceSource MakeSequenceSource()
        {
            SequenceSource seqSource = new SequenceSource(null, null, ArtemisServerResolutionManager.Get().NextSeqSourceRootID, true); // TODO
            seqSource.SetWaitForClientEnable(true);
            return seqSource;
        }

        protected virtual void MakeBarriers(SequenceSource seqSource)
        {

        }

        protected virtual ClientResolutionAction MakeResolutionAction(
            Dictionary<ActorData, ClientActorHitResults> actorToHitResults,
            SequenceSource seqSource)
        {
            List<ServerClientUtils.SequenceStartData> seqStartDataList = MakeSequenceStartList(seqSource);
            Dictionary<Vector3, ClientPositionHitResults> posToHitResults = MakePosToHitResultsList(actorToHitResults, seqStartDataList);
            ClientAbilityResults abilityResults = new ClientAbilityResults(m_caster.ActorIndex, (int)ActionType, seqStartDataList, actorToHitResults, posToHitResults);
            return new ClientResolutionAction(ResolutionActionType.AbilityCast, abilityResults, null, null);
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

        protected virtual Dictionary<Vector3, ClientPositionHitResults> MakePosToHitResultsList(
             Dictionary<ActorData, ClientActorHitResults> actorToHitResults,
             List<ServerClientUtils.SequenceStartData> seqStartDataList)
        {
            return new Dictionary<Vector3, ClientPositionHitResults>();
            // TODO
        }

        protected virtual void MakeAnimations(SequenceSource SeqSource)
        {
            ActorAnimation anim = new ActorAnimation(ArtemisServerResolutionManager.Get().Turn)
            {
                animationIndex = (short)(ActionType + 1),
                actionType = ActionType,
                targetPos = GetTargetPos(), // TODO
                actorIndex = m_caster.ActorIndex,
                cinematicCamera = false, // TODO taunts
                tauntNumber = -1,
                reveal = true,
                playOrderIndex = (sbyte)Animations.Count, // TODO sort animations?
                groupIndex = (sbyte)Animations.Count, // TODO what is it? it seems it almost always equals playOrdexIndex
                bounds = MakeBounds(), // TODO
                HitActorsToDeltaHP = MakeAnimActorToDeltaHP(),
                SeqSource = SeqSource
            };
            Make_000C_X_0014_Z(out anim._000C_X, out anim._0014_Z);

            Animations.Add(anim);
        }

        protected virtual Vector3 GetTargetPos()
        {
            return m_ability.Targeter.LastUpdateFreePos;
        }

        protected virtual Bounds MakeBounds()
        {
            Bounds bounds = new Bounds(m_caster.CurrentBoardSquare.GetWorldPosition(), new Vector3(4, 3, 4));
            foreach (var actorTarget in m_ability.Targeter.GetActorsInRange())
            {
                bounds.Encapsulate(actorTarget.m_actor.GetTravelBoardSquareWorldPosition()); // TODO hostile visibility
            }
            bounds.Encapsulate(GetTargetPos());
            return bounds;
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

        protected virtual void Make_000C_X_0014_Z(out List<byte> x, out List<byte> y)
        {
            x = new List<byte>();
            y = new List<byte>();
        }
    }
}
