using UnityEngine;
using System.Collections;
using System;

public class PlacementSphere : SyncGameState {

	public GameObject cutOut; // To be set on server by PlacePlayerMade. This is the "placement" with the CollideCheck.

	public Vector3 fromPosition;
	public Vector3 toPosition;

	public Quaternion objRotation;
	public byte placeType;

	public byte team;

	public GameObject deathVisualExplosion;

	float getSpeed(float distance) {
		return 15.0f * Mathf.Sqrt(distance + 15);
	}

	// Server Only
	void SpawnCutOut()
	{
		GameObject obj = (GameObject)Instantiate(cutOut, toPosition, objRotation);//null;
		obj.GetComponent<CollideCheck>().team = team;
		obj.GetComponent<CollideCheck>().playerOwner = playerOwner;
		// obj.GetComponent<CollideCheck>().playerOperationView = playerOwnerOperationView;
		obj.GetComponent<CollideCheck>().placeType = placeType;
		obj.GetComponent<CollideCheck> ().ServerPlacementValidCheck ();
	}

	public override void InitStart(bool isThisMine) {

		// todoState - Set Parent (here and PlayerMade, and every syncgamestate)

		if (OperationNetwork.isServer) {
			tickSpawnedAt = ServerState.tickNumber;
			fromPosition = transform.position;

			// toPosition set by PlacePlayerMade.
		}

		transform.parent = GameObject.Find("Miscellaneous").transform;
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner) {
		return 4;
	}

	public override object getObjectThis(int num, bool isPlayerOwner) {
		switch (num)
		{
		case 0: return tickSpawnedAt;
		case 1: return fromPosition;
		case 2: return toPosition;
		case 3: return team;

		default: return null;
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		tickSpawnedAt = (int)data [0];
		fromPosition = (Vector3)data [1];
		toPosition = (Vector3)data [2];
		team = (byte)data [3];

		float distance = Vector3.Distance (fromPosition, toPosition);
		float speed = getSpeed(distance);

		transform.position = Vector3.Lerp (fromPosition, toPosition, getLifeTimeInterp () * speed / distance);
		return 4;
	}

	public override void ServerSyncFixedUpdate() {
		float distance = Vector3.Distance (fromPosition, toPosition);
		float speed = getSpeed(distance);

		// Perhaps the sphere should start slower & end slower..?

		if (getLifeTimeServer() * speed >= distance)
		{
			// Spawn the CutOut:
			SpawnCutOut();
			// Destroy this:
			exists = false;
		} else
		{
			transform.position = Vector3.Lerp (fromPosition, toPosition, getLifeTimeServer () * speed / distance);
		}
	}

	public override void OnDeath ()
	{
		GameObject deathParticles = (GameObject)Instantiate(deathVisualExplosion, toPosition, Quaternion.identity);
		Destroy(deathParticles, 1);
	}
}
