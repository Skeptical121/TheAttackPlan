using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using UnityEngine.SceneManagement;

public class OperationNetwork : MonoBehaviour {

	public const short ClientsOnly = 0;
	public const short ClientsAndServer = 1;
	public const short ToServer = 2; // Only for use by clients; only place clients can send RPCs
	public const short ServerObject = 3; // Specifies the sender was from "no one." Eventually this will essentially be phased out.
	public const short FromServerClient = 9; // The "client" # for the server.

	public static int initialConnected = -1; // -1 = Not connected. 0 = Connected, not sent out initial connect. 1 = 

	public static bool connected = false;
	public static bool isServer = false;
	public static bool isHeadless = false; // This is the setting that should be set to true if the server'll be run on linux vps

	// These static variables are NOT being set by OptionsMenu resetStaticVariables! (todo)
	public static bool isDemo = false;
	public static int lastTickLoaded = -1;
	public static float timeStartedAt = 0;
	public static byte[] dataToReadIn = null;
	public static int currentByte = 0; // Implies maximum of 2gb.
	public static bool timeReceivedForDemo = false;
	public const int maxDemoData = 4000;

	public GameObject[] instantiatableObjectsToSet;
	public static GameObject[] instantiatableObjects; // Assigned in the inspector. <255 for now.. (Note that it's not <256!) (can easily be increased to 16384). NOTE!!!!! 256 is SAVED FOR "null" (todo)

	public static List<SyncGameState> operationObjects = new List<SyncGameState>(300);
	public List<SyncGameState> operationObjectsNOT_TO_USE_JUST_TO_SEE_IN_INSPECTOR;
		
	// Server side only:
	public static List<float> destroyedAt = new List<float>(); // To avoid tick issues, it waits ~5 seconds, (at least 1 tick) before replacing a null reference on the server

	public static List<float> operationGameObjectSpawnTimes = new List<float>(); // SERVER ONLY!
	public static List<List<object[]>> operationGameObjectBufferedRPCs = new List<List<object[]>>(); // SERVER ONLY!
	public static List<byte[]> operationGameObjectsData = new List<byte[]>();

	// Client only:

	// This is to try to line up the time correctly.. It is the deltaTime that controls the "GameTime" that the client is using. It is very important.
	public static float properDeltaTime = -1; // It will be positive if the server joins first. This can only be used for stuff coming from the server..
	static float[] averageDeltaTime = new float[40];

	public static float serverTime
	{
		set
		{
			serverTimeValue = value;
		}
		get
		{
			if (OperationNetwork.isServer)
				return Time.time;
			else
				return serverTimeValue;
		}
	}

	static float serverTimeValue = 0f; // For clients.

	// This can remove the objects, and it'll wait for thousands of objects to be spawned before using it again; thus eliminating some issues..
	// HOWEVER, this is NOT a good solution to this, as a perfect one should really consider recently removed ids.

