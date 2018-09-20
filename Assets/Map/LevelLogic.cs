using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.UI;

public class LevelLogic : SyncGameState {
	// Everything in LevelLogic follows a naming / parenting convention to have most everything set, with the exception of stuff like spawn times.

	// LevelLogic also handles HUD that relates to the game. (Not including kill feed / scoreboard)

	public const int numSync = 4;

	public const float DecayRate = 0.2f; // Reverting capture rate.
	public const int startTime = 60;
	public const int roundTimer = 720; // 12 minutes. (Obviously should / can vary per map.)
	public const int betweenRoundTime = 7; //6000; 

	static GameObject gameInfoHud;

	static GameObject gameClock = null;

	// CAPTURE POINTS:
	static List<GameObject> hudPoints = new List<GameObject>();
	static List<GameObject> CapturePoints = new List<GameObject>(); // Set in InitStart
	static List<float> captureTime = new List<float>(); // Set in InitStart
	static List<byte> teamOwned = new List<byte>();  // Set in InitStart
	static List<float> captureProgress = new List<float>(); // Set in InitStart

	// SPAWN ROOMS:
	static List<List<List<GameObject>>> spawnPoints = new List<List<List<GameObject>>>();
	static List<List<GameObject>> spawnRooms = new List<List<GameObject>>();
	static List<List<GameObject>> spawnRoomWalls = new List<List<GameObject>>(); // The walls are primarily here to prevent phase shifting through the spawn room in start phase (?)

	// HEALTH PACKS / OTHER STUFF LIKE AMMO PICKUPS:
	static List<GameObject> healthPacks = new List<GameObject>();
	static int renderHealthPacks;

	int gameStartedAt = -1;
	byte lastWinner = 2;
	bool inGame = false;
	// The finality of it all is here. This IS the pre-instantiated object that derives from syncGameState.

	// It has been redone in place of MainCode due to massive code changes.

	public override void InitStart(bool isThisMine) {

	}

	// Start must be used for this because of getbitchoiceslength
	public static void InitLevelLogic() {
		hudPoints = new List<GameObject>();
		CapturePoints = new List<GameObject>();
		captureTime = new List<float>();
		teamOwned = new List<byte>();
		captureProgress = new List<float>();

		// The entire level is INTERPRETED by name.
		spawnPoints = new List<List<List<GameObject>>>();
		spawnRooms = new List<List<GameObject>>();
		spawnRoomWalls = new List<List<GameObject>>();

		healthPacks = new List<GameObject>();

		// We need to FIND the entire hud, so nothing has to be set by code or by inspector, just by name.
		// However, unlike player hud, this HUD is already ready to go.
		addTeamLists();
		Transform gr = GameObject.Find("GameRun").transform;

		gameInfoHud = GameObject.Find ("PlayerHud").transform.Find ("GameInfoHud").gameObject;
		gameClock = gameInfoHud.transform.Find ("GameClockBackground").Find ("GameClock").gameObject;

		for (int i = 0; i < gr.childCount; i++) {
			Transform c = gr.GetChild (i);
			if (c.name.StartsWith ("BlueSpawnPoint")) {
				addToList (c, spawnPoints[0]);
			} else if (c.name.StartsWith ("RedSpawnPoint")) {
				addToList (c, spawnPoints[1]);
			} else if (c.name.StartsWith ("BlueSpawn")) {
				addToList (c, spawnRooms[0]);
			} else if (c.name.StartsWith ("RedSpawn")) {
				addToList (c, spawnRooms[1]);
			} else if (c.name.StartsWith ("BlueWall")) {
				addToList (c, spawnRoomWalls[0]);
			} else if (c.name.StartsWith ("RedWall")) {
				addToList (c, spawnRoomWalls[1]);
			} else if (c.name.StartsWith ("CapPoint")) {
				addToList (c, CapturePoints);
			}
		}

		for (int i = 0; i < CapturePoints.Count; i++) {
			captureTime.Add (int.Parse(CapturePoints [i].name.Substring (CapturePoints [i].name.LastIndexOf ("_") + 1, CapturePoints [i].name.LastIndexOf ("S") - CapturePoints [i].name.LastIndexOf ("_") - 1)));
			teamOwned.Add (1); // Owner starts off as red
			captureProgress.Add (0f);
			hudPoints.Add (gameInfoHud.transform.Find ("CapturePoint" + (i + 1)).Find("Charge").gameObject);
		}

		Transform iterator = gr.Find("HealthPacks").transform;

		int size = iterator.childCount;
		for (int i = 0; i < iterator.childCount; i++)
		{
			healthPacks.Add(iterator.GetChild(i).gameObject);
			healthPacks[i].GetComponent<HealthPack>().healthPackID = (byte)i;
		}
		renderHealthPacks = 0;
	}

