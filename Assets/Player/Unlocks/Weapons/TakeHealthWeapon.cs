using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

// Goes along with TakeHealth because TakeHealth needs to extend BlowUp.
public class TakeHealthWeapon : ProjectileGunScript {

	public override int getUnlockPosition() {
		return 0;
	}

	public override void LoadUnlockObject() {
		// Take this opportunity to load in pTHW stuff:

		// Also can load other things here:
		if (parentPlayerMove.GetComponent<Combat> ().team == 0) {
			unlockObject = Resources.Load ("BlueHealingGun") as GameObject;
			healingBlast = Resources.Load ("HealingBlastBlue") as GameObject;
			bulletPrefab = Resources.Load ("BlueMarksmanParticle") as GameObject;
		} else {
			unlockObject = Resources.Load ("RedHealingGun") as GameObject;
			healingBlast = Resources.Load ("HealingBlastRed") as GameObject;
			bulletPrefab = Resources.Load ("RedMarksmanParticle") as GameObject;
		}
	}
		
	public GameObject healingBlast;

	public static int healthTakenReq = 1000;
	public float healthTaken = 0; // healthTaken is 100% server driven, and should be sent to EVERY player, but at the very least the controlling player.
	float timeSinceLastHeal = 1000000; // Large # 

	public override void PlayerAndServerAlways(PlayerInput pI, bool equipped) {
		base.PlayerAndServerAlways (pI, equipped);


		if (pI.cancelKey) {
			
		}

		if (OperationNetwork.isServer) // HealthTaken is 100% server driven.
		{
			timeSinceLastHeal += pI.frameTime;
			if (timeSinceLastHeal < 0.4f) // 0.4 seconds for each heal.
			{
				healthTaken += 7.975f * pI.frameTime; // 8hp per second.
			} else
			{
				healthTaken += 10.5f * pI.frameTime; // 10.5hp per second.
			}

			// Heal 1 every 0.4 seconds.
			if (parentPlayerMove.GetComponent<Combat>().health < parentPlayerMove.GetComponent<Combat>().maxHealth && timeSinceLastHeal > 0.4f) // 0.4 seconds for each heal.
			{
				timeSinceLastHeal = 0f;
				parentPlayerMove.GetComponent<Combat>().TakeDamage(-1, Combat.OTHER); // (Net loses 1 health taken for this). Note that this is not considered healing.
			}

			if (pI.ultimateAbilityKey && healthTaken >= healthTakenReq)
			{

				healthTaken += (-healthTakenReq);
				// Buff all nearby allies by ~250, 200, and 150.
				Transform iterator = GameObject.Find("Players").transform;

				List<Transform> players = new List<Transform>();
				List<float> distances = new List<float>();

				// Players are effected by projectiles:
				for (int i = 0; i < iterator.childCount; i++)
				{
					Transform t = iterator.GetChild(i);

					// Has to be on same team:
					if (t.GetComponent<Combat>().team == parentPlayerMove.GetComponent<Combat>().team && t != parentPlayerMove.transform)
					{
						float distance = Vector3.Distance(t.position, parentPlayerMove.transform.position);

						bool added = false;
						for (int n = 0; n < distances.Count; n++)
						{
							if (distance < distances[n])
							{
								players.Insert(n, t);
								distances.Insert(n, distance);
								added = true;
								break;
							}
						}
						if (!added)
						{
							players.Add(t);
							distances.Add(distance);
						}

						t.GetComponent<Combat>().TakeDamage(-30, Combat.HEALING); // Everyone gets a 30 buff.
					}
				}

				if (distances.Count > 0)
				{
					float healthGiveOut = 400 / Mathf.Sqrt(Mathf.Min(distances.Count, 5));
					for (int i = 0; i < Mathf.Min(distances.Count, 5); i++)
					{
						players[i].GetComponent<Combat>().TakeDamage(-healthGiveOut / (1 + distances[i] / 40.0f), Combat.HEALING);
						healthGiveOut *= 0.75f;
					}
				}
				// Give yourself a buff of 100:
				parentPlayerMove.GetComponent<Combat>().TakeDamage(-100, Combat.HEALING);
				if (distances.Count == 0)
				{
					// Give yourself an additonal buff of 350:
					parentPlayerMove.GetComponent<Combat>().TakeDamage(-350, Combat.HEALING);
				}
			}
		}
	}

	public override string GetUnlockName() {
		return "Healing Gun";
	}

	public override void UpdateServerAndPlayer(PlayerInput pI, bool runEffects)
	{
		base.UpdateServerAndPlayer (pI, runEffects);
	}

	public override bool hasSecondaryFire() {
		return true;
	}

	// Fires projectile:
	public override void secondaryFire (PlayerInput pI)
	{
		Combat playerHit;
		float playerDamageDone;
		base.fire(pI, -1, -1, 0, 0, out playerHit, out playerDamageDone);
	}

	public override float getSecondaryFireTime() {
		return 15f;
	}

	public override float GetSpeed()
	{
		return 2f;
	}

	public override int getTotalShots()
	{
		return 3; // A really large number
	}

	public override float getReloadTime()
	{
		return 0.8f;
	}

	public override float getFireTime()
	{
		return 0.6f;
	}

	// This could be done in the same way bullets are done..

	// Server only:
	public void FireTakeHealth(Vector3 cameraForward)
	{
		// This comes from the gun, oddly enough:
		GameObject hB = (GameObject)MonoBehaviour.Instantiate(healingBlast, parentPlayerMove.mainCamera.transform.position + cameraForward * 2.8f, Quaternion.LookRotation(cameraForward)); // It gets made at the tip of the gun.

		hB.GetComponent<SyncGameState> ().playerOwner = parentPlayerMove.plyr; // Redudant.. not used..

		OperationNetwork.OperationAddSyncState (hB); // hB doesn't have any syncing properties beside position / rotation. (TODO implement)

		// Server also calculates the collision right here:

		// Note that HEALING #s are hard coded within BlowUp!
		ExplodeDetection.BlowUp (parentPlayerMove.mainCamera.transform.position + parentPlayerMove.mainCamera.transform.forward * 2.6f, // Note how it heals in a different location!
			parentPlayerMove.mainCamera.transform.position, 20f, 2.2f, 1f, 0.5f, parentPlayerMove.gameObject, 
			parentPlayerMove.GetComponent<Combat> ().team, ref healthTaken, parentPlayerMove.plyr);
	}
		
	public override void fire (PlayerInput pI, float missDistance, float missAngle, int isThisShotgun, int shotIndex, out Combat playerHit, out float playerDamageDone)
	{

		if (OperationNetwork.isServer) {
			// This is VERY similar to Projectile's fire:
			FireTakeHealth(parentPlayerMove.mainCamera.transform.forward);
		}

		// Discarded:
		playerHit = null;
		playerDamageDone = 0;

	}
}