	void initUnlocks() {
		ClassControl.unlocks = new Unlock[12];
		ClassControl.unlocks [0] = new ShotGun (); // Primary - Scientist (1)
		ClassControl.unlocks [1] = new PlaceShield (); // ATTACK - Scientist

		ClassControl.unlocks [2] = new Pistol (); // Primary - Physicist (1)
		ClassControl.unlocks [3] = new PlaceIcicle (); // Physicist (E)
		ClassControl.unlocks [4] = new Throwable (); // ATTACK - Physicist

		ClassControl.unlocks [5] = new ProjectileGunScript (); // Primary - Inventor (1)
		ClassControl.unlocks [6] = new PlaceEarthMound (); // ATTACK - Inventor
		ClassControl.unlocks [7] = new PlaceMirror (); // Inventor (E)

		ClassControl.unlocks [8] = new MeeleeWeapon (); // Primary - Hijacker (1)
		ClassControl.unlocks [9] = new PoleVault();

		ClassControl.unlocks [10] = new TakeHealthWeapon (); // Primary - Team Leader (1)
		ClassControl.unlocks [11] = new PlaceTrap ();


		for (int i = 0; i < ClassControl.unlocks.Length; i++) {
			ClassControl.unlocks [i].setUnlockType ((byte)i);
		}

		PlacePlayerMade.placementSphere = Resources.Load ("PlacementSphere") as GameObject;
		AddCutOut(0, Resources.Load("ArrowTriggerPlacement"), CollideCheck.canPlaceOnGround);
		AddCutOut(1, Resources.Load("ArrowTrapPlacement"), CollideCheck.canPlaceAnywhere);
		// Index 2 is free
		AddCutOut(3, Resources.Load("HomingSpherePlacement"), CollideCheck.canPlaceAnywhere);
		AddCutOut(4, Resources.Load("DirtMoundPlacement"), CollideCheck.canPlaceOnGround);
		AddCutOut(5, Resources.Load("IciclePlacement"), CollideCheck.canPlaceAnywhere);
		AddCutOut(6, Resources.Load("LaserFieldPlacement"), CollideCheck.canPlaceOnWalls);
		AddCutOut(7, Resources.Load("MirrorPlacement"), CollideCheck.canPlaceOnWalls); // Should be walls and ceiling
		AddCutOut(8, Resources.Load("SawBladesPlacement"), CollideCheck.canPlaceOnGround); // they could be placed on walls.. but the rotation is wrong currently
		AddCutOut(9, Resources.Load("SentryPlacement"), CollideCheck.canPlaceOnGround);
		AddCutOut(10, Resources.Load("ShieldPlacement"), CollideCheck.canPlaceOnGround); // Again, placing on walls isn't out of the question here
		AddCutOut(11, Resources.Load("SpikesPlacement"), CollideCheck.canPlaceOnGround);
		AddCutOut(12, Resources.Load("SwingingBallPlacement"), CollideCheck.canPlaceOnGround);
		AddCutOut(13, Resources.Load("WindMillPlacement"), CollideCheck.canPlaceAnywhere2);
	}

	static void AddCutOut(byte id, UnityEngine.Object cutout, byte canPlaceWhere) {
		PlacePlayerMade.cutOuts [id] = (GameObject)cutout;
		((GameObject)cutout).GetComponent<CollideCheck> ().placeType = id;
		((GameObject)cutout).GetComponent<CollideCheck> ().canPlaceOn = canPlaceWhere;
	}
		
	void Start () {

		operationObjectsNOT_TO_USE_JUST_TO_SEE_IN_INSPECTOR = operationObjects;

		LevelLogic.InitLevelLogic ();

		initUnlocks ();

		instantiatableObjects = instantiatableObjectsToSet;
		for (int i = 0; i < instantiatableObjects.Length; i++)
		{
			// Sets the actual "objectID" of each instantiatable object on the prefabs for easy access:

			// This is no longer used, syncGameState will handle the type:

			if (instantiatableObjects [i]) {
				if (instantiatableObjects [i].GetComponent<SyncGameState> ()) {
					instantiatableObjects [i].GetComponent<SyncGameState> ().objectType = (byte)i;
				}
			}
		}

		// The following are only used by server:
		operationGameObjectSpawnTimes.Add(0); // Spawn time is not used for this..
		operationGameObjectBufferedRPCs.Add(new List<object[]>());
		operationGameObjectsData.Add(null); // Not instantiatable..
	}

	public static void checkTime(float givenTime, float actualTime)
	{
		if (isDemo) {
			properDeltaTime = -timeStartedAt;
			return;
		}
		if (properDeltaTime == -1)
		{
			properDeltaTime = givenTime - actualTime;
		}
		else
		{
			float wrongDelta = (givenTime - actualTime) - properDeltaTime;

			Interp.shiftBuffer(averageDeltaTime, wrongDelta);
			float avg = 0;
			for (int i = 0; i < averageDeltaTime.Length; i++)
			{
				avg += averageDeltaTime[i];
			}
			avg /= averageDeltaTime.Length;

			// Send it to the average, smoothly
			properDeltaTime += avg * 0.1f;
			for (int i = 0; i < averageDeltaTime.Length; i++)
			{
				averageDeltaTime[i] -= avg * 0.1f;
			}
		}
	}

