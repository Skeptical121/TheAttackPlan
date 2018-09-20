using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// SyncPlayer depends on Player for it's initStart (and first SetInformation to do setPlayerOwner)
public class SyncPlayer : SyncGameState {

	public GameObject damageNumber;

	public float playerTime = 0; // This is the running counter of frameTime.
	// Thus, time changes accordingly.

	bool ranBeforeFirstInterp = false;


	public int lastTickLoaded = -1; // This is for triggers

	public TriggerSet currentTriggerSet = new TriggerSet();
	public TriggerSet playerTriggerSet = new TriggerSet();

	public const byte FIRE_TRIGGER = 0;
	public const byte SECONDARY_FIRE_TRIGGER = 1;
	public const byte RELOAD_TRIGGER = 2;
	public const byte HITSCAN = 3; // This will be sent multiple times if there is multiple hitscan bullets; thus shotgun sends a lot
	public const byte JUMP_TRIGGER = 4;

	// PLayer triggers
	public const byte DAMAGE_NUMBER = 0;
	public const byte DAMAGE_TAKEN = 1;

	void Update() {

		if (GetComponent<PlayerMove> ().thisIsMine) {
			// Get Input Commands. ALL of them. Turns out stuff like hitscan that causes damage needs to send all this information.

			GetComponent<PlayerMove>().PlayerMoveRun ();
			OperationNetwork.CheckForPlayerInputSend ();

			// Send data to server.

		}
	}

	// Certain triggers will have a variable amount of data
	public override object getTriggerData(byte index, byte[] data, ref int bytePosition, bool isPlayerOwner) {
		if (!isPlayerOwner) {
			if (index == HITSCAN) {
				return DataInterpret.interpretObject (data, ref bytePosition, new Vector3[]{ }, null, -1, false);
			}
		} else {
			if (index == DAMAGE_NUMBER) {
				return new DamageNumber (data, ref bytePosition);
			} else if (index == DAMAGE_TAKEN) {
				return new DamageNumber(data, ref bytePosition);
			}
		}
		return null;
	}

	// Player side - this passes the data to compareData in playerMove.

	// All
	public override void InitStart(bool isThisMine) {

		foreach (Transform child in transform) {
			if (child.name.Contains ("PersonModelObject") || child.name.Contains ("Armor") || child.name.Contains ("Cube") || child.name.Contains ("Cylinder") || child.name.Contains ("Curve")) {
				child.gameObject.GetComponent<SkinnedMeshRenderer> ().updateWhenOffscreen = true; // yawn
			}
		}

		GetComponent<PlayerAnimation> ().InitStartAnimation ();

		damageNumber = Resources.Load ("DamageNumber") as GameObject;
		GetComponent<Combat> ().damageIndicatorPrefab = Resources.Load ("DamageIndicatorObject") as GameObject;


		transform.parent = GameObject.Find("Players").transform;

		if (OperationNetwork.isServer) {
			if (GameManager.PlayerExists (playerOwner)) {
				float subtract = GameManager.GetPlayer (playerOwner).serverPlayerTimeSave + Time.time - GameManager.GetPlayer (playerOwner).timeSavedAt;
				for (int i = 0; i < GameManager.GetPlayer (playerOwner).trapCoolDownsStartedAt.Length; i++) {
					GameManager.GetPlayer (playerOwner).trapCoolDownsStartedAt [i] -= subtract;
				}
			}

			beforeFirstInterp ();
		}

		if (GetComponent<ClassControl> ().classNum == 0) {
			if (!OperationNetwork.isHeadless) {
				GetComponent<PlayerAnimation>().SetArmorRender (GetComponent<PlayerMove>().isArmorOn);
			}
		}

		// Switching teams should be handled by PlayerListHandler / Player. (Where player name, player team, player kills, player deaths, etc. is stored)
	}

