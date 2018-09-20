using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

// LevelLogic handles some of the game specific hudd
public class PlayerHud : MonoBehaviour {

	public Texture inWall;
	public Texture phase;

	public GameObject[] worldHealthBar;
	static GameObject[] wHealthBar;
	static List<Transform> healthBarsParents = new List<Transform>();
	static List<GameObject> healthBarsDisplayed = new List<GameObject>();
	static List<MonoBehaviour> playerMadeHealths = new List<MonoBehaviour>();

	Image buffHud;
	Text healthHudText;
	Image healthHud;
	Image respawnHud;

	GameObject scoreBoard;
	List<List<GameObject>> scoreBoardPlayers = new List<List<GameObject>>();
	List<List<Text>> scoreBoardKills = new List<List<Text>>();
	List<List<Text>> scoreBoardDeaths = new List<List<Text>>();

	static GameObject playerHud;

	public List<Transform> killFeed = new List<Transform>();

	GameObject classSelectionMenu;
	Image[] classSelectionTeams = new Image[3];
	List<Image> classTypes = new List<Image>();


	public static bool isTrapSelectionMenuOpen = false;
	public static int whichGroupTypeSelecting = -1;

	static GameObject trapSelectionZoneMessage;

	GameObject[] trapPlayerHudTypes;
	byte[] displayTraps;


	GameObject classHud;
	GameObject playerObjectHudLoaded;

	GameObject[] trapsHud;
	GameObject[,] trapsChoices;

	// Network Settings:
	GameObject networkSettings;
	float lastNetworkSettingsRead;
	float dataSentPerSecond;

	public static void addHealthBar(Transform worldObjectParent, MonoBehaviour healthScript, int team) {
		healthBarsParents.Add (worldObjectParent);
		healthBarsDisplayed.Add((GameObject)MonoBehaviour.Instantiate(wHealthBar[team], playerHud.transform));
		playerMadeHealths.Add (healthScript);
	}

	void HealthBarUpdate() {
		for (int i = 0; i < healthBarsDisplayed.Count; i++) {
			if (healthBarsParents [i]) {
				if (Player.thisPlayer && Player.thisPlayer.playerCamera) {
					Vector3 viewPortPos = Player.thisPlayer.playerCamera.GetComponent<Camera> ().WorldToViewportPoint (healthBarsParents [i].position);
					if (viewPortPos.z > 0) {
						float distance = Vector3.Distance (Player.thisPlayer.playerCamera.transform.position, healthBarsParents [i].position);

						// Now we're going to do a raycast to see if it is on screen:
						if (!Physics.Raycast(Player.thisPlayer.playerCamera.transform.position, 
							Vector3.Normalize(healthBarsParents [i].position - Player.thisPlayer.playerCamera.transform.position), distance, 1 << 0 | 1 << 11)) {
							healthBarsDisplayed [i].SetActive (true);
							Vector3 refRes = playerHud.GetComponent<CanvasScaler> ().referenceResolution;
							// Position on screen, if it is on screen:
							healthBarsDisplayed [i].GetComponent<RectTransform> ().anchoredPosition = 
								new Vector2 (viewPortPos.x * refRes.x - refRes.x / 2, 
									viewPortPos.y * refRes.y - refRes.y / 2);

							float width = 170;
							float height = 50;

							healthBarsDisplayed [i].GetComponent<RectTransform> ().sizeDelta = new Vector2(width / distance, height / distance);

							float healthPercent;
							// We will take this opportunity to update the health value as well:
							if (playerMadeHealths [i] is PlayerMade) {
								healthPercent = ((PlayerMade)playerMadeHealths [i]).getHealth () / ((PlayerMade)playerMadeHealths [i]).getMaxHealth ();
							} else {
								// Is Combat:
								healthPercent = ((Combat)playerMadeHealths [i]).health / ((Combat)playerMadeHealths [i]).maxHealth;
							}
							if (healthPercent > 1) {
								// For now, just make it bigger:
								healthBarsDisplayed [i].GetComponent<RectTransform> ().sizeDelta = new Vector2(width * healthPercent / distance, height / distance);
								healthPercent = 1;
							}
							healthBarsDisplayed [i].transform.Find ("HealthFill").GetComponent<Image> ().fillAmount = healthPercent;
							healthBarsDisplayed [i].GetComponent<Image> ().fillAmount = 1 - healthPercent;
							continue;
						}
					}
					healthBarsDisplayed [i].SetActive (false);
				}
			} else {
				Destroy (healthBarsDisplayed [i]);
				healthBarsParents.RemoveAt (i);
				healthBarsDisplayed.RemoveAt (i);
				playerMadeHealths.RemoveAt (i);
				i--;
			}
		}
	}

