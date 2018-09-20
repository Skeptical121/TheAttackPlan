using UnityEngine;
using System.Collections;

public class ShowInvalidBoundingBoxPMO : MonoBehaviour {

	// This is not just restricted to PMOs, it can include stuff like cap points

	float lastTimeShown = -1;
	bool beingShown = false;

	GameObject invalidBoundingBox = null;

	void Start() {
		invalidBoundingBox = transform.Find ("InvalidBoundingBox").gameObject; // It's simpler this way rather than setting it in the inspector
		invalidBoundingBox.transform.localScale = GetComponent<BoxCollider> ().size;
		invalidBoundingBox.transform.localPosition = GetComponent<BoxCollider> ().center;
	}

	public void updateBeingShown() {
		lastTimeShown = Time.time;
	}

	void Update () {
		if (beingShown != (lastTimeShown != -1 && Time.time - lastTimeShown < Time.deltaTime + 0.0001f) && invalidBoundingBox != null) {
			beingShown = (lastTimeShown != -1 && Time.time - lastTimeShown < Time.deltaTime + 0.0001f);
			invalidBoundingBox.GetComponent<Renderer> ().enabled = beingShown;
		}
	}
}
