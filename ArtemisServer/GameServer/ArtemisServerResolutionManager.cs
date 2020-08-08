using Theatrics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ArtemisServer.GameServer.Abilities;

namespace ArtemisServer.GameServer
{
    class ArtemisServerResolutionManager : MonoBehaviour
    {
        private static ArtemisServerResolutionManager instance;

        private Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> TargetedActorsThisTurn;
        private Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> TargetedActorsThisPhase;
        private List<ClientResolutionAction> Actions;
        private List<ActorAnimation> Animations;
        private List<Barrier> Barriers;
        internal AbilityPriority Phase { get; private set; }
        internal Turn Turn;

        private uint m_nextSeqSourceRootID = 0;
        private int m_nextBarrierGuid = 0;

        public uint NextSeqSourceRootID => m_nextSeqSourceRootID++;  // TODO check SequenceSource.s_nextID
        public int NextBarrierGuid => m_nextBarrierGuid++;

        private HashSet<long> TheatricsPendingClients = new HashSet<long>();

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        private void AdvancePhase()
        {
            Phase++;
            if (Phase == AbilityPriority.DEPRICATED_Combat_Charge)
            {
                Phase++;
            }
        }

        public bool ResolveNextPhase()
        {
            bool lastPhase = false;

            TargetedActorsThisPhase = new Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>>();
            Actions = new List<ClientResolutionAction>();
            Animations = new List<ActorAnimation>();
            Barriers = new List<Barrier>();

            var sab = Artemis.ArtemisServer.Get().SharedActionBuffer;

            if (Turn == null)
            {
                Turn = new Turn()
                {
                    TurnID = GameFlowData.Get().CurrentTurn
                };
                TargetedActorsThisTurn = new Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>>();
            }

            while (Actions.Count == 0)
            {
                AdvancePhase();
                if (Phase >= AbilityPriority.NumAbilityPriorities)
                {
                    Log.Info("Abilities resolved");
                    lastPhase = true;
                    break;
                }
                Log.Info($"Resolving {Phase} abilities");

                foreach (ActorData actor in GameFlowData.Get().GetActors())
                {
                    GameFlowData.Get().activeOwnedActorData = actor;
                    ResolveAbilities(actor, Phase);
                }
                GameFlowData.Get().activeOwnedActorData = null;

                Utils.Add(ref TargetedActorsThisTurn, TargetedActorsThisPhase);
            }

            sab.Networkm_abilityPhase = Phase; // TODO check this

            UpdateTheatricsPhase();

            if (lastPhase)
            {
                Turn = null;
                GameFlowData.Get().activeOwnedActorData = null;
                sab.Networkm_actionPhase = ActionBufferPhase.AbilitiesWait;
                sab.Networkm_abilityPhase = AbilityPriority.Prep_Defense;
                Phase = AbilityPriority.INVALID;
                return false;
            }

            SendToAll((short)MyMsgType.StartResolutionPhase, new StartResolutionPhase()
            {
                CurrentTurnIndex = GameFlowData.Get().CurrentTurn,
                CurrentAbilityPhase = Phase,
                NumResolutionActionsThisPhase = Actions.Count
            });

            // TODO friendly/hostile visibility
            Log.Info($"Sending {Actions.Count} actions");
            foreach (ClientResolutionAction action in Actions)
            {
                Log.Info($"Sending action: {action.GetDebugDescription()}, Caster actor: {action.GetCaster().ActorIndex}, Action: {action.GetSourceAbilityActionType()}");
                SendToAll((short)MyMsgType.SingleResolutionAction, new SingleResolutionAction()
                {
                    TurnIndex = GameFlowData.Get().CurrentTurn,
                    PhaseIndex = (int)Phase,
                    Action = action
                });
            }

            foreach (Barrier barrier in Barriers)
            {
                BarrierManager.Get().AddBarrier(barrier, true, out var _);
                // TODO AddBarrier updates ability blocking. Should we update vision/movement/cover?
            }

            // TODO process ClientResolutionManager.SendResolutionPhaseCompleted
            return true;
        }

