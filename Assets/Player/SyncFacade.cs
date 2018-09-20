using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SyncFacade : SyncGameState {

	// Note that playerOwner does not have to be synced, because the actual player handles all this interaction..


	public override void InitStart (bool isThisMine)
	{
		GetComponent<Animator> ().SetLayerWeight (4 + 0, 1); // Sets to scientist
	}

	public override void ServerSyncFixedUpdate()
	{
		// Here we could run something akin to player move; but right now the facade player doesn't move

		// It should probably be shown that the facade has been frozen in place.
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner)
	{
		return 2;
	}

	public override object getObjectThis(int num, bool isPlayerOwner)
	{
		switch (num)
		{
		case 0: return transform.position;
		case 1: return transform.eulerAngles.y;
		default: return null;
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine)
	{
		transform.position = (Vector3)data[0];
		transform.eulerAngles = new Vector3 (transform.eulerAngles.x, (float)data [1], transform.eulerAngles.z);
		return 2;
	}

	public override void OnDeath ()
	{
		// The facade is never controlled, so the only important thing is to spawn the ragdoll:

		// No ragdoll for now

		// GetComponent<Combat>().die(Vector3.zero);
	}
}