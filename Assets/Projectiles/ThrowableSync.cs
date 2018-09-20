using UnityEngine;
using System.Collections;

public class ThrowableSync : PlayerMade {

	// Throwable is (0.6, 0.6, 0.9) (dynamic, static, bounciness) for OLD bouncy settings.

	public const float OWN_TEAM_MULTIPLIER = 1.5f;


	bool hitGround = false;

	public override short getMaxHealth() {
		return 700;
	}

	public override float getLifeTime() {
		return 10.0f;
	}
		
	// Throwable does NOT blow up! It creates a death sphere!
	public override void OnDeath ()
	{
		if (deathExplosionPrefab != null && OperationNetwork.isServer) {
			GameObject deathExplosion = (GameObject)Instantiate (deathExplosionPrefab, transform.position, transform.rotation);
			deathExplosion.GetComponent<SphereDamage> ().playerOwner = playerOwner;
			deathExplosion.GetComponent<SphereDamage> ().team = team;
			OperationNetwork.OperationAddSyncState (deathExplosion);
		}
	}

	// For Physics projectiles, this is a thing. Icicle / arrow blow up on collision..
	void OnCollisionEnter(Collision collision)
	{

		if (!OperationNetwork.isServer)
			return;

		int ignoreTeam = 21 - gameObject.layer;

		if (!hitGround)
		{
			GameObject hit = collision.gameObject;
			if (hit.layer == 16 + ignoreTeam)
			{
				alive = false;
				diedAt = ServerState.tickNumber;
			}
			else if (hit.layer == 17 - ignoreTeam)
			{
				// Doesn't explode on ally collision
			}
			else if (hit.CompareTag("Mirror"))
			{
				// Currently throwable doesn't bounce correctly off mirrors.
			}
			else if (hit.CompareTag("Ignore"))
			{
				// Doesn't explode. What is "Ignore" tag layer used for?
			}
			else if (hit.CompareTag("Shield") || hit.CompareTag("Icicle"))
			{
				if ((hit.transform.gameObject.layer - 10) / 5 != ignoreTeam && hit.gameObject.layer - 20 != ignoreTeam)
				{
					alive = false;
					diedAt = ServerState.tickNumber;
				}
			}
			else if (hit.CompareTag("ShieldParent"))
			{
				if ((hit.transform.gameObject.layer - 10) / 5 != ignoreTeam && hit.gameObject.layer - 20 != ignoreTeam)
				{
					alive = false;
					diedAt = ServerState.tickNumber; // This should force the "death" of this, and trigger the death (Explosion) next HealthVar Update.
				}
			}
			// Once it hits something, it can't explode
			else
			{
				hitGround = true;
			}
		}
	}

}
