using UnityEngine;
using System.Collections;
using System;

// Collide check is not spawned on client side, it is server only.

// Collide check also handles the SERVER side of spawning buildables.
public class CollideCheck : MonoBehaviour
{

	public short playerOwner;
	public OperationView playerOperationView;
	public byte placeType;
	public byte team;

	public GameObject[] playerMadeObjects;

	// Some of these variables can be set for the "cutOut" creation process found in OperationNetwork init:
	// Currently, however, they are only used for traps..
	public const byte canPlaceOnGround = 0;
	public const byte canPlaceOnWalls = 1;
	public const byte canPlaceAnywhere = 2;
	public const byte canPlaceAnywhere2 = 3;
	public byte canPlaceOn = canPlaceOnGround; // This is set on init, not in inspector

	public Vector3 offset = Vector3.zero; // This can be set in inspector

	public bool IsPlacementValid() {
		// Checks for collision:
		if (GetComponent<BoxCollider> ()) {
			Collider[] collidersHit = Physics.OverlapBox (transform.TransformPoint (GetComponent<BoxCollider> ().center), 
				                         new Vector3 (GetComponent<BoxCollider> ().size.x * transform.lossyScale.x / 2, 
					                         GetComponent<BoxCollider> ().size.y * transform.lossyScale.y / 2, 
					                         GetComponent<BoxCollider> ().size.z * transform.lossyScale.z / 2), transform.rotation, LayerLogic.PlacePlayerMadeCollision ());
			if (collidersHit.Length > 0) {
				// We need to go and find which PlayerMadeObject it collided with, because Physics.CheckBox doesn't give us that information, and Physics.BoxCast doesn't work for this.
				foreach (Collider c in collidersHit) {
					if (c.gameObject.layer == 19) { // Must be AntiPlacement layer
						c.GetComponent<ShowInvalidBoundingBoxPMO> ().updateBeingShown ();
					}
				}
				return false;
			}
			if (canPlaceOn == canPlaceOnWalls || canPlaceOn == canPlaceAnywhere) {
				int mult = 1;
				if (canPlaceOn == canPlaceAnywhere)
					mult = -1;
				Vector3 center = GetComponent<BoxCollider> ().center + new Vector3(0, 0, GetComponent<BoxCollider> ().size.z / 2) * mult;
				float widthRad = GetComponent<BoxCollider> ().size.x / 2;
				float heightRad = GetComponent<BoxCollider> ().size.y / 2;
				for (float nX = -0.8f; nX <= 0.8f; nX += 0.4f) {
					for (float nY = -0.8f; nY <= 0.8f; nY += 0.4f) {
						if ((nX != 0 || nY != 0) && !checkTypeWall (center, widthRad, heightRad, mult, nX, nY))
							return false;
					}
				}
				return true;
			} else {
				Vector3 center = GetComponent<BoxCollider> ().center - new Vector3(0, GetComponent<BoxCollider> ().size.y / 2, 0);
				float widthRad = GetComponent<BoxCollider> ().size.x / 3;
				float heightRad = GetComponent<BoxCollider> ().size.z / 3;
				for (float nX = -0.8f; nX <= 0.8f; nX += 0.4f) {
					for (float nY = -0.8f; nY <= 0.8f; nY += 0.4f) {
						if ((nX != 0 || nY != 0) && !checkTypeFloor (center, widthRad, heightRad, nX, nY))
							return false;
					}
				}
				return true;
			}
		}
		return true;
	}

	public bool checkTypeWall(Vector3 center, float widthRad, float heightRad, float mult, float x, float y) {
		return Physics.Raycast (transform.TransformPoint (center + new Vector3 (widthRad * x, heightRad * y, 0)) - transform.forward * 0.1f * mult, transform.forward * mult, 0.6f, 1 << 0);
	}

	public bool checkTypeFloor(Vector3 center, float widthRad, float heightRad, float x, float y) {
		return Physics.Raycast (transform.TransformPoint (center + new Vector3 (widthRad * x, 0, heightRad * y)) + transform.up * 0.1f, transform.up * -1, 0.6f, 1 << 0);
	}

	public void ServerPlacementValidCheck() {

		bool isPlacementValid = IsPlacementValid ();



		if (isPlacementValid) {
			GameObject properObj = (GameObject)Instantiate(playerMadeObjects[team], transform.position, transform.rotation);

			properObj.GetComponent<PlayerMade> ().playerOwner = playerOwner;

			OperationNetwork.OperationAddSyncState (properObj);
			Destroy(gameObject);

		} else {
			// Did not place!! Prediction FAILURE!! RESET COOLDOWN

			// Simple system now. Reset cooldown!!

			if (GameManager.PlayerExists (playerOwner)) {
				bool found = false;
				if (GameManager.GetPlayer (playerOwner).playerObject != null) {
					foreach (Unlock unlock in GameManager.GetPlayer(playerOwner).playerObject.GetComponent<ClassControl>().getUnlocks()) {
						if (unlock is PlacePlayerMade && !(unlock is PlaceTrap) && ((PlacePlayerMade)unlock).cutOut.GetComponent<CollideCheck>().placeType == placeType) {
							if (unlock is PlaceIcicle) {
								((PlaceIcicle)unlock).AmmoStored = Mathf.Min (((PlaceIcicle)unlock).AmmoStored + 1, PlaceIcicle.maxAmmoStored);
							} else {
								Debug.LogError ("Resette");
								((PlacePlayerMade)unlock).coolDownStartedAt = -1000f;
							}


							found = true;
							break;
						}
					}
				}
				// Could be trap:
				if (!found) {
					for (int i = 0; i < GameManager.GetPlayer(playerOwner).trapTypes.Length; i++) {
						Debug.Log (placeType + ": " + GameManager.GetPlayer (playerOwner).trapTypes [i]);
						if (GameManager.GetPlayer (playerOwner).trapTypes [i] == placeType) {
							Debug.Log ("Reset");
							GameManager.GetPlayer(playerOwner).trapCoolDownsStartedAt [i] = GameManager.GetPlayer(playerOwner).resetTrapCoolDownsTo [i];
						}
					}
				}

			}



			Destroy(gameObject);
			return;
		}
	}

}
