using UnityEngine;
using System.Collections;
using System;

public class PlaceLaserField : PlacePlayerMade
{

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("LaserFieldPlacement") as GameObject;
	}

	public override int getUnlockPosition() {
		return 3;
	}

	public override void setPlacingRotation(Vector3 normal)
	{
		placing.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(normal.x, normal.z) * 180 / Mathf.PI + 180f, 0);

		placing.transform.position += normal * 0.12f;
	}

	public override bool normalIsValid(RaycastHit hit, Vector3 normal)
	{
		return normal.y <= 0.5f; // "Math.Abs()"
	}

	public override int getPlacingLength()
	{
		return 12; // Was 10
	}

	public override float getCoolDown()
	{
		return 162500f; // Large #, above 125000, (referenced in ClassControl -> switchCheck)
	}

	public override string GetUnlockName() {
		return "Laser Field";
	}
}
