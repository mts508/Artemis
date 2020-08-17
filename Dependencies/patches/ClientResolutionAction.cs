using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientResolutionAction : IComparable
{
	public ClientResolutionAction(
		ResolutionActionType type,
		ClientAbilityResults abilityResults,
		ClientEffectResults effectResults,
		ClientMovementResults moveResults)
	{
		m_type = type;
		m_abilityResults = abilityResults;
		m_effectResults = effectResults;
		m_moveResults = moveResults;
	}

	public void ClientResolutionAction_SerializeToStream(ref IBitStream stream)
	{
		sbyte actionType = (sbyte)m_type;
		stream.Serialize(ref actionType);

		switch (m_type)
		{
			case ResolutionActionType.AbilityCast:
				m_abilityResults.SerializeToStream(ref stream);
				break;
			case ResolutionActionType.EffectAnimation:
			case ResolutionActionType.EffectPulse:
				m_effectResults.SerializeToStream(ref stream);
				break;
			case ResolutionActionType.EffectOnMove:
			case ResolutionActionType.BarrierOnMove:
			case ResolutionActionType.PowerupOnMove:
			case ResolutionActionType.GameModeOnMove:
				m_moveResults.SerializeToStream(ref stream);
				break;
		}
	}

	// GetHitResults + moveResults
	public void GetAllHitResults(out Dictionary<ActorData, ClientActorHitResults> actorHitResList, out Dictionary<Vector3, ClientPositionHitResults> posHitResList)
	{
		actorHitResList = null;
		posHitResList = null;
		if (m_abilityResults != null)
		{
			actorHitResList = m_abilityResults.GetActorHitResults();
			posHitResList = m_abilityResults.GetPosHitResults();
			return;

		}
		if (m_effectResults != null)
		{
			actorHitResList = m_effectResults.GetActorHitResults();
			posHitResList = m_effectResults.GetPosHitResults();
			return;
		}
		if (m_moveResults != null)
		{
			m_moveResults.GetHitResults(out actorHitResList, out posHitResList);
			return;
		}
	}

	// GetCaster + moveResults
	public ActorData GetAllCaster()
	{
		return GetCaster() ?? m_moveResults?.GetCaster();
	}
}
