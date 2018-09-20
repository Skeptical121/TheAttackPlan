using UnityEngine;
using System.Collections;

// INCLUDES: ExplodeDetection (todo)
public abstract class InterpRotate : PhysicsProjectile {

	Quaternion initialRotation = Quaternion.identity;
	bool setRotation = false;

	public override void InitStart (bool isThisMine)
	{
		base.InitStart (isThisMine);
		initialRotation = transform.rotation;
	}

	public override void ServerSyncFixedUpdate()
	{
		if (transform.position != lastPosition) {
			transform.rotation = Quaternion.LookRotation (Vector3.Normalize (transform.position - lastPosition));
		}
		base.ServerSyncFixedUpdate (); // Order needs to be like this here because lastPosition is set here
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner)
	{
		return base.getBitChoicesLengthThis (isPlayerOwner) + 1;
	}

	public override object getObjectThis(int num, bool isPlayerOwner)
	{
		int childNum = num - base.getBitChoicesLengthThis (isPlayerOwner);
		switch (childNum)
		{
		case 0: return initialRotation;

		default: return base.getObjectThis(num, isPlayerOwner);
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine)
	{
		int previousTotal = base.SetInformation (data, a, b, isThisFirstTick, isThisMine);
		if (!setRotation) {
			transform.rotation = (Quaternion)data [previousTotal];
			setRotation = true;
		}

		if (b != null) {
			if ((Vector3)b [1] == (Vector3)a [1]) { // This is assuming this parent has "Vector3" as element 1. (todo)
				// not moving.
			} else {
				transform.rotation = Quaternion.LookRotation (Vector3.Normalize ((Vector3)b [1] - (Vector3)a [1])); // rotation is very iffy. However, the hitBoxes for these objects are sphere.
			}
		}
		return previousTotal + 1;
	}

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

	public virtual void OnHit(Collision collision) {
		BlowUp (collision.transform);
		transform.position = lastPosition;
	}
}
