using UnityEngine;
using System.Collections;
using System;

public class PlaceEarthMound : PlacePlayerMade
{

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("DirtMoundPlacement") as GameObject;
	}

	public override int getUnlockPosition() {
		return 3;
	}

	public override string getHudType () {
		return "Cooldown";
	}

	public override int getPlacingLength()
	{
		return 16;
	}

	public override float getCoolDown()
	{
		return 16.0f;
	}

	public override string GetUnlockName() {
		return "Hover Block";
	}
}
