using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkThroughWalls {

	// A PlayerMove implementation of PlayerMove..


	// We need to sync BOTH positions:
	Vector3 posA;
	Vector3 posB;

	// We need to sync the direction:
	Vector3 dir;

	// And we need to sync the "travel percentage"
	public float travelPercent = -1; // Set to -1 to indicate not travelling
	public bool walkingThroughWalls = false; // hmm

	public bool RunPlayerAndServer(PlayerInput pI, Vector3 moveDir, Transform t) {
		if (!walkingThroughWalls) {
			// We first need to detect if you are walking STRAIGHT into a wall:
			RaycastHit hit;

			float fullRadius = t.GetComponent<CapsuleCollider> ().radius * t.localScale.x;

			if (Physics.Raycast (t.position, t.rotation * Vector3.forward, out hit, 0.025f + fullRadius, LayerLogic.WorldColliders ()) &&
				Physics.Raycast (t.position, t.rotation * Vector3.forward, out hit, 0.025f + fullRadius, LayerLogic.WorldColliders ())) { // Possibly you can only walk into default colliders
				if (moveDir.z > 0) {
					// The character is "walking into a wall"

					// Enter!
					if (Physics.Raycast (t.position, hit.normal, out hit, 18f, LayerLogic.WorldColliders ())) {

						float radius = fullRadius * 0.69f; // 1.0f would be full. Note 0.7f is necessary for diagonals
						float height = t.GetComponent<CapsuleCollider> ().height * t.localScale.x * 0.41f; // 0.5f would be full

						if (Physics.OverlapBox(hit.point + hit.normal * t.GetComponent<CapsuleCollider>().radius * t.localScale.x, 
							new Vector3(radius, height, radius)).Length == 0) {


							posA = t.position;
							dir = t.rotation * Vector3.forward;
							travelPercent = 0;
							walkingThroughWalls = true;

							posB = hit.point + hit.normal * t.GetComponent<CapsuleCollider>().radius * t.localScale.x;
						
						}
					}
				}
			}
			return false;
		} else {
			// For now, you can't move backwards
			travelPercent += (pI.frameTime * 4f); // Auto move
			if (travelPercent < 0.5f) {
				t.position = posA + dir * t.GetComponent<CapsuleCollider> ().radius * t.localScale.x * travelPercent * 4;
			} else {
				t.position = posB - dir * t.GetComponent<CapsuleCollider> ().radius * t.localScale.x * (1 - travelPercent) * 4;
			}
			// Also set transparency..
			if (travelPercent > 1) {
				travelPercent = -1; // Note, INTERP!! (-1 should be an exception to interp for float TODO!!)
				walkingThroughWalls = false;
				t.position = posB;
			}

			return true;
		}

		// The initial entrance location is the "legal" location. (Also the exit point, of course)

	}
}
