using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// Player as stored by the server. And self
public class Player : SyncGameState {

	public static short myID = 9; // This is set by server, and it is the first thing the player recieves. 
	//It is only once the player has recieved "Player" that connected is considered to be true. (Player will be recieved on the first GameState)


	// When you connect, to be "connected" you recieve your "Player", thus if you are connected, you have a "Player"
	public static Player thisPlayer = null;



	public GameObject[] playerObjects; // Set on inspector
	public GameObject[] facadePlayerObjects; // Set on inspector

	public string playerName = "n/a";

	public byte team = 0; // 2 = spectator.
	public byte classNum = 0;

	public short killedByPlayer = OperationNetwork.ServerObject;

	public short kills;
	public short deaths;

	int respawnTimerStartedAt = -1; // = -1;
									// bool alive?


	// Not all of this is stored on clients, but a lot of it is, sometimes in unconventional syncing ways.

	// A reference to the player camera: (a spectater has a "Player" object as well)
	public GameObject playerCamera = null;

	// PlayerObject should be set through "initStart" of player. (todo) -also set it to null on death.
	public GameObject playerObject = null;

	// Used on server only, but these could be saved for anyone.
	public ArrowLauncher arrowLauncher = null;
	public ArrowTrigger arrowTrigger = null;

	public byte[] myRandomTrapChoices = { 255, 255, 255, 255, 255, 255 }; // 2 * 3 of them. Set by server for every player on round change

	public byte[] trapTypes = {255, 255}; // You can select up to (2) traps at a time. This seems reasonable. Selecting between trap / trigger is done with a key; and instead of switching you to default, it will you switch you to the other trap / trigger after you've place the trap / trigger.
	// traps have coolDowns..
	// to be sure, the cool down is entirely predicted..
	public float[] trapCoolDownsStartedAt = {-10000, -10000}; // This is iterated to the maximum held traps


	public float[] resetTrapCoolDownsTo = {-10000,-10000}; // SERVER only
	public float serverPlayerTimeSave = 0f;
	public float timeSavedAt = 0f;

	Dictionary<Type, float> coolDownsStartedAt = new Dictionary<Type, float>(); // Maintain coolDowns on PlacePlayerMades. Really only relevant for sentry. 

	// Server

	// This is done once per round
	public void setTrap(int index, byte trap) {
		trapTypes [index] = trap;
	}

	// This is how you make an object into a "Player" object so it doesn't run on the interp simulation
	public override short getPreditivePlayerOwner() {
		return playerOwner;
	}

	// Player only:
	// This uses Time.time rather than playerTime. It can easily adjust accordingly as playerTime = 0
	public float getCoolDownToSetTo(Type type) {
		if (coolDownsStartedAt.ContainsKey (type)) {
			return coolDownsStartedAt[type];
		} else {
			coolDownsStartedAt.Add (type, -1);
			return -1;
		}
	}

	// Note that icicle doesn't use this.
	// This uses Time.time rather than playerTime.
	public void setCoolDown(Type type, float time) {
		if (coolDownsStartedAt.ContainsKey (type)) {
			coolDownsStartedAt [type] = time;
		} else {
			Debug.LogError ("Error: Player -> setCoolDown, type not set! This will happen if the value 162500f changes for getCoolDown() for traps");
		}
	}

	public override void InitStart(bool isThisMine) {
		// Player exists ALWAYS, so InitStart will make the object either the player's object or not:


		// Note that "SetInformation" will be called WITHOUT connected = true.


		// OnConnect:
		if (OperationNetwork.isServer) {
			respawnTimerStartedAt = ServerState.tickNumber;
			setRandomTrapChoices ();
			serverPlayerTimeSave = 0f;
			timeSavedAt = Time.time; // Can't have people having traps right away..
		}
	}

	public void setRandomTrapChoices() {
		List<byte> choices = new List<byte> ();
		foreach (byte val in BuyNewTrap.trapIndecies.Keys) {
			choices.Add (val);
		}

		for (int i = 0; i < myRandomTrapChoices.Length; i++) {
			int randomIndex = UnityEngine.Random.Range(0, choices.Count - 1);
			myRandomTrapChoices [i] = choices[randomIndex];
			choices.RemoveAt (randomIndex);
		}
	}

