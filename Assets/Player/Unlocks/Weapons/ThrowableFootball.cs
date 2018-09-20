using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowableFootball : Throwable {

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		// Then we overwrite, because Throwable is actually an unlock itself
		throwablePrefab = Resources.Load ("Football") as GameObject;
	}

	public override float getCoolDown() {
		return 4.5f;
	}

	public override float getAppearTime() {
		return -0.25f; // This means an instant transition
	}

	public override float getLaunchTime() {
		return 0.2f;
	}

	public override void Throw(GameObject throwObj, Vector3 fireDir) {
		throwObj.GetComponent<CapsuleCollider>().enabled = true;

		throwObj.GetComponent<Rigidbody> ().constraints = RigidbodyConstraints.None; // FreezeRotation;

		throwObj.GetComponent<Rigidbody> ().angularDrag = 0.8f;

		// Throws the football really fast:
		throwObj.GetComponent<Rigidbody>().velocity = 17f * fireDir; // + GetComponent<PlayerMove>().moveDirection + GetComponent<PlayerMove>().effectDirection;

		throwObj.layer = 20 + parentPlayerMove.GetComponent<Combat> ().team;

		throwObj.GetComponent<FootballSync>().enabled = true;
		throwObj.GetComponent<FootballSync> ().ignoreTeam = parentPlayerMove.GetComponent<Combat> ().team;
		throwObj.GetComponent<FootballSync> ().playerOwner = parentPlayerMove.plyr;
	}
}
