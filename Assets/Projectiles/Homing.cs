using UnityEngine;
using System.Collections;

public class Homing : PhysicsProjectile {

	public float homingCapabilities = -1; // Only is homing towards players. Slowly diminishes:
	public float homingCapabilitiesLossPerSecond = 0f;

	public override void InitStart (bool isThisMine)
	{
		base.InitStart (isThisMine);
		homingCapabilities = 20;
		homingCapabilitiesLossPerSecond = 1.5f;

		expDmg = 34; // Was 30
		maxDist = 2.4f;
		initDmgPercent = 0.75f;
		leastDmgPercent = 0.45f;

		knockBackMult = 1.4f;
	}

	public virtual float getMaxHomingDistance() {
		return 20f;
	}

	public virtual float getMaxSpeed() {
		return 12f;
	}

	public override void ServerSyncFixedUpdate () {
		base.ServerSyncFixedUpdate ();

		if (homingCapabilities > 0)
		{
			homingCapabilities -= homingCapabilitiesLossPerSecond * Time.fixedDeltaTime;
			if (homingCapabilities > 0)
			{
				Transform iterator = GameObject.Find("Players").transform;
				// Players are effected by projectiles:
				for (int i = 0; i < iterator.childCount; i++)
				{
					Transform t = iterator.GetChild(i);
					//if (!t.GetComponent<PlayerMove>().isPhasing())
					//{
						Vector3 cP = t.GetComponent<CharacterController>().ClosestPointOnBounds(transform.position);
						float distance = Vector3.Distance(cP, transform.position);
						if (distance < getMaxHomingDistance() && ignoreTeam != t.GetComponent<Combat>().team)
						{
							GetComponent<Rigidbody>().AddForce(Vector3.Normalize(cP - transform.position) * homingCapabilities * Time.fixedDeltaTime, ForceMode.Impulse);
							float speed = Vector3.Distance (Vector3.zero, GetComponent<Rigidbody> ().velocity);
							float maxSpeed = getMaxSpeed();
							if (speed > maxSpeed) {
								GetComponent<Rigidbody> ().velocity = GetComponent<Rigidbody> ().velocity * (maxSpeed / speed);
							}
						}
					//}
				}
			}
		}
	}

	// Homing Spheres blow up on contact:
	void OnCollisionEnter(Collision collision)
	{

		// Detect collisions:

		// Assumed to be sphere scaling factor:
		//Collider[] colliders = Physics.OverlapSphere(transform.position, transform.localScale.x, LayerLogic.ProjectileCollision(ignoreTeam));
		//if (colliders.Length > 0) {
		// Gets first collider.. for now..
		if (OperationNetwork.isServer) {
			BlowUp (collision.transform);
			transform.position = lastPosition;
		}
		//}
	}
}
