using System.Collections.Generic;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

public class ActorTeamSensitiveData : NetworkBehaviour, IGameEventListener
{
	// was empty
	public void MarkAsDirty(DirtyBit bit)
	{
		SetDirtyBit((uint)bit);
	}
	
	// was empty
	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		uint setBits = uint.MaxValue;
		var initialPos = writer.Position;
		if (!forceAll)
		{
			setBits = syncVarDirtyBits;
			writer.WritePackedUInt32(setBits);
		}

		writer.Write((sbyte)m_actorIndex);

		if (IsBitDirty(setBits, DirtyBit.FacingDirection))
		{
			writer.Write((short)VectorUtils.HorizontalAngle_Deg(m_facingDirAfterMovement));
		}

		if (IsBitDirty(setBits, DirtyBit.MoveFromBoardSquare))
		{
			writer.Write((short)(MoveFromBoardSquare?.x ?? -1));
			writer.Write((short)(MoveFromBoardSquare?.y ?? -1));
		}

		if (IsBitDirty(setBits, DirtyBit.InitialMoveStartSquare))
		{
			writer.Write((short)(InitialMoveStartSquare?.x ?? -1));
			writer.Write((short)(InitialMoveStartSquare?.y ?? -1));
		}

		if (IsBitDirty(setBits, DirtyBit.LineData))
		{
			writer.Write(ServerClientUtils.CreateBitfieldFromBools(m_movementLine != null,
				m_numNodesInSnaredLine != 0, false, false, false, false, false, false));
			if (m_movementLine != null)
			{
				LineData.SerializeLine(m_movementLine, writer);
			}

			if (m_numNodesInSnaredLine != 0)
			{
				writer.Write((sbyte)m_numNodesInSnaredLine);
			}
		}

		if (IsBitDirty(setBits, DirtyBit.MovementCameraBound))
		{
			writer.Write((short)MovementCameraBounds.center.x);
			writer.Write((short)MovementCameraBounds.center.z);
			writer.Write((short)MovementCameraBounds.size.x);
			writer.Write((short)MovementCameraBounds.size.z);
		}

		if (IsBitDirty(setBits, DirtyBit.Respawn))
		{
			writer.Write((short)(RespawnPickedSquare?.x ?? -1));
			writer.Write((short)(RespawnPickedSquare?.y ?? -1));

			writer.Write(false); // TODO respawningThisTurn

			writer.Write((short)m_respawnAvailableSquares.Count);
			foreach (var square in m_respawnAvailableSquares)
			{
				writer.Write((short)square.x);
				writer.Write((short)square.y);
			}
		}

		if (IsBitDirty(setBits, DirtyBit.QueuedAbilities) || IsBitDirty(setBits, DirtyBit.AbilityRequestDataForTargeter))
		{
			SerializeAbilityRequestData(writer);
		}

		if (IsBitDirty(setBits, DirtyBit.QueuedAbilities))
		{
			short queuedAbilitiesBitmask = 0;
			for (var index = 0; index < 14; ++index)
			{
				var flag = (short)(1 << index);
				if (m_queuedAbilities[index])
				{
					queuedAbilitiesBitmask |= flag;
				}
			}

			writer.Write(queuedAbilitiesBitmask);
		}

		if (IsBitDirty(setBits, DirtyBit.ToggledOnAbilities))
		{
			short toggledOnAbilitiesBitmask = 0;
			for (var index = 0; index < 14; ++index)
			{
				var flag = (short)(1 << index);
				if (m_abilityToggledOn[index])
				{
					toggledOnAbilitiesBitmask |= flag;
				}
			}

			writer.Write(toggledOnAbilitiesBitmask);
		}

		return initialPos != writer.Position;
	}

	// added
	public void SetAbilityRequestData(List<ActorTargeting.AbilityRequestData> value)
	{
		if (m_abilityRequestData.Equals(value))
		{
			return;
		}
		m_abilityRequestData = value;
		MarkAsDirty(DirtyBit.AbilityRequestDataForTargeter);
	}

	// added
	public LineData.LineInstance MovementLine
	{
		set
		{
			MarkAsDirty(DirtyBit.LineData);
			m_movementLine = value;
		}
		get
		{
			MarkAsDirty(DirtyBit.LineData);
			return m_movementLine;
		}
	}
}