	void Awake()
	{
		BuyNewTrap.init ();

		playerHud = GameObject.Find ("PlayerHud");

		classHud = playerHud.transform.Find ("ClassHud").gameObject;

		respawnHud = playerHud.transform.Find ("RespawnTimerPanel").transform.Find("Charge").GetComponent<Image>();
		wHealthBar = worldHealthBar;
		buffHud = playerHud.transform.Find("DamageBuffPanel").transform.Find("Charge").GetComponent<Image>();
		healthHud = playerHud.transform.Find ("HealthBarPanel").transform.Find("Charge").GetComponent<Image>();
		healthHudText = healthHud.transform.parent.Find ("Text").GetComponent<Text> ();

		scoreBoard = playerHud.transform.Find ("ScoreBoardPanel").gameObject;
		Transform[] killsHeader = {scoreBoard.transform.Find ("BlueKillsHeader"), scoreBoard.transform.Find ("RedKillsHeader")};
		Transform[] deathsHeader = {scoreBoard.transform.Find ("BlueDeathsHeader"), scoreBoard.transform.Find ("RedDeathsHeader")};
		scoreBoardPlayers.Add (new List<GameObject> ());
		scoreBoardPlayers.Add (new List<GameObject> ());
		scoreBoardKills.Add (new List<Text> ());
		scoreBoardKills.Add (new List<Text> ());
		scoreBoardDeaths.Add (new List<Text> ());
		scoreBoardDeaths.Add (new List<Text> ());
		foreach (Transform element in scoreBoard.transform) {
			if (element.name.StartsWith ("BluePlayer") || element.name.StartsWith ("RedPlayer")) {
				int team = 0;
				if (element.name.StartsWith ("RedPlayer"))
					team = 1;
				
				int num = LevelLogic.getNumber (element.name);
				while (scoreBoardPlayers[team].Count <= num) {
					scoreBoardPlayers[team].Add (null);
					scoreBoardKills[team].Add (null);
					scoreBoardDeaths[team].Add (null);
				}
				scoreBoardPlayers [team] [num] = element.gameObject;
				GameObject kill = (GameObject)GameObject.Instantiate (killsHeader [team].gameObject, scoreBoard.transform);
				kill.GetComponent<RectTransform> ().position = new Vector3 (kill.GetComponent<RectTransform> ().position.x, element.position.y, 0);
				scoreBoardKills [team] [num] = kill.GetComponent<Text> ();
				GameObject death = (GameObject)GameObject.Instantiate (deathsHeader [team].gameObject, scoreBoard.transform);
				death.GetComponent<RectTransform> ().position = new Vector3 (death.GetComponent<RectTransform> ().position.x, element.position.y, 0);
				scoreBoardDeaths [team] [num] = death.GetComponent<Text> ();
			}
		}

		GameObject killFeed = GameObject.Find ("KillFeed");
		foreach (Transform element in killFeed.transform) {
			this.killFeed.Add (element.Find("Text"));
		}

		networkSettings = GameObject.Find ("NetworkSettings");

		classSelectionMenu = GameObject.Find ("ClassSelectionPanel");
		classSelectionTeams [0] = classSelectionMenu.transform.Find ("BlueTeamSelect").GetComponent<Image>();
		classSelectionTeams [0].GetComponent<SelectClass> ().num = 0;
		classSelectionTeams [1] = classSelectionMenu.transform.Find ("RedTeamSelect").GetComponent<Image>();
		classSelectionTeams [1].GetComponent<SelectClass> ().num = 1;
		classSelectionTeams [2] = classSelectionMenu.transform.Find ("SpectateTeamSelect").GetComponent<Image>();
		classSelectionTeams [2].GetComponent<SelectClass> ().num = 2;

		foreach (Transform element in classSelectionMenu.transform) {
			if (element.name.StartsWith ("Class")) {
				int classNumber = LevelLogic.getNumber (element.name);
				while (classTypes.Count <= classNumber) {
					classTypes.Add (null);
				}
				classTypes[classNumber] = element.GetComponent<Image>();
				element.GetComponent<SelectClass> ().num = (byte)classNumber;
			}
		}

		GameObject trapPlayerHud = GameObject.Find ("TrapDisplay");
		trapPlayerHudTypes = new GameObject[2];
		for (int i = 0; i < 2; i++) {
			trapPlayerHudTypes[i] = trapPlayerHud.transform.Find ("Trap" + i).gameObject;
			trapPlayerHudTypes[i].SetActive (false);
		}
		displayTraps = new byte[]{255, 255};
		trapSelectionZoneMessage = GameObject.Find ("TrapSelectionZoneMessage");
		GameObject trapSelectionHud = GameObject.Find("TrapSelectionHud");
		trapsHud = new GameObject[2]; //BuyNewTrap.NUM_TRAP_GROUPS];
		for (int i = 0; i < 2; i++) {
			trapsHud [i] = trapSelectionHud.transform.Find ("Traps" + i).gameObject;
		}
		GameObject trap = trapSelectionHud.transform.Find("Trap").gameObject;
		trapsChoices = new GameObject[2,3];
		for (int i = 0; i < trapsHud.Length; i++) {
			// 3 choices for each trap:
			for (int n = 0; n < 3; n++) {
				GameObject newTrap = (GameObject)Instantiate (trap, trapsHud[i].transform);
				newTrap.GetComponent<RectTransform> ().anchoredPosition = new Vector2 (125 +  n * 220, 0);
				trapsChoices [i, n] = newTrap;
			}
		}

		trap.SetActive (false);

		HudUpdate ();
	}

