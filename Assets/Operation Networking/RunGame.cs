
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.IO;

public class RunGame : MonoBehaviour
{

	bool started = false;

	public static ServerAcceptor myServer = null;

	public static List<ServerPerson> myServerThreads = new List<ServerPerson>();

	public static ClientPerson myClient = null;

	// Use this for initialization
	void Start()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		// Other stuff
		Application.targetFrameRate = 90; // Currently, there are issues with the game relying on framerate for simulation. (Like mainCamera rotation) -which is unavoidable.

		// This guarantees the RPCs will be set before the game connects:
		OperationView.initRPCs();

	}

	void StartServer()
	{
		started = true;
		OperationNetwork.isServer = true;

		myServer = new ServerAcceptor();
		myServer.StartServer ();

		// This is to get the server player to connect:
		OperationNetwork.initialConnected = 0; // The server player doesn't necessarily have to connect first.


		// Demo maker:
		ServerPerson sP = new ServerPerson(myServer);
		sP.setDemo ();
		// Revert addID:
		ServerPerson.addID--;

		RunGame.myServerThreads.Add(sP);
		// PlayerListHandler does not consider demo client.

		// Instantly connect the demo client:
		sP.connected = true;
	}

	void OnApplicationQuit()
	{
		Disconnect();
	}

	public static void Disconnect()
	{
		OperationNetwork.connected = false;
		Debug.LogError("Disconnect");
		try
		{
			if (OperationNetwork.isServer)
			{
				if (OperationNetwork.getClient(32767) != null) {
					OperationNetwork.getClient(32767).file.Close();
				}
				if (myServer != null) {
					NetworkTransport.Shutdown();
				}
			}
			else
			{
				if (myClient != null) {
					myClient.disconnect();
				}
			}
		}
		catch (Exception e)
		{
			Debug.LogError("On Disconnect: " + e.Message);
		}
	}
		
	void Update()
	{

		// This should only be uncommented for Linux VPS Server run on start!
		#if ATTACK_PLAN_DEDICATED_SERVER
		if (!started) {
			// Application.targetFrameRate = 5; // Again, framerate is unimportant on server
			StartServer();
			// It'll be pretty obvious:
			if (Player.thisPlayer) 
				Player.thisPlayer.team = 2; // Spectator (Never spawns.) 
			PlayerInformation.steamName = "Test Server";
			GameObject.Find("DeathCamera").GetComponent<Camera>().enabled = false;
		}
		#endif // ATTACK_PLAN_DEDICATED_SERVER
		
	}

	static string ipAddress = "127.0.0.1"; // "104.200.137.98"; //"127.0.0.1"; //"184.66.34.224"; //"104.200.137.98";
	static string port = "26800";

	void OnGUI()
	{
		if (!started)
		{
			ipAddress = GUI.TextField (new Rect (Screen.width / 2 - 150, Screen.height / 2 + 110, 300, 40), ipAddress);
			port = GUI.TextField (new Rect (Screen.width / 2 - 150, Screen.height / 2 + 160, 300, 40), port);

			if (GUI.Button(new Rect(Screen.width / 2 - 300, Screen.height / 2 + 120, 100, 20), "Start Server"))
			{
				// Server
				if (Input.GetKey(KeyCode.S))
				{
					// This is NOT where you uncomment to run server on linux as OnGUI doesn't run there!
					StartServer();
					// It'll be pretty obvious:
					Player.thisPlayer.team = 2; // Spectator (Never spawns.) 
					PlayerInformation.steamName = "Test Server";
					GameObject.Find("DeathCamera").GetComponent<Camera>().enabled = false;


					//ClassSelectionMenu.classSelectionMenuOpen = false;
				}
				else {
					StartServer();
				}
			}

			if (GUI.Button(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 60, 300, 40), "Connect"))
			{
				started = true;

				myClient = new ClientPerson(ipAddress, port);
				myClient.StartClient ();
			} else if (GUI.Button(new Rect(Screen.width / 2 - 300, Screen.height / 2 + 150, 100, 20), "Watch Demo")) {

				// This will fail on the frame it happens if no demo file: (Or it's too big?)
				if (File.Exists ("TestDemo.dem")) {
					OperationNetwork.dataToReadIn = File.ReadAllBytes("TestDemo.dem");
				}

				Debug.LogError ("Watching Demo");

				started = true;
				// timeStartedAt = Time.time; // Demo time is simply, "Time.time - timeStartedAt" currently. Basically, it reads the input data until time is after demoTime.
				OperationNetwork.isDemo = true;
				Player.myID = 32767;
				OperationNetwork.timeStartedAt = Time.time;
				OperationNetwork.currentByte = 0; // Implies maximum of 2gb.
				OperationNetwork.timeReceivedForDemo = false;

				// Demo watcher doesn't get a player.

				OptionsMenu.classSelectionMenuOpen = false;
				OptionsMenu.ChangeLockState ();
			}
		}
	}
}