        public void ResolveAbilities(ActorData actor, AbilityPriority priority)
        {
            AbilityData abilityData = actor.gameObject.GetComponent<AbilityData>();

            // I didn't find any code that calculates what an ability hits aside from UpdateTargeting which is
            // used to draw targeters on the client. In order for it to work on the server we need to
            // * set actor as active owned actor data -- calculations rely on this
            // * AppearAtBoardSquare to set actor's current board square
            // * patch TargeterUtils so that RemoveActorsInvisibleToClient isn't called on the server
            // * disable ActorData.IsVisibleToClient on server (break cover otherwise)
            // * ..?
            foreach (ActorTargeting.AbilityRequestData ard in actor.TeamSensitiveData_authority.GetAbilityRequestData())
            {
                Ability ability = abilityData.GetAbilityOfActionType(ard.m_actionType);

                if (ability.m_runPriority != priority)
                {
                    continue;
                }
                Log.Info($"Resolving {ability.m_abilityName} for {actor.DisplayName}");

                AbilityResolver resolver = GetAbilityResolver(actor, ability, priority, ard);
                resolver.Resolve();
                Actions.AddRange(resolver.Actions);
                Animations.AddRange(resolver.Animations);
                Utils.Add(ref TargetedActorsThisPhase, resolver.TargetedActors);
                Barriers.AddRange(resolver.Barriers);
            }
        }

        public IEnumerator WaitForTheatrics()
        {
            TheatricsPendingClients.Clear();

            foreach (long clientId in TheatricsManager.Get().m_playerConnectionIdsInUpdatePhase)
            {
                TheatricsPendingClients.Add(clientId);
            }
            Log.Info($"Waiting for {TheatricsPendingClients.Count} ({TheatricsManager.Get().m_playerConnectionIdsInUpdatePhase.Count}) clients to perform theatrics");

            while (TheatricsPendingClients.Count > 0)  // TODO add timelimit
            {
                yield return new WaitForSeconds(1);
            }
        }

        private void UpdateTheatricsPhase()
        {
            while (Turn.Phases.Count < (int)Phase)
            {
                Turn.Phases.Add(new Phase(Turn)
                {
                    Index = (AbilityPriority)Turn.Phases.Count
                });
            }

            if (Phase < AbilityPriority.NumAbilityPriorities)
            {
                Dictionary<int, int> actorIndexToDeltaHP = Utils.GetActorIndexToDeltaHP(TargetedActorsThisPhase);
                List<int> participants = new List<int>(actorIndexToDeltaHP.Keys);

                foreach (var action in Actions)
                {
                    int actorIndex = action.GetCaster().ActorIndex;
                    if (!participants.Contains(actorIndex))
                    {
                        participants.Add(actorIndex);
                    }
                }

                Phase phase = new Phase(Turn)
                {
                    Index = Phase,
                    ActorIndexToDeltaHP = actorIndexToDeltaHP,
                    ActorIndexToKnockback = new Dictionary<int, int>(), // TODO
                    Participants = participants, // TODO: add other participants (knockback, energy change, etc)
                    Animations = Animations
                };

                Turn.Phases.Add(phase);
            }

            UpdateTheatrics();
        }

        private void UpdateTheatrics()
        {
            var Theatrics = TheatricsManager.Get();
            Theatrics.m_turn = Turn;
            Theatrics.m_turnToUpdate = Turn.TurnID;
            Theatrics.SetDirtyBit(uint.MaxValue);

            AbilityPriority phase = Phase == AbilityPriority.NumAbilityPriorities ? AbilityPriority.INVALID : Phase;

            Theatrics.PlayPhase(phase);
        }

        public void OnClientResolutionPhaseCompleted(NetworkConnection conn, GameMessageManager.ClientResolutionPhaseCompleted msg)
        {
            Player player = GameFlow.Get().GetPlayerFromConnectionId(conn.connectionId);
            ActorData actor = GameFlowData.Get().FindActorByActorIndex(msg.ActorIndex);

            if (actor.gameObject.GetComponent<PlayerData>().m_player.m_connectionId != conn.connectionId)
            {
                Log.Warning($"OnClientResolutionPhaseCompleted: {actor.DisplayName} does not belong to player {player.m_accountId}!");
            }

            TheatricsPendingClients.Remove(player.m_accountId);
        }

