using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public abstract class Unlock : ICloneable
{
	public const byte none = 255;

	public PlayerMove parentPlayerMove;

	public GameObject hudElement;

	// Similary to whichBind, this is set:
	public int whichBind = -1; // None. (No need to set in inspector)

	public KeyCode keyToSwitchTo; // Set by the person's binds.

	public GameObject unlockObject; // Always connected to right hand currently.
	public GameObject viewmodelUnlockObject;

	// These are predicted entirely by the player using "playerTime" to make the gameplay smooth:

	// On client, these are integers. On Server, they are floats because of the use of "playerTime"


	// NOTE! It is absolutely imparative that all init of Objects is done in "InitAll" because the object is CLONED as a SHALLOW COPY.


	public FrameTrigger firedAt; // Set in InitAll
	public FrameTrigger reloadedAt; // Set in InitAll

	public FrameTrigger secondaryFiredAt;

	// Note that recoil is entirely dependant on "firedAt" It could be simulated backwards.

	public FrameTrigger equippedAt; // This is after the FULL switch animation. Set in InitAll

	byte unlockType;

	// EXAMPLES:
	// So, ALL syncing shall happen on the integer ticks, and the integer ticks only.

	// OpNetGS stuff:

	// Player uses playerTime for its timing here.
	// Server uses playerTime for its timing as well.

	// Client uses Interp.getLifeTime() based on tick #s from the server.

	// This is for stuff that needs to exist at all times.
	public void InitAll(PlayerMove parentPlayerMove) {
		firedAt = new FrameTrigger ();
		secondaryFiredAt = new FrameTrigger ();
		reloadedAt = new FrameTrigger ();
		equippedAt = new FrameTrigger ();
		this.parentPlayerMove = parentPlayerMove;
		SetUnlockObject(); // This is actually everything that needs to be set with unlocks. Unlocks are already within the character. (This INCLUDES trap placing modules, they all use the same object!)
	}

	// This is for stuff that relies on playerOwner.
	// Player ONLY:
	public virtual void AfterFirstInterp()
	{
		SetViewmodelUnlockObject ();
	}

	// This is only run on the unlock equipped that is not currently being updated.
	public virtual void UpdateServerAndPlayer(PlayerInput pI, bool runEffects) {
		
	}

	public virtual void PlayerAndServerAlways(PlayerInput pI, bool equipped) {

	}


	// Sync is done by PlayerSync using standardized data set here in Unlock:
	// Stuff like animations will run automatically according to the animation types chosen in each subclass.
	public virtual void SyncData() {

	}

	public abstract bool canEnable();
	public abstract bool canDisable();

	public abstract void enable();
	public abstract void disable();

	public void setUnlockType(byte type) {
		this.unlockType = type;
	}
	public byte getUnlockType() { // as in ID.
		return unlockType;
	}
	public abstract int getUnlockPosition(); // as in Primary vs Secondary vs Tertiary

	// Mid enable / disable is just the disable / enable of the renderer. This can be overridden if unlockObject doesn't exist.
	public virtual void midEnable()
	{
		unlockObject.SetActive (true);

		// Viewmodels:
		if (parentPlayerMove.thisIsMine)
		{
			viewmodelUnlockObject.SetActive (true);
		}
	}

	public virtual void midDisable()
	{
		unlockObject.SetActive (false); //GetComponent<Renderer> ().enabled = false;

		// Viewmodels:
		if (parentPlayerMove.thisIsMine)
		{
			viewmodelUnlockObject.SetActive (false); //GetComponent<Renderer> ().enabled = false;
		}
	}

	// Player Only. Used for PlacePlayerMade only.
	public virtual void PlayerUnlockFixedUpdate() {

	}

	public virtual void LoadUnlockObject() {

	}

	public virtual void interp() {

	}

	public virtual Transform getGunBone(Transform playerObjTransform) {
		return playerObjTransform.Find ("Armature").Find ("Pelvis").
		Find ("Stomach").Find ("Chest").Find ("UpperArm_R").Find ("LowerArm_R").Find ("Wrist_R").Find ("GunBone");
	}

	// Done in InitStart. Automatic.
	void SetUnlockObject()
	{
		LoadUnlockObject ();

		if (unlockObject != null) {
			// Generate gun from fireFrom.

			// gunBone is different for specific unlocks:

			Transform gunBone = getGunBone (parentPlayerMove.transform);

			if (gunBone.Find (unlockObject.name)) {
				unlockObject = gunBone.Find (unlockObject.name).gameObject;
			} else {
				GameObject uO = unlockObject;
				unlockObject = (GameObject)MonoBehaviour.Instantiate (unlockObject, gunBone, false);
				unlockObject.name = uO.name;
				unlockObject.SetActive (false);
			}
		}
		// This should also set the hud of whoever the owner of this player is.
	}

	public void SetViewmodelUnlockObject() {
		if (unlockObject != null) {
			// All red players have PlacementGun added on spawn
			viewmodelUnlockObject = getGunBone(parentPlayerMove.playerView.viewmodelPlayer.transform).Find (unlockObject.name).gameObject;
		}
	}

	public static GameObject setHudElement(Transform t, String type, String name, int bind, RectTransform beside, float origSpaceNeeded) {
		if (type == "Other")
			type = "Gun"; // For these purposes:
		if (OptionsMenu.hudPanels.ContainsKey(type)) {
			GameObject panel = MonoBehaviour.Instantiate(OptionsMenu.hudPanels[type]);

			// Ordered by add order
			float spaceNeeded = PlayerHud.getHudHeightUsed (t);

			panel.transform.SetParent (t, false);
			// This is a terrible way to set position, but it should work for now.
			if (type == "Cooldown" || type == "Trap" || type == "Gun" || type == "Other" || type == "Generic") { // Traps should probably give more information
				panel.transform.Find ("Text").GetComponent<Text> ().text = "(" + OptionsMenu.getBindString(OptionsMenu.binds [bind]) + ") " + name;
			} else if (type == "Icicle") {
				panel.transform.Find ("Text").GetComponent<Text> ().text = "(" + OptionsMenu.getBindString(OptionsMenu.binds [bind]) + ")";
			}
			// Special case:
			if (type == "Gun3Modes") {
				panel.transform.Find ("Gun1").Find ("Text").GetComponent<Text> ().text = "(" + OptionsMenu.getBindString(OptionsMenu.binds [OptionsMenu.PRIMARY_1]) + ") " + name;
				panel.transform.Find ("Gun2").Find ("Text").GetComponent<Text> ().text = "(" + OptionsMenu.getBindString(OptionsMenu.binds [OptionsMenu.PRIMARY_2]) + ") Accurate-Fire";
				panel.transform.Find ("Gun3").Find ("Text").GetComponent<Text> ().text = "(" + OptionsMenu.getBindString(OptionsMenu.binds [OptionsMenu.PRIMARY_3]) + ") Conductive-Fire";
			}
			if (beside == null) {
				panel.GetComponent<RectTransform> ().anchoredPosition = new Vector2 (panel.GetComponent<RectTransform> ().anchoredPosition.x, -280 + (int)(spaceNeeded - origSpaceNeeded));
			} else {
				panel.GetComponent<RectTransform> ().anchoredPosition = new Vector2 (panel.GetComponent<RectTransform> ().anchoredPosition.x, beside.anchoredPosition.y);
			}
			return panel;
		}
		return null;
	}

	// To be overrided, of course
	public virtual string GetUnlockName() {
		return GetType ().Name;
	}

	// of Transform t
	public virtual void setHudElement(Transform t, float origNum)
	{
		hudElement = setHudElement (t, getHudType (), GetUnlockName(), whichBind, null, origNum);
	}

	public abstract string getHudType ();

	// tickNumber used on CLIENT ONLY
	public void FireTriggered(bool runEffects, int tickNumber) // reloadedAt has passed. (Or player / server has reloaded)
	{
		if (OperationNetwork.isServer || parentPlayerMove.thisIsMine) {
			firedAt.trigger (parentPlayerMove.GetComponent<SyncPlayer> ().getTime ());
			parentPlayerMove.GetComponent<SyncPlayer> ().currentTriggerSet.trigger (SyncPlayer.FIRE_TRIGGER);
		} else {
			firedAt.triggerClient (tickNumber);
		}
		// Sound:
		if (runEffects) {
			SoundHandler.soundHandler.PlayFireSound (GetType (), unlockObject.transform);
			parentPlayerMove.GetComponent<PlayerAnimation> ().animTrigger ("FireTrigger");
		}
	}

	// tickNumber used on CLIENT ONLY
	public void SecondaryFireTriggered(bool runEffects, int tickNumber) // reloadedAt has passed. (Or player / server has reloaded)
	{
		if (OperationNetwork.isServer || parentPlayerMove.thisIsMine) {
			secondaryFiredAt.trigger (parentPlayerMove.GetComponent<SyncPlayer> ().getTime ());
			parentPlayerMove.GetComponent<SyncPlayer> ().currentTriggerSet.trigger (SyncPlayer.SECONDARY_FIRE_TRIGGER);
		} else {
			secondaryFiredAt.triggerClient (tickNumber);
		}
		// Sound:
		if (runEffects) {
			SoundHandler.soundHandler.PlaySecondaryFireSound (GetType (), unlockObject.transform);
			parentPlayerMove.GetComponent<PlayerAnimation> ().animTrigger ("SecondaryFireTrigger");
		}
	}

	public abstract string getAnimationType();

	public virtual float getSwitchToTime()
	{
		return 0.25f; // Standard. Note it was 0.125f
	}

	public virtual float getSwitchFromTime()
	{
		return 0.25f; // Standard. Note it was 0.125f
	}

	// hmm..
	public virtual void StartUnlock() {

	}

	// Hmm?
	public virtual void setCharge() {

	}

	public object Clone() {
		return this.MemberwiseClone ();
	}

}
