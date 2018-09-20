using UnityEngine;
using System.Collections;

public class PlaceSawBlade : PlacePlayerMade
{
	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("SawBladesPlacement") as GameObject;
	}

	public override int getUnlockPosition() {
		return 3;
	}

	public override int getPlacingLength()
	{
		return 14;
	}

	public override string GetUnlockName() {
		return "Saw Blade";
	}

	public override float getCoolDown()
	{
		return 162500f; // Large #, above 125000, (referenced in ClassControl -> switchCheck)
	}
}