	public void beforeFirstInterp() {
		

		if (playerOwner == Player.myID) {
			GetComponent<PlayerMove> ().thisIsMine = true;
		} else if (OperationNetwork.isServer) {
			GetComponent<PlayerMove>().mainCamera = new GameObject();

			GetComponent<PlayerMove>().mainCamera.transform.parent = transform;
			GetComponent<PlayerMove>().mainCamera.transform.localPosition = GetComponent<PlayerMove>().GetMainCameraLocalPos();
		}
			
		if (GameManager.PlayerExists (playerOwner)) {
			GameManager.GetPlayer (playerOwner).playerObject = gameObject;
		}

		GetComponent<PlayerMove> ().plyr = playerOwner;
		GetComponent<ClassControl> ().InitStartClass ();


		GetComponent<PlayerMove> ().playerView = new PlayerView (GetComponent<PlayerMove>(), playerOwner == Player.myID); // SetOwner



		if (GameManager.PlayerExists (playerOwner)) {
			GameManager.GetPlayer (playerOwner).playerCamera = GetComponent<PlayerMove> ().mainCamera;
		}
	}

	// Client, Player, and Server. Returns a time in seconds.
	public float getTotalTimeSince(float num)
	{
		if (num == -1)
		{
			// -1 is "invalid", as in it happened a very long time ago. >15 seconds is generally not relevant anymore.
			return 625000f;
		}
		if (GetComponent<PlayerMove>().thisIsMine) // Player gets priority over Server
		{
			return playerTime - num; // This would have to be switched to "playerTime" if predictionErrors rely on this.
		} else if (OperationNetwork.isServer)
		{
			return playerTime - num;
		} else
		{
			return Interp.getLifeTime((int)num);
		}
	}

	public float getTime() {
		if (GetComponent<PlayerMove> ().thisIsMine) {
			return playerTime; //Time.time;
		} else if (OperationNetwork.isServer) {
			return playerTime;
		} else {
			return Interp.getTickNumber (); // Hmm
		}
	}

	public override void AfterFirstInterp() {
		GetComponent<PlayerAnimation> ().AfterFirstInterpAnimation ();
		GetComponent<ClassControl> ().AfterFirstInterpClass ();
		
		// This is a particularly handy setting that can't be set in the editor:
		if (GetComponent<PlayerMove>().thisIsMine || OperationNetwork.isServer)
			GetComponent<CharacterController>().enableOverlapRecovery = true;
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner) {
		if (isPlayerOwner) {
			return getPlayerStateStartIndex() + PlayerState.getBitChoicesLength(GetComponent<Combat>().team, GetComponent<ClassControl>().classNum);
		} else {
			if (GetComponent<ClassControl> ().classNum == 1)
				return 9;
			else
				return 8;
		}
	}

	// This is how you make an object into a "Player" object so it doesn't run on the interp simulation
	public override short getPreditivePlayerOwner() {
		return playerOwner;
	}

	int getPlayerStateStartIndex() {
		if (GetComponent<ClassControl> ().classNum == 4)
			return 5;
		else
			return 4;
	}

	// There is a limited amount of information that must be sent to only the player. 
	// ALL of this information can come in multiple / none form, with the exception of Predi
	// It includes:
	/*
	PredictionError
	DamageNumber Callback
	DamageHit From (Vector3 / amount)
	*/
	public override object getObjectThis(int num, bool isPlayerOwner) {
		if (!isPlayerOwner) {
			switch (num) {
			case 0:
				return playerOwner; // This has to be first.
			case 1:
				return (short)Mathf.CeilToInt (GetComponent<Combat> ().health);
			case 2:
				return transform.position;
			case 3:
				return new YRotation (transform.eulerAngles.y);
			case 4: 
				return new UpDownRotation (-GetComponent<PlayerMove> ().mainCamera.transform.eulerAngles.x);
			case 5:
				return GetComponent<ClassControl> ().nextUnlock;
			case 6:
				return currentTriggerSet;
			case 7:
				// Boolean data for player:
				byte returnByte = 0;
				if (GetComponent<PlayerMove> ().isGrounded)
					returnByte += (byte)(1 << 0);
				if (GetComponent<PlayerMove> ().isCrouched)
					returnByte += (byte)(1 << 1);
				if (GetComponent<PlayerMove> ().isPhasing () || GetComponent<PlayerMove> ().isSpeedBoosting ())
					returnByte += (byte)(1 << 2);
				if (GetComponent<PlayerMove> ().puttingArmorOn)
					returnByte += (byte)(1 << 3);
				return returnByte;
			case 8:
				if (GetComponent<ClassControl> ().classNum == 1)
					return GetComponent<ClassControl> ().getUnlockEquippedWithType<Pistol> ().mode;
				else
					return null;
			default:
				return null;
			}
		} else {
			switch (num) {
			case 0:
				return playerOwner; // This has to be first.
			case 1:
				return (short)Mathf.CeilToInt (GetComponent<Combat> ().health);
			case 2:
				return playerTriggerSet;
			case 3:
				if (OperationNetwork.isServer)
					return OperationNetwork.getClient (playerOwner).lastPlayerInputGroupID;
				else
					return (short)0; // This is just used for type anyways
			default:
				if (GetComponent<ClassControl> ().classNum == 4 && num == 4)
					return GetComponent<ClassControl> ().getHealthTaken ();
				return PlayerState.getObject(num - getPlayerStateStartIndex(), this, GetComponent<PlayerMove>());
			}
		}
	}
		
