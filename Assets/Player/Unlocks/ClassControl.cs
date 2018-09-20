using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System;

public class ClassControl : MonoBehaviour {
	
	public static Color[] HudBGColors = {new Color(30 / 255f, 80 / 255f, 180 / 255f, 150 / 255f), new Color(180 / 255f, 50 / 255f, 30 / 255f, 150 / 255f)};

	// Unlock is slightly inefficient, but very practical in how its done.


	// nextUnlock is set during START of switch. At END of switch, whichUnlock is set to nextUnlock. Switching is a "special case", essentially. isSwitching is an important check.
	public byte whichUnlock = 255; // Which Unlock is set after switchUnlock is over.
	public byte nextUnlock = 255; // It creates the unlock if it doesn't exist. Note that Unlock needs to be created even for clients because of recoil stuff, etc.

	public FrameTrigger switchedAt = new FrameTrigger();


	// From here, the class can selected which weapons / player mades can be used.
	public static Unlock[] unlocks;

	Unlock[] UnlocksEquipped = new Unlock[4];

	public int classNum = 0; // Not seperated into different files yet. (Or ever?)

	public bool isBot = false; // This MUST NOT be used on clients. Server use only!

	public Unlock[] getUnlocks() {
		return UnlocksEquipped;
	}

	public bool canDisable()
	{
		if (isSwitching())
			Debug.LogError ("Must check NOT isSwitching before using this: ClassControl -> getUnlockEquipped");

		if (whichUnlock == Unlock.none) {
			return true;
		} else
		{
			return getUnlockEquipped().canDisable();
		}
	}

	// This should always be used AFTER isSwitching is known to be false -on Player and Server; on client, this is totally fine
	public Unlock getUnlockEquipped()
	{
		if (isSwitching() && (OperationNetwork.isServer || GetComponent<PlayerMove>().thisIsMine))
			Debug.LogError ("Must check NOT isSwitching before using this: ClassControl -> getUnlockEquipped");

		if (whichUnlock == Unlock.none)
			return null;
		
		for (int i = 0; i < UnlocksEquipped.Length; i++)
		{
			if (UnlocksEquipped [i] != null && UnlocksEquipped [i].getUnlockType() == whichUnlock)
				return UnlocksEquipped [i];
		}
		Debug.LogError("Failure to get unlock");
		return null;
	}

	// This is only called during a prediction error: (And quite rarely at that)
	public void enableUnlockEquippedRegardlessOfSwitching(bool enable)
	{
		if (whichUnlock == Unlock.none)
			return;

		for (int i = 0; i < UnlocksEquipped.Length; i++)
		{
			if (UnlocksEquipped [i] != null && UnlocksEquipped [i].getUnlockType () == whichUnlock) {
				if (enable)
					UnlocksEquipped [i].enable ();
				else
					UnlocksEquipped [i].disable ();
				return;
			}
		}
		Debug.LogError("Failure to get unlock");
		return;
	}

	void FixedUpdate() {
		// This is for "PlacePlayerMade"
		if (GetComponent<PlayerMove> ().thisIsMine) {
			for (int i = 0; i < UnlocksEquipped.Length; i++) {
				if (UnlocksEquipped [i] != null) {
					UnlocksEquipped [i].PlayerUnlockFixedUpdate ();
				}
			}
		}
	}

	// The following are quite reasonable to be null. At least one of them is fine to be null:

	// These should only be used when switching:
	Unlock getWhichUnlock() {
		for (int i = 0; i < UnlocksEquipped.Length; i++)
		{
			if (UnlocksEquipped[i] != null && UnlocksEquipped [i].getUnlockType() == whichUnlock)
				return UnlocksEquipped [i];
		}
		return null;
	}

	Unlock getNextUnlock() {
		for (int i = 0; i < UnlocksEquipped.Length; i++)
		{
			if (UnlocksEquipped[i] != null && UnlocksEquipped [i].getUnlockType() == nextUnlock)
				return UnlocksEquipped [i];
		}
		return null;
	}

