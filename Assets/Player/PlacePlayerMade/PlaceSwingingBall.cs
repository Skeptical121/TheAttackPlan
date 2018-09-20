using UnityEngine;
using System.Collections;

public class PlaceSwingingBall : PlacePlayerMade
{
	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("SwingingBallPlacement") as GameObject;
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
		return "Swinging Spike Ball";
	}

	public override void setPlacingRotation(Vector3 normal)
	{
		base.setPlacingRotation (normal);
		placing.transform.position += normal * 6f;
	}


}
