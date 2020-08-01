using AbilityContextNamespace;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ArtemisServer.GameServer.Targeters
{
	class TargeterResolver_BounceLaser : TargeterResolver
	{
		public TargeterResolver_BounceLaser(AbilityUtil_Targeter targeter, AbilityTarget target, ActorData caster, Ability ability, int targeterIndex)
			: base(targeter, target, caster, ability)
		{
			OverrideTargeter(new Targeter_BounceLaser(targeter as AbilityUtil_Targeter_BounceLaser, ability), targeterIndex);
		}

		public class Targeter_BounceLaser : AbilityUtil_Targeter_BounceLaser
		{
			private List<HitActorContext> m_hitActorContext = new List<HitActorContext>();
			public List<Vector3> m_laserEndpoints = new List<Vector3>();

			public new ReadOnlyCollection<HitActorContext> GetHitActorContext()
			{
				return m_hitActorContext.AsReadOnly();
			}

			public bool m_includeInvisibles = true; // hardcoded false in AbilityUtil_Targeter_BounceLaser

			public Targeter_BounceLaser(AbilityUtil_Targeter_BounceLaser t, Ability ability)
				: base(
					  ability,
					  t.m_width,
					  t.m_maxDistancePerBounce,
					  t.m_maxTotalDistance,
					  t.m_maxBounces,
					  t.m_maxTargetsHit,
					  t.m_bounceOnActors)
			{
				SetTargeterRangeDelegates(t.m_extraTotalDistanceDelegate, t.m_extraDistancePerBounceDelegate, t.m_extraBouncesDelegate);
				InitKnockbackData(t.m_knockbackDistance, t.m_knockbackType, t.m_maxKnockbackTargets, t.m_extraKnockdownDelegate);
				m_penetrateTargetsAndHitCaster = t.m_penetrateTargetsAndHitCaster;
			}

			public override void UpdateTargetingMultiTargets(AbilityTarget currentTarget, ActorData targetingActor, int currentTargetIndex, List<AbilityTarget> targets)
			{
				ClearActorsInRange();
				m_hitActorContext.Clear();
				Vector3 travelBoardSquareWorldPositionForLos = targetingActor.GetTravelBoardSquareWorldPositionForLos();
				Vector3 aimDirection = currentTarget?.AimDirection ?? targetingActor.transform.forward;
				float maxDistancePerBounce = m_maxDistancePerBounce + (m_extraDistancePerBounceDelegate != null ? m_extraDistancePerBounceDelegate() : 0f);
				float maxTotalDistance = m_maxTotalDistance + (m_extraTotalDistanceDelegate != null ? m_extraTotalDistanceDelegate() : 0f);
				int maxBounces = m_maxBounces + (m_extraBouncesDelegate != null ? Mathf.RoundToInt(m_extraBouncesDelegate()) : 0);
				int maxTargetsHit = m_maxTargetsHit;
				if (m_ability is ScoundrelBouncingLaser && CollectTheCoins.Get() != null)
				{
					maxTotalDistance += CollectTheCoins.Get().m_bouncingLaserTotalDistance.GetBonus_Client(targetingActor);
					maxDistancePerBounce += CollectTheCoins.Get().m_bouncingLaserBounceDistance.GetBonus_Client(targetingActor);
					maxBounces += Mathf.RoundToInt(CollectTheCoins.Get().m_bouncingLaserBounces.GetBonus_Client(targetingActor));
					maxTargetsHit += Mathf.RoundToInt(CollectTheCoins.Get().m_bouncingLaserPierces.GetBonus_Client(targetingActor));
				}
				bool penetrateTargetsAndHitCaster = m_penetrateTargetsAndHitCaster;
				List<Vector3> endpoints = VectorUtils.CalculateBouncingLaserEndpoints(
					travelBoardSquareWorldPositionForLos,
					aimDirection,
					maxDistancePerBounce,
					maxTotalDistance,
					maxBounces,
					targetingActor,
					m_width,
					maxTargetsHit,
					m_includeInvisibles, // was false
					GetAffectedTeams(),
					m_bounceOnActors,
					out Dictionary<ActorData, AreaEffectUtils.BouncingLaserInfo> bounceHitActors,
					out List<ActorData> orderedHitActors,
					null,
					penetrateTargetsAndHitCaster);
				if (penetrateTargetsAndHitCaster && endpoints.Count > 1)
				{
					float totalMaxDistanceInSquares = maxTotalDistance - (endpoints[0] - travelBoardSquareWorldPositionForLos).magnitude / Board.Get().squareSize;
					Vector3 normalized = (endpoints[1] - endpoints[0]).normalized;
					VectorUtils.CalculateBouncingLaserEndpoints(
						endpoints[0],
						normalized,
						maxDistancePerBounce,
						totalMaxDistanceInSquares,
						maxBounces,
						targetingActor,
						m_width,
						0,
						m_includeInvisibles, // was false
						targetingActor.GetTeams(),
						m_bounceOnActors,
						out Dictionary<ActorData, AreaEffectUtils.BouncingLaserInfo> _,
						out List<ActorData> orderedHitActors2,
						null,
						false,
						false);
					if (orderedHitActors2.Contains(targetingActor))
					{
						AddActorInRange(targetingActor, targetingActor.GetTravelBoardSquareWorldPositionForLos(), targetingActor, AbilityTooltipSubject.Self);
					}
				}
				foreach (var hitActor in bounceHitActors)
				{
					AddActorInRange(hitActor.Key, hitActor.Value.m_segmentOrigin, targetingActor);
					if (hitActor.Value.m_endpointIndex > 0)
					{
						SetIgnoreCoverMinDist(hitActor.Key, true);
					}
				}

				HitActorContext item = default(HitActorContext);
				for (int i = 0; i < orderedHitActors.Count; i++)
				{
					ActorData hitActor = orderedHitActors[i];
					AreaEffectUtils.BouncingLaserInfo bouncingLaserInfo = bounceHitActors[hitActor];
					item.actor = hitActor;
					item.segmentIndex = bouncingLaserInfo.m_endpointIndex;
					m_hitActorContext.Add(item);
					ActorHitContext actorHitContext = m_actorContextVars[hitActor];
					actorHitContext.source = targetingActor.GetTravelBoardSquareWorldPositionForLos();
					actorHitContext.context.SetInt(TargetSelect_BouncingLaser.s_cvarEndpointIndex.GetKey(), bouncingLaserInfo.m_endpointIndex);
					actorHitContext.context.SetInt(TargetSelect_BouncingLaser.s_cvarHitOrder.GetKey(), i);
				}

				m_laserEndpoints = endpoints;

				//CreateLaserHighlights(travelBoardSquareWorldPositionForLos, endpoints);
				//if (targetingActor == GameFlowData.Get().activeOwnedActorData)
				//{
				//	ResetSquareIndicatorIndexToUse();
				//	AreaEffectUtils.OperateOnSquaresInBounceLaser(m_indicatorHandler, travelBoardSquareWorldPositionForLos, endpoints, m_width, targetingActor, false);
				//	HideUnusedSquareIndicators();
				//}

				if (m_knockbackDistance > 0f)
				{
					int movementArrowIndex = 0;
					EnableAllMovementArrows();
					for (int i = 0; i < orderedHitActors.Count; i++)
					{
						ActorData hitActor = orderedHitActors[i];
						if (hitActor.GetTeam() == targetingActor.GetTeam() ||
							m_maxKnockbackTargets > 0 && i >= m_maxKnockbackTargets)
						{
							continue;
						}
						float knockbackDistance = m_knockbackDistance + (m_extraKnockdownDelegate != null ? m_extraKnockdownDelegate(hitActor) : 0f);
						AreaEffectUtils.BouncingLaserInfo bouncingLaserInfo = bounceHitActors[hitActor];
						Vector3 aimDir = endpoints[bouncingLaserInfo.m_endpointIndex] - bouncingLaserInfo.m_segmentOrigin;
						BoardSquarePathInfo path = KnockbackUtils.BuildKnockbackPath(hitActor, m_knockbackType, aimDir, bouncingLaserInfo.m_segmentOrigin, knockbackDistance);
						movementArrowIndex = AddMovementArrowWithPrevious(hitActor, path, TargeterMovementType.Knockback, movementArrowIndex);
					}
					SetMovementArrowEnabledFromIndex(movementArrowIndex, false);
				}
			}
		}
	}
}
