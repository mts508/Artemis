using Theatrics;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

namespace ArtemisServer.GameServer
{
    class ArtemisServerResolutionManager : MonoBehaviour
    {
        private static ArtemisServerResolutionManager instance;

        private Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> TargetedActors;
        private List<ClientResolutionAction> Actions;
        private List<ActorAnimation> Animations;
        internal AbilityPriority Phase { get; private set; }
        private Turn Turn;

        private uint NextSeqSourceRootID = 0;

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

            TargetedActors = new Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>>();
            Actions = new List<ClientResolutionAction>();
            Animations = new List<ActorAnimation>();

            var sab = Artemis.ArtemisServer.Get().SharedActionBuffer;

            if (Turn == null)
            {
                Turn = new Turn()
                {
                    TurnID = GameFlowData.Get().CurrentTurn
                };
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
            }

            // TODO check this
            //sab.Networkm_abilityPhase = Phase;

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

            // TODO process ClientResolutionManager.SendResolutionPhaseCompleted
            return true;
        }

        private void UpdateTheatricsPhase()
        {
            while (Turn.Phases.Count < (int)Phase)
            {
                Turn.Phases.Add(new Phase(Turn));
            }

            Dictionary<int, int> actorIndexToDeltaHP = GetActorIndexToDeltaHP(TargetedActors);
            List<int> participants = new List<int>(actorIndexToDeltaHP.Keys);

            Phase phase = new Phase(Turn)
            {
                Index = Phase,
                ActorIndexToDeltaHP = actorIndexToDeltaHP,
                ActorIndexToKnockback = new Dictionary<int, int>(), // TODO
                Participants = participants, // TODO: add other participants (knockback, energy change, etc)
                Animations = Animations
            };

            Turn.Phases.Add(phase);
            UpdateTheatrics();
        }

        private void UpdateTheatrics()
        {
            var Theatrics = TheatricsManager.Get();
            Theatrics.m_turn = Turn;
            Theatrics.m_turnToUpdate = Turn.TurnID;
            Theatrics.SetDirtyBit(uint.MaxValue);

            AbilityPriority phase = Phase == AbilityPriority.NumAbilityPriorities ? AbilityPriority.INVALID : Phase;

            // Theatrics.m_phaseToUpdate = phase;
            Theatrics.PlayPhase(phase);
        }

            private Dictionary<int, int> GetActorIndexToDeltaHP(Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> targetedActors)
        {
            Dictionary<int, int> actorIndexToDeltaHP = new Dictionary<int, int>();
            foreach (var targetedActor in targetedActors)
            {
                int actorIndex = targetedActor.Key.ActorIndex;
                targetedActor.Value.TryGetValue(AbilityTooltipSymbol.Healing, out int healing);
                targetedActor.Value.TryGetValue(AbilityTooltipSymbol.Damage, out int damage);
                targetedActor.Value.TryGetValue(AbilityTooltipSymbol.Absorb, out int absorb);  // TODO: how does absorb count here? (does it count at all, does absorb from previous phase somehow affect calculations?)
                int deltaHP = absorb + healing - damage;
                actorIndexToDeltaHP.Add(actorIndex,  deltaHP);
            }
            return actorIndexToDeltaHP;
        }

        private void SendToAll(short msgType, MessageBase msg)
        {
            foreach (ActorData actor in GameFlowData.Get().GetActors())
            {
                //if (!actor.GetPlayerDetails().IsHumanControlled) { continue; }
                actor.connectionToClient?.Send(msgType, msg);
            }
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
                    ability.Targeters[i].SetLastUpdateCursorState(ard.m_targets[i]);
                }

