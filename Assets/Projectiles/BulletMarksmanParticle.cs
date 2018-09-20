using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletMarksmanParticle : Bullet {

	public override void BlowUp (Transform c)
	{
		// Do nothing
	}

	public override void OnDeath ()
	{
		// Do nothing
	}

	public override float getMaximumLifeTime() {
		return 60.0f; // Obviously should be less
	}

	void Update() {
		// This effect goes through walls. This seems reasonable. The effect will probably be abandoned anyways..
		Collider[] plyrs = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius, 1 << 8 | 1 << 22);
	}

	public override void ServerSyncFixedUpdate ()
	{
		base.ServerSyncFixedUpdate ();

		if (ServerState.tickNumber % SawScript.numTicksPerHit == 0) {
			float ht = 0f; // Not used
			ExplodeDetection.BlowUp (transform.position, transform.position, 100f * Time.fixedDeltaTime * SawScript.numTicksPerHit, GetComponent<SphereCollider> ().radius, 0.05f, 2f, null, ignoreTeam, ref ht, playerOwner);
		}
	}
}
