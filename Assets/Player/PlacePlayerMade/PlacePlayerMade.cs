using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Reflection;
using System.Collections.Generic;

public abstract class PlacePlayerMade : Unlock
{

	public static GameObject[] cutOuts = new GameObject[14];

	public static GameObject placementSphere;
	public GameObject cutOut;

	public float coolDownStartedAt = -1; // Player & Server


	float placingRotation = 0; // In degrees
	bool isPlacing = false;

	public GameObject placing = null;


	// Note that placeType is only used for its type currently. It does not use PlacePlayerMade placePlayerMades byte dictionary because it does not need to.
	public static void AddObject(PlayerMove parentPlayerMove, Vector3 pos, Quaternion rot, byte placeType, byte team, short playerOwner)
	{
		try {
			GameObject obj = (GameObject)MonoBehaviour.Instantiate (placementSphere, 
				parentPlayerMove.transform.TransformPoint (parentPlayerMove.GetComponent<CapsuleCollider> ().center) /* Approximate for now.*/, 
				parentPlayerMove.mainCamera.transform.rotation);

			GameObject cutOut = cutOuts [placeType];

			obj.GetComponent<PlacementSphere>().cutOut = cutOut;
			obj.GetComponent<PlacementSphere>().team = team;
			obj.GetComponent<PlacementSphere>().playerOwner = playerOwner;
			obj.GetComponent<PlacementSphere> ().placeType = placeType;
			obj.GetComponent<PlacementSphere>().toPosition = pos; // fromPosition is set by initStart.
			obj.GetComponent<PlacementSphere>().objRotation = rot;

			OperationNetwork.OperationAddSyncState (obj);
		} catch (Exception e) {
			Debug.LogError ("Failure in adding trap: " + placeType + ", " + e.Message);
		}
	}

	public override void AfterFirstInterp ()
	{
		if (parentPlayerMove.thisIsMine && Player.thisPlayer && getCoolDown() != 162500f) {
			// This is fine in case of icicle, it'll just be set to -1
			float cdsa = Player.thisPlayer.getCoolDownToSetTo (GetType ());
			if (cdsa == -1) {
				coolDownStartedAt = -1;
			} else {
				coolDownStartedAt = cdsa - Time.time; // This will produce a negative #.. Time.time being used here is correct.
				// In case it becomes -1:
				if (coolDownStartedAt == -1) {
					coolDownStartedAt -= 0.0001f;
				}
			}
		}
		base.AfterFirstInterp ();
	}

	public bool isEnabledStill()
	{
		return isPlacing;
	}

	public override void UpdateServerAndPlayer(PlayerInput pI, bool runEffects)
	{
		if (!parentPlayerMove.thisIsMine || !runEffects) {
			// Assumes it has been placed:
			if (pI.fireKey) {
				if (!(this is PlaceTrap)) {
					setCoolDown ();
					if (!(this is PlaceIcicle))
						parentPlayerMove.GetComponent<ClassControl>().defaultSetup(runEffects); // hmm
				} else {
					if (OperationNetwork.isServer) {
						if (GameManager.PlayerExists (parentPlayerMove.plyr)) {
							// We would do it here.. but we don't know which trap was placed. Thus "PlacementSphere" takes care of this part of the code

							((PlaceTrap)this).setTrapCoolDown ();

							// ...
						}
					}
				}
			}
			return;
		}

		if (runEffects) {
			bool fireKey = false;
			// The following is "Player Only".

			if (isPlacing && canEnable())
			{
				if (placing == null)
				{
					enable();
				}

				placingRotation += (Input.GetAxisRaw("Mouse ScrollWheel")) * 10f * 45f; // meh (TODO DON'T USE Input!)

				if (Input.GetMouseButtonDown(2)) // Middle click (TODO DON'T USE Input!)
				{
					// Middle click = rotate:
					placingRotation += 90f; // hmm..
				}

				bool valid;
				bool wasPlaced = setCutOutOnFlatGround (out valid);
				valid = valid && placing.GetComponent<CollideCheck> ().IsPlacementValid ();
				SetRenderers(wasPlaced, valid);

				if (pI.fireKey && canPlace() && wasPlaced && valid && (!(this is PlaceTrap) || ((PlaceTrap)this).canPlaceTrap()))
				{
					AddObject();

					if (!(this is PlaceTrap)) {
						setCoolDown ();

						if (this is PlaceIcicle) {
							fireKey = true;
							isPlacing = true;
						} else {
							fireKey = true;
							parentPlayerMove.GetComponent<ClassControl> ().defaultSetup (runEffects);
						}
					} else {
						fireKey = true;

						((PlaceTrap)this).setTrapCoolDown (); // Player side "trapPlacing" var used
					}
				}
			}

			pI.fireKey = fireKey; // Override..
		}
	}

	public virtual bool canPlace() {
		return true;
	}

	public virtual void setCoolDown()
	{
		coolDownStartedAt = parentPlayerMove.GetComponent<SyncPlayer> ().getTime ();
		if (parentPlayerMove.thisIsMine && GameManager.GetPlayer(parentPlayerMove.plyr) && getCoolDown() != 162500f) {
			GameManager.GetPlayer(parentPlayerMove.plyr).setCoolDown(GetType(), Time.time);
		}
	}

	public abstract float getCoolDown();

