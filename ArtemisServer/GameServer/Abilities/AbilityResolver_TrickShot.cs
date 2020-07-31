using ArtemisServer.GameServer.Targeters;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ArtemisServer.GameServer.Abilities
{
    class AbilityResolver_TrickShot : AbilityResolver
    {
        public AbilityResolver_TrickShot(ActorData actor, Ability ability, AbilityPriority priority, ActorTargeting.AbilityRequestData abilityRequestData)
            : base(actor, ability, priority, abilityRequestData)
        { }

        protected override TargeterResolver MakeTargeterResolver(Ability ability, int index, AbilityUtil_Targeter targeter, AbilityTarget target, ActorData caster)
        {
             return new TargeterResolver_BounceLaser(targeter, target, caster, ability, index);
        }

        protected override Sequence.IExtraSequenceParams[] MakeExtraParams()
        {
            TargeterResolver_BounceLaser.Targeter_BounceLaser targeter = CurrentTargeterResolver.Targeter as TargeterResolver_BounceLaser.Targeter_BounceLaser;

            List<Vector3> segmentPts = new List<Vector3>();
            foreach (Vector3 v in targeter.m_laserEndpoints)
            {
                segmentPts.Add(new Vector3(v.x, Board.Get().LosCheckHeight, v.z));
            }

            Dictionary<ActorData, AreaEffectUtils.BouncingLaserInfo> laserTargets = new Dictionary<ActorData, AreaEffectUtils.BouncingLaserInfo>();
            foreach (var t in targeter.GetHitActorContext())
            {
                laserTargets.Add(t.actor, new AreaEffectUtils.BouncingLaserInfo(Vector3.zero, t.segmentIndex));
            }

            BouncingShotSequence.ExtraParams param = new BouncingShotSequence.ExtraParams()
            {
                doPositionHitOnBounce = true,
                useOriginalSegmentStartPos = false,
                segmentPts = segmentPts,
                laserTargets = laserTargets
            };
            return param.ToArray();
        }

        protected override Dictionary<Vector3, ClientPositionHitResults> MakePosToHitResultsList(
             Dictionary<ActorData, ClientActorHitResults> actorToHitResults,
             List<ServerClientUtils.SequenceStartData> seqStartDataList)
        {
            var res = new Dictionary<Vector3, ClientPositionHitResults>();
            if (actorToHitResults.Count == 0)
            {
                // Adding fake positional hit at the end of the line so that the client actually shows the shot.
                // It's not how the original server tackled this.
                List<Vector3> segmentPts = (seqStartDataList[0].GetExtraParams()[0] as BouncingShotSequence.ExtraParams).segmentPts;
                res.Add(segmentPts[segmentPts.Count - 1], new ClientPositionHitResults(
                    new List<ClientEffectStartData>(),
                    new List<ClientBarrierStartData>(),
                    new List<int>(),
                    new List<int>(),
                    new List<ServerClientUtils.SequenceEndData>(),
                    new List<ClientMovementResults>()));
            }
            return res;
        }

        protected override Dictionary<ActorData, int> MakeAnimActorToDeltaHP()
        {
            Dictionary<ActorData, int> actorIndexToDeltaHP = base.MakeAnimActorToDeltaHP();
            actorIndexToDeltaHP.Add(m_caster, 0);  // TODO is this needed?
            return actorIndexToDeltaHP;
        }

        protected override void Make_000C_X_0014_Z(out List<byte> x, out List<byte> y)
        {
            byte _x = (byte)m_caster.CurrentBoardSquare.x;
            byte _y = (byte)m_caster.CurrentBoardSquare.y;

            x = new List<byte>() { _x, _x, _x };
            y = new List<byte>() { _y, _y, _y };
        }
    }
}
