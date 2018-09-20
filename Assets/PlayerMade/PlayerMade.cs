using UnityEngine;
using System.Collections;

public abstract class PlayerMade : SyncGameState {

	// Set In Inspector
	public byte team = 1;

	// Team, therefore is NOT synced.

	// Set In Script with use of GetLifeTime() and diedAt
	public bool alive = true;

	// SYNCED
	float health = 1; // Health is set to maxHealth in InitStart
	public int diedAt = -1; // -1 if not dead. Indicates the "lifeTime" it diedAt.


	bool building = true;

	public GameObject deathExplosionPrefab = null;


	public Transform healthBarParent = null; // The "green" part (the health part)
	public Transform healthBar = null;
		
	public override void InitStart(bool isThisMine) {
		health = getMaxHealth(); // Hmm.

		if (OperationNetwork.isServer) {
			tickSpawnedAt = ServerState.tickNumber;
		}

		transform.parent = GameObject.Find("PlayerMade").transform;

		// Find healthBar:
		setHealthBar();

		initBuildAnimation ();
	}

	public virtual void setHealthBar() {
		// Now does this:
		PlayerHud.addHealthBar(transform.Find("HealthBar"), this, team);
	}

	public virtual void UpdateServerAndInterp(bool building, bool dieing, float lifeTime) {
		
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner) {
		return 6;
	}

