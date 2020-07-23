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
}
