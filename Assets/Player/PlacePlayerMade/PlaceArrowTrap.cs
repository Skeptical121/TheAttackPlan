using UnityEngine;
using System.Collections;
using System;

public class PlaceArrowTrap : PlacePlayerMade
{

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("ArrowTrapPlacement") as GameObject;
	}

	public override int getUnlockPosition() {
		return 3; // Arrow Trigger needs a spot.
	}

	public override void setPlacingRotation(Vector3 normal)
	{
		placing.transform.rotation = Quaternion.LookRotation(normal);
		placing.transform.position += normal * 0.125f;
	}

	public override bool normalIsValid(RaycastHit hit, Vector3 normal)
	{
		return true;
	}

	public override string GetUnlockName() {
		return "Arrow Trap";
	}

	public override int getPlacingLength()
	{
		return 40;
	}

	public override float getCoolDown()
	{
		return 162500f; // Large #, above 125000, (referenced in ClassControl -> switchCheck)
	}
}
