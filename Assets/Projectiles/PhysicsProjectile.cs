using UnityEngine;
using System.Collections;

public abstract class PhysicsProjectile : Projectile {


	public Vector3 lastPosition;

	public Vector3 lastVelocity;

	public override void InitStart (bool isThisMine)
	{
		base.InitStart (isThisMine);
		lastPosition = transform.position;
	}

	public override void SetInitialVelocity (Vector3 initVel)
	{
		GetComponent<Rigidbody> ().velocity = initVel;
		lastVelocity = initVel;
	}

	public override void ServerSyncFixedUpdate()
	{
		base.ServerSyncFixedUpdate ();
		lastVelocity = GetComponent<Rigidbody> ().velocity;
		lastPosition = transform.position;
		// Physics projectiles rely on physics for their simulation.

		// RotationProjectiles (Arrow, Icicle), both explode on contact.

		// Collision test. This can include mirrors and destruction.
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
			case 0: return transform.position;

			default: return base.getObjectThis(num, isPlayerOwner);
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine)
	{
		int previousTotal = base.SetInformation (data, a, b, isThisFirstTick, isThisMine);
		transform.position = (Vector3)data[previousTotal];
		return previousTotal + 1;
	}


}
