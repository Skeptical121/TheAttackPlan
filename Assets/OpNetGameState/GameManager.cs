using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {

	public GameObject levelSyncToSet; // Set in editor.
	public static GameObject levelSync;

	public static GameManager thisGameManager;

	public GameObject playerSyncToSet;
	public static GameObject playerSync;
	// Player is DEFINED by GameManager, and thus, Player has "control" over GameManager.

	public static List<short> recentPlayerKillers = new List<short>();
	public static List<short> recentPlayerDeaths = new List<short>();
	public static List<int> playerKillTickTimes = new List<int>();

	// GameManager has access to LevelLogic.

	static Dictionary<short,Player> players;

	void Start() {
		playerSync = playerSyncToSet;
		levelSync = levelSyncToSet;
		thisGameManager = this;

		recentPlayerKillers = new List<short>();
		recentPlayerDeaths = new List<short>();
		playerKillTickTimes = new List<int>();

		players = new Dictionary<short,Player>();
	}

	public static List<Player> getPlayersOnTeam(byte team) {
		List<Player> teamPlayers = new List<Player> ();
		foreach (Player player in players.Values) {
			if (player.team == team) {
				teamPlayers.Add (player);
			}
		}
		// Sort teamPlayers:
		teamPlayers.Sort((x, y) => string.Compare(x.playerName, y.playerName));
		return teamPlayers;
	}
		
	public static void PlayerStat(short killer, short death) {
		if (killer != death && PlayerExists (killer)) {
			GetPlayer (killer).kills++;
		}
		if (PlayerExists (death)) {
			GetPlayer (death).deaths++;
		}
	}

	public static void AddToKillFeed(short killer, short death) {
		recentPlayerKillers.Add (killer);
		recentPlayerDeaths.Add (death);
		if (OperationNetwork.isServer) {
			playerKillTickTimes.Add (ServerState.tickNumber);
		} else {
			playerKillTickTimes.Add (Interp.getTickNumber());
		}
		UpdateKillFeed ();
	}

	// There will be a maximum of 6 elements in the list:
	public static void UpdateKillFeed() {
		int maximumDisplay = 5;
		for (int i = 0; i < playerKillTickTimes.Count; i++) {
			float maxTime = 5f;
			if (Player.thisPlayer != null && (recentPlayerKillers [i] == Player.thisPlayer.playerOwner || recentPlayerDeaths [i] == Player.thisPlayer.playerOwner)) {
				maxTime = 10f;
			}
			if (OperationNetwork.isServer) {
				if (ServerState.getLifeTime (playerKillTickTimes [i]) > maxTime) {
					recentPlayerKillers.RemoveAt (i);
					recentPlayerDeaths.RemoveAt (i);
					playerKillTickTimes.RemoveAt (i);
				}
			} else {
				if (Interp.getLifeTime (playerKillTickTimes [i]) > maxTime) {
					recentPlayerKillers.RemoveAt (i);
					recentPlayerDeaths.RemoveAt (i);
					playerKillTickTimes.RemoveAt (i);
				}
			}
		}
		if (playerKillTickTimes.Count > 5) {
			// Remove above 5!!
			int indexOfLongestTime = 0;
			float longestTime = 0f;
			for (int i = 0; i < playerKillTickTimes.Count; i++) {
				float lifeTime;
				if (OperationNetwork.isServer) {
					lifeTime = ServerState.getLifeTime (playerKillTickTimes [i]);
				} else {
					lifeTime = Interp.getLifeTime (playerKillTickTimes [i]);
				}
				if (lifeTime > longestTime) {
					longestTime = lifeTime;
					indexOfLongestTime = i;
				}
			}
			recentPlayerKillers.RemoveAt (indexOfLongestTime);
			recentPlayerDeaths.RemoveAt (indexOfLongestTime);
			playerKillTickTimes.RemoveAt (indexOfLongestTime);
		}
	}

	// InitialConnect is key.
	public static void InitialConnect() {

		if (OperationNetwork.isServer) {
			GameObject lSync = Instantiate (levelSync);
			OperationNetwork.OperationAddSyncState (lSync);

			OperationNetwork.connected = true; // Server connection works like this.
			PlayerConnect(PlayerInformation.steamName, OperationNetwork.FromServerClient);
		} else {
			// Sends out request for "Connect" (with name). ID is sent to the player.
			OperationView.RPC(null, "PlayerConnect", OperationNetwork.ToServer, PlayerInformation.steamName);
		}
	}

	// Server only. This is called manually by the server.
	// OperationRPC
	public static void PlayerConnect(string name, short who) // Information that should be sent to new players: team, kills, deaths
	{

		if (who != OperationNetwork.FromServerClient) {
			OperationNetwork.getClient (who).connected = true; // This is so no data is sent before the following data:

			OperationNetwork.sendDataToSpecificClient (BitConverter.GetBytes (who), who);
		}

		GameObject pSObject = MonoBehaviour.Instantiate (playerSync);
		// Set ID, name, etc. (todo)
		pSObject.GetComponent<Player> ().playerOwner = who;
		pSObject.GetComponent<Player> ().playerName = name;
		pSObject.GetComponent<Player> ().team = 0; // Default (Could be based on an "autobalance" system.
		pSObject.GetComponent<Player> ().classNum = 0; // Default (Could be random)
		OperationNetwork.OperationAddSyncState (pSObject);

	}

	public static void AddPlayer(short playerOwner, Player player) {
		players.Add (playerOwner, player);
	}

	public static void RemovePlayer(short playerOwner) {
		players.Remove (playerOwner);
	}

	public static Player GetPlayer(short playerOwner) {
		if (players.ContainsKey(playerOwner)) {
			return players [playerOwner];
		} else {
			Debug.LogError ("NEED TO CHECK if Player Exists before calling this! GameManager -> GetPlayer");
			return null;
		}
	}

	public static bool PlayerExists(short playerOwner) {
		return players.ContainsKey (playerOwner);
	}
		
	public static void DestroyPlayerRelated(short playerOwner) {
		if (PlayerExists (playerOwner)) {
			Player player = GetPlayer (playerOwner);
			if (player.playerObject) {
				player.playerObject.GetComponent<Combat> ().Kill (OperationNetwork.ServerObject, false); // Doesn't record stat.. hmm.. (so it only sets exists = false)
			}
			foreach (SyncGameState sgs in OperationNetwork.operationObjects) {
				if (sgs != null && sgs.playerOwner == playerOwner) {
					sgs.exists = false; // This'll set exists to false again for the player. It'll also delete the player..
				}
			}
		}
	}

	// Always call revertColliderStates after this
	public static void rewindStatesToTick(float tickNumber) {
		foreach (Player p in players.Values) {
			if (p.playerObject) {
				p.playerObject.GetComponent<PlayerAnimation> ().goBackToTick (tickNumber);
			}
		}
	}

	// ONLY to be used directly after (and ALWAYS after) calling rewindStatesToTick
	public static void revertColliderStates() {
		foreach (Player p in players.Values) {
			if (p.playerObject) {
				p.playerObject.GetComponent<PlayerAnimation> ().revert ();
			}
		}
	}

}
