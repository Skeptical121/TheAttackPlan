using UnityEngine;
using System.Collections;

public class BulletLauncher : Trap {

	// Set In Inspector
	public GameObject bullet; // Used for homing sphere currently

	// Server only
	int ticksSinceLaunch = 0;


	public override short getMaxHealth () {
		return 400;
	}

	public override float getLifeTime () {
		return 36.0f;
	}

	public override float getBuildTime () {
		return 4f;
	}

	public override void buildAnimation (float lifeTime) {
		// Something
	}

	public override void endBuildAnimation() {
		
	}

	public override void UpdateServerAndInterp (bool building, bool dieing, float lifeTime)
	{
		// Reveals the trap over the initial time:
		if (lifeTime < getBuildTime()) {
			// Build handles this
		} else {
			if (OperationNetwork.isServer) {
				ticksSinceLaunch++;
				if (ticksSinceLaunch % 100 == 0) { // 50 ticks is 1 second
					float maxMissAngle = 14f; // Was 12f
					// Launch arrow:
					Vector2 randInUnitCircle = UnityEngine.Random.insideUnitCircle;
					Vector3 direction = randInUnitCircle * maxMissAngle * Mathf.PI / 180.0f; // This is here because the accuracy, is on average less when using a cone to do inaccuracy. This increases the amount of shots that are more accurate. In the future it could just use tan, or maybe tan with this.
					direction.z = 1.0f;
					//direction = transform.TransformDirection(direction.normalized);
					Quaternion fireRot = Quaternion.LookRotation(transform.forward) * Quaternion.LookRotation(direction.normalized);
					Vector3 fireDir = fireRot * Vector3.forward;

					GameObject arrowObj = (GameObject)Instantiate(bullet, transform.position + transform.forward * 0.3f, fireRot);
					arrowObj.GetComponent<SyncGameState> ().playerOwner = playerOwner;
					arrowObj.GetComponent<Projectile>().SetInitialVelocity(fireDir * Random.Range(5, 8));
					OperationNetwork.OperationAddSyncState (arrowObj);
				}
			}
		}
	}
}
