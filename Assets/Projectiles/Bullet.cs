using System;
using UnityEngine;
using UnityEngine.Networking;

// INCLUDES: ExplodeDetection
public class Bullet : Projectile
{
	// Note that tickSpawnedAt is changed every time Bullet hits a mirror.

	public Vector3 initialPosition;
	public Vector3 initialVelocity;

	Vector3 lastVelocity; // Server only.

	// float blowUpTime = -1; // For use with ExplodeDetection. (Not used..)

	public override void InitStart (bool isThisMine)
	{
		// Might as well set this stuff on client.. hmm..

		expDmg = 65; // was 55
		maxDist = 2.7f; // was 2.4f
		initDmgPercent = 0.75f; // was 0.8f
		leastDmgPercent = 0.45f; // was 0.4f

		knockBackMult = 1.4f;

		if (OperationNetwork.isServer) {
			lastVelocity = initialVelocity; // initialVelocity is set on instantiate.
		}

		base.InitStart (isThisMine);
	}

	public override void SetInitialVelocity (Vector3 initVel)
	{
		initialVelocity = initVel;
		// Also set initialPosition:
		initialPosition = transform.position;
	}

	// Server only:
	public override float getExpDamage()
	{
		return expDmg * Mathf.Pow(0.8f, getLifeTimeServer());
	}
		
	public override void ServerSyncFixedUpdate ()
	{
		base.ServerSyncFixedUpdate ();
		if (exists) {
			// Detect collisions:
			RaycastHit hitInfo;
			Vector3 forward = Vector3.Normalize (lastVelocity);
			// Assumed to be sphere scaling factor:
			if (Physics.SphereCast (new Ray (transform.position, forward), transform.localScale.x, out hitInfo,
				   Vector3.Distance (Vector3.zero, lastVelocity * Time.fixedDeltaTime), LayerLogic.ProjectileCollision (ignoreTeam))) {
				if (hitInfo.collider.CompareTag ("Mirror")) {
					Vector3 mirrorForward = hitInfo.transform.Find ("ReflectPlane").up;
					// Sets initial velocity / initial position /  accordingly. (Also resets tickSpawnedAt)
					initialVelocity = Vector3.Reflect (lastVelocity, mirrorForward);
					lastVelocity = initialVelocity;
					// Rather than doing a proper reflection, the reflection that is done is as if the projectile goes through the mirror- this will speed up the projectile though.
					// This could be done better, of course, to avoid this speed up.
					Plane plane = new Plane (mirrorForward, hitInfo.transform.Find ("ReflectPlane").position);

					float enter;
					plane.Raycast (new Ray (transform.position, forward), out enter);

					initialPosition = transform.position + Vector3.ProjectOnPlane (forward * enter, mirrorForward) * 2;
					transform.position = initialPosition;
					tickSpawnedAt = ServerState.tickNumber;

					if (ignoreTeam == hitInfo.transform.GetComponent<MirrorLogic> ().team) {
						expDmg *= 2; // Double damage
					}
				} else {
					BlowUp (hitInfo.transform);
				}
			}
		}

		// Still move if exists = false (?)
		transform.position = initialPosition + initialVelocity * getLifeTimeServer();
		transform.forward = Vector3.Normalize(initialVelocity);
	}

	// Networking!!

	public override int getBitChoicesLengthThis (bool isPlayerOwner)
	{
		return base.getBitChoicesLengthThis (isPlayerOwner) + 3;
	}

	public override object getObjectThis(int num, bool isPlayerOwner)
	{
		int childNum = num - base.getBitChoicesLengthThis (isPlayerOwner);
		switch (childNum)
		{
		case 0: return tickSpawnedAt;
		case 1: return initialPosition;
		case 2: return initialVelocity;

		default: return base.getObjectThis(num, isPlayerOwner);
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine)
	{
		int previousTotal = base.SetInformation (data, a, b, isThisFirstTick, isThisMine);

		tickSpawnedAt = (int)data[previousTotal];
		initialPosition = (Vector3)data [previousTotal + 1];
		initialVelocity = (Vector3)data [previousTotal + 2];

		// Use this data accordingly:
		transform.position = initialPosition + initialVelocity * getLifeTimeInterp();

		transform.forward = Vector3.Normalize(initialVelocity);

		return previousTotal + 3;
	}

	public override void OnDeath ()
	{
		base.OnDeath ();

		Transform rP = transform.Find ("RocketParticles");

		ParticleSystem.EmissionModule em = rP.GetComponent<ParticleSystem>().emission;
		ParticleSystem.MinMaxCurve r = em.rate;
		r.constantMin = 0;
		r.constantMax = 0;
		r.constant = 0;
		em.rate = r;

		// Gives time for particles to disappear:
		rP.parent = null;

		Destroy (rP.gameObject, 10.0f);
	}

	// Non physics projectiles have no collider. THUS, Collision is more on a tick per tick basis.

	
}