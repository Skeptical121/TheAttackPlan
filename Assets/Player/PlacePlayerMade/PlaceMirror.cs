using UnityEngine;
using System.Collections;
using System;

public class PlaceMirror : PlacePlayerMade
{

	public override int getUnlockPosition() {
		return 2;
	}

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("MirrorPlacement") as GameObject;
	}

	public override string getHudType () {
		return "Cooldown";
	}

	public override int getPlacingLength()
	{
		return 32;
	}

	public override float getCoolDown()
	{
		return 30.0f; // Was 16.0f, Also lifetime was 45 seconds. With health being added to mirror, lifetime is now 20 seconds.
	}

	public override string GetUnlockName() {
		return "Mirror";
	}

	public override bool normalIsValid(RaycastHit hit, Vector3 normal)
	{
		return normal.y < 0.000001f;//hit.transform.CompareTag("MirrorEdge");
	}

	public override void setPlacingRotation(Vector3 normal)
	{
		placing.transform.rotation = Quaternion.LookRotation(-normal); //Quaternion.Euler(0, normal, 0);
		placing.transform.position += normal * 0.14f;
	}
}
