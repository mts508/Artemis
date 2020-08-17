using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ArtemisServer.GameServer
{
    class ArtemisServerBarrierManager : MonoBehaviour
    {
        private static ArtemisServerBarrierManager instance;

        // So we don't have to patch the dll every time
        private Dictionary<int, BarrierPayload> m_barrierPayload = new Dictionary<int, BarrierPayload>();

        public void UpdateTurn()
        {
            List<Barrier> barriersToRemove = new List<Barrier>();
            foreach (Barrier barrier in BarrierManager.Get().m_barriers)
            {
                barrier.m_time.age++;
                if (GetBarrierPayload(barrier)?.RemoveAtTurnEnd ?? false)
                {
                    Log.Info($"Barrier by {barrier.Caster.DisplayName} requested to be removed");
                    barriersToRemove.Add(barrier);
                }
                else if (barrier.m_time.age >= barrier.m_time.duration)
                {
                    Log.Info($"Barrier by {barrier.Caster.DisplayName} expired");
                    barriersToRemove.Add(barrier);
                    GetBarrierPayload(barrier)?.OnExpire?.Invoke(barrier);
                }
            }
            RemoveBarriers(barriersToRemove);
        }

        public void SetBarrierPayload(Barrier barrier, BarrierPayload payload)
        {
            m_barrierPayload[barrier.m_guid] = payload;
        }

        private BarrierPayload GetBarrierPayload(Barrier barrier)
        {
            m_barrierPayload.TryGetValue(barrier.m_guid, out var payload);
            return payload;
        }

        private void RemoveBarriers(List<Barrier> barriersToRemove)
        {
            if (barriersToRemove.Count > 0)
            {
                foreach (Barrier barrier in barriersToRemove)
                {
                    Log.Info($"Removing barrier by {barrier.Caster.DisplayName}");
                    BarrierManager.Get().RemoveBarrier(barrier);
                    SharedEffectBarrierManager.Get().EndBarrier(barrier.m_guid);
                    GetBarrierPayload(barrier)?.OnEnd?.Invoke(barrier);
                }
                BarrierManager.Get().CallRpcUpdateBarriers();
            }
        }

        public List<ClientResolutionAction> OnMovement(Dictionary<int, BoardSquarePathInfo> paths)
        {
            // sort
            Dictionary<int, ActorData> actorsByIndex = Utils.GetActorByIndex();
            List<MovementNode> nodes = new List<MovementNode>();
            foreach (var p in paths)
            {
                for (var n = p.Value; n != null; n = n.next)
                {
                    actorsByIndex.TryGetValue(p.Key, out var actor);
                    nodes.Add(new MovementNode
                    {
                        pathInfo = n,
                        actor = actor
                    });
                }
            }
            nodes.Sort();

            var actions = new List<ClientResolutionAction>();
            var hitActorToBarrierRootIds = new Dictionary<int, HashSet<uint>>(); // storing root id because some barriers are multisegment (segments have different guids but the same root id)
            foreach (var node in nodes)
            {
                if (node.pathInfo.prev == null)
                {
                    continue;
                }
                foreach (Barrier barrier in BarrierManager.Get().m_barriers)
                {
                    if (barrier.CrossingBarrier(node.pathInfo.square.GetWorldPosition(), node.pathInfo.prev.square.GetWorldPosition()))
                    {
                        if (!hitActorToBarrierRootIds.ContainsKey(node.actor.ActorIndex))
                        {
                            hitActorToBarrierRootIds[node.actor.ActorIndex] = new HashSet<uint>();
                        }
                        if (hitActorToBarrierRootIds[node.actor.ActorIndex].Contains(barrier.BarrierSequenceSource.RootID))
                        {
                            Log.Info($"{node.actor.DisplayName} hit barrier by {barrier.Caster.DisplayName} [ignoring repeated hit]");
                            continue;
                        }
                        else
                        {
                            Log.Info($"{node.actor.DisplayName} hit barrier by {barrier.Caster.DisplayName}");
                            hitActorToBarrierRootIds[node.actor.ActorIndex].Add(barrier.BarrierSequenceSource.RootID);
                        }

                        if (node.actor.GetTeam() != barrier.GetBarrierTeam() && barrier.m_onEnemyMovedThrough != null)
                        {
                            GetBarrierPayload(barrier)?.EnemyHit(barrier);
                            var response = barrier.m_onEnemyMovedThrough;
                            Log.Info($"{node.actor.DisplayName} hit barrier by {barrier.Caster.DisplayName}: dmg {response.m_damage}, hlg {response.m_healing}, tec {response.m_techPoints}");
                            var actorToHitResults = new Dictionary<ActorData, ClientActorHitResults>
                                {{ node.actor, MakeClientActorHitResults(barrier, response, node.actor) }};
                            actions.Add(MakeResolutionAction(node, barrier, response.m_sequenceToPlay, actorToHitResults));
                            OnBarrierHit(barrier, node.actor);
                        }
                        else if (node.actor.GetTeam() == barrier.GetBarrierTeam() && barrier.m_onAllyMovedThrough != null)
                        {
                            GetBarrierPayload(barrier)?.AllyHit(barrier);
                            // TODO ally barrier hit
                        }
                    }
                }
            }
            return actions;
        }

        private ClientActorHitResults MakeClientActorHitResults(Barrier barrier, GameplayResponseForActor response, ActorData hitter)
        {
            var casterStats = barrier.Caster.GetActorStats();
            var casterStatus = barrier.Caster.GetActorStatus();
            bool empowered = casterStatus?.HasStatus(StatusType.Empowered) ?? false;
            bool weakened = casterStatus?.HasStatus(StatusType.Weakened) ?? false;
            int finalDamage = casterStats?.CalculateOutgoingDamageForTargeter(response.m_damage) ?? response.m_damage;
            int finalHealing = casterStats?.CalculateOutgoingHealForTargeter(response.m_healing) ?? response.m_healing;
            int techPointGainForCaster = 0;
            var payload = GetBarrierPayload(barrier);
            if (payload != null && payload.GetTechPointsForCaster != null)
            {
                foreach (var tpi in payload.GetTechPointsForCaster(barrier))
                {
                    switch (tpi.m_type)
                    {
                        case TechPointInteractionType.RewardOnDamage_OncePerCast:
                            if (finalDamage > 0 && payload.Hits == 1)
                            {
                                techPointGainForCaster += tpi.m_amount;
                            }
                            break;
                        case TechPointInteractionType.RewardOnDamage_PerTarget:
                            if (finalDamage > 0)
                            {
                                techPointGainForCaster += tpi.m_amount;
                            }
                            break;
                        case TechPointInteractionType.RewardOnHit_OncePerCast:
                            if (payload.Hits == 1)
                            {
                                techPointGainForCaster += tpi.m_amount;
                            }
                            break;
                        case TechPointInteractionType.RewardOnHit_PerAllyTarget:
                            if (barrier.Caster.GetTeam() == hitter.GetTeam())
                            {
                                techPointGainForCaster += tpi.m_amount;
                            }
                            break;
                        case TechPointInteractionType.RewardOnHit_PerEnemyTarget:
                            if (barrier.Caster.GetTeam() != hitter.GetTeam())
                            {
                                techPointGainForCaster += tpi.m_amount;
                            }
                            break;
                        case TechPointInteractionType.RewardOnHit_PerTarget:
                            techPointGainForCaster += tpi.m_amount;
                            break;
                    }
                }
            }
            return new ClientActorHitResultsBuilder()
                .SetDamage(finalDamage, barrier.GetCenterPos(), false, empowered && !weakened, weakened && !empowered) // TODO apply weakened/empowered/energized/etc in builder
                .SetHealing(finalHealing)
                .SetTechPoints(response.m_techPoints, 0, techPointGainForCaster)  // TODO maybe response.m_techPoints is for caster too?
                .SetRevealTarget()
                .SetCanBeReactedTo(false)
                .Build();
        }

        private void OnBarrierHit(Barrier barrier, ActorData hitter)
        {
            bool remove = false;
            List<Barrier> barriers = GetJointBarriers(barrier);
            foreach (var b in barriers)
            {
                if (b.m_maxHits > 0)
                {
                    if (b.m_maxHits == 1)
                    {
                        remove = true;
                        break;
                    }
                    b.m_maxHits--;
                }
            }
            if (remove)
            {
                RemoveBarriers(barriers);
            }
        }

        private ClientResolutionAction MakeResolutionAction(
            MovementNode node,
            Barrier barrier,
            GameObject sequenceToPlay,
            Dictionary<ActorData, ClientActorHitResults> actorToHitResults)
        {
            SequenceSource seqSource = new SequenceSource(null, null, ArtemisServerResolutionManager.Get().NextSeqSourceRootID, true);
            ServerClientUtils.SequenceStartData seqStart = new ServerClientUtils.SequenceStartData(
                sequenceToPlay,
                node.pathInfo.square,
                new ActorData[] { node.actor },
                barrier.Caster,
                seqSource);
            ClientBarrierResults barrierResults = new ClientBarrierResults(
                barrier.m_guid,
                barrier.Caster,
                actorToHitResults,
                new Dictionary<Vector3, ClientPositionHitResults>());
            ClientMovementResults movementResults = new ClientMovementResults(
                node.actor,
                node.pathInfo,
                new List<ServerClientUtils.SequenceStartData> { seqStart },
                null,
                barrierResults,
                null,
                null);
            return new ClientResolutionAction(ResolutionActionType.BarrierOnMove, null, null, movementResults);
        }

        private List<Barrier> GetJointBarriers(Barrier barrier)
        {
            uint id = barrier.BarrierSequenceSource.RootID;
            List<Barrier> result = new List<Barrier>();
            foreach (Barrier b in BarrierManager.Get().m_barriers)
            {
                if (b.BarrierSequenceSource.RootID == id)
                {
                    result.Add(b);
                }
            }
            return result;
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public static ArtemisServerBarrierManager Get()
        {
            return instance;
        }

        private class MovementNode : IComparable<MovementNode>
        {
            public BoardSquarePathInfo pathInfo;
            public ActorData actor;

            public int CompareTo(MovementNode other)
            {
                return pathInfo.moveCost.CompareTo(other.pathInfo.moveCost); // or f_cost?
            }

            public Vector3 GetWorldPosition()
            {
                return pathInfo.square.GetWorldPosition();
            }
        }
    }

    public class BarrierPayload
    {
        public delegate void BarrierDelegate(Barrier barrier);
        public delegate TechPointInteraction[] TechPointBarrierDelegate(Barrier barrier);

        public BarrierDelegate OnEnemyHit;
        public BarrierDelegate OnAllyHit;
        public BarrierDelegate OnExpire;
        public BarrierDelegate OnEnd;

        public TechPointBarrierDelegate GetTechPointsForCaster;

        public int EnemyHits;
        public int AllyHits;
        public int Hits => EnemyHits + AllyHits;
        public bool RemoveAtTurnEnd = false;

        public void EnemyHit(Barrier barrier)
        {
            EnemyHits++;
            OnEnemyHit?.Invoke(barrier);
        }

        public void AllyHit(Barrier barrier)
        {
            AllyHits++;
            OnAllyHit?.Invoke(barrier);
        }
    }
}
