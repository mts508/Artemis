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
}