	void sendGameStatesToPlayer(GameState serverState, ServerPerson theClient)
	{
		if (theClient.connected)
		{

			// This is easy enough, we send both game states every time, so both game states are expected in the 1, 2 order each time.
			bool changedData = theClient.lastSentGameState[theClient.lastSentGameState.Length - 1] != null; // Obviously server overrides this

			// First, we strip serverState of its playerData when getting its data:


			byte[] data1 = serverState.getDataWithoutPlayerData (changedData, theClient.id, theClient.shouldNextSendOutBeFullSendOut);
			theClient.shouldNextSendOutBeFullSendOut = false;

			// Now, we create the player gamestate, and send that:
			Interp.shiftBuffer(theClient.lastSentGameState);
			theClient.lastSentGameState[0] = new GameState(theClient.lastSentGameState, ServerState.tickNumber, theClient.id); // lastSentGameState is used to determine if changedData here
			byte[] data2 = theClient.lastSentGameState[0].getChangedData();

			byte[] data = new byte[data1.Length + data2.Length];
			Buffer.BlockCopy (data1, 0, data, 0, data1.Length);
			Buffer.BlockCopy (data2, 0, data, data1.Length, data2.Length);
			theClient.SendData(data, false);
		}
	}

	void Update () {

		if ((RunGame.myClient != null && !OperationNetwork.isServer) || OperationNetwork.isDemo) {
			if (RunGame.myClient != null)
				RunGame.myClient.ReadMessages();

			GetComponent<Interp>().InterpUpdate ();
		}

		if (Input.GetKeyDown (KeyCode.G)) {
			Time.timeScale = 0.02f;
		} else if (Input.GetKeyDown (KeyCode.H)) {
			Time.timeScale = 1;
		}


		if (OperationNetwork.isDemo) {
			if (Input.GetKeyDown (KeyCode.Alpha1)) {
				Time.timeScale *= 0.5f;
			} else if (Input.GetKeyDown (KeyCode.Alpha2)) {
				Time.timeScale *= 2;
			} else if (Input.GetKeyDown (KeyCode.Alpha3)) {
				Time.timeScale = 1;
			}
				
			// Read.. execute.. This will disconnect the client on the last ~10 seconds.

			try {
				byte[] readInData = new byte[OperationNetwork.maxDemoData];
				// Just in case:
				int x = 0;
				while (OperationNetwork.lastTickLoaded < Interp.getTickNumber() + 5) { // Only laods in 5 ticks ahead
					OperationNetwork.timeReceivedForDemo = false;
					Buffer.BlockCopy(OperationNetwork.dataToReadIn, OperationNetwork.currentByte, readInData, 0, 
						Math.Min(OperationNetwork.maxDemoData, OperationNetwork.dataToReadIn.Length - OperationNetwork.currentByte));
					int oldByte = OperationNetwork.currentByte;
					OperationNetwork.clientReceivedData(readInData); // Any amount is fine, but more will generally be better
					x++;
					if (x > 100) {
						Debug.LogError("MAJOR DEMO FAILURE");
						break;
					}
				}
			} catch (Exception e) {
				Debug.LogError(e);
				OperationNetwork.isDemo = false;

				// This is not necessarily stable:
				RunGame.Disconnect();
				OptionsMenu.ResetStaticVariables();
				SceneManager.LoadScene(0); // Reloads map.
			}
		}
	}