        private AbilityResolver GetAbilityResolver(ActorData actor, Ability ability, AbilityPriority priority, ActorTargeting.AbilityRequestData abilityRequestData)
        {
            if (ability.m_abilityName == "Trick Shot")
            {
                Log.Info("AbilityResolver_TrickShot");
                return new AbilityResolver_TrickShot(actor, ability, priority, abilityRequestData);
            }
            else if (ability.m_abilityName == "Trapwire")
            {
                Log.Info("AbilityResolver_TrapWire");
                return new AbilityResolver_TrapWire(actor, ability, priority, abilityRequestData);
            }
            return new AbilityResolver(actor, ability, priority, abilityRequestData);
        }

        public void ApplyTargets()
        {
            if (TargetedActorsThisTurn == null)
            {
                Log.Error("ArtemisServerResolutionManager.ApplyTargets: No targets to apply!");
            }

            Log.Info("Turn results:");
            foreach (var targetInfo in TargetedActorsThisTurn)
            {
                ActorData target = targetInfo.Key;
                Log.Info($" - {target.DisplayName}");
                foreach (var tooltip in targetInfo.Value)
                {
                    Log.Info($" - - {tooltip.Key} {tooltip.Value}");
                }
                targetInfo.Value.TryGetValue(AbilityTooltipSymbol.Damage, out int damage);
                targetInfo.Value.TryGetValue(AbilityTooltipSymbol.Healing, out int healing);
                targetInfo.Value.TryGetValue(AbilityTooltipSymbol.Absorb, out int absorb);
                targetInfo.Value.TryGetValue(AbilityTooltipSymbol.Energy, out int energy);
                int deltaHP = Mathf.Min(absorb - damage, 0) + healing;
                target.SetHitPoints(target.HitPoints + deltaHP);
                target.SetTechPoints(target.TechPoints + energy);
                Log.Info($" - {target.DisplayName}: deltaHP {deltaHP} ({target.HitPoints} total), deltaEnergy {energy} ({target.TechPoints} total)");
            }
        }

        private void SendToAll(short msgType, MessageBase msg)
        {
            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                //if (!actor.GetPlayerDetails().IsHumanControlled) { continue; }
                actor.connectionToClient?.Send(msgType, msg);
            }
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

        public class StartResolutionPhase : MessageBase
        {
            public int CurrentTurnIndex;
            public AbilityPriority CurrentAbilityPhase;
            public int NumResolutionActionsThisPhase;

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(CurrentTurnIndex);
                writer.Write((sbyte)CurrentAbilityPhase);
                writer.Write((sbyte)NumResolutionActionsThisPhase);
            }

            public override void Deserialize(NetworkReader reader)
            {
                CurrentTurnIndex = reader.ReadInt32();
                CurrentAbilityPhase = (AbilityPriority)reader.ReadSByte();
                NumResolutionActionsThisPhase = reader.ReadSByte();
            }
        }

        public class SingleResolutionAction : MessageBase
        {
            public int TurnIndex;
            public int PhaseIndex;
            public ClientResolutionAction Action;

            public override void Serialize(NetworkWriter writer)
            {
                writer.WritePackedUInt32((uint)TurnIndex);
                writer.Write((sbyte)PhaseIndex);
                IBitStream stream = new NetworkWriterAdapter(writer);
                Action.ClientResolutionAction_SerializeToStream(ref stream);
            }

            public override void Deserialize(NetworkReader reader)
            {
                TurnIndex = (int)reader.ReadPackedUInt32();
                PhaseIndex = reader.ReadSByte();
                IBitStream stream = new NetworkReaderAdapter(reader);
                Action = ClientResolutionAction.ClientResolutionAction_DeSerializeFromStream(ref stream);
            }
        }
    }
}