	public static void addTeamLists() {
		spawnPoints.Add(new List<List<GameObject>>());
		spawnPoints.Add(new List<List<GameObject>>());
		spawnRooms.Add(new List<GameObject>());
		spawnRooms.Add(new List<GameObject>());
		spawnRoomWalls.Add(new List<GameObject>());
		spawnRoomWalls.Add(new List<GameObject>());
	}

	public static void addToList(Transform t, List<GameObject> g) {
		int num = getNumber (t.name);
		while (g.Count <= num) {
			g.Add (null);
		}
		g[num] = t.gameObject;
	}

	public static void addToList(Transform t, List<List<GameObject>> g) {
		int num = getNumber (t.name);
		while (g.Count <= num) {
			g.Add (new List<GameObject>());
		}
		g[num].Add (t.gameObject);
	}

	public static int getNumber(String name) {
		return int.Parse(name.Substring(name.LastIndexOf("S") + 1)) - 1;
	}

	// We can rely on the fact that the same map is loaded on the client and server
	public override int getBitChoicesLengthThis(bool isPlayerOwner) {
		return numSync + CapturePoints.Count * 2;
	}

	public override object getObjectThis(int num, bool isPlayerOwner) {
		switch (num)
		{
		case 0: return gameStartedAt;
		case 1: return lastWinner;
		case 2: return BitConverter.GetBytes(inGame)[0];
		case 3: return renderHealthPacks;
		default: 
			if (num < numSync + captureProgress.Count) {
				return captureProgress [num - numSync];
			} else {
				return teamOwned[num - numSync - captureProgress.Count];
			}
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		gameStartedAt = (int)data [0];
		lastWinner = (byte)data [1];

		inGame = BitConverter.ToBoolean(new byte[]{(byte)data [2]}, 0);

		int newRHP = (int)data [3];
		if (renderHealthPacks != (int)data [3]) {
			
			for (int i = 0; i < healthPacks.Count; i++) {
				if (((renderHealthPacks & (1 << i)) != 0) && ((newRHP & (1 << i)) == 0)) {
					// Health pack has just been taken. This is a prominent example of how changing in states should trigger stuff like sounds, etc.
					SoundHandler.soundHandler.PlayHealthPackTakenSound(healthPacks[i].transform); // Only needs to play on clients, of course
				}
				healthPacks[i].GetComponent<Renderer> ().enabled = (newRHP & (1 << i)) != 0;
			}
			renderHealthPacks = (int)data [3];
		}

		for (int i = 0; i < captureProgress.Count; i++) {
			captureProgress [i] = (float)data [numSync + i];
			teamOwned [i] = (byte)data [numSync + captureProgress.Count + i];
		}
		return numSync + captureProgress.Count * 2;
	}

	public override void ServerSyncFixedUpdate() {
		ServerGameUpdate ();

		// Health packs:

		renderHealthPacks = 0;
		for (int i = 0; i < healthPacks.Count; i++) {
			if (healthPacks [i].GetComponent<HealthPack> ().exists()) {
				renderHealthPacks += 1 << i;
			}
		}
	}

	void ServerGameUpdate() {
		if (inGame) {
			// Fully check the cap points:
			for (int i = 0; i < CapturePoints.Count; i++) {
				if (teamOwned [i] == 0) // Cap point cannot be recapped once owned by the attackers (blue)
					continue;
				
				if (teamOwned [i] == 1) {

					Collider[] players = Physics.OverlapBox (CapturePoints [i].transform.position, CapturePoints [i].transform.lossyScale / 2f, Quaternion.identity, 1 << 8 | 1 << 22);

					int bluePlayers = 0;
					int redPlayers = 0;
					for (int n = 0; n < players.Length; n++) {
						if (players [n] is UnityEngine.CapsuleCollider) { // If not phasing, you can cap. Also, as to avoid hitting the same player twice, only capsule collider is used.
							if (players [n].gameObject.GetComponent<Combat> ().team == 0) {
								bluePlayers++;
							} else {
								redPlayers++;
							}
						}
					}
					// Cap Logic:
					if (redPlayers > 0) {
						if (bluePlayers == 0) {
							// Revert capture:
							captureProgress [i] -= Time.fixedDeltaTime * DecayRate * 2; // Triple the decay rate.
						} else {
							// Halt capture:
							captureProgress [i] += Time.fixedDeltaTime * DecayRate; // As to cancel out the decay.
						}
					} else if (bluePlayers > 0) {
						captureProgress [i] += Time.fixedDeltaTime * DecayRate; // As to cancel out the decay.
						float addRate = 1;
						for (int p = 0; p < bluePlayers; p++) {
							captureProgress [i] += Time.fixedDeltaTime * addRate;
							addRate *= 0.6f;
						}
					}
					if (captureProgress [i] > captureTime [i]) {
						captureProgress [i] = captureTime [i]; // So it interps correctly. This value becomes irrelivant once the point is capped by blue team.
						teamOwned [i] = 0;
						if (i == CapturePoints.Count - 1) {
							// Win the game:
							GameOver ((byte)0);
							return;
						}
						break; // Wait until next tick for next point to "open up"
					}
					captureProgress [i] -= Time.fixedDeltaTime * DecayRate;
					if (captureProgress [i] < 0) {
						captureProgress [i] = 0;
					}
					break; // The next cap points can not be capped until this one is capped.
				}
			}


			// The ability to fully restart the game..
			if (ServerState.getLifeTime (gameStartedAt) > roundTimer) { // 4 minutes to cap currently.
				// Game over. No overtime currently.
				GameOver ((byte)1); // Winner = red (1, defenders)
				return;
			}
		} else {
			if (ServerState.getLifeTime (gameStartedAt) > betweenRoundTime) {
				StartGame ();
			}
		}
	}

	public static GameObject getSpawnPoint(int team) {
		for (int i = 0; i < teamOwned.Count; i++) {
			if (teamOwned [i] == 0) {
				if (i == teamOwned.Count - 1)
					i--;
				return spawnPoints [team] [i + 1][0]; // Defaults to 1 spawn for now.. TODO
			}
		}
		return spawnPoints [team] [0][0];
	}

	// Server side only
	void StartGame() {
		inGame = true;
		gameStartedAt = ServerState.tickNumber;
		for (int i = 0; i < CapturePoints.Count; i++) {
			teamOwned[i] = 1; // Owner starts off as red
			captureProgress[i] = 0f;
		}
	}

	// Server side only
	void GameOver(byte winner) {
		inGame = false;
		lastWinner = winner;
		gameStartedAt = ServerState.tickNumber;

		// Kills every player, destroys every player made object:
		ResetGame();
	}

	// There is a way to do this in which every player related object is destroyed, but server owned objects are a possibility.
	void ResetGame() {

		// Reset credits:
		List<Player> players = GameManager.getPlayersOnTeam (1);
		foreach (Player player in players) {
			player.setRandomTrapChoices (); // Resets everyone's traps choices, regardless of team.
			for (int i = 0; i < player.trapTypes.Length; i++) {
				player.trapTypes [i] = 255;
			}
		}

		Transform iterator = GameObject.Find ("Players").transform;

		for (int i = 0; i < iterator.childCount; i++)
		{
			GameObject plyr = iterator.GetChild(i).gameObject;
			plyr.GetComponent<Combat>().Kill(OperationNetwork.ServerObject, false); // Overrides phase shift. (Does not record this death). Does not instantly respawn player... hmm.. todo! Note that this could cause an infinite loop as the server player would keep respawning / dieing.
		}

		// ALL player made items need to be cleared:
		iterator = GameObject.Find("PlayerMade").transform;

		for (int i = 0; i < iterator.childCount; i++)
		{
			SyncGameState obj = iterator.GetChild(i).GetComponent<SyncGameState>();
			obj.exists = false;
		}

		iterator = GameObject.Find("Miscellaneous").transform;

		for (int i = 0; i < iterator.childCount; i++)
		{
			SyncGameState obj = iterator.GetChild(i).GetComponent<SyncGameState>();
			obj.exists = false;
		}
	}

	// Update handles updating HUD for everyone: PlayerHud still handles most of the hud
	// This is not just HUD code, it contains some code for enabling starting walls
	void Update() {
		if (OperationNetwork.connected) {
			
			float lifeTime;
			if (OperationNetwork.isServer) {
				lifeTime = ServerState.getLifeTime (gameStartedAt);
			} else {
				lifeTime = Interp.getLifeTime (gameStartedAt);
			}

			if (gameClock != null) {
				if (inGame) {
					int timeLeft = (int)(roundTimer - lifeTime);
					gameClock.GetComponent<Text> ().text = timeLeft / 60 + ":" + (timeLeft % 60).ToString ("00") + " remaining";
				} else {
					int timeLeft = (int)(betweenRoundTime - lifeTime);
					gameClock.GetComponent<Text> ().text = timeLeft / 60 + ":" + (timeLeft % 60).ToString ("00") + " until next game starts";
				}
				for (int i = 0; i < CapturePoints.Count; i++) {
					float capPercent = captureProgress [i] / captureTime [i];
					if (teamOwned [i] == 0) {
						capPercent = 1;
					}
					hudPoints [i].transform.parent.GetComponent<Image> ().fillAmount = capPercent;
					hudPoints [i].GetComponent<Image> ().fillAmount = 1 - capPercent;
				}
			}
				
			spawnRoomWalls [0] [0].GetComponent<Collider> ().enabled = !inGame || lifeTime < startTime;
			spawnRoomWalls [0] [0].GetComponent<Renderer>().enabled = !inGame || lifeTime < startTime;
		}
	}

	public override void OnDeath ()
	{

	}
}