	void FixedUpdate()
	{
		

		if (connected)
		{
			if (isServer)
			{
					

				RunGame.myServer.ReadMessages ();

				// Run Game, essentially:

				// Main run:
				for (int i = 0; i < OperationNetwork.operationObjects.Count; i++)
				{
					if (OperationNetwork.operationObjects[i] != null) {
						OperationNetwork.operationObjects [i].didIteration ();
						OperationNetwork.operationObjects[i].ServerSyncFixedUpdate ();
					}
				}

				bool didRun = true;
				int infiniteLoopSaver = 0;
				while (didRun) {
					didRun = false;

					// Newly added syncGameStates should be run:
					List<SyncGameState> lsggN = new List<SyncGameState>(OperationNetwork.operationObjects);

					for (int i = 0; i < lsggN.Count; i++)
					{
						if (lsggN[i] != null && lsggN[i].shouldDoJustAdded()) {
							lsggN [i].didIteration ();
							lsggN[i].ServerSyncFixedUpdate ();
							didRun = true;
						}
					}

					if (infiniteLoopSaver++ > 100) {
						OperationNetwork.connected = false;
						for (int i = 0; i < lsggN.Count; i++)
						{
							if (lsggN[i] != null && lsggN[i].shouldDoJustAdded()) {
								Debug.LogError ("Infinite Loop failure of: " + lsggN [i]);
							}
						}
						Debug.LogError ("Infinite Loop Saver!");
						break;
					}
				}

				GameState serverGameState = new GameState (ServerState.previousGameStates, ServerState.tickNumber, (short)-1); // Main GameState

				Interp.shiftBuffer(ServerState.previousGameStates);
				ServerState.previousGameStates[0] = serverGameState;

				// Now send out the new data:
				for (int i = 0; i < RunGame.myServerThreads.Count; i++)
				{
					// It has its own gamestates to go by. If the client has recently connected, it simply sends the entire gamestate, 
					sendGameStatesToPlayer(serverGameState, RunGame.myServerThreads[i]);
				}

				List<int> queuedForDeath = new List<int> (); // This is because "OnDeath" can trigger the setting of exists = false to other objects

				// Now delete the objects that have been marked as [exists = false]
				for (int i = 0; i < OperationNetwork.operationObjects.Count; i++) {
					if (OperationNetwork.operationObjects [i] != null) {
						if (!OperationNetwork.operationObjects [i].exists) {
							queuedForDeath.Add (i);
						}
					}
				}
				for (int i = OperationNetwork.operationObjects.Count - 1; i >= 0; i--) {
					if (queuedForDeath.Contains(i)) {
						try {
							// Remember, this is server side only:
							OperationNetwork.operationObjects [i].OnDeath (); // Run onDeath action for Server
						} catch (Exception e) {
							Debug.LogError ("Error on death: " + e.StackTrace);
						}
						while (OperationNetwork.destroyedAt.Count <= i) {
							OperationNetwork.destroyedAt.Add (162500000f); // Large #. Never used anyways, because it's only used after an object is destroyed.
						}
						OperationNetwork.destroyedAt [i] = Time.time;
						Destroy (OperationNetwork.operationObjects [i].gameObject);
						OperationNetwork.operationObjects [i] = null;
					}
				}

				// Now, iterate the tick #:
				ServerState.iterateTickNumber();
			}
			else
			{
				fixedUpdateCounter++;
				if (fixedUpdateCounter > 5) {
					// Reliable data only gets sent every 100ms:
					RunGame.myClient.actuallySendData ();
				}
					
			}
		}
	}

	int fixedUpdateCounter = 0;

	static float lastTime = 0;
	public static void CheckForPlayerInputSend() {
		if (connected && !isServer) {
			if (Time.time - lastTime > 1f) {
				lastTime = Time.time;
			}
			// Send input data:

			RunGame.myClient.sendInputData ();
		}
	}

	public static Vector3 getVector3(byte[] data, int start)
	{
		return new Vector3(BitConverter.ToSingle(data, start), BitConverter.ToSingle(data, start + 4), BitConverter.ToSingle(data, start + 8));
	}

	public static Quaternion getQuaternion(byte[] data, int start)
	{
		return new Quaternion(BitConverter.ToSingle(data, start), BitConverter.ToSingle(data, start + 4), BitConverter.ToSingle(data, start + 8), BitConverter.ToSingle(data, start + 12));
	}