	// This is the proper way:
	public static U getUnlock<U>() where U : Unlock { 
		for (int i = 0; i < unlocks.Length; i++) {
			if (unlocks [i].GetType() == typeof(U)) {
				return (U)unlocks[i];
			}
		}
		Debug.LogError ("Failed to getUnlock - ClassControl - Generic Implementation");
		return default(U);
	}

	public static Unlock getUnlock(byte unlockType) {
		for (int i = 0; i < unlocks.Length; i++) {
			if (unlocks [i].getUnlockType() == unlockType) {
				return (Unlock)unlocks[i];
			}
		}
		Debug.LogError ("Failed to getUnlock - ClassControl - Non-Generic Implementation");
		return null;
	}

	// This is the proper way:
	public U getUnlockEquippedWithType<U>() where U : Unlock { 
		for (int i = 0; i < UnlocksEquipped.Length; i++) {
			if (UnlocksEquipped [i] != null && UnlocksEquipped [i].GetType() == typeof(U)) {
				return (U)UnlocksEquipped[i];
			}
		}
		// Whatever implementation we do, the way traps are done will probably be done through RPC. Thus, it can become out of sync with player input.
		// Therefore, either the Server or Player must trust that this trap exists:

		SetUnlock<U>(); // This creates the unlock IF it doesn't exist for the player already!! (Only done with traps & obstacles)

		return getUnlockEquippedWithType<U> (); // possible infinite recursion, yikes
	}

	public void SetUnlock(int unlockSlot, byte unlockType) {
		Unlock unlock = getUnlock (unlockType);
		UnlocksEquipped [unlockSlot] = (Unlock)unlock.Clone ();
		UnlocksEquipped [unlockSlot].setUnlockType (unlock.getUnlockType ()); // Just to be consistent.
		UnlocksEquipped [unlockSlot].whichBind = unlockSlot;
		UnlocksEquipped[unlockSlot].InitAll (GetComponent<PlayerMove>());
	}

	public void SetUnlock<U>() where U : Unlock {
		Unlock unlock = getUnlock<U>();
		SetUnlock<U> (unlock.getUnlockPosition ());
	}

	// This overrides unlockSlot if called directly: (It can ONLY be used on init directly though to sync with clients)
	// This is used by ALL
	public void SetUnlock<U>(int unlockSlot) where U : Unlock {
		Unlock unlock = getUnlock<U>();
		UnlocksEquipped [unlockSlot] = (Unlock)unlock.Clone ();
		UnlocksEquipped [unlockSlot].setUnlockType (unlock.getUnlockType ()); // Just to be consistent.
		UnlocksEquipped [unlockSlot].whichBind = unlockSlot;
		UnlocksEquipped[unlockSlot].InitAll (GetComponent<PlayerMove>());
	}

	public void InitStartClass () {
		// The existance of unlocks is important to ALL.
		UnlocksEquipped = new Unlock[6];
		if (classNum == 0) {
			// Unlocks do not rely on eachother.

			GetComponent<PlayerMove> ().walkThroughWalls = new WalkThroughWalls ();

			SetUnlock<ShotGun> (OptionsMenu.PRIMARY_1);

			// OFFENCIVE ABILITY:
			if (GetComponent<Combat> ().team == 0) {
				SetUnlock<PlaceShield> (OptionsMenu.OFFENCIVE_ABILITY);
			} else {
				SetUnlock<PlaceTrap> (OptionsMenu.TRAP_1);
			}
		} else if (classNum == 1) {
			SetUnlock<Pistol> (OptionsMenu.PRIMARY_1);
			SetUnlock<PlaceIcicle> (OptionsMenu.MAIN_ABILITY);

			// OFFENCIVE ABILITY:
			if (GetComponent<Combat> ().team == 0) {
				SetUnlock<Throwable> (OptionsMenu.OFFENCIVE_ABILITY);
			} else {
				SetUnlock<PlaceTrap> (OptionsMenu.TRAP_1);
			}
		} else if (classNum == 2) {
			SetUnlock<ProjectileGunScript> (OptionsMenu.PRIMARY_1);
			SetUnlock<PlaceEarthMound> (OptionsMenu.MAIN_ABILITY);

			// OFFENCIVE ABILITY:
			if (GetComponent<Combat> ().team == 0) {
				SetUnlock<PlaceMirror> (OptionsMenu.OFFENCIVE_ABILITY);
			} else {
				SetUnlock<PlaceTrap> (OptionsMenu.TRAP_1);
			}
		} else if (classNum == 3) {
			SetUnlock<MeeleeWeapon> (OptionsMenu.PRIMARY_1);
			SetUnlock<PoleVault> (OptionsMenu.MAIN_ABILITY);
			if (GetComponent<Combat> ().team == 0) {
			} else {
				SetUnlock<PlaceTrap> (OptionsMenu.TRAP_1);
			}
		} else if (classNum == 4) {
			SetUnlock<TakeHealthWeapon> (OptionsMenu.PRIMARY_1);

			if (GetComponent<Combat> ().team == 0) {
				
			} else {
				SetUnlock<PlaceTrap> (OptionsMenu.TRAP_1);
			}
		}

		// Enable gun to start with: (Done for all clients)
		if (OperationNetwork.isServer || GetComponent<PlayerMove> ().thisIsMine) {
			defaultSetup (true); // Only called once
		}
	}

