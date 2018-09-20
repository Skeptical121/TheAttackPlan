using UnityEngine;
using System.Collections;
using System;

public class PlaceShield : PlacePlayerMade {

	public override int getUnlockPosition() {
		return 2;
	}

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("ShieldPlacement") as GameObject;
	}

	public override string getHudType () {
		return "Cooldown";
	}

	public override int getPlacingLength()
	{
		return 45; // Massive range
	}

	public override float getCoolDown()
	{
		return 14.0f;
	}

	public override string GetUnlockName() {
		return "Shield";
	}
}