	// New to sync: todo make it so it replaces null stuff.
	public static void OperationAddSyncState(GameObject gameObject) {
		for (int i = 0; i < OperationNetwork.operationObjects.Count; i++) {
			if (OperationNetwork.operationObjects [i] == null && Time.time - OperationNetwork.destroyedAt [i] > 60) { // 5 seconds is not plenty in the world of 3 second timeouts, etc. 60 seconds SHOULD be good enough, but it can be increased almost as much as we need
				gameObject.GetComponent<SyncGameState>().objectID = (short)i;
				gameObject.GetComponent<SyncGameState>().InitStart(false); // Might as well do InitStart after setting objectID.
				gameObject.GetComponent<SyncGameState>().AfterFirstInterp();
				OperationNetwork.operationObjects [i] = gameObject.GetComponent<SyncGameState> ();
				return;
			}
		}
		gameObject.GetComponent<SyncGameState>().objectID = (short)OperationNetwork.operationObjects.Count;
		gameObject.GetComponent<SyncGameState>().InitStart(false); // Might as well do InitStart after setting objectID.
		gameObject.GetComponent<SyncGameState>().AfterFirstInterp();
		OperationNetwork.operationObjects.Add(gameObject.GetComponent<SyncGameState>());
	}

	public static ServerPerson getClient(short id)
	{
		for (int i = 0; i < RunGame.myServerThreads.Count; i++)
		{
			if (RunGame.myServerThreads[i].id == id)
			{
				return RunGame.myServerThreads[i];
			}
		}
		return null;
	}

	// Server only:
	public static void sendDataToSpecificClient(byte[] data, int id)
	{
		if (connected)
		{
			for (int i = 0; i < RunGame.myServerThreads.Count; i++)
			{
				if (RunGame.myServerThreads[i].id == id)
				{
					RunGame.myServerThreads[i].SendData(data, true);
					return;
				}
			}
		}
	}

	// Client only:
	public static void sendDataToServer(byte[] data)
	{
		if (connected)
		{
			RunGame.myClient.SendData(data);
		}
	}


	public static void serverReceivedData(byte[] data, int fromWho)
	{
		try {

			int endPoint = data.Length;

			// RPC call. RPCs can be -> Client OR -> Server
			// The next byte will contain the object ID:
			short index = BitConverter.ToInt16(data, 0);
			if (index == 0 || (operationObjects.Count > 0 && operationObjects[index] != null && operationObjects[index].GetComponent<OperationView>() != null && operationObjects[index].GetComponent<OperationView>().gameObject != null))
			{
				byte[] rpcData = new byte[data.Length - 3];
				Buffer.BlockCopy(data, 3, rpcData, 0, data.Length - 3);
				endPoint = operationObjects[index].GetComponent<OperationView>().ReceiveRPC(data[2], fromWho, rpcData) + 3;
			} else
			{
				byte[] rpcData = new byte[data.Length - 3];
				Buffer.BlockCopy(data, 3, rpcData, 0, data.Length - 3);
				endPoint = OperationView.ReceiveFakeRPC(data[2], fromWho, rpcData) + 3;
			}

			if (endPoint < data.Length)
			{
				byte[] nextData = new byte[data.Length - endPoint];
				Buffer.BlockCopy(data, endPoint, nextData, 0, data.Length - endPoint);
				serverReceivedData(nextData, fromWho);
			}

		} catch (Exception e)
		{
			Debug.LogError("Error at server receive: " + e.Message);
		}
	}

	static int runMovementCommandData(ServerPerson theClient, short packetID, byte[] data, bool runPlayerInput, short index) {
		byte numFrames = data [0];
		int endPoint = 1;
		for (int i = 0; i < numFrames; i++) {
			byte length = data [endPoint++];
			byte[] dataForPlayerInput = new byte[length];
			Buffer.BlockCopy (data, endPoint, dataForPlayerInput, 0, length);
			endPoint += length;

			if (index != -1 && runPlayerInput) {
				operationObjects [index].GetComponent<PlayerMove> ().movementData (packetID, dataForPlayerInput);
			}
		}
		return endPoint;
	}

