using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public abstract class HitscanGun : GunScript {

	public GameObject soundEffect;

	public float damageDealt = 0f; // Damage per bullet

	public float maxMissAngle;

	Vector3 miss(Quaternion forwardRot, Vector3 forward, float missDistance, float missAngle)
	{
		if (missDistance < 0)
		{
			UnityEngine.Random.State oldState = UnityEngine.Random.state;
	
			// Becomes deterministic for a quick little bit:
			UnityEngine.Random.InitState((int)((long)(parentPlayerMove.GetComponent<SyncPlayer>().playerTime * 10000.0) % 2000000000));
			Vector3 direction = -missDistance * UnityEngine.Random.insideUnitCircle * maxMissAngle * Mathf.PI / 180.0f * UnityEngine.Random.Range(0.5f, 1f); // This is here because the accuracy, is on average less when using a cone to do inaccuracy. This increases the amount of shots that are more accurate. In the future it could just use tan, or maybe tan with this.
			
			UnityEngine.Random.state = oldState; // As to maintain randomness otherwise.. not really necessary
			direction.z = 1.0f;
			return Quaternion.LookRotation(forward) * Quaternion.LookRotation(direction.normalized) * Vector3.forward;
		} else
		{
			Vector3 direction = new Vector3(Mathf.Cos(missAngle), Mathf.Sin(missAngle), 0) * Mathf.Tan(missDistance * Mathf.PI / 180.0f); // If miss distance is under 90 degrees, this will be under 1.
			direction.z = 1.0f;
			return Quaternion.LookRotation(forward) * Quaternion.LookRotation(direction.normalized) * Vector3.forward;

		}
	}

	// Player only
	// -1 for missDistance is used to indicate a random missDistance & random missAngle.
	public override void fire(PlayerInput pI, float missDistance, float missAngle, int isThisShotgun, int shotIndex, out Combat playerHit, out float playerDamageDone)
	{
		float dmg = DamageCircle.touchingDamageCircle(parentPlayerMove.timeSinceTouchingDamageCircle, damageDealt, 0.25f);

		missDistance /= (1 + 1f * DamageCircle.isTouchingDamageCircle(parentPlayerMove.timeSinceTouchingDamageCircle)); // Doubles accuracy for damage circle.

		Vector3 fireFromPos;

		if (parentPlayerMove.thisIsMine && !parentPlayerMove.GetComponent<ClassControl> ().isBot) {
			// Adjust so it comes out of the viewmodel gun:
			fireFromPos = parentPlayerMove.mainCamera.transform.position +
				(parentPlayerMove.mainCamera.transform.rotation * Quaternion.Inverse (parentPlayerMove.playerView.viewmodelCamera.transform.rotation)) *
			(viewmodelUnlockObject.transform.position
					- parentPlayerMove.playerView.viewmodelCamera.transform.position);
		} else {
			fireFromPos = unlockObject.transform.position;
		}
		// Modify firePos here if guns are off with method override.


		// Hang on!
		if (OperationNetwork.isServer && isThisShotgun == 0) {
			GameManager.rewindStatesToTick(pI.tickNumber);
		}

		Vector3[] posToSend = HitscanGun.hitscanShoot(parentPlayerMove.mainCamera.transform.position, fireFromPos, 
		miss(parentPlayerMove.mainCamera.transform.rotation, parentPlayerMove.mainCamera.transform.forward, missDistance, missAngle), unlockObject, 
		parentPlayerMove.GetComponent<PlayerAnimation>(), soundEffect, getBulletType(), parentPlayerMove.GetComponent<Combat>().team, dmg, 0.5f, 5 - (int)(3 * DamageCircle.isTouchingDamageCircle(parentPlayerMove.timeSinceTouchingDamageCircle)), 200,
			isThisShotgun, this, out playerHit, out playerDamageDone, parentPlayerMove.plyr);

		// Special case, PISTOL!
		if (this is Pistol)
			((Pistol)this).attemptConductiveExplode (posToSend [posToSend.Length - 1]);

		// Hang on!
		if (OperationNetwork.isServer && isThisShotgun == 0) {
			GameManager.revertColliderStates();
		}
		

		// The hitscan bullets need to be sent to the clients
		if (OperationNetwork.isServer) {
			Vector3[] sendPos = new Vector3[posToSend.Length - 1];
			Array.Copy (posToSend, 1, sendPos, 0, sendPos.Length);
			parentPlayerMove.GetComponent<SyncPlayer> ().currentTriggerSet.trigger (SyncPlayer.HITSCAN, sendPos);
		}
	}

	// Non Player:
	public void setFirstBulletPosition(Vector3[] hitscanData) {
		hitscanData[0] = unlockObject.transform.position;
	}

	public virtual string getBulletType() {
		return "BulletDefault";
	}

	public void hitscanData(Vector3[] hitscanData) {
		Vector3[] allHitscanData = new Vector3[hitscanData.Length + 1];
		Array.Copy (hitscanData, 0, allHitscanData, 1, hitscanData.Length);
		setFirstBulletPosition(allHitscanData);
		createBullet (allHitscanData, unlockObject, parentPlayerMove.GetComponent<PlayerAnimation> (), soundEffect, getBulletType());
	}


	// STATIC IMPLEMENTATIONS:

	// In no particular order.
	public static Vector3[] hitscanShoot(Vector3 pos, Vector3 fireFromPos, Vector3 forward, GameObject fireFrom, PlayerAnimation playerAnim, GameObject soundEffect, string bulletType, int team, float damage, float knockBackMult, int damageFallOff, float maxDistance, int shotgun, HitscanGun hsGun, out Combat playerHit, out float damageDone, short playerSender)
	{
		bool isPlayerHitscan = pos != fireFromPos; // Hitscan ALWAYS fires from a different location than the face. The sentry fires from the same place, and thus is a "serverObject" technically.

		playerHit = null;
		damageDone = 0;

		RaycastHit hit;
		float dist = maxDistance;

		int layersToHit = LayerLogic.HitscanShootLayer(team); // Can't hit own team.

		List<Vector3> positions = new List<Vector3>();
		positions.Add(fireFromPos); // The effect comes from the gun
		Vector3 lastPos = pos;

		int numHitMirror = 0;

		bool hitscanHitSomething = false;
		while (Physics.Raycast(pos, forward, out hit, dist, layersToHit)) // Not ignore mirror, players, or placement, or ragdolls, not decor, of course
		{
			// Allied Building can be hit. Most "ignore this"




			if ((hit.transform.CompareTag("Shield") || hit.transform.CompareTag("ShieldParent")) && (hit.transform.gameObject.layer - 10) / 5 == team)
			{
				// ignore the shield:
				pos = hit.point + forward * 0.01f; // This "distance" reduces performance on these hitscan bullets significantly..
				dist -= 0.01f; // As to prevent an infinite loop. (Could be an issue)
				continue;
			}
			// add it.
			lastPos = hit.point;
			dist -= hit.distance; // Note how this could be used to calculate damage falloff.
			pos = hit.point;
			positions.Add(pos);
			if (!hit.transform.CompareTag("Mirror"))
			{
				if (fireFrom != null || OperationNetwork.isServer) {
					// This is a COPY-PASTE (Similar) to Projectile.directHit()
					if (hit.transform.gameObject.layer == 17 - team) {
						// Finds the actual player object
						Transform plyr = hit.transform;
						do {
							plyr = plyr.parent;
						} while (plyr.GetComponent<Combat> () == null);
						float dmg = getDamage (damage, dist / maxDistance, damageFallOff);

						// Headshots no longer do any extra damage.

						// Instead of doing damage, add up the damage, then do the damage.. in the case that this is a shotgun scenario:
						if (shotgun == 0) {
							plyr.GetComponent<Combat> ().TakeDamage (dmg, knockBackMult, forward, false, Combat.SINGLE_BULLET, playerAnim != null, fireFromPos, playerSender);
						} else if (shotgun == 1) {
							playerHit = plyr.GetComponent<Combat> ();
							damageDone = dmg;
						}
					} else if (hit.transform.gameObject.layer == 10 || hit.transform.gameObject.layer == 15 || hit.transform.gameObject.layer == 20 || hit.transform.gameObject.layer == 21) {
						if (PlayerMade.IsFriendlyIcicle (hit.transform, team)) { // General Exception
							PlayerMade.TakeDamageObjectG (hit.transform, getDamage (damage, dist / maxDistance, damageFallOff) * 1000, fireFromPos, isPlayerHitscan, playerSender);
						} else if (PlayerMade.IsFriendlyThrowable (hit.transform, team)) { // Specific Exception
							PlayerMade.TakeDamageObjectG (hit.transform, getDamage (damage, dist / maxDistance, damageFallOff) * ThrowableSync.OWN_TEAM_MULTIPLIER, fireFromPos, isPlayerHitscan, playerSender);
						} else if (PlayerMade.IsEnemy (hit.transform, team) || PlayerMade.IsFriendlyThrowable (hit.transform, team)) { // It must hit a shield / shieldparent.. note that shieldparent's usefulness is deprecated / icicle, AND hit an enemy.
							PlayerMade.TakeDamageObjectG (hit.transform, getDamage (damage, dist / maxDistance, damageFallOff), fireFromPos, isPlayerHitscan, playerSender);
							// Damage to buildings do not have damage numbers associated with them.. any damage saved would just be added up.
						}
					}
				}

				// The positions are instantly used for Player: (Or server)
				if (fireFrom != null) {
					createBullet (positions.ToArray (), fireFrom, playerAnim, soundEffect, bulletType);
				}
				hitscanHitSomething = true;
				break;
			}

			// Hit mirror:
			forward = Vector3.Reflect(forward, hit.normal);
			if (team == hit.transform.GetComponent<MirrorLogic>().team)
			{
				damage *= 2; // Twice the damage.
			}
			numHitMirror++;
			if (numHitMirror >= 16) { // Maximum 16 hits on mirror. Probably going to have max 1 mirror anyways
				break;
			}
		}
		if (!hitscanHitSomething) {
			positions.Add (pos + forward * dist);
			if (fireFrom != null) {
				createBullet (positions.ToArray (), fireFrom, playerAnim, soundEffect, bulletType);
			}
		}
		return positions.ToArray();
	}

	// Where distance = (maxDist - dist) / maxDist. Note this means that damage fall off actually is more the further you get.
	public static float getDamage(float damage, float dist, int damageFallOff)
	{
		// Standard damageFallOff is among 3.
		return damage * Mathf.Pow(dist, damageFallOff); // 65% of the damage when 20 units away. 20 units is pretty far.
	}

	// In no particular order.

	// This is for creating a "default" bullet. Customization methods can be added later.
	public static void createBullet(Vector3[] hitPos, GameObject fireFrom, PlayerAnimation playerAnim, GameObject soundEffect, string bulletName)
	{
		
		GameObject fakeRenderBulletPrefab = Resources.Load (bulletName) as GameObject; // This could be inefficient.

		GameObject fakeRenderBullet = (GameObject)MonoBehaviour.Instantiate(fakeRenderBulletPrefab, hitPos[0], Quaternion.LookRotation(hitPos[1] - hitPos[0]));
		fakeRenderBullet.GetComponent<BulletPos>().shotPositions = hitPos;
		fakeRenderBullet.GetComponent<BulletPos>().init ();

		
		GameObject fireEffect = Resources.Load ("FireAutoMuzzle") as GameObject; // This could be inefficient.

		// Create beginning:
		GameObject hitBeg = (GameObject)MonoBehaviour.Instantiate(fireEffect, hitPos[0], Quaternion.LookRotation(hitPos[1] - hitPos[0]));
		hitBeg.transform.parent = fireFrom.transform;
		MonoBehaviour.Destroy(hitBeg, 0.3f);

		GameObject hitEffect = Resources.Load ("OnHitAuto") as GameObject; // This could be inefficient.

		// Create final:
		GameObject hitEff = (GameObject)MonoBehaviour.Instantiate(hitEffect, hitPos[hitPos.Length - 1], Quaternion.LookRotation(hitPos[hitPos.Length - 2] - hitPos[hitPos.Length - 1]));
		MonoBehaviour.Destroy(hitEff, 0.3f);
	}
}
