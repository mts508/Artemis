using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BoardSquare : MonoBehaviour
{
	// renamed (was renamed to GetWorldPositionNormal)
	public Vector3 GetWorldPosition()
	{
		return new Vector3(worldX, height, worldY);
	}

	// renamed
	public Vector3 GetWorldPositionForLoS()
	{
		Vector3 worldPosition = GetWorldPosition();
		worldPosition.y += s_LoSHeightOffset;
		return worldPosition;
	}

	// renamed (was renamed to GetWorldPosition)
	public Vector3 GetWorldPositionBaseline()
	{
		Vector3 result = new Vector3(worldX, height, worldY);
		if (Board.Get() != null)
		{
			result.y = Board.Get().BaselineHeight;
		}
		return result;
	}
}