	public override object getObjectThis(int num, bool isPlayerOwner) {
		switch (num)
		{
		case 0: return tickSpawnedAt;
		case 1: return transform.position;
		case 2: return transform.rotation;
		case 3: return (short)Mathf.CeilToInt(health);
		case 4: return diedAt;
		case 5: return playerOwner;

		default: return null;
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		tickSpawnedAt = (int)data [0];
		transform.position = (Vector3)data [1];
		transform.rotation = (Quaternion)data [2];
		health = (short)data [3]; // Health is not interp'd. Any short / int is not.

		diedAt = (int)data[4];
		alive = getLifeTimeInterp() < diedAt || diedAt == -1;

		playerOwner = (short)data [5];

		if (building) {
			if (getLifeTimeInterp () < getBuildTime () && alive) {
				buildAnimation (getLifeTimeInterp());
			} else {
				building = false;
				endBuildAnimation ();
			}
		}
		if (!alive) {
			deathAnimation (Interp.getLifeTime(diedAt));
		}

		UpdateServerAndInterp (getLifeTimeInterp () < getBuildTime (), !alive, getLifeTimeInterp());

		return 6;
	}
		
	public abstract short getMaxHealth ();
	public abstract float getLifeTime ();

	public virtual float getDeathTime() {
		return 0f;
	}
	public virtual float getBuildTime () {
		return 0f;
	}

	public virtual void initBuildAnimation() {
	}
	public virtual void buildAnimation (float lifeTime) {
	}
	public virtual void endBuildAnimation() {
	}
	public virtual void deathAnimation(float deathTime) {
	}

	void Update() {
		if (healthBar != null)
		{
			healthBar.localScale = new Vector3(1.01f * health / getMaxHealth(), 1.01f, 1.01f);
			healthBar.localPosition = new Vector3(-0.505f + 0.505f * health / getMaxHealth(), 0, -0.01f);
			if (Player.thisPlayer != null && Player.thisPlayer.playerCamera != null)
			{
				healthBar.parent.rotation = Quaternion.LookRotation(healthBar.parent.position - Player.thisPlayer.playerCamera.transform.position);
			}
		}
	}

	public virtual void disablePM() {
		exists = false;
	}

	public override void ServerSyncFixedUpdate() {
		if (alive && getLifeTimeServer () > getLifeTime()) {
			alive = false;
			diedAt = ServerState.tickNumber;
		}
		if (!alive && ServerState.getLifeTime(diedAt) > getDeathTime()) {
			disablePM ();
			// This gets removed instantly-- however, it is important that there are checks for exists = false
			return;
		}
			
		if (building) {
			if (getLifeTimeServer() < getBuildTime () && alive) {
				buildAnimation (getLifeTimeServer());
			} else {
				building = false;
				endBuildAnimation ();
			}
		}
		if (!alive) {
			deathAnimation (ServerState.getLifeTime(diedAt));
		}
		UpdateServerAndInterp (getLifeTimeServer () < getBuildTime (), !alive, getLifeTimeServer());
	}

	public virtual void Death() {

	}

	public override void OnDeath ()
	{
		if (deathExplosionPrefab != null) {
			GameObject deathExplosion = (GameObject)Instantiate (deathExplosionPrefab, transform.position, transform.rotation);
			// destroy..
		}
	}

	public float getHealth() {
		return health;
	}


	public virtual void TakeDamageObject(float amount, Vector3 fromWhere, bool isHitscan, short playerSender)
	{
		if (!OperationNetwork.isServer) {
			// this method is still called when not server because that's how the hitscan method works currently- this could be used for predicted blood / damage effects!! (And should be!!!) 
			return;
		}

		// Should this be before the !OperationNetwork.isServer check?
		if (!exists || !alive)
			return;

		if (building)
		{
			// Severly reduce damage taken if sentry when it is contained within a box
			if (name.Contains("Sentry"))
			{
				amount = amount * 0.2f;
			}
		}

		// Check if playerSender is alive: (It's pretty much like it never happened)
		if (isHitscan && (!GameManager.PlayerExists(playerSender) || GameManager.GetPlayer(playerSender).playerObject == null))
			return;

		if (this is ThrowableSync) {
			GetComponent<Rigidbody> ().AddForce (Vector3.Normalize(transform.position - fromWhere) * 2f * amount + 2f * amount * Vector3.up);
		}

		health -= amount;

		if (health <= 0 && health + amount > 0) // As to confirm this only gets called once. Kind of iffy with healing, ofc
		{
			health = 0; // Hmm..

			// die.
			alive = false;
			diedAt = ServerState.tickNumber;
		}
	}




	// Some useful things:

	public static PlayerMade GetPlayerMade(Transform transformHit)
	{
		if (transformHit.GetComponent<PlayerMade>())
		{
			return transformHit.GetComponent<PlayerMade>();
		}
		else if (transformHit.parent.GetComponent<PlayerMade>())
		{
			return transformHit.parent.GetComponent<PlayerMade>();
		}
		else if (transformHit.parent.parent.GetComponent<PlayerMade>())
		{
			return transformHit.parent.parent.GetComponent<PlayerMade>();
		}
		Debug.LogError("Failure! PlayerMade -> GetPlayerMade: Did not find PlayerMade! - in object (" + transformHit.name + ")");
		return null;
	}

	// Generalized TakeDamageObject, as per use in any COLLISION case scenario:
	// The return variable is there for the exception in stuff like rockets.
	public static Transform TakeDamageObjectG(Transform transformHit, float amount, Vector3 fromWhere, bool isHitscan, short playerSender)
	{
		// It is derived to transformHit because hit could vary in type.

		// No object with a collider that is considered here shall NOT be a playerMadeObject with a PROPER PlayerMade. 
		// The collider MUST be made to be different layer if it shouldn't be able to be damaged.

		short shortAmount = (short)(Mathf.RoundToInt (amount));

		if (transformHit.GetComponent<PlayerMade>())
		{
			transformHit.GetComponent<PlayerMade>().TakeDamageObject(shortAmount, fromWhere, isHitscan, playerSender);
			return transformHit;
		} else if (transformHit.parent.GetComponent<PlayerMade>())
		{
			transformHit.parent.GetComponent<PlayerMade>().TakeDamageObject(shortAmount, fromWhere, isHitscan, playerSender);
			return transformHit.parent;
		} else if (transformHit.parent.parent.GetComponent<PlayerMade>())
		{
			transformHit.parent.parent.GetComponent<PlayerMade>().TakeDamageObject(shortAmount, fromWhere, isHitscan, playerSender);
			return transformHit.parent.parent;
		}
		Debug.LogError("Failure! PlayerMade -> TakeDamageObjectG: Did not find PlayerMade! - in object (" + transformHit.name + ")");
		return null;
	}

	// Another generalized set of methods:

	public static bool IsEnemy(Transform transformHit, int team)
	{
		return (transformHit.CompareTag("Shield") || transformHit.CompareTag("ShieldParent") || transformHit.CompareTag("Icicle")) && 
			(transformHit.gameObject.layer == 15 - team * 5 || transformHit.gameObject.layer == 21 - team);
	}

	public static bool IsFriendlyBuilding(Transform transformHit, int team)
	{
		return (transformHit.CompareTag("Shield") || transformHit.CompareTag("ShieldParent") || transformHit.CompareTag("Icicle")) &&
			transformHit.gameObject.layer == 10 + team * 5;
	}

	public static bool IsFriendlyIcicle(Transform transformHit, int team)
	{
		return transformHit.CompareTag("Icicle") && transformHit.gameObject.layer == 10 + team * 5;
	}

	public static bool IsFriendlyThrowable(Transform transformHit, int team)
	{
		return transformHit.CompareTag("Shield") && transformHit.gameObject.layer == 20 + team;
	}

}
