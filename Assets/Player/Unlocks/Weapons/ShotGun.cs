using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShotGun : HitscanGun {

	public override int getUnlockPosition() {
		return 0;
	}

	// Use this for initialization
	public override void StartUnlock () {
		base.StartUnlock ();
		damageDealt = 11f;
		maxMissAngle = 2.4f;
	}

	public override void LoadUnlockObject() {
		if (parentPlayerMove.GetComponent<Combat> ().team == 0) {
			unlockObject = Resources.Load ("BlueShotGun") as GameObject;
		} else {
			unlockObject = Resources.Load ("RedShotGun") as GameObject;
		}
	}

	public override string getBulletType() {
		return "BulletShotGun";
	}

	public override void fire(PlayerInput pI, float missDistance, float missAngle, int isThisShotgun, int shotIndex, out Combat playerHitFromBase, out float playerDamageDoneFromBase)
	{
		// if (OperationNetwork.isServer) {
			// 7 shotgun shells in a consistent pattern: (like tf2)
			List<Combat> playersHit = new List<Combat> ();
			List<float> playersDamageDone = new List<float> ();

			Combat playerHit;
			float playerDamageDone;

			//if (OperationNetwork.isServer) {
				//recentlyGeneratedHitscanData = new Vector3[7][]; // Note that there are 7 shots!
			//}// else {
			//	pI.hitscanData = new Vector3[7][];
			//}

			// Hang on!
			if (OperationNetwork.isServer) {
				GameManager.rewindStatesToTick(pI.tickNumber);
			}


			base.fire (pI, 0, 0, 1, 0, out playerHit, out playerDamageDone);
			addToList (playersHit, playersDamageDone, playerHit, playerDamageDone);
			for (int i = 0; i < 6; i++) {
				base.fire (pI, maxMissAngle, i * Mathf.PI / 3, 1, i + 1, out playerHit, out playerDamageDone);
				addToList (playersHit, playersDamageDone, playerHit, playerDamageDone);
			}

			// Hang on!
			if (OperationNetwork.isServer) {
				GameManager.revertColliderStates();
			}

			for (int i = 0; i < playersHit.Count; i++) {
				// The following is copied from the "fire" method from HitscanGun:
				playersHit [i].TakeDamage (playersDamageDone [i], 0.5f, parentPlayerMove.mainCamera.transform.forward, false, Combat.SHOTGUN, true, parentPlayerMove.transform.position, parentPlayerMove.plyr);
			}

			playerHitFromBase = null;
			playerDamageDoneFromBase = 0;
		//} else {
			// It gets passed along:
			//setFirstBulletPositions(pI.hitscanData);
			//for (int i = 0; i < pI.hitscanData.Length; i++) {
			//	createBullet (pI.hitscanData [i], unlockObject, parentPlayerMove.GetComponent<PlayerAnimation> (), soundEffect);
			//}
			//recentlyGeneratedHitscanData = pI.hitscanData; // Just passes it along.

			//playerHitFromBase = null;
			//playerDamageDoneFromBase = 0;
		//}
	}


	public static void addToList(List<Combat> playersHit, List<float> playersDamageDone, Combat playerHit, float playerDamageDone)
	{
		if (playerHit == null)
			return;

		int index = playersHit.IndexOf(playerHit);
		if (index != -1)
		{
			playersDamageDone[index] += playerDamageDone;
		} else
		{
			playersHit.Add(playerHit);
			playersDamageDone.Add(playerDamageDone);
		}
	}

	public override string GetUnlockName() {
		return "Shotgun";
	}

	public override int getTotalShots()
	{
		return 4;
	}

	public override float getReloadTime()
	{
		return 1.7f;
	}

	public override float getFireTime()
	{
		return 0.8f;
	}
}
