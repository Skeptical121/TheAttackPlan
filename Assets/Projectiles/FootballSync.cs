using UnityEngine;
using System.Collections;

public class FootballSync : PhysicsProjectile
{

	// Server only
	int numHits = 0;

	// Use this for initialization
	public override void InitStart(bool isThisMine)
	{
		base.InitStart (isThisMine);

		expDmg = 50;
		maxDist = 2.0f;
		initDmgPercent = 0.7f;
		leastDmgPercent = 0.3f;

		knockBackMult = 0.8f;

		lastPosition = transform.position;

	}

	public override float getExpDamage()
	{

		// Speed is usually among 20 -> up to 30ish

		// 2.4 is the multiplier for the up & down icicle, with 15 speed. Thus 30 * 2.4 = 72


		return expDmg * Mathf.Pow(0.6f, getLifeTimeServer()); // Maximum 15 * 5.5 damage (82.5)
	}

	public override float getMaximumLifeTime() {
		return 7.0f;
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner) {
		return base.getBitChoicesLengthThis (isPlayerOwner) + 1;
	}

	public override object getObjectThis(int num, bool isPlayerOwner) {
		int childNum = num - base.getBitChoicesLengthThis (isPlayerOwner);

		switch (childNum)
		{
		case 0: return transform.rotation;

		default: return base.getObjectThis(num, isPlayerOwner);
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		int previousTotal = base.SetInformation (data, a, b, isThisFirstTick, isThisMine);
		transform.rotation = (Quaternion)data[previousTotal];
		return previousTotal + 1;
	}

	// This is IDENTICAL code to InterpRotate. Football is currently deprecated, however.
	void OnCollisionEnter(Collision collision)
	{

		// Detect collisions:

		// Assumed to be sphere scaling factor:

		// Gets first collider.. for now..
		if (OperationNetwork.isServer && exists) { // Can only hit one thing
			if (collision.transform.CompareTag ("Mirror")) {
				// Reflect
				transform.position = lastPosition;
				Vector3 mirrorForward = collision.transform.Find ("ReflectPlane").up;
				// Sets initial velocity / initial position /  accordingly. (Also resets tickSpawnedAt)
				GetComponent<Rigidbody> ().velocity = Vector3.Reflect (lastVelocity, mirrorForward);
				if (ignoreTeam == collision.transform.GetComponent<MirrorLogic>().team) {
					expDmg *= 2; // Double damage
				}
			} else {
				OnHit (collision);
			}
		}
	}

	// This is DIFFERENT to InterpRotate because of the IF statement
	void OnHit(Collision collision) {
		if (collision.gameObject.layer == 17 - ignoreTeam && numHits++ <= 4) { // numHits so you can't be hit by a football that's on the ground
			BlowUp (collision.transform);
			transform.position = lastPosition;
		}
	}

}
