using ArtemisServer.GameServer.Targeters;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ArtemisServer.GameServer.Abilities
{
    class AbilityResolver_TrapWire : AbilityResolver
    {
        private GridPos m_targetSquare;
        private GridPos m_secondarySquare;
        private Vector3 m_targetPos;

        private ScoundrelTrapWire Ability => m_ability as ScoundrelTrapWire;
        private AbilityUtil_Targeter_Grid Targeter => m_ability.Targeter as AbilityUtil_Targeter_Grid;

        public AbilityResolver_TrapWire(ActorData actor, Ability ability, AbilityPriority priority, ActorTargeting.AbilityRequestData abilityRequestData)
            : base(actor, ability, priority, abilityRequestData)
        {
            BoardSquare square = GetGameplayRefSquare(abilityRequestData.m_targets[0], m_caster);
            m_targetSquare = square.GetGridPosition();
            m_targetPos = GetHighlightGoalPos(Targeter, abilityRequestData.m_targets[0], m_caster); // TODO fix height if it matters (probably just different heights on different maps)
            Vector3 pos = m_targetPos * 2 - square.GetWorldPosition();
            m_secondarySquare = Board.Get().GetSquare(pos).GetGridPosition();
        }

        // based on AbilityUtil_Targeter_Grid
        private static BoardSquare GetGameplayRefSquare(AbilityTarget currentTarget, ActorData targetingActor)
        {
            return Board.Get().GetSquare(currentTarget.GridPos);
        }

        // copied from AbilityUtil_Targeter_Grid
        private static Vector3 GetHighlightGoalPos(AbilityUtil_Targeter_Grid targeter, AbilityTarget currentTarget, ActorData targetingActor)
        {
            BoardSquare gameplayRefSquare = GetGameplayRefSquare(currentTarget, targetingActor);
            if (gameplayRefSquare != null)
            {
                Vector3 centerOfGridPattern = AreaEffectUtils.GetCenterOfGridPattern(targeter.m_pattern, currentTarget.FreePos, gameplayRefSquare);
                Vector3 travelBoardSquareWorldPosition = targetingActor.GetTravelBoardSquareWorldPosition();
                centerOfGridPattern.y = travelBoardSquareWorldPosition.y + 0.1f;
                return centerOfGridPattern;
            }
            return Vector3.zero;
        }

        protected override Vector3 GetTargetPos()
        {
            return m_targetPos;
        }

        protected override void MakeBarriers(SequenceSource seqSource)
        {
            StandardBarrierData data = Ability.ModdedBarrierData();
            BarrierPayload payload = new BarrierPayload();
            payload.OnEnemyHit = delegate (Barrier barrier)
                {
                    payload.RemoveAtTurnEnd = true;
                };
            payload.GetTechPointsForCaster = barrier => Ability.GetBaseTechPointInteractions();
            foreach (Vector3 facingDir in new List<Vector3>() { new Vector3(0, 0, 1), new Vector3(1, 0, 0) })
            {
                var barrier = Utils.ConsBarrier(m_caster, data, m_targetPos, facingDir, seqSource, Ability.ModdedBarrierSequencePrefab());
                Barriers.Add(barrier);
                ArtemisServerBarrierManager.Get().SetBarrierPayload(barrier, payload);
            }
        }

        protected override Dictionary<Vector3, ClientPositionHitResults> MakePosToHitResultsList(
             Dictionary<ActorData, ClientActorHitResults> actorToHitResults,
             List<ServerClientUtils.SequenceStartData> seqStartDataList)
        {
            if (Barriers.Count != 2)
            {
                Log.Error($"Lockwood's ({m_caster.DisplayName}) Trapwire resolution failed! {Barriers.Count} barriers instead of 2!");
                return new Dictionary<Vector3, ClientPositionHitResults>();
            }

            return new Dictionary<Vector3, ClientPositionHitResults>(){
                {
                    m_targetPos,
                    new ClientPositionHitResults(
                    new List<ClientEffectStartData>(),
                    new List<ClientBarrierStartData>()
                    {
                        new ClientBarrierStartData(Barriers[0].m_guid, Barriers[0].GetSequenceStartDataList(), Barrier.BarrierToSerializeInfo(Barriers[0])),
                        new ClientBarrierStartData(Barriers[1].m_guid, new List<ServerClientUtils.SequenceStartData>(), Barrier.BarrierToSerializeInfo(Barriers[1]))
                    },
                    new List<int>(),
                    new List<int>(),
                    new List<ServerClientUtils.SequenceEndData>(),
                    new List<ClientMovementResults>())
                }
            };
        }

        protected override void Make_000C_X_0014_Z(out List<byte> x, out List<byte> y)
        {
            byte _x = (byte)m_caster.CurrentBoardSquare.x;
            byte _y = (byte)m_caster.CurrentBoardSquare.y;

            x = new List<byte>() { _x, (byte)m_targetSquare.x, _x, (byte)m_secondarySquare.x };
            y = new List<byte>() { _y, (byte)m_targetSquare.y, _y, (byte)m_secondarySquare.y };
        }

        protected override ServerClientUtils.SequenceStartData MakeSequenceStart(SequenceSource seqSource)
        {
            ServerClientUtils.SequenceStartData result = new ServerClientUtils.SequenceStartData(
                m_ability.m_sequencePrefab,
                m_targetPos,
                new ActorData[] { },
                m_caster,
                seqSource,
                MakeExtraParams());
            Log.Info($"SequenceStartData: prefab: {result.GetSequencePrefabId()}, pos: {result.GetTargetPos()}, actors: {result.GetTargetActorsString()}");
            return result;
        }
    }
}
