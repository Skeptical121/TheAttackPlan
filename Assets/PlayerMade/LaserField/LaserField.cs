using UnityEngine;
using System.Collections;

public class LaserField : Trap {

	// Set In Inspector
	public GameObject[] laserFieldGuns;

	// Set In InitStart for Server, SYNCED
	float[] randomLaserDefinition = new float[0]; // Note how this is NOT initialized to null! The type needs to be here to sync properly!

	public override short getMaxHealth () {
		return 800;
	}

	public override float getLifeTime () {
		return 75f;
	}

	public override void InitStart (bool isThisMine)
	{
		base.InitStart (isThisMine);

		for (int i = 0; i < laserFieldGuns.Length; i++) {
			LaserShoot ls = laserFieldGuns[i].GetComponent<LaserShoot>();
			ls.playerOwner = playerOwner;
			ls.InitStart ();
		}

		if (OperationNetwork.isServer) {
			randomLaserDefinition = new float[laserFieldGuns.Length * 4];
			for (int i = 0; i < laserFieldGuns.Length; i++) {
				RotateLaser rL = laserFieldGuns[i].GetComponent<RotateLaser>();
				rL.InitStartServer();
				randomLaserDefinition [i * 4 + 0] = rL.rotX;
				randomLaserDefinition [i * 4 + 1] = rL.rotY;
				randomLaserDefinition [i * 4 + 2] = rL.changeX;
				randomLaserDefinition [i * 4 + 3] = rL.changeY;
			}
		}
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner) {
		return base.getBitChoicesLengthThis (isPlayerOwner) + 1;
	}

	public override object getObjectThis(int num, bool isPlayerOwner) {
		int childNum = num - base.getBitChoicesLengthThis (isPlayerOwner);
		switch (childNum)
		{
		case 0: return randomLaserDefinition;

		default: return base.getObjectThis(num, isPlayerOwner);
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		int previousTotal = base.SetInformation (data, a, b, isThisFirstTick, isThisMine);

		// Doesn't even bother setting randomLaserDefinition if it isn't null. (It doesn't change during its life time)
		if (randomLaserDefinition.Length == 0) {
			randomLaserDefinition = (float[])data [previousTotal];
			for (int i = 0; i < laserFieldGuns.Length; i++) {
				RotateLaser rL = laserFieldGuns[i].GetComponent<RotateLaser>();
				rL.InitStartClient (randomLaserDefinition, i);
			}
		}

		for (int i = 0; i < laserFieldGuns.Length; i++) {
			RotateLaser rL = laserFieldGuns[i].GetComponent<RotateLaser>();
			rL.UpdateLaser (getLifeTimeInterp());
		}

		return previousTotal + 1;
	}

	public override void ServerSyncFixedUpdate() {
		base.ServerSyncFixedUpdate ();
		for (int i = 0; i < laserFieldGuns.Length; i++) {
			RotateLaser rL = laserFieldGuns[i].GetComponent<RotateLaser>();
			rL.UpdateLaser (getLifeTimeServer());
		}
	}


	// ANIMATION:

}