                CalculateTargetedActors(actor, ard.m_actionType, ability);
            }
        }

        public void ApplyTargets()
        {
            foreach (ActorData target in TargetedActors.Keys)
            {
                foreach (AbilityTooltipSymbol symbol in TargetedActors[target].Keys)
                {
                    int value = TargetedActors[target][symbol];
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

        // Based on ActorTargeting.CalculateTargetedActors
        public void CalculateTargetedActors(ActorData instigator, AbilityData.ActionType actionType, Ability abilityOfActionType)
        {
            Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> currentTargetedActors = new Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>>();
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
                            if (!currentTargetedActors.ContainsKey(actorTarget.m_actor))
                            {
                                currentTargetedActors[actorTarget.m_actor] = new Dictionary<AbilityTooltipSymbol, int>();
                            }
                            if (!currentTargetedActors[actorTarget.m_actor].ContainsKey(key))
                            {
                                currentTargetedActors[actorTarget.m_actor][key] = 0;
                            }
                            currentTargetedActors[actorTarget.m_actor][key] += keyValuePair.Value;
                        }
                    }
                }
                num++;
            }

            SequenceSource SeqSource = new SequenceSource(null, null, NextSeqSourceRootID++, true); // TODO
            SeqSource.SetWaitForClientEnable(true);

            foreach (var targetedActor in currentTargetedActors)
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
                    Actions.Add(MakeResolutionAction(
                        instigator,
                        actionType,
                        abilityOfActionType,
                        targetedActor.Key,
                        hitResults,
                        SeqSource));
                }

                TargetedActors.Add(targetedActor.Key, targetedActor.Value);
            }

            Dictionary<int, int> actorIndexToDeltaHP = GetActorIndexToDeltaHP(currentTargetedActors);
            Dictionary<ActorData, int> actorToDeltaHP = new Dictionary<ActorData, int>();
            Vector3 targetPos = abilityOfActionType.Targeter.LastUpdateFreePos;  // just testing
            byte x = (byte)instigator.CurrentBoardSquare.x;  // just testing
            byte y = (byte)instigator.CurrentBoardSquare.y;  // just testing
            foreach (var actorIndexAndDeltaHP in actorIndexToDeltaHP)
            {
                ActorData actor = GameFlowData.Get().FindActorByActorIndex(actorIndexAndDeltaHP.Key);
                if (actor != null)
                {
                    actorToDeltaHP.Add(actor, Math.Sign(actorIndexAndDeltaHP.Value));
                }
            }
            Animations.Add(new ActorAnimation(Turn)
            {
                animationIndex = (short)(actionType + 1),
                actionType = actionType,
                targetPos = targetPos, // TODO
                actorIndex = instigator.ActorIndex,
                cinematicCamera = false, // TODO taunts
                tauntNumber = -1,
                reveal = true,
                playOrderIndex = (sbyte)Animations.Count, // TODO sort animations?
                groupIndex = (sbyte)Animations.Count, // TODO what is it?
                bounds = new Bounds(instigator.CurrentBoardSquare.GetWorldPosition(), new Vector3(10, 3, 10)), // TODO
                HitActorsToDeltaHP = actorToDeltaHP,
                SeqSource = SeqSource,
                _000C_X = new List<byte>() { x, x, x },  // just testing
                _0014_Z = new List<byte>() { y, y, y },  // just testing
            }); ;
        }

        private ClientResolutionAction MakeResolutionAction(
            ActorData instigator,
            AbilityData.ActionType actionType,
            Ability abilityOfActionType,
            ActorData target,
            ClientActorHitResults hitResults,
            SequenceSource seqSource)
        {
            List<ServerClientUtils.SequenceStartData> seqStartDataList = new List<ServerClientUtils.SequenceStartData>()
            {
                MakeSequenceStart(instigator, abilityOfActionType, seqSource)
            };
            Dictionary<ActorData, ClientActorHitResults> actorToHitResults = new Dictionary<ActorData, ClientActorHitResults>();
            actorToHitResults.Add(target, hitResults);
            Dictionary<Vector3, ClientPositionHitResults> posToHitResults = new Dictionary<Vector3, ClientPositionHitResults>();  // TODO

            ClientAbilityResults abilityResults = new ClientAbilityResults(instigator.ActorIndex, (int)actionType, seqStartDataList, actorToHitResults, posToHitResults);

            return new ClientResolutionAction(ResolutionActionType.AbilityCast, abilityResults, null, null);
        }

        private ServerClientUtils.SequenceStartData MakeSequenceStart(
            ActorData instigator,
            Ability ability,
            SequenceSource seqSource)
        {
            List<AbilityUtil_Targeter.ActorTarget> actorTargets = ability.Targeter.GetActorsInRange();
            ActorData[] targetActorArray = new ActorData[actorTargets.Count];
            for(int i = 0; i < actorTargets.Count; ++i)
            {
                targetActorArray[i] = actorTargets[i].m_actor;
            }
            Sequence.IExtraSequenceParams[] extraParams = null;

            if (ability.m_abilityName == "Trick Shot")
            {
                AbilityUtil_Targeter_BounceLaser targeter = ability.Targeter as AbilityUtil_Targeter_BounceLaser;
                List<Vector3> segmentPts = new List<Vector3>();
                foreach (Vector3 v in targeter.segmentPts)
                {
                    segmentPts.Add(new Vector3(v.x, Board.Get().LosCheckHeight, v.z));
                }
                Dictionary<ActorData, AreaEffectUtils.BouncingLaserInfo> laserTargets = new Dictionary<ActorData, AreaEffectUtils.BouncingLaserInfo>();
                foreach (var t in actorTargets)
                {
                    laserTargets.Add(t.m_actor, new AreaEffectUtils.BouncingLaserInfo(Vector3.zero, segmentPts.Count - 1)); // TODO
                }

                BouncingShotSequence.ExtraParams param = new BouncingShotSequence.ExtraParams()
                {
                    doPositionHitOnBounce = true,
                    useOriginalSegmentStartPos = false,
                    segmentPts = segmentPts,
                    laserTargets = laserTargets
                };
                extraParams = param.ToArray();
            }
            ServerClientUtils.SequenceStartData result = new ServerClientUtils.SequenceStartData(
                ability.m_sequencePrefab,
                instigator.CurrentBoardSquare,
                targetActorArray,
                instigator,
                seqSource,
                extraParams);
            Log.Info($"SequenceStartData: prefab: {result.GetSequencePrefabId()}, pos: {result.GetTargetPos()}, actors: {result.GetTargetActorsString()}");
            return result;
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