	// This will set the player's position before setOwner() is called.
	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		playerOwner = (short)data [0];
		GetComponent<PlayerMove> ().plyr = playerOwner;

		if (!ranBeforeFirstInterp) {
			ranBeforeFirstInterp = true;
			beforeFirstInterp ();
		}

		if (isThisMine) {
			// Absolutely NO interp is done when isThisMine = true!!

			GetComponent<Combat> ().health = (short)data [1]; // Duplicate because Player needs this information as well as Clients. (On a non-Prediction Error basis, as it is not predicted)

			// This is only called when new data is received.. although that data can be identical since no frames were simulated..

			if (GetComponent<ClassControl> ().classNum == 4)
				GetComponent<ClassControl> ().setHealthTaken ((float)data [4]); // health taken is kind of like health in terms of how it syncs

			playerTriggerSet.AddTriggerSet ((TriggerSet)data [2]);

			// Trigger triggers:
			for (int i = 0; i < playerTriggerSet.triggerIndecies.Count; i++) {
				byte index = playerTriggerSet.triggerIndecies [i];
				int triggerTick = playerTriggerSet.triggerTickNumbers [i];
				object obj = playerTriggerSet.triggerData [i];
				if (index == DAMAGE_NUMBER) {
					DamageNumber d = (DamageNumber)obj;
					Vector3 spawnPos = d.posData + new Vector3 (UnityEngine.Random.Range (-0.1f, 0.1f), 1f, UnityEngine.Random.Range (-0.1f, 0.1f));
					GameObject dmgNum = (GameObject)Instantiate (damageNumber, spawnPos, Quaternion.identity);
					dmgNum.GetComponent<LookAtPlayer> ().DamageDone ((short)d.damage); // Hitsound / Setting of text
					//dmgNum.
					Destroy (dmgNum, 0.5f); // Could be effected by gravity as well
				}

				if (index == DAMAGE_TAKEN) {
					DamageNumber d = (DamageNumber)obj;
					GetComponent<Combat> ().addDamageIndicator (d);
				}
			}

			object[] finalObjects = new object[data.Length - getPlayerStateStartIndex()];
			Array.Copy (data, getPlayerStateStartIndex(), finalObjects, 0, data.Length - getPlayerStateStartIndex());

			GetComponent<PlayerMove> ().predictionErrorTest.testForPredictionError ((short)data [3], finalObjects, GetComponent<PlayerMove> ());

			return getBitChoicesLengthThis (true); // This is discarded, so it's not important.
		} else {


			GetComponent<Combat> ().health = (short)data [1];
			transform.position = (Vector3)data [2];
			transform.eulerAngles = new Vector3 (transform.eulerAngles.x, ((YRotation)data [3]).interpValue, transform.eulerAngles.z);
			transform.GetComponent<PlayerMove> ().currentPlayerRotUpDown = ((UpDownRotation)data [4]).interpValue;

			GetComponent<ClassControl> ().clientInterp ((byte)data [5]);

			Unlock nextUnlock = null;
			if (!GetComponent<ClassControl> ().isSwitching ()) {
				nextUnlock = GetComponent<ClassControl> ().getUnlockEquipped ();
			}

			// Client side uses a global representation of Unlock:
			currentTriggerSet.AddTriggerSet ((TriggerSet)data [6]);

			// Trigger triggers:
			for (int i = 0; i < currentTriggerSet.triggerIndecies.Count; i++) {
				byte index = currentTriggerSet.triggerIndecies [i];
				int triggerTick = currentTriggerSet.triggerTickNumbers [i];
				object obj = currentTriggerSet.triggerData [i];
				if (index == FIRE_TRIGGER) {
					if (nextUnlock != null)
						nextUnlock.FireTriggered (true, triggerTick);
				} else if (index == SECONDARY_FIRE_TRIGGER) {
					if (nextUnlock != null)
						nextUnlock.SecondaryFireTriggered (true, triggerTick);
				} else if (index == RELOAD_TRIGGER) {
					if (nextUnlock != null && nextUnlock is GunScript)
						((GunScript)nextUnlock).ReloadTriggered (true, triggerTick);
				} else if (index == HITSCAN) {
					if (nextUnlock != null && nextUnlock is HitscanGun)
						((HitscanGun)nextUnlock).hitscanData ((Vector3[])obj);
				}
			}
				
			GetComponent<PlayerMove> ().isGrounded = (((byte)data [7] & (1 << 0)) != 0);
			GetComponent<PlayerMove> ().isCrouched = (((byte)data [7] & (1 << 1)) != 0);
			GetComponent<PlayerMove> ().ClientPhasingOrSpeedBoosting = (((byte)data [7] & (1 << 2)) != 0);
			GetComponent<PlayerMove> ().puttingArmorOn = (((byte)data [7] & (1 << 3)) != 0);

			if (GetComponent<ClassControl> ().classNum == 1)
				GetComponent<ClassControl> ().getUnlockEquippedWithType<Pistol> ().setMode ((byte)data [8]);

			GetComponent<PlayerMove> ().UpdateCrouchedRep (Interp.lastDeltaTickNumber * Time.fixedDeltaTime);
			GetComponent<PlayerMove> ().UpdateArmorRep (Interp.lastDeltaTickNumber * Time.fixedDeltaTime);

			// Client updates:
			GetComponent<PlayerMove>().PlayerMoveRun ();

			return 9; // This is discarded, so it's not important.
		}
	}

	public override void ServerSyncFixedUpdate() {
		// This is a system in which damage #s / damage taken might be sent 1 tick late; however, it should work.
		if (GetComponent<ClassControl> ().isBot) {
			// This is Server only, of course
			GetComponent<AI> ().onFixedUpdate ();
		}
		GetComponent<PlayerAnimation> ().savePlayerHitBoxPositionsOnServer ();
	}

	public override void OnDeath ()
	{

		// Death is a necessary process regardless of if the ragdoll should spawn, or even if the deathCamera will be used.
		if (OperationNetwork.isServer && GameManager.PlayerExists (playerOwner)) {
			GameManager.GetPlayer(playerOwner).StartRespawnTimer ();
			GameManager.GetPlayer (playerOwner).serverPlayerTimeSave = playerTime;
			GameManager.GetPlayer (playerOwner).timeSavedAt = Time.time;
		}

		// DEATH LOGIC:
		if (GetComponent<PlayerMove> ().thisIsMine) {

			Player.thisPlayer.playerObject = null;

			OptionsMenu.ChangeLockState ();

			Player.thisPlayer.playerCamera = GameObject.Find ("DeathCamera");

			GameObject.Find ("DeathCamera").GetComponent<Camera> ().enabled = true;
			GameObject.Find ("DeathCamera").GetComponent<AudioListener> ().enabled = true;
			GameObject.Find ("DeathCamera").transform.position = GetComponent<PlayerMove> ().mainCamera.transform.position;
			GameObject.Find ("DeathCamera").transform.rotation = GetComponent<PlayerMove> ().mainCamera.transform.rotation;


			// Destroys player made objects that haven't been placed, essentially:
			Transform clientSideOnly = GameObject.Find ("ClientSideOnly").transform;
			foreach (Transform child in clientSideOnly) {
				Destroy (child.gameObject);
			}
		}
			
		// Create ragdoll: (And plays death sound within ragdoll)

		// if facade, teleport to facade:

		GetComponent<Combat>().die(Vector3.zero);

		if (GameManager.PlayerExists (playerOwner)) {
			GameManager.AddToKillFeed (GameManager.GetPlayer (playerOwner).killedByPlayer, playerOwner);
		}
	}


}