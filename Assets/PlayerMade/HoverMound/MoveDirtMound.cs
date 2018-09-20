using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

// Dirt mound should probably have a healthVar for consistency?
public class MoveDirtMound : PlayerMade
{

	const float timeToGoUp = 2;
	const float timeToPause = 16; // Time it pauses at "top"
	const float timeToGoDown = 3;

	float scale = 0;

	Transform[,] emps = new Transform[5,5]; // Earth mound positions

	public override short getMaxHealth ()
	{
		return 250; // Hover mound has no health. Of course, it could.. but probably not.
	}

	public override float getLifeTime ()
	{
		return timeToGoUp + timeToPause + timeToGoDown;
	}

	public override float getBuildTime ()
	{
		return 32f; // This is ALWAYS building. As it relies on its lifeTime.
		// Note that laserField just directly uses SetInformation / ServerSyncFixedUpdate, but this is because they are already overrided, and it is practical that a laser field would have a build animation in the future.
	}

	public override void initBuildAnimation ()
	{
		UpdateMound (0);
	}

	public override void buildAnimation (float lifeTime)
	{
		UpdateMound (lifeTime);
	}

	public override void setHealthBar() {
		// No health bar on EarthMound.
	}

	// Needs a rigidbody.
	void OnTriggerStay(Collider hit)
	{

		if (!OperationNetwork.isServer)
		{
			return;
		}


		if (hit.gameObject.layer == 8 || hit.gameObject.layer == 22)
		{
			Transform plyr = hit.transform;
			Combat combat = plyr.GetComponent<Combat>();
		}
	}

	public void UpdateMoundArmature(List<Vector3> playerPositions) {
		for (int x = 0; x < 5; x++) {
			for (int y = 0; y < 5; y++) {
				emps [x,y].localScale = new Vector3(Mathf.Cos(Mathf.Sqrt((2f - x) * (2f - x) + (2f - y) * (2f - y)) / 1.85f), 1, 1); // x
				foreach (Vector3 pos in playerPositions) {
					float dist = Vector2.Distance (new Vector2(-pos.x, pos.y), new Vector2 ((x - 2) * 2, (y - 2) * 2));
					float maxDist = 2f;
					if (dist < maxDist) {
						// Max pos z is around 8 right now
						// Min pos z is around 4
						if (dist < 1f) { // This is the closest distance in a 2 grid:
							emps [x, y].localScale = new Vector3 (Mathf.Min(1 - Mathf.Min (Mathf.Max ((3.1f - pos.z) / 1.3f, 0), 1), emps [x, y].localScale.x), 1, 1);
						} else {
							emps [x, y].localScale = new Vector3 (Mathf.Min(1 - Mathf.Min (Mathf.Max ((3.1f - pos.z) / 1.3f * Mathf.Sqrt (maxDist * maxDist - dist * dist) / maxDist, 0), 1), emps [x, y].localScale.x), 1, 1);
						}
					}
				}
			}
		}
	}

	public void UpdateMound(float netTime)
	{
		if (emps [0,0] == null) {
			foreach (Transform b in transform.Find("Armature")) {
				int val = int.Parse (b.name.Substring (1));
				emps [val / 5,val % 5] = b;
			}
		}
		if (netTime < timeToGoUp) {
			scale = netTime / timeToGoUp;
		} else if (netTime < timeToGoUp + timeToPause) {
			scale = 1;
		} else if (netTime < timeToGoUp + timeToPause + timeToGoDown) {
			// Note that timeToGoUp + timeToPause < netTime < timeToGoUp + timeToPause + timeToGoDown
			scale = (timeToGoUp + timeToPause + timeToGoDown - netTime) / timeToGoDown;
		} // else should be deleted. timeToGoUp + timeToPause + timeToGoDown = getLifeTime();
			
		transform.localScale = new Vector3(0.8f, 0.2f + 1.6f * scale, 0.8f);
	}
}