	// Neither weapon is running their code if switching is occurring. Also, no other weapon can be switched to.
	// Client, Player, and Server
	public bool isSwitching()
	{
		return whichUnlock != nextUnlock;
	}

	// This is fine to run for multiple frames.. midEnable / midDisable just sets renderers to false / true
	bool isMidSwitchOver() {
		if (getWhichUnlock () == null) {
			return GetComponent<SyncPlayer> ().getTotalTimeSince (switchedAt.getTriggerTime(GetComponent<PlayerMove>())) >= 0;
		}
		return GetComponent<SyncPlayer> ().getTotalTimeSince (switchedAt.getTriggerTime(GetComponent<PlayerMove>())) >= getWhichUnlock ().getSwitchFromTime ();
	}

	bool isSwitchingOver() {
		if (getWhichUnlock () == null) {
			return GetComponent<SyncPlayer> ().getTotalTimeSince (switchedAt.getTriggerTime(GetComponent<PlayerMove>())) >= 0 + getNextUnlock ().getSwitchToTime();
		}
		return GetComponent<SyncPlayer> ().getTotalTimeSince (switchedAt.getTriggerTime(GetComponent<PlayerMove>())) >= getWhichUnlock ().getSwitchFromTime () + getNextUnlock ().getSwitchToTime();
	}
		

	// This does NOT go through prediction error, as it never fails this.
	public void PlayerAndServer(PlayerInput pI, bool runEffects) // runEffects is true when player does its first prediction run, but not when predictione error is done. It is also set to true on server (as it deals with animation)
	{


		// Player Input can suggest changes in weapon, which has been premodified to be reasonable.
		// Thus, this does NOT use "canEnable" check, it assumes the playerInput is valid.

		byte ust = pI.unlockSwitchTo;
		if (pI.cancelKey) {
			if (runEffects && PlayerHud.isTrapSelectionMenuOpen) {
				if (PlayerHud.whichGroupTypeSelecting == -1) {
					PlayerHud.isTrapSelectionMenuOpen = false;
					OptionsMenu.ChangeLockState ();
					ust = 0;
				} else {
					PlayerHud.whichGroupTypeSelecting = -1;
				}
			} else {
				ust = 0;
			}
		}
		
		if (ust != 255) {
			
			if (ust == OptionsMenu.PRIMARY_1 || ust == OptionsMenu.PRIMARY_2 || ust == OptionsMenu.PRIMARY_3) {
				ust = OptionsMenu.PRIMARY_1;
			} else if (ust == OptionsMenu.TRAP_1 || ust == OptionsMenu.TRAP_2) {
				ust = OptionsMenu.TRAP_1;
			}
			if (ust < OptionsMenu.NUM_SWITCH_TO_BINDS) {
				if (getUnlocks () [ust] != null) { // Trap binds are set / unset as before
					byte nextUnlock = getUnlocks () [ust].getUnlockType ();

					if (classNum == 1 && ust == OptionsMenu.PRIMARY_1) {
						((Pistol)getUnlocks()[ust]).UpdatePistolMode(pI);
					}

					if (ust == OptionsMenu.TRAP_1 && runEffects) {
						if (pI.unlockSwitchTo == OptionsMenu.TRAP_1)
							((PlaceTrap)getUnlocks () [ust]).trapPlacing = 0;
						else
							((PlaceTrap)getUnlocks () [ust]).trapPlacing = 1;
					}

					if (!isSwitching () && getUnlocks () [ust].canEnable () && canDisable ()) {
						if (getUnlockEquipped () != getUnlocks () [ust]) {
							// This assumes switching is possible. It is neccesary that the check for switching is done in PlayerInput to maintain consistency.
							SetNextUnlock (nextUnlock, runEffects, true);
						}

					}
				}
			}

		}

		switchCheck(); // This needs to go before isSwitching.


		for (int i = 0; i < UnlocksEquipped.Length; i++) {
			if (UnlocksEquipped [i] != null) {
				UnlocksEquipped [i].PlayerAndServerAlways (pI, !isSwitching() && getUnlockEquipped() == UnlocksEquipped[i]);
			}
		}

		if (!isSwitching()) // Switching is handled by "InterpAll"
		{
			Unlock usingUnlock = getUnlockEquipped();
			if (usingUnlock != null) { // This happens on spawn currently ..-> doesn't have to.
				usingUnlock.UpdateServerAndPlayer (pI, runEffects);
			} else {
				// Because of how placePlayerMade work, fireKey needs to be set to false because of issues..
				pI.fireKey = false;
			}
		}

		if (GetComponent<PlayerMove> ().thisIsMine) {
			updateHud ();
		}
	}