	public void SwitchTeam(byte team, byte classNum) {
		if (team != this.team || this.classNum != classNum) {
			if (playerObject != null) {
				bool inSpawnRoom = playerObject.GetComponent<PlayerMove> ().isInSpawnRoom;
				playerObject.GetComponent<Combat> ().Kill (OperationNetwork.ServerObject, false);
				if (inSpawnRoom) {
					// Instant respawn:
					respawnTimerStartedAt -= (int)(getNetRespawnTimer () / Time.fixedDeltaTime);
				}
			}
			this.team = team;
			this.classNum = classNum;
		}
	}

	public override void AfterFirstInterp() {

		// First should check if player doesn't exist in case of double RPC send:
		GameManager.AddPlayer (playerOwner, this);

		if (playerOwner == myID) {
			Player.thisPlayer = this;
			// Set time picked last trap:
			// ..
			// Gives you a trap right away; actually.
			OptionsMenu.classSelectionMenuOpen = true;
			OptionsMenu.ChangeLockState ();

			// Player camera is set:
			if (Player.thisPlayer.playerCamera == null)
				Player.thisPlayer.playerCamera = GameObject.Find ("DeathCamera");
		}
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner) {
		if (isPlayerOwner) {
			return 9 + trapTypes.Length;
		} else {
			return 8;
		}
	}

	public override object getObjectThis(int num, bool isPlayerOwner) {
		switch (num)
		{
		case 0: return playerOwner;
		case 1: return playerName;
		case 2: return team;
		case 3: return classNum;
		case 4: return respawnTimerStartedAt;
		case 5: return killedByPlayer;
		case 6: return kills;
		case 7: return deaths;
		}
		if (isPlayerOwner) {
			switch (num) {
			case 8:
				return trapTypes [0];
			case 9:
				return trapTypes [1];
			case 10:
				return myRandomTrapChoices;
			}
		}
		return null;
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		playerOwner = (short)data [0];
		playerName = (string)data [1];
		team = (byte)data [2];
		classNum = (byte)data [3];
		respawnTimerStartedAt = (int)data [4];
		killedByPlayer = (short)data [5];
		kills = (short)data [6];
		deaths = (short)data [7];
		if (isThisMine) {
			// Get essential trap information:
			trapTypes = new byte[]{(byte)data [8], (byte)data [9]};
			myRandomTrapChoices = (byte[])data [10];
		return 9 + trapTypes.Length;
		}
		return 8;
	}

	public void attemptBuyTrap(byte row, byte type) {
		if (trapTypes [row] == 255) {
			GetComponent<OperationView> ().RPC ("BuyTrap", OperationNetwork.ToServer, row, type);
			return;
		}
	}

	// OperationRPC
	public void BuyTrap(byte row, byte type) {
		if (trapTypes [row] == 255) {
			setTrap (row, type);
			return;
		}
		// Pay cost
	}

	public float getRespawnTimer() {
		if (OperationNetwork.isServer) {
			return getNetRespawnTimer() - ServerState.getLifeTime (respawnTimerStartedAt);
		} else {
			return getNetRespawnTimer() - Interp.getLifeTime(respawnTimerStartedAt);
		}
	}

	public float getNetRespawnTimer() {
		return 2.0f; // Respawn timer of 2 seconds.
	}

	// Server only:
	public void StartRespawnTimer() {
		respawnTimerStartedAt = ServerState.tickNumber;
	}

	public override void ServerSyncFixedUpdate() {
		if (playerObject == null && team < 2 && ServerState.getLifeTime (respawnTimerStartedAt) > getNetRespawnTimer()) {
			SpawnPlayer();
		}
	}

	// Server side:
	void SpawnPlayer() {
		// Picks a spawn point here:
		GameObject sP = LevelLogic.getSpawnPoint (team);
		// Picks a spawn point here:
		GameObject playerObject = (GameObject)MonoBehaviour.Instantiate (playerObjects[team * (playerObjects.Length / 2) + classNum], sP.transform.position, sP.transform.rotation);
		playerObject.GetComponent<SyncGameState> ().playerOwner = playerOwner;
		OperationNetwork.OperationAddSyncState (playerObject);
	}

	public override void OnDeath ()
	{
		Debug.LogError ("Removal");
		// Remove ALL references to player!

		GameManager.RemovePlayer(playerOwner); // Voila.
	}
}
