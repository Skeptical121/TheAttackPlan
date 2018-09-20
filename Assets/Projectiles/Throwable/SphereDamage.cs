using UnityEngine;
using System.Collections;

public class SphereDamage : SyncGameState {

	public int team = 0;

	float sizeMult = 7f;
	float halfTime = 2f;
	float maxLifeTime = 5f; // Total


	public override void InitStart(bool isThisMine) {

		// todoState - Set Parent (here and PlayerMade, and every syncgamestate)
		transform.parent = GameObject.Find("PlayerMade").transform;

		if (OperationNetwork.isServer) {
			tickSpawnedAt = ServerState.tickNumber;
		}
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner) {
		return 3;
	}

	public override object getObjectThis(int num, bool isPlayerOwner) {
		switch (num)
		{
		case 0: return tickSpawnedAt;
		case 1: return transform.position;
		case 2: return playerOwner;

		default: return null;
		}
	}

	void setSize(float lifeTime) {
		float scl;
		if (lifeTime >= maxLifeTime) {
			if (OperationNetwork.isServer) {
				exists = false;
			}
			return;
		} else if (lifeTime < halfTime) {
			scl = Mathf.Log (1 + lifeTime * sizeMult, 1.36f); // Increase number to decrease size
		} else {
			scl = Mathf.Log (1 + (halfTime - (lifeTime - halfTime) * halfTime / (maxLifeTime - halfTime)) * sizeMult, 1.36f); // Increase number to decrease size
		}
		transform.localScale = new Vector3(scl / 5f, scl / 5f, scl / 5f);
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		tickSpawnedAt = (int)data [0];
		transform.position = (Vector3)data [1];
		playerOwner = (short)data [2];
		setSize (getLifeTimeInterp ());
		return 3;
	}

	public override void ServerSyncFixedUpdate() {
		setSize (getLifeTimeServer ());

		// This thing will murder traps in their sleep. 50 * 10 = 500 DPS, minimum of 200 DPS. 
		if (ServerState.tickNumber % SawScript.numTicksPerHit == 0) {
			float NOT_HEALTH_TAKEN = -1;
			ExplodeDetection.BlowUp (transform.position, transform.position, 10f * SawScript.numTicksPerHit, transform.localScale.x * 2.5f, 0.40f, 0.70f, null, team, ref NOT_HEALTH_TAKEN, playerOwner);
		}



	}

	public override void OnDeath ()
	{

	}
}
