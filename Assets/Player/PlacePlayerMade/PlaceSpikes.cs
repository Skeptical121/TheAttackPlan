using UnityEngine;
using System.Collections;

public class PlaceSpikes : PlacePlayerMade
{
	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("SpikesPlacement") as GameObject;
	}

	public override int getUnlockPosition() {
		return 3;
	}

	public override int getPlacingLength()
	{
		return 12;
	}

	public override float getCoolDown()
	{
		return 162500f; // Large #, above 125000, (referenced in ClassControl -> switchCheck)
	}
		
	public override string GetUnlockName() {
		return "Spikes";
	}

	public override void setPlacingRotation(Vector3 normal)
	{
		base.setPlacingRotation(normal);
		placing.transform.position += normal * 2f;
		placing.transform.position = new Vector3(Mathf.Round(placing.transform.position.x + 0.5f), placing.transform.position.y, Mathf.Round(placing.transform.position.z));
		Quaternion rot = Quaternion.LookRotation(normal);
		placing.transform.rotation = Quaternion.identity;
	}
}
