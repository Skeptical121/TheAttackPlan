using UnityEngine;
using System.Collections;
using System;

public abstract class Projectile : SyncGameState {

	// These values are overrided by whatever takes the place of Projectile.
	public float expDmg;
	public float maxDist;
	public float initDmgPercent;
	public float leastDmgPercent; // leastDmgPercent <= initDmgPercent
	public float knockBackMult;

	public int ignoreTeam; // Determined by TYPE. Should be set on prefab!

	public GameObject deathExplosion;

	public override void InitStart (bool isThisMine)
	{
		if (OperationNetwork.isServer) {
			tickSpawnedAt = ServerState.tickNumber;
		}
	}

	public virtual float getExpDamage()
	{
		return expDmg;
	}

	public abstract void SetInitialVelocity (Vector3 initVel);

	public override void ServerSyncFixedUpdate()
	{
		// Projectiles last 12 seconds at max..
		if (getLifeTimeServer() > getMaximumLifeTime()) {
			exists = false;
		}
	}

	// Just in case. This is used for football primarily, which overrides this time.
	public virtual float getMaximumLifeTime() {
		return 20.0f;
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner)
	{
		return 1;
	}

	public override object getObjectThis(int num, bool isPlayerOwner)
	{
		switch (num)
		{
			case 0: return playerOwner;

			default: return null;
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine)
	{
		playerOwner = (short)data[0];
		return 1;
	}

	public override void OnDeath ()
	{
		if (deathExplosion != null) {
			GameObject deathExp = (GameObject)Instantiate (deathExplosion, transform.position, Quaternion.identity); // Explosions should face in the direction they hit.. just a thought. (TODO)
			Destroy (deathExp, 10.0f);
		}
	}

	public virtual void BlowUp(Transform c) {
		GameObject exception = directHit (c);

		float healthTakenThrowaway = -1;

		ExplodeDetection.BlowUp (transform.position, transform.position, getExpDamage () * initDmgPercent, maxDist, leastDmgPercent / initDmgPercent, 
			knockBackMult, exception, ignoreTeam, ref healthTakenThrowaway, playerOwner);

		exists = false;
	}


	// This is a COPY-PASTE (Similar) to HitscanGun.hitscanShoot()
	public GameObject directHit(Transform c) {
		if (c.gameObject.layer == 17 - ignoreTeam)
		{
			// Finds the actual player object
			Transform plyr = c;
			while (plyr.GetComponent<Combat>() == null)
			{
				plyr = plyr.parent;
			} 
			float dmg = getExpDamage ();

			// No headshots..

			plyr.GetComponent<Combat>().TakeDamage(dmg, knockBackMult, Vector3.Normalize(plyr.position - transform.position), false, Combat.BLOW_UP, false, transform.position, playerOwner);
			return plyr.gameObject;
		}
		else if (c.gameObject.layer == 10 || c.gameObject.layer == 15 || c.gameObject.layer == 20 || c.gameObject.layer == 21)
		{
			if (PlayerMade.IsFriendlyIcicle(c, ignoreTeam))
			{ // General Exception
				// c.gameObject.GetComponent<LaunchIcicle>().TakeDamageIcicle(getExpDamage()); // Still launches right now.. hmm..
				return PlayerMade.TakeDamageObjectG(c, getExpDamage() * 100, transform.position, false, playerOwner).gameObject;
			}
			else if (PlayerMade.IsFriendlyThrowable(c, ignoreTeam))
			{ // Specific Exception
				return PlayerMade.TakeDamageObjectG(c, getExpDamage() * ThrowableSync.OWN_TEAM_MULTIPLIER, transform.position, false, playerOwner).gameObject;
			}
			else if (PlayerMade.IsEnemy(c, ignoreTeam) || PlayerMade.IsFriendlyThrowable(c, ignoreTeam))
			{ // It must hit a shield / shieldparent.. note that shieldparent's usefulness is deprecated / icicle, AND hit an enemy.
				return PlayerMade.TakeDamageObjectG(c, getExpDamage(), transform.position, false, playerOwner).gameObject;
				// Damage to buildings do not have damage numbers associated with them.. any damage saved would just be added up.
			}
		}
		// Else hit like a wall or something
		return null;
	}
}