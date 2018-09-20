using UnityEngine;
using System.Collections;

public class PlaceSentry : PlacePlayerMade
{

	public override int getUnlockPosition() {
		return 3;
	}

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("SentryPlacement") as GameObject;
	}

	public override string getHudType () {
		return "Cooldown";
	}

	public override int getPlacingLength()
	{
		return 7;
	}

	public override float getCoolDown()
	{
		return 60.0f;
	}

	public override string GetUnlockName() {
		return "Sentry";
	}
}
