using UnityEngine;
using System.Collections;

public class Icicle : InterpRotate
{
	Vector3 prevPrevVelocity = Vector3.zero;
	Vector3 prevVelocity = Vector3.zero;

	// Use this for initialization
	public override void InitStart(bool isThisMine)
	{
		base.InitStart (isThisMine);

		expDmg = 20;
		maxDist = 5.0f; // Was 6.0f
		initDmgPercent = 1f;
		leastDmgPercent = 0.3f;

		knockBackMult = 0.8f;

		lastPosition = transform.position;

	}

	public override void ServerSyncFixedUpdate ()
	{
		base.ServerSyncFixedUpdate ();
		prevPrevVelocity = prevVelocity;
		prevVelocity = GetComponent<Rigidbody> ().velocity;
	}

	public override float getExpDamage()
	{

		// Speed is usually among 20 -> up to 30ish

		// 2.4 is the multiplier for the up & down icicle, with 15 speed. Thus 30 * 2.4 = 72


		return Mathf.Min((expDmg + Vector3.Distance(Vector3.zero, prevPrevVelocity)) * Mathf.Pow(1.6f, getLifeTimeServer()), expDmg * 4f); // Maximum 15 * 5.5 damage (82.5)
	}

}
