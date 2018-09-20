using UnityEngine;
using System.Collections;
using System;

public class MeeleeWeapon : GunScript {


	public override int getUnlockPosition() {
		return 0;
	}

	bool targetHit = false; // This is so meelee can only hit one enemy.

	public override void StartUnlock ()
	{
		base.StartUnlock ();
	}

	public override void LoadUnlockObject() {
		if (parentPlayerMove.GetComponent<Combat> ().team == 0) {
			unlockObject = Resources.Load ("BlueMeeleeWeapon") as GameObject;
		} else {
			unlockObject = Resources.Load ("RedMeeleeWeapon") as GameObject;
		}
	}

	public override void fire(PlayerInput pI, float missDistance, float missAngle, int isThisShotgun, int shotIndex, out Combat playerHit, out float playerDamageDone) {

		targetHit = false;

		// Meelee check is done during the swing.

		playerHit = null; // Discarded
		playerDamageDone = 0; // Discarded
	}

	public override void UpdateServerAndPlayer (PlayerInput pI, bool runEffects)
	{
		base.UpdateServerAndPlayer (pI, runEffects);
		if (runEffects) {
			float timeSinceFire = parentPlayerMove.GetComponent<SyncPlayer> ().getTotalTimeSince (firedAt.getTriggerTime (parentPlayerMove));
			if (timeSinceFire >= getDamageTime (false) && timeSinceFire < getDamageTime (true)) {
				swingDamage (pI); // This is also done player side to predict blood effects
			}
		}
	}

	public override void PlayerAndServerAlways(PlayerInput pI, bool equipped) {
		base.PlayerAndServerAlways (pI, equipped);

		if (!equipped) {
			// The swinging animation is stopped because of the switch of the weapons.
			firedAt.reset();

			// Now, weapons shouldn't be able to be switched right after being fired - (for reload it'll just stop the reload)
		}
	}

	public override string getHudType () {
		return "Other";
	}

	void swingDamage(PlayerInput pI)
	{
		if (targetHit)
			return;

		// Hang on!
		if (OperationNetwork.isServer) {
			GameManager.rewindStatesToTick(pI.tickNumber);
		}

		// To predict stuff like blood effects:
		float damage = getDamage();
		damage = DamageCircle.touchingDamageCircle(parentPlayerMove.timeSinceTouchingDamageCircle, damage, 0.5f);
		// Currently, meelee does not collide with walls & stuff. (Nor allied players)
		Collider[] hitObjects = Physics.OverlapBox(parentPlayerMove.mainCamera.transform.position + parentPlayerMove.mainCamera.transform.forward * 1.3f, new Vector3(0.6f, 0.5f, 1.9f), parentPlayerMove.mainCamera.transform.rotation, 1 << (22 - 14 * parentPlayerMove.GetComponent<Combat>().team) | 1 << (15 - 5 * parentPlayerMove.GetComponent<Combat>().team));

		// For now, hit everything, possibly an interesting mechanic.

		// Find the closest one, and hit that one. It also needs to be closest to the center?
		float bestDistance = 1.0f; // Max Range. (Kind of redundant, but it's used to round off the edges)
		Collider bestHit = null;
		foreach (Collider hit in hitObjects)
		{
			float dist = Vector3.Distance(parentPlayerMove.mainCamera.transform.position, hit.ClosestPointOnBounds(parentPlayerMove.mainCamera.transform.position));

			if (dist >= bestDistance)
				continue;

			if (hit.gameObject.layer == 22 - 14 * parentPlayerMove.GetComponent<Combat>().team)
			{
				if (hit is UnityEngine.CapsuleCollider && hit.gameObject.GetComponent<Combat>().team != parentPlayerMove.GetComponent<Combat>().team)
				{
					bestDistance = dist;
					bestHit = hit;
				}
			}
			else if (PlayerMade.IsEnemy(hit.transform, parentPlayerMove.GetComponent<Combat>().team))
			{
				bestDistance = dist;
				bestHit = hit;
			}
		}

		// It's a copy and paste of the code above as far as if statements go:
		if (bestHit != null)
		{
			targetHit = true;
			if (bestHit.gameObject.layer == 22 - 14 * parentPlayerMove.GetComponent<Combat>().team)
			{
				if (bestHit is UnityEngine.CapsuleCollider && bestHit.gameObject.GetComponent<Combat>().team != parentPlayerMove.GetComponent<Combat>().team)
				{
					bestHit.gameObject.GetComponent<Combat>().TakeDamage(damage, 0.75f, parentPlayerMove.mainCamera.transform.forward, false, Combat.MEELEE, true, parentPlayerMove.transform.position, parentPlayerMove.plyr); // isHitscan because it's determined playerSide. (dmg shouldn't be done if dead)
				}
			}
			else if (PlayerMade.IsEnemy(bestHit.transform, parentPlayerMove.GetComponent<Combat>().team))
			{
				PlayerMade.TakeDamageObjectG(bestHit.transform, damage, parentPlayerMove.transform.position, true, parentPlayerMove.plyr);
			}
		}


		// Hang on!
		if (OperationNetwork.isServer) {
			GameManager.revertColliderStates();
		}
	}

	// Should be less than getSwingTime()
	float getDamageTime(bool endTime)
	{
		// Was 0.35f. (General)
		if (!endTime) // endTime (start)
			return 0.3f;
		else // endTime (end)
			return 0.4f;
	}

	public float getDamage()
	{
		return 50f;
	}

	public override bool isReloadableType() {
		return false;
	}
		
	public override string getAnimationType()
	{
		return "Meelee";
	}
		
	public override int getTotalShots()
	{
		return -1; // N/A
	}

	public override float getReloadTime()
	{
		return 0f; // N/A
	}

	public override float getFireTime()
	{
		return 0.7f;
	}
}
