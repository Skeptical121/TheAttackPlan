using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HoverMoundCollision : MonoBehaviour {

	List<Vector3> playerPositions = new List<Vector3>();
	MoveDirtMound hdm = null;

	void Update() {
		if (hdm == null)
			hdm = transform.parent.GetComponent<MoveDirtMound> ();

		// This is obviously a little inefficient
		Vector3 extents = GetComponent<BoxCollider> ().size / 2;
		extents = new Vector3 (extents.x * transform.lossyScale.x, extents.y * transform.lossyScale.y, extents.z * transform.lossyScale.z);
		Collider[] people = Physics.OverlapBox (transform.TransformPoint (GetComponent<BoxCollider> ().center), extents, transform.rotation, 1 << 8 | 1 << 22);
		for (int i = 0; i < people.Length; i++) {
			playerPositions.Add (transform.InverseTransformPoint (people [i].transform.position - Vector3.down * 1)); // Sends position of feet
		}

		hdm.UpdateMoundArmature (playerPositions);
		playerPositions.Clear ();
	}
}
