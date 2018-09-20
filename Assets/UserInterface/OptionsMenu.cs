using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class OptionsMenu : MonoBehaviour {

	public static Dictionary<string,GameObject> hudPanels = null;
	public GameObject[] panels;

	public static bool optionsMenuOpen = false;
	public static bool classSelectionMenuOpen = false;

	static int numBinds = 9;
	bool isSettingBind = false; // This should be set to false when switching on / off options menu.
	bool[] settingBind = new bool[numBinds]; // E, F, Shift are the current rebindables.

	public static KeyCode[] binds = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.E, KeyCode.F, KeyCode.Q, KeyCode.T, KeyCode.LeftShift, KeyCode.U};
	string[] bindInfo = {"Primary: ", "Primary (2): ", "Primary (3): ", " Main Ability: ", "Offence Ability: ", "Trap 1: ", "Trap 2: ", "Movement: ", "Ultimate: "};

	public static string getBindString(KeyCode kC) {
		if (kC.ToString ().StartsWith ("Alpha")) {
			return kC.ToString ().Substring (5);
		} else {
			return kC.ToString ();
		}
	}

	public const int PRIMARY_1 = 0;
	public const int PRIMARY_2 = 1;
	public const int PRIMARY_3 = 2;

	public const int TRAP_1 = 5;
	public const int TRAP_2 = 6;

	public const int MAIN_ABILITY = 3;
	public const int OFFENCIVE_ABILITY = 4;

	public const int NUM_SWITCH_TO_BINDS = 7;

	public const int ULTIMATE_ABILITY = 8;

	public const int MOVEMENT_ABILITY_BIND = 7;

	// The binds for the keys only apply on spawn.


	void Start () {
		if (hudPanels == null) {
			hudPanels = new Dictionary<string,GameObject> ();
			foreach (GameObject panel in panels) {
				hudPanels.Add (panel.name, panel);
			}
		}
	}

	public static bool IsLockState()
	{
		return !optionsMenuOpen && !classSelectionMenuOpen && !Communication.typing && (OperationNetwork.connected || OperationNetwork.isDemo) && !PlayerHud.isTrapSelectionMenuOpen;
	}

	// Changes lock state to the appropiate lockState. 
	// Note that this method might not actaully change the lockState, if say the options menu is closed and the class selection menu is still open.
	public static void ChangeLockState()
	{
		if (IsLockState())
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		else
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}
	
	// Update is called once per frame
	void Update () {

		if (OperationNetwork.connected && Input.GetKeyDown(KeyCode.C))
		{
			// For now, classSelectionMenuOpen doesn't work when alive.
			classSelectionMenuOpen = !classSelectionMenuOpen;
			OptionsMenu.ChangeLockState();
		}

		if (!optionsMenuOpen && Input.GetKeyDown(KeyCode.Escape))
		{
			// This for loop is not necessary.
			for (int n = 0; n < numBinds; n++)
			{
				settingBind[n] = false;
			}
			isSettingBind = false;
			optionsMenuOpen = !optionsMenuOpen;

			ChangeLockState();
		}

		// This is because unity captures the escape button and sets "lockState" to none. Note that the player needs to click to get this to work..
		if (IsLockState())
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}


		if (isSettingBind)
		{
			if (Input.anyKeyDown)
			{
				foreach (KeyCode vKey in System.Enum.GetValues(typeof(KeyCode)))
				{
					if (Input.GetKeyDown(vKey) && vKey != KeyCode.Mouse0 && vKey != KeyCode.Mouse1)
					{
						for (int i = 0; i < numBinds; i++)
						{
							if (settingBind[i])
							{
								binds[i] = vKey;
								isSettingBind = false;
								settingBind[i] = false;
								break;
							}
						}
						break;
					}
				}
			}
		}
	}

	void OnGUI()
	{
		if (optionsMenuOpen)
		{
			GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
			GUI.Box(new Rect(Screen.width / 2 - 10, Screen.height / 2 - 100, 100, 25), "Volume");
			AudioListener.volume = Mathf.Pow(GUI.HorizontalSlider(new Rect(Screen.width / 2 + 100, Screen.height / 2 - 95, 200, 20), Mathf.Sqrt(AudioListener.volume), 0, 1), 2);
			GUI.Box(new Rect(Screen.width / 2 - 10, Screen.height / 2 - 60, 100, 25), "Sensitivity");
			PlayerMove.sensitivity = GUI.HorizontalSlider(new Rect(Screen.width / 2 + 100, Screen.height / 2 - 55, 200, 20), PlayerMove.sensitivity, 0.025f, 6f);

			
			for (int i = 0; i < numBinds; i++)
			{
				if (settingBind[i])
				{
					GUI.Button(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 100 + i * 25, 180, 25), bindInfo[i] + "...");
				}
				else if (GUI.Button(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 100 + i * 25, 180, 25), bindInfo[i] + "'" + getBindString(binds[i]) + "'"))
				{
					for (int n = 0; n < numBinds; n++)
					{
						settingBind[n] = false;
					}
					settingBind[i] = true;
					isSettingBind = true;
				}
			}

			if (GUI.Button(new Rect(Screen.width - 200, 200, 150, 50), "Resume"))
			{
				optionsMenuOpen = false;
				ChangeLockState();
				return;
			}
				

			if (GUI.Button(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 220, 140, 40), "Disconnect"))
			{

				// Disconnects from everything, resets all static variables. Also closes escape menu?
				RunGame.Disconnect();
				ResetStaticVariables();
				SceneManager.LoadScene(0); // Reloads map.
			}

			if (GUI.Button(new Rect(Screen.width / 2 + 10, Screen.height / 2 + 220, 140, 40), "Quit"))
			{
				Application.Quit();
			}
		}
	}

	// todo Make sure all static variables are here.
	public static void ResetStaticVariables()
	{
		print("Resetting Static Variables");

		ServerPerson.addID = 10;

		Interp.actualPing = 0;
		Interp.usedPing = 0;
		Interp.displayPing = 0;

		OperationNetwork.connected = false;
		OperationNetwork.isServer = false;
		OperationNetwork.isDemo = false;

		OperationNetwork.operationObjects = new List<SyncGameState>(300); // Objects that can be network instantiated. Presumed to be < 256 of them.. Their byte id is the first thing.
		OperationNetwork.operationGameObjectSpawnTimes = new List<float>(); // SERVER ONLY!
		OperationNetwork.operationGameObjectBufferedRPCs = new List<List<object[]>>(); // SERVER ONLY!
		OperationNetwork.operationGameObjectsData = new List<byte[]>();

		RunGame.myServer = null;
		RunGame.myServerThreads = new List<ServerPerson>();
		RunGame.myClient = null;

		SoundHandler.soundHandler = null;

		classSelectionMenuOpen = false;

		OptionsMenu.optionsMenuOpen = false; // Heh

		print("Will Reload Scene Now.");
	}
}
