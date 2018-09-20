using UnityEngine;
using System.Collections;

public class SyncHealingBlast : SyncGameState {

	// This doesn't even care about team. It is literally a render only thing.

	// TODO: Deprecate this and use a similar method to how hitscan bullets work. (Note, that there is absolutely no need to do this though)


	public override void InitStart(bool isThisMine) {
		if (OperationNetwork.isServer) {
			tickSpawnedAt = ServerState.tickNumber;
		}
		transform.parent = GameObject.Find("PlayerMade").transform;
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner) {
		return 2;
	}

	public override object getObjectThis(int num, bool isPlayerOwner) {
		switch (num)
		{
		case 0: return transform.position;
		case 1: return transform.rotation;

		default: return null;
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		transform.position = (Vector3)data [0];
		transform.rotation = (Quaternion)data [1];
		return 2;
	}


	public override void ServerSyncFixedUpdate() {
		if (getLifeTimeServer() > 5f) // Effect lasts less than 5 seconds.
			this.exists = false;
	}

	public override void OnDeath ()
	{
		
	}

}