	void UpdateTrapSelectionZoneMenu() {
		bool willBeActive = Player.thisPlayer != null && !isTrapSelectionMenuOpen && 
							Player.thisPlayer.playerObject != null && Player.thisPlayer.playerObject.GetComponent<PlayerMove> ().isInTrapSelectionZone;
		trapSelectionZoneMessage.SetActive (willBeActive);
	}
		
	void OnGUI()
	{
		if (OperationNetwork.connected && Player.thisPlayer != null && Player.thisPlayer.playerObject != null && 
			Player.thisPlayer.playerObject.GetComponent<ClassControl>().classNum == 0) {
			PlayerMove pMove = Player.thisPlayer.playerObject.GetComponent<PlayerMove> ();
			float playerTime = pMove.GetComponent<SyncPlayer> ().playerTime;

			float delta = Mathf.Abs(playerTime - pMove.movementAbilityCoolDownStartedAt - PlayerMove.phaseTime);

			
			if (pMove.isPhasing ()) {
				float smallPortion = PlayerMove.phaseTime / 12.0f;
				if (playerTime - pMove.movementAbilityCoolDownStartedAt < smallPortion) {
					GUI.color = new Color (1.0f, 1.0f, 1.0f, (playerTime - pMove.movementAbilityCoolDownStartedAt) * 12.0f);
				}
				GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), phase);
				
				GUI.color = new Color (1.0f, 1.0f, 1.0f, 1.0f);

				if (pMove.walkThroughWalls.walkingThroughWalls) {
					delta = Mathf.Min (delta, Mathf.Max(0, Mathf.Abs (0.5f - pMove.walkThroughWalls.travelPercent) - 0.25f) * 4);
				}
			}
			
			
			if (delta < 1f) {
				GUI.color = new Color (1.0f, 1.0f, 1.0f, (1 - delta) / 1.0f);
				GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), inWall);
			}
		}

		if (Input.GetKey (KeyCode.N) && OperationNetwork.connected && !OperationNetwork.isServer && Player.thisPlayer) {
			// Shows net stats: (if client)
			Texture2D t2d = new Texture2D (1, 1);
			t2d.SetPixel(0, 0, new Color(0, 0.8f, 0));
			t2d.wrapMode = TextureWrapMode.Repeat;
			t2d.Apply ();
			GUIStyle style = new GUIStyle ();
			style.normal.background = t2d;

			float timeToShow = 5f;
			float scaleFactor = 0.6f; // Pixels per byte
			float runningTotal = 0f;
			for (int i = 1; i < RunGame.myClient.previousDataInRate.Length; i++) {
				if (Time.time - RunGame.myClient.previousDataInTimes [i - 1] < timeToShow) {
					runningTotal += RunGame.myClient.previousDataInRate [i - 1];
					if (RunGame.myClient.previousDataInTimes [i - 1] == RunGame.myClient.previousDataInTimes [i])
						continue;
					GUI.Box (new Rect (new Vector2 (50 + (Time.time - RunGame.myClient.previousDataInTimes [i - 1]) * 400f / timeToShow, 400 - runningTotal * scaleFactor / 2), new Vector2 (5, runningTotal * scaleFactor)), "", style);
					runningTotal = 0;
				} else {
					break;
				}
			}
		}
		HudUpdate ();
	}

	public static float getHudHeightUsed(Transform t) {
		float spaceNeeded = 0;
		// Find children that require more space:
		foreach (Transform c in t) {
			spaceNeeded += ((RectTransform)c).sizeDelta.y;
		}
		return spaceNeeded;
	}

	void HudUpdate() {
		GameManager.UpdateKillFeed ();

		HealthBarUpdate ();

		UpdateTrapSelectionZoneMenu ();

		for (int i = 0; i < trapsHud.Length; i++) {
			trapsHud [i].SetActive (isTrapSelectionMenuOpen);
		}
		
		respawnHud.transform.parent.gameObject.SetActive (Player.thisPlayer != null && Player.thisPlayer.playerObject == null);
		if (Player.thisPlayer != null && Player.thisPlayer.playerObject == null) {
			respawnHud.fillAmount = Mathf.Clamp01 (1 - Player.thisPlayer.getRespawnTimer () / Player.thisPlayer.getNetRespawnTimer ());
			respawnHud.transform.parent.GetComponent<Image>().fillAmount = Mathf.Clamp01 (Player.thisPlayer.getRespawnTimer () / Player.thisPlayer.getNetRespawnTimer ());
			int respawnTimer = (int)(Player.thisPlayer.getRespawnTimer () + 1);
			if (respawnTimer == 1) {
				respawnHud.transform.parent.Find ("Text").GetComponent<Text> ().text = "Respawning in 1 second.";
			} else {
				respawnHud.transform.parent.Find ("Text").GetComponent<Text> ().text = "Respawning in " + respawnTimer + " seconds.";
			}
		}

	// Class Selection Hud gets priority
	if (Player.thisPlayer && Player.thisPlayer.team == 1 && !OptionsMenu.classSelectionMenuOpen) {
			bool needTrap = false;
			for (int i = 0; i < Player.thisPlayer.trapTypes.Length; i++) {
				if (Player.thisPlayer.trapTypes [i] == 255)
					needTrap = true;
			}
			if (needTrap && !PlayerHud.isTrapSelectionMenuOpen) {
				PlayerHud.isTrapSelectionMenuOpen = true;

				OptionsMenu.ChangeLockState ();
			} else if (!needTrap && PlayerHud.isTrapSelectionMenuOpen) {
				PlayerHud.isTrapSelectionMenuOpen = false;
				OptionsMenu.ChangeLockState ();
			}

		}

		if (Player.thisPlayer && PlayerHud.isTrapSelectionMenuOpen) {
			for (int x = 0; x < trapsChoices.GetLength (0); x++) {
				for (int y = 0; y < trapsChoices.GetLength (1); y++) {
					trapsChoices [x,y].transform.Find ("Text").GetComponent<Text> ().text = BuyNewTrap.trapNames [BuyNewTrap.trapIndecies[Player.thisPlayer.myRandomTrapChoices[x * trapsChoices.GetLength(1) + y]]];
					trapsChoices [x,y].GetComponent<BuyNewTrap> ().buyId = Player.thisPlayer.myRandomTrapChoices [x * trapsChoices.GetLength (1) + y];
					trapsChoices [x, y].GetComponent<BuyNewTrap> ().rowID = (byte)x;
				}
			}
		}

		// This is how HUD should work for all unlocks:
		if (Player.thisPlayer) {
			for (int i = 0; i < displayTraps.Length; i++) {
				byte actual = Player.thisPlayer.trapTypes [i];
				if (Player.thisPlayer.team != 1) {
					actual = 255;
				}
				if (displayTraps [i] != actual) {
					displayTraps [i] = actual;
					if (displayTraps [i] == 255 || Player.thisPlayer.team != 1) {
						trapPlayerHudTypes [i].SetActive (false);
					} else {
						trapPlayerHudTypes [i].SetActive (true);
						trapPlayerHudTypes [i].transform.Find ("Text").GetComponent<Text> ().text = "(" + OptionsMenu.getBindString(OptionsMenu.binds [OptionsMenu.TRAP_1 + i]) + ") " + BuyNewTrap.trapNames [BuyNewTrap.trapIndecies[displayTraps [i]]];
					}
				}


				if (displayTraps[i] != 255 && Player.thisPlayer.playerObject) {
					int trapTypeIndex = BuyNewTrap.trapIndecies[Player.thisPlayer.trapTypes [i]];
					PlacePlayerMade.setCharge (trapPlayerHudTypes [i], 
					Mathf.Clamp01((Player.thisPlayer.playerObject.GetComponent<SyncPlayer>().playerTime - Player.thisPlayer.trapCoolDownsStartedAt [i]) / (float)(BuyNewTrap.baseTrapCosts [trapTypeIndex] * BuyNewTrap.maxTrapsLoaded[trapTypeIndex] * Time.fixedDeltaTime)), 
					1f / BuyNewTrap.maxTrapsLoaded[trapTypeIndex]);
				}
			}
		}

		if (OperationNetwork.connected) {
			if (Time.time - lastNetworkSettingsRead > 1) {
				if (OperationNetwork.isServer) {
					dataSentPerSecond = OperationNetwork.getClient (32767).dataSent / (Time.time - lastNetworkSettingsRead);
					OperationNetwork.getClient (32767).dataSent = 0;
					networkSettings.GetComponent<Text> ().text = (int)(dataSentPerSecond / 100) / 10.0f + " KB/s";
				} else {
					dataSentPerSecond = RunGame.myClient.dataOutRate / (Time.time - lastNetworkSettingsRead);
					RunGame.myClient.dataOutRate = 0;
					float dataRecievedPerSecond = RunGame.myClient.dataInRate / (Time.time - lastNetworkSettingsRead);
					RunGame.myClient.dataInRate = 0;

					networkSettings.GetComponent<Text> ().text = "Sent: " + (int)(dataSentPerSecond / 100) / 10.0f + 
						" KB/s Recieved: " + (int)(dataRecievedPerSecond / 100) / 10.0f + " KB/s";
				}
				lastNetworkSettingsRead = Time.time;
			}
		}

		if (OperationNetwork.connected && Player.thisPlayer != null && Player.thisPlayer.playerObject != null) {
			PlayerMove pMove = Player.thisPlayer.playerObject.GetComponent<PlayerMove> ();
			float touchPercent = DamageCircle.isTouchingDamageCircle (pMove.timeSinceTouchingDamageCircle);
			if (touchPercent > 0) {
				buffHud.transform.parent.GetComponent<Image> ().enabled = true;
				buffHud.enabled = true;
				buffHud.fillAmount = touchPercent;
			} else {
				// Disable the rendering of it:
				buffHud.transform.parent.GetComponent<Image> ().enabled = false;
				buffHud.enabled = false;
			}
			healthHud.enabled = true;
			healthHud.transform.parent.GetComponent<Image> ().enabled = true;
			healthHudText.enabled = true;
			healthHud.transform.parent.GetComponent<Image> ().fillAmount = 1 - pMove.GetComponent<Combat> ().health / pMove.GetComponent<Combat> ().maxHealth;
			healthHud.fillAmount = pMove.GetComponent<Combat> ().health / pMove.GetComponent<Combat> ().maxHealth;
			healthHudText.text = (short)pMove.GetComponent<Combat> ().health + " HP";

			if (Player.thisPlayer.playerObject != playerObjectHudLoaded) {
				playerObjectHudLoaded = Player.thisPlayer.playerObject;

				float origNum = getHudHeightUsed (classHud.transform);

				// Unload old hud:
				foreach (Transform t in classHud.transform) {
					if (t.name != "AdjacentClassHud")
						Destroy (t.gameObject);
				}
				foreach (Transform t in classHud.transform.Find("AdjacentClassHud")) {
					Destroy (t.gameObject);
				}

				// Create new hud:
				foreach (Unlock unlock in Player.thisPlayer.playerObject.GetComponent<ClassControl>().getUnlocks()) {
					if (unlock != null)
						unlock.setHudElement (classHud.transform, origNum);
				}

				int classLoaded = Player.thisPlayer.playerObject.GetComponent<ClassControl> ().classNum;

				// Phase through & Speed Boost are not unlocks, despite having some similar qualities: (Like the HUD)
				if (classLoaded == 0) {
					Player.thisPlayer.playerObject.GetComponent<PlayerMove> ().hudElement = Unlock.setHudElement (classHud.transform, "Cooldown", "Phase Shift", OptionsMenu.MOVEMENT_ABILITY_BIND, null, origNum);
					Player.thisPlayer.playerObject.GetComponent<PlayerMove> ().hudElement2 = Unlock.setHudElement (classHud.transform, "Armor", "Armor", OptionsMenu.MAIN_ABILITY, null, origNum);
				} else if (classLoaded == 3) {
					Player.thisPlayer.playerObject.GetComponent<PlayerMove> ().hudElement = Unlock.setHudElement (classHud.transform, "Cooldown", "Speed Boost", OptionsMenu.MOVEMENT_ABILITY_BIND, null, origNum);
				} else if (classLoaded == 4) {
					if (OptionsMenu.hudPanels.ContainsKey ("Healthtaken")) {
						Player.thisPlayer.playerObject.GetComponent<PlayerMove> ().hudElement = Unlock.setHudElement (classHud.transform, "Healthtaken", "Health Taken", OptionsMenu.ULTIMATE_ABILITY, null, origNum);
					}
				}
					
					// The updating for the HUD is handled within the unlocks / class itself
					
			}
		} else {
			// Hide hud completely:
			buffHud.transform.parent.GetComponent<Image> ().enabled = false;
			buffHud.enabled = false;
			healthHud.enabled = false;
			healthHud.transform.parent.GetComponent<Image> ().enabled = false;
			healthHudText.enabled = false;
		}

		// SCOREBOARD:
		if (Input.GetKey (KeyCode.Tab)) {
			scoreBoard.SetActive (true);
			for (byte team = 0; team < 2; team++) {
				List<Player> players = GameManager.getPlayersOnTeam (team);
				for (int i = 0; i < scoreBoardPlayers[team].Count; i++) {
					if (i < players.Count) {
						scoreBoardPlayers [team] [i].SetActive (true);
						scoreBoardPlayers [team] [i].transform.Find ("Text").GetComponent<Text> ().text = players [i].playerName;
						Color c = scoreBoardPlayers [team] [i].GetComponent<Image> ().color;
						if (players [i].playerObject != null) {
							scoreBoardPlayers [team] [i].GetComponent<Image> ().color = new Color(c.r, c.g, c.b, 1f);
						} else {
							scoreBoardPlayers [team] [i].GetComponent<Image> ().color = new Color(c.r, c.g, c.b, 0.2f);
						}
						scoreBoardKills [team] [i].text = "" + players[i].kills;
						scoreBoardDeaths [team] [i].text = "" + players[i].deaths;
					} else {
						scoreBoardPlayers [team] [i].SetActive (false);
						scoreBoardKills [team] [i].text = "";
						scoreBoardDeaths [team] [i].text = "";
					}
				}
			}
		} else {
			scoreBoard.SetActive (false);
		}

		// Display kill feed:
		for (int i = 0; i < killFeed.Count; i++) {
			if (GameManager.recentPlayerKillers.Count > i) {
				killFeed [i].parent.gameObject.SetActive (true);
				string killDisplay = "";
				if (GameManager.PlayerExists (GameManager.recentPlayerKillers [i])) {
					killDisplay = GameManager.GetPlayer(GameManager.recentPlayerKillers [i]).playerName + " killed ";
				}
				if (GameManager.PlayerExists (GameManager.recentPlayerDeaths [i])) {
					killFeed [i].GetComponent<Text> ().text = killDisplay + GameManager.GetPlayer(GameManager.recentPlayerDeaths [i]).playerName;
				}
			} else {
				killFeed [i].parent.gameObject.SetActive (false);
			}
		}

		// Class Selection Menu:
		if (OptionsMenu.classSelectionMenuOpen && Player.thisPlayer != null) {
			classSelectionMenu.SetActive (true);
			Color color;
			if (Player.thisPlayer.team == 0) {
				color = new Color (0, 0, 1f, 0.5f);
			} else if (Player.thisPlayer.team == 1) {
				color = new Color (1f, 0, 0, 0.5f);
			} else {
				color = new Color (1f, 1f, 1f, 0.5f);
			}

			classSelectionMenu.GetComponent<Image> ().color = color;
			for (byte i = 0; i < classSelectionTeams.Length; i++) {
				if (Player.thisPlayer.team == i) {
					classSelectionTeams [i].color = new Color (classSelectionTeams [i].color.r, classSelectionTeams [i].color.g, classSelectionTeams [i].color.b, 1f);
				} else {
					classSelectionTeams [i].color = new Color (classSelectionTeams [i].color.r, classSelectionTeams [i].color.g, classSelectionTeams [i].color.b, 0.6f);
				}
			}
			for (byte i = 0; i < classTypes.Count; i++) {
				if (Player.thisPlayer.team == 0) {
					classTypes [i].gameObject.SetActive (true);
					if (Player.thisPlayer.classNum == i) {
						classTypes [i].color = new Color (0.2f, 0.2f, 1f, 1f);
					} else {
						classTypes [i].color = new Color (0.7f, 0.7f, 1f, 0.8f);
					}
				} else if (Player.thisPlayer.team == 1) {
					classTypes [i].gameObject.SetActive (true);
					if (Player.thisPlayer.classNum == i) {
						classTypes [i].color = new Color (1f, 0.2f, 0.2f, 1f);
					} else {
						classTypes [i].color = new Color (1f, 0.7f, 0.7f, 0.8f);
					}
				} else {
					classTypes [i].gameObject.SetActive (false);
				}
			}
		} else {
			classSelectionMenu.SetActive (false);
		}
	}
}
