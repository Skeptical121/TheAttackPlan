using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class Throwable : Unlock {

	public override int getUnlockPosition() {
		return 2;
	}

	public GameObject throwablePrefab;
	public GameObject rightHand;

	public GameObject throwable = null;

	bool isEnabled = false;

	public float coolDownStartedAt = -1;

	bool launched = false; // Relies on time only going forward

	public override void LoadUnlockObject() {
		rightHand = parentPlayerMove.transform.Find ("Armature").Find ("Pelvis").
			Find ("Stomach").Find ("Chest").Find ("UpperArm_R").Find ("LowerArm_R").Find ("Wrist_R").gameObject;
		throwablePrefab = Resources.Load ("ThrowableBlue") as GameObject;
	}

	public virtual float getCoolDown() {
		return 15f;
	}

	public override void interp ()
	{
		Update (parentPlayerMove.GetComponent<SyncPlayer>().getTotalTimeSince(equippedAt.getTriggerTime(parentPlayerMove)));
	}

	public override void UpdateServerAndPlayer (PlayerInput pI, bool runEffects)
	{
		if (runEffects) { // The code right now relies on time going forward..
			Update (parentPlayerMove.GetComponent<SyncPlayer> ().getTotalTimeSince (equippedAt.getTriggerTime (parentPlayerMove)));
		}
	}

	public virtual float getAppearTime() {
		return 0.4f;
	}

	public virtual float getLaunchTime() {
		return 0.7f;
	}

	public void appear() {
		// It appears
		throwable = (GameObject)MonoBehaviour.Instantiate(throwablePrefab, rightHand.transform.TransformPoint(-0.35f, 0, 0), rightHand.transform.rotation); // Note that this is a fake projectile on both sides
		throwable.transform.parent = rightHand.transform;
		if (throwable.GetComponent<ThrowableSync> ()) {
			throwable.GetComponent<ThrowableSync> ().enabled = false;
			throwable.GetComponent<CapsuleCollider>().enabled = false;
		} else if (throwable.GetComponent<FootballSync> ()) {
			throwable.GetComponent<FootballSync> ().enabled = false;
			throwable.GetComponent<CapsuleCollider>().enabled = false;
		} else {
			throwable.GetComponent<GolfBallHoming> ().enabled = false;
			throwable.GetComponent<SphereCollider>().enabled = false;
		}
	}

	void Update(float timeSinceChargeStarted)
	{
		if (!launched && timeSinceChargeStarted > getAppearTime()) {
			if (throwable == null)
			{
				appear ();
			}

			// Launch
			if (timeSinceChargeStarted > getLaunchTime())
			{
				launched = true;
				if (throwable != null)
				{
					MonoBehaviour.Destroy(throwable); // Hmm.. it'll desync on the player side. (And possibly client side, for that matter)
					throwable = null;
					if (OperationNetwork.isServer)
					{
						GameObject throwObj = (GameObject)MonoBehaviour.Instantiate(throwablePrefab, rightHand.transform.TransformPoint(-0.35f, 0, 0), rightHand.transform.rotation);


						// Note how yDir is negative for both:
						float yDir;
						if (parentPlayerMove.thisIsMine || OperationNetwork.isServer)
						{
							yDir = -parentPlayerMove.mainCamera.transform.eulerAngles.x;
						}
						else {
							yDir = parentPlayerMove.currentPlayerRotUpDown;
						}

						yDir = yDir * Mathf.PI / 180.0f;
						if (yDir > Math.PI)
						{
							yDir -= Mathf.PI * 2;
						}
						if (yDir < -Math.PI)
						{
							yDir += Mathf.PI * 2;
						}

						float yDirIncrease = Mathf.PI / 12.0f;

						Vector3 fireDir;
						if (yDir > -yDirIncrease)
							fireDir = Vector3.RotateTowards(parentPlayerMove.transform.forward, Vector3.up, yDir + yDirIncrease, 1f);
						else
							fireDir = Vector3.RotateTowards(parentPlayerMove.transform.forward, Vector3.down, -yDir - yDirIncrease, 1f);

						Throw(throwObj, fireDir);

						OperationNetwork.OperationAddSyncState (throwObj);
						// We could keep the moveDirection / effectDirection of the Z direction, and perhaps the Y direction (if it is > 0)
					}

					// Player and Server:
					if (OperationNetwork.isServer || parentPlayerMove.thisIsMine) {
						setCoolDown ();
						isEnabled = false;
						parentPlayerMove.GetComponent<ClassControl> ().defaultSetup (true); // This is a method which is only called when runEffects = true
					}
				}
			}
		}
	}

	// Sets what can be changed for a throwable
	public virtual void Throw(GameObject throwObj, Vector3 fireDir) {
		throwObj.GetComponent<SphereCollider>().enabled = true;
		throwObj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
		throwObj.GetComponent<Rigidbody>().velocity = 12f * fireDir;
		throwObj.GetComponent<ThrowableSync>().enabled = true;
		throwObj.GetComponent<ThrowableSync> ().playerOwner = parentPlayerMove.plyr;
	}

	public void setCoolDown()
	{
		coolDownStartedAt = parentPlayerMove.GetComponent<SyncPlayer> ().getTime ();
	}

	public override bool canEnable()
	{
		return parentPlayerMove.GetComponent<SyncPlayer>().getTotalTimeSince(coolDownStartedAt) >= getCoolDown();
	}

	public override bool canDisable()
	{
		return !isEnabled; // Must be disabled automatically! (It is a non-interuptable animation)
	}

	public override void enable()
	{
		isEnabled = true;
		launched = false;
	}

	public override void disable()
	{
		MonoBehaviour.Destroy(throwable); // Hmm.. it'll desync on the player side. (And possibly client side, for that matter)
		throwable = null;
		// disable is done manually by setting isEnabled = false, so there's no need to set it again.
	}

	public override string getAnimationType()
	{
		return "Throw";
	}

	// No stuff yet for this:
	public override void midEnable() {
		if (throwable == null && getAppearTime () == -0.25f) {
			appear ();
		}
	}

	public override void midDisable() {
	}

	public override string getHudType () {
		return "Cooldown";
	}

	public override void setCharge()
	{
		float percentCoolDown = parentPlayerMove.GetComponent<SyncPlayer> ().getTotalTimeSince (coolDownStartedAt) / getCoolDown ();
		if (percentCoolDown >= 1) {
			percentCoolDown = 1;
			hudElement.transform.Find ("Charge").GetComponent<Image> ().color = new Color (0, 1f, 0);
		} else {
			hudElement.transform.Find("Charge").GetComponent<Image> ().color = new Color (1f, 1f, 1f);
		}

		hudElement.transform.Find ("Charge").GetComponent<Image> ().fillAmount = percentCoolDown;
		hudElement.GetComponent<Image> ().fillAmount = 1 - percentCoolDown;
	}
}
