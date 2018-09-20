using UnityEngine;
using System.Collections;

public class PlaceHomingSphere : PlacePlayerMade {

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("HomingSpherePlacement") as GameObject;
	}

	public override int getUnlockPosition() {
		return 3;
	}

	public override int getPlacingLength()
	{
		return 14;
	}

	public override float getCoolDown()
	{
		return 162500f; // Large #, above 125000, (referenced in ClassControl -> switchCheck)
	}

	public override bool normalIsValid(RaycastHit hit, Vector3 normal)
	{
		return true;
	}

	public override string GetUnlockName() {
		return "Homing Rocket Launcher";
	}

	public override void setPlacingRotation(Vector3 normal)
	{
		base.setPlacingRotation (normal);
		placing.transform.rotation = Quaternion.LookRotation (normal);
		placing.transform.position += normal * 0.02f;
	}
}
