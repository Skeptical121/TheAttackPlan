using UnityEngine;
using System.Collections;
using System;

public class PlaceArrowTrigger : PlacePlayerMade
{

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("ArrowTriggerPlacement") as GameObject;
	}

	public override int getUnlockPosition() {
		return 2;
	}

	public override void setPlacingRotation(Vector3 normal)
	{
		placing.transform.rotation = Quaternion.LookRotation(normal, parentPlayerMove.mainCamera.transform.forward);
		placing.transform.position += normal * 0.1f;
	}

	public override bool normalIsValid(RaycastHit hit, Vector3 normal)
	{
		return normal.y > 0.5f;
	}

	public override string GetUnlockName() {
		return "Trap Trigger";
	}

	public override int getPlacingLength()
	{
		return 16;
	}

	public override float getCoolDown()
	{
		return 162500f; // Large #, above 125000, (referenced in ClassControl -> switchCheck)
	}
}
