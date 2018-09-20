using UnityEngine;
using System.Collections;

public class PlaceWindMill : PlacePlayerMade {

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("WindMillPlacement") as GameObject;
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

	public override string GetUnlockName() {
		return "Windmill";
	}

	public override void setPlacingRotation(Vector3 normal)
	{
		base.setPlacingRotation (normal);
		placing.transform.position += normal * 4f;
	}
}