	// Player and Server
	public void SetNextUnlock(byte unlockToSetTo, bool runEffects, bool setSwitchedAt) {
		if (whichUnlock != unlockToSetTo) {
			Unlock usingUnlock = getUnlockEquipped ();
			if (usingUnlock != null) {
				usingUnlock.disable ();
			}
				
			// Trigger animation:
			nextUnlock = unlockToSetTo;

			if (setSwitchedAt) {
				switchedAt.trigger (GetComponent<SyncPlayer> ().getTime ()); // This'll be overwritten
			}

			if (runEffects) {
				GetComponent<PlayerAnimation> ().animTrigger (getNextUnlock ().getAnimationType () + "Trigger");
			}
		} else if (!setSwitchedAt) {
			nextUnlock = unlockToSetTo;
		}
	}

	public int getUnlockIndex(Unlock unlock) {
		for (int i = 0; i < UnlocksEquipped.Length; i++)
		{
			if (UnlocksEquipped [i] == unlock) {
				return i;
			}
		}
		return -1;
	}

	// ALL
	void switchCheck() {
		// Creates unlock if it doesn't exist here. Player, Server, and Client could do it this way in general.

		// Now check for switching:
		if (isSwitching()) {
			if (isMidSwitchOver()) {
				midSwitch();
			}

			if (isSwitchingOver ()) {
				// Set unlock accordingly:
				whichUnlock = nextUnlock;

				Unlock unlockEquipped = getUnlockEquipped ();
				if (OperationNetwork.isServer || GetComponent<PlayerMove> ().thisIsMine) {
					unlockEquipped.equippedAt.trigger (GetComponent<SyncPlayer> ().getTime ());
				} else {
					unlockEquipped.equippedAt.interpTrigger (true);
				}
				unlockEquipped.enable ();
			}
		}

	}

	public void clientInterp(byte unlockID) {
		if (unlockID != nextUnlock) {

			// This assumes switching is possible. (Client side this is obviously fine)
			Unlock usingUnlock = getUnlockEquipped();
			if (usingUnlock != null) {
				usingUnlock.disable ();
			}

			nextUnlock = unlockID;

			GetComponent<PlayerAnimation> ().animTrigger (getNextUnlock ().getAnimationType () + "Trigger");

			switchedAt.interpTrigger (true); // "Triggers" always start at the START of the tick.
		}

		switchCheck ();

		// Now we run client interp:
		Unlock currUnlock = getUnlockEquipped();
		if (currUnlock != null) {
			currUnlock.interp ();
		}
	}