	public static void serverReceivedInput(byte[] data, int fromWho, short packetID)
	{

		// We need to go out and find the index for this object:
		short index = -1;
		if (GameManager.GetPlayer ((short)fromWho).playerObject != null) {
			index = GameManager.GetPlayer ((short)fromWho).playerObject.GetComponent<SyncGameState> ().objectID;
		}

		// In the model where commands are sent more than once to avoid excessive lag due to packet loss,
		// We must check that the packetID is not the same. 
		ServerPerson thisClient = getClient((short)fromWho);


		bool runPlayerInput = true;
		if ((short)(thisClient.lastPlayerInputGroupID + 1) == packetID) {
			// Good to go
			thisClient.lastPlayerInputGroupID++;

			// By definition, this should never happen:
			if (thisClient.inputCommands.ContainsKey (packetID)) {
				Debug.LogError ("Packet existed but wasn't run! OperationNetwork -> serverReceivedInput");
				thisClient.inputCommands.Remove (packetID);
			}

			// I would assume its limited to 30ish (20)
			for (int i = 1; i < 30; i++) {
				if (thisClient.inputCommands.ContainsKey ((short)(packetID + i))) {
					thisClient.lastPlayerInputGroupID = (short)(packetID + i);
					runMovementCommandData (thisClient, packetID, thisClient.inputCommands [(short)(packetID + i)], true, index);
					thisClient.inputCommands.Remove ((short)(packetID + i));
				} else {
					break;
				}
			}


			// This allows for a maximum of (8) buffered input commands, which is WAY more than we should ever have.. probably.. -but now out of order needs to be considered
		} else {
			bool found = false;

			// OLD packets: (This includes the same packet, of course).
			for (int i = 0; i < 160; i++) {
				if ((short)(thisClient.lastPlayerInputGroupID - i) == packetID) {
					found = true;
					runPlayerInput = false;
					break;
				}
			}
			// We assume that these are simply packets that are slightly out of order.. 
			if (!found) {

				// FUTURE packets. Note that 30 means that in the current system, after not receiving a certain packet in 400ms, you will rubber band for that entire second. TODO make it so it runs what it can when rubber banding occurs
				for (int i = 2; i < 20; i++) {
					if ((short)(thisClient.lastPlayerInputGroupID + i) == packetID) {
						found = true;
						runPlayerInput = false;
						if (!thisClient.inputCommands.ContainsKey(packetID)) {
							thisClient.inputCommands.Add (packetID, data);
						}
						break;
					}
				}
				// First, we are going to check if the command was from before. These simply need to be skipped


				// Otherwise, we assume the command is too late and we run all buffered input commands including this one to "catch up"
				if (!found) {
					Debug.LogError ("Rubber Banding!!! " + thisClient.lastPlayerInputGroupID + " / " + packetID);

					thisClient.inputCommands.Clear();

					// And start off input from hereon out:
					runPlayerInput = true;
					thisClient.lastPlayerInputGroupID = packetID;
				}
			}


			// Note that packets more out of order than the above limit will cause all previous packets to be skipped.
			// Running an out of order packet will cause all packets available to run, regardless if +1 order is maintained.
		}
		// The code after this will run to iterate through the data, but with a bool to see if the code should actually be simulated.

		int endPoint = runMovementCommandData (thisClient, packetID, data, runPlayerInput, index);

		if (endPoint < data.Length)
		{
			// And then the next input command is read: (We technically don't have to run this if the player doesn't exist)
			byte[] nextData = new byte[data.Length - endPoint];
			Buffer.BlockCopy(data, endPoint, nextData, 0, data.Length - endPoint);
			serverReceivedInput(nextData, fromWho, (short)(packetID + 1));
		}
	}

	// Client only receives data from server:
	public static void clientReceivedData(byte[] data)
	{
		bool isInOrder;
		// Before receving data, we must check if the choke is within valid range

		int nextIndex = GameManager.thisGameManager.GetComponent<Interp> ().recieveData (data, true, out isInOrder); // Main GS always comes first
		if (isInOrder) {
			byte[] nextData = new byte[data.Length - nextIndex];
			Buffer.BlockCopy (data, nextIndex, nextData, 0, nextData.Length);
			nextIndex = GameManager.thisGameManager.GetComponent<Interp> ().recieveData (nextData, false, out isInOrder); // nextIndex not used after here
		}
	}
}
