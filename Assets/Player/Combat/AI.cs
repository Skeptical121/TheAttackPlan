using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AI : MonoBehaviour {

	Dictionary<GameObject,float> peopleSeen = new Dictionary<GameObject,float>(); // If you see a person in the last second, the bot "knows" where you are, even if you hide behind a wall.
	UnityEngine.AI.NavMeshAgent nma = null;

	float goalRotation;
	float goalUpDownRotation;

	int numTicks = 0;

	// Called by SyncPlayer:
	public void onFixedUpdate() {

		if (numTicks % 10 == 0) {
			// Every 0.2 seconds, an evaluation is made. (Reaction time?)
			reactionTimeTick();
		}

		// Tracking:
		if (numTicks % 5 == 0) {

		}

		numTicks++;
	}

	void reactionTimeTick() {
		Transform iterator = GameObject.Find("Players").transform;

		for (int i = 0; i < iterator.childCount; i++)
		{
			GameObject plyr = iterator.GetChild(i).gameObject;
			// RaycastHit hitInfo;
			if (!Physics.Raycast (transform.position, (transform.rotation * Quaternion.Euler (0, -GetComponent<PlayerMove> ().mainCamera.transform.eulerAngles.x, 0)) * Vector3.forward, 50f, 1 << 0 | 1 << 11)) {
				// Set target to this player, assuming this player is the closest AND within sight. Sight = 120 degrees or something for bots
			}
		}
	}

	// Most decisions are not made here.
	public void runAI(PlayerInput pI) {
		if (nma == null)
			nma = GetComponent<UnityEngine.AI.NavMeshAgent> ();

		float dist = Vector3.Distance (nma.nextPosition, transform.position);
		if (dist < 0.0001f) {
			// Close enough.
		} else {
			// Don't change where your looking if there is something the bot is doing;
			pI.rotation = Quaternion.LookRotation(nma.nextPosition - transform.position).eulerAngles.y;
			int dX = 0;
			int dZ = 1;
			pI.dX = dX;
			pI.dZ = dZ;
			pI.fireKey = true;
		}
	}

	// This is after movement has occurred on the player:
	public void afterPlayerInputRun() {
		nma.nextPosition = transform.position;
	}
}
