using UnityEngine;
using System.Collections;
using System;

public class PlaceDamageBox : PlacePlayerMade
{

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("DamageBoxPlacement") as GameObject;
	}

	public override int getUnlockPosition() {
		return 2;
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
		return 24.0f;
	}

	public override void setPlacingRotation(Vector3 normal)
	{
		placing.transform.position += new Vector3(0, 1, 0);
		base.setPlacingRotation (normal);
	}

	public override string GetUnlockName() {
		return "Circle of Power";
	}
}