	public override bool canEnable()
	{
		return parentPlayerMove.GetComponent<SyncPlayer>().getTotalTimeSince(coolDownStartedAt) >= getCoolDown();
	}

	// Can't think of a reason this wouldn't be true
	public override bool canDisable()
	{
		return true;
	}

	public override void enable()
	{
		if (parentPlayerMove.thisIsMine)
		{
			instantiateObject();
		}
	}

	public override void disable()
	{
		// "reset"
		isPlacing = false;
		placingRotation = 0;
		if (placing != null)
			MonoBehaviour.Destroy(placing);
		placing = null;
	}

	public override string getAnimationType()
	{
		return "Placing";
	}

	public virtual void instantiateObject()
	{
		// This method is called in update every frame in PlaceTrap checking for Cutout to not be null.. or if Cutout has changed
		if (cutOut != null) {
			isPlacing = true;

			placing = MonoBehaviour.Instantiate (cutOut);
			placing.transform.parent = GameObject.Find ("ClientSideOnly").transform;
			// placeType is already set on prefab

			// This isn't so important.. this is what setRenderers is for..
			if (placing.GetComponent<Renderer>())
				placing.GetComponent<Renderer> ().enabled = false;
		}
	}

	public override void LoadUnlockObject() {
		unlockObject = Resources.Load ("PlacementGun") as GameObject; // Resources.Load ("LaserGun") as GameObject;
	}

	public void SetRenderers(bool wasPlaced, bool valid) {
		if (placing.GetComponent<Renderer>())
			placing.GetComponent<Renderer>().enabled = valid;
		foreach (Transform placementChild in placing.transform) {
			if (placementChild.CompareTag ("Valid")) {
				placementChild.GetComponent<Renderer> ().enabled = valid;
			} else if (placementChild.CompareTag ("Invalid")) {
				placementChild.GetComponent<Renderer> ().enabled = !valid && wasPlaced;
			}
		}

	}

	public void AddObject() {
		parentPlayerMove.GetComponent<OperationView>().RPC("AddPlayerMade", OperationNetwork.ToServer, placing.GetComponent<CollideCheck>().placeType, placing.transform.position, placing.transform.rotation); // This should actually be the thing that decides the cool down
	}

	public virtual void setPlacingRotation(Vector3 normal)
	{
		placing.transform.rotation *= Quaternion.Euler(0, parentPlayerMove.transform.rotation.eulerAngles.y + placingRotation, 0);
		placing.transform.position += normal * cutOut.GetComponent<CollideCheck> ().offset.y;
	}

	// As much as steep ramp sentries / saws seem like a fun idea, steep ramps in this game will not be having placeables beyond icicles on them:
	public virtual bool normalIsValid(RaycastHit hit, Vector3 normal)
	{
		return normal.y >= 0.799f; // Note that 0.71 (sqrt 2) is the "45 degree" point. < 37 degrees this is.
	}

	public abstract int getPlacingLength();

	bool setCutOutOnFlatGround(out bool valid)
	{
		valid = true;
		RaycastHit hit = new RaycastHit();
		// It used to be that the player capsule collider prevented placement. Now, only the hitboxes prevent placement.
		if (Physics.Raycast(parentPlayerMove.mainCamera.transform.position, parentPlayerMove.mainCamera.transform.forward, out hit, 1000, (1 << 0 | 1 << 10 | 1 << 14 | 1 << 15)))
		{
			if (hit.transform.gameObject.layer == 0 || hit.transform.gameObject.layer == 11 || (hit.transform.gameObject.layer == 15 && (this is PlaceSpikes || this is PlaceEarthMound) && hit.transform.name.Contains("RedSawBlades"))) {
				if (normalIsValid(hit, hit.normal)) {
					// Place it
					placing.transform.position = hit.point + hit.normal * 0.01f;
					placing.transform.rotation = Quaternion.Euler(Mathf.Asin(hit.normal.z) * 180 / Mathf.PI, 0, -Mathf.Asin(hit.normal.x) * 180 / Mathf.PI);

					setPlacingRotation(hit.normal);

					// Check length, and set didHit = true if too far. NOTE: This check is not done on the server! (todo, maybe?)
					if (Vector3.Distance(parentPlayerMove.transform.position, placing.transform.position) > getPlacingLength())
					{
						valid = false;
					}
					return true;
				}
				//}
			}
		}
		return false;
	}

	public override string getHudType () {
		return "n/a"; //"Trap";
	}

	public override void setCharge()
	{
		float percentCoolDown = parentPlayerMove.GetComponent<SyncPlayer> ().getTotalTimeSince (coolDownStartedAt) / getCoolDown ();
		setCharge(hudElement, percentCoolDown, 1f);
	}

	public static void setCharge(GameObject hE, float percentCoolDown, float percentToBeValid) {
		if (percentCoolDown >= percentToBeValid) {
			hE.transform.Find ("Charge").GetComponent<Image> ().color = new Color (0, 1f, 0);
		} else {
			hE.transform.Find("Charge").GetComponent<Image> ().color = new Color (1f, 1f, 1f);
		}
		if (percentCoolDown >= 1)
			percentCoolDown = 1;

		hE.transform.Find ("Charge").GetComponent<Image> ().fillAmount = percentCoolDown;
		hE.GetComponent<Image> ().fillAmount = 1 - percentCoolDown;
	}
}