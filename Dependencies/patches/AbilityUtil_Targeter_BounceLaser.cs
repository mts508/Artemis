using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public partial class AbilityUtil_Targeter_BounceLaser : AbilityUtil_Targeter
{
	public List<Vector3> segmentPts;
	
	public void CreateLaserHighlights(Vector3 originalStart, List<Vector3> laserAnglePoints)
	{
		Vector3 originalStart2 = originalStart + new Vector3(0f, 0.1f - BoardSquare.s_LoSHeightOffset, 0f);
		float worldWidth = this.m_width * Board.Get().squareSize;
		if (base.Highlight == null)
		{
			base.Highlight = HighlightUtils.Get().CreateBouncingLaserCursor(originalStart2, laserAnglePoints, worldWidth);
		}
		else
		{
			UIBouncingLaserCursor component = base.Highlight.GetComponent<UIBouncingLaserCursor>();
			component.OnUpdated(originalStart2, laserAnglePoints, worldWidth);
		}
		
		// Added
		segmentPts = laserAnglePoints;
	}
}
