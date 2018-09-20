using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GolfSwing : Throwable {

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		// Then we overwrite, because Throwable is actually an unlock itself
		throwablePrefab = Resources.Load ("GolfBall") as GameObject;
	}

	public override float getCoolDown() {
		return 20f;
	}

	public override float getAppearTime() {
		return 2f;
	}

	public override float getLaunchTime() {
		return 2f;
	}

	public override void Throw(GameObject throwObj, Vector3 fireDir) {

		throwObj.GetComponent<SphereCollider>().enabled = true;
		throwObj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
		throwObj.GetComponent<Rigidbody>().velocity = 60f * fireDir;
		throwObj.GetComponent<GolfBallHoming>().enabled = true;
		throwObj.GetComponent<GolfBallHoming> ().playerOwner = parentPlayerMove.plyr;

		throwObj.GetComponent<AudioSource> ().Play ();
		throwObj.GetComponent<ParticleSystem> ().Play ();
	}
}