	public void AfterFirstInterpClass()
	{

		GetComponent<PlayerAnimation>().setArmLayerWeights(classNum);

		if (GetComponent<PlayerMove>().thisIsMine)
		{

			// Enable Hud Elements:
			foreach (Unlock unlock in UnlocksEquipped)
			{
				if (unlock != null) {
					unlock.AfterFirstInterp ();
				}
			}
		}

		foreach (Unlock unlock in UnlocksEquipped)
		{
			if (unlock != null) {
				unlock.StartUnlock ();
			}
		}
	}

	public void disableAllHud(Transform plyrHud)
	{
		// Removes Class Hud
		Transform hudEl = plyrHud.Find ("ClassHud");
		for (int k = hudEl.childCount - 1; k >= 0; k--)
		{
			Destroy (hudEl.GetChild (k).gameObject);
		}
	}

	public void setHealthTaken(float value) {
		if (classNum == 4) {
			TakeHealthWeapon thw = (TakeHealthWeapon)getUnlockEquippedWithType<TakeHealthWeapon>();
			thw.healthTaken = (float)value;
		}
	}

	public float getHealthTaken() {
		if (classNum == 4) {
			TakeHealthWeapon thw = (TakeHealthWeapon)getUnlockEquippedWithType<TakeHealthWeapon>();
			return thw.healthTaken;
		}
		return 0f;
	}

	void updateHud() {
		for (int i = 0; i < UnlocksEquipped.Length; i++) {
			if (UnlocksEquipped [i] != null && UnlocksEquipped[i].getHudType() == "Cooldown" && UnlocksEquipped[i].hudElement != null) {
				UnlocksEquipped [i].setCharge ();
			}
			if (UnlocksEquipped [i] is TakeHealthWeapon && GetComponent<PlayerMove>().hudElement != null) {
				// Render "Health Taken"
				TakeHealthWeapon thw = (TakeHealthWeapon)UnlocksEquipped [i];
				if (thw.healthTaken >= TakeHealthWeapon.healthTakenReq) {
					// Can use "ultimate" / "ubercharge"
					GetComponent<PlayerMove>().hudElement.GetComponent<Text>().color = Color.green;
					GetComponent<PlayerMove>().hudElement.GetComponent<Text>().text = "(" + OptionsMenu.getBindString(OptionsMenu.binds[OptionsMenu.ULTIMATE_ABILITY]) + ") Health Taken: " + Mathf.FloorToInt(thw.healthTaken);
				} else {
					GetComponent<PlayerMove>().hudElement.GetComponent<Text>().color = Color.yellow;
					GetComponent<PlayerMove>().hudElement.GetComponent<Text>().text = "Health Taken: " + Mathf.FloorToInt(thw.healthTaken);
				}
			}
		}
		// Phase shift (isn't an unlock) OR speed bost (isn't an unlock)
		if ((classNum == 0 || classNum == 3) && GetComponent<PlayerMove>().hudElement != null)
		{
			GetComponent<PlayerMove>().setCharge(classNum);

			if (GetComponent<PlayerMove> ().hudElement2 != null) {
				GetComponent<PlayerMove> ().hudElement2.transform.Find("Armor").GetComponent<Text> ().text = "Armor" + " (" + OptionsMenu.getBindString(OptionsMenu.binds[OptionsMenu.MAIN_ABILITY]) + ")";
				PlacePlayerMade.setCharge (GetComponent<PlayerMove> ().hudElement2, GetComponent<PlayerMove> ().isArmorOn, 1f);
				if (GetComponent<PlayerMove> ().isArmorOn == 0) {
					GetComponent<PlayerMove> ().hudElement2.GetComponent<Image> ().color = new Color (0f, 0.7f, 0f);
				} else {
					GetComponent<PlayerMove> ().hudElement2.GetComponent<Image> ().color = new Color (133f / 255f, 133f / 255f, 133f / 255f);
				}

			}
		}
	}

	public void midSwitch()
	{
		if (getWhichUnlock () != null) {
			getWhichUnlock ().midDisable ();
		}
		getNextUnlock().midEnable();
	}

	public void defaultSetup(bool runEffects)
	{
		SetNextUnlock(UnlocksEquipped[0].getUnlockType(), runEffects, true);
	}

}
