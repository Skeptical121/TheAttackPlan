using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

// This is the global method that REPRESENTS the world for clients.

// The only thing it doesn't cover clientside is player movement / commands.
public class Interp : MonoBehaviour {

	// The rules of interp:

	// tickNumber will skip over 65536, so relative tickNumbers MUST be called from relative Tick Number appropiate methods here.

	// Time should NEVER go backwards. - this would cause events like deathAnimations to possibly happen twice. (or more)

	// Every tick must be accounted for, even if the framerate is slow enough that it skips the tick. 
	// This is, once again, for render purposes like deathAnimation (and possibly other short term effects)
	// Such render effects should all have a Time.time advance on their effects on spawn to account for being "late" to them, (so they're not just playing like 0.2s in the future to everything else for example)

	public static float interp = 0.025f;

	public static int lastTickLoaded = -1; // No ticks loaded. Generally don't use this unless it's for reading purposes

	public static float lastDeltaTickNumber = 0; // Always positive
	// Official tick number:
	static float tickNumber = 0; // It is set to recievedTick - interp, on an average basis. This is the official tick number.

	public static float currentTickDelta = 0;
	
	GameState[] receivedFullStates = new GameState[12]; // Old code saved 12, 12 seems like plenty, considering that interp is 1.25 ticks.

	GameState lastReceivedPlayerState = null;


	// This isn't really interp related, but it is client related:
	// Ping time is really only updated for prediction. (Which is very important for sentry, earthMound, and should be used in more cases)
	public static float actualPing = 0; // Updated by player
	public static float usedPing = 0; // Even for displaying, this is used, as it is technically more accurate due to actualPing being effected by individual packets being slow.
	public static float displayPing = 0;
	public GameObject pingPanel;

	// Thus, assumed to be > 5FPS. If not, ticks will be skipped, which will mess up some render stuff.. this is fine.

	// This should only be used to set initial lifeTimes for SHORT DURATION triggers.
	public static int getTickNumber() {
		return (int)tickNumber;
	}

	public static float getRepTickNumber() {
		return tickNumber;
	}

	// Use this for initialization
	void Start() {
		// A thought: The map should load in on connect.
	}

	void updateTickNumberFixedUpdate()
	{
		if (OperationNetwork.connected && !OperationNetwork.isServer && !OperationNetwork.isDemo) {
			if (currentTickDelta > 0) {
				tickNumber += currentTickDelta / 8; // "Half life" of 1/10ish seconds. Seems reasonable.
			}
		}
	}

	void FixedUpdate() {
		// As to be fairly consistent:
		updateTickNumberFixedUpdate();
	}

	void UpdateTickNumberUpdate()
	{
		if (OperationNetwork.connected && !OperationNetwork.isServer && !OperationNetwork.isDemo) {
			if (currentTickDelta < 0) {
				// The best that can be done is pausing time. However, for obvious reasons, the pace should be fairly consistent.
				// Thus, the slowest possible is 1/2 time.

				// This is based on framerate..
				tickNumber += Mathf.Max (currentTickDelta / 8, -(Time.deltaTime / Time.fixedDeltaTime) * 0.5f); // Time might have to pause anyways, so limiting it too much is silly.
			}
		}
	}

	void UpdatePing() {
		if (OperationNetwork.connected && !OperationNetwork.isServer && !OperationNetwork.isDemo) {
			// When dead, ping is not updated.. hmm..

			usedPing = actualPing + Mathf.Pow (0.001f, Time.deltaTime) * (usedPing - actualPing);
			displayPing = usedPing + Mathf.Pow (0.5f, Time.deltaTime) * (displayPing - usedPing);

			pingPanel.GetComponent<Text> ().text = "Ping: " + ((int)(displayPing * 1000) - 25); // 25ms is currently the exact calculation of the average RTT in ideal conditions.
			// Player side updates = 50. 20ms per update, thus 10ms
			// Server side updates = 50. 20ms per update, thus 10ms
			// Interp = 25ms. The latest value is used, which means 1/5 of the time, interp will miss this. This is the fault of how I've done the code, but it's only 5ms here.
		}
	}

	public void InterpUpdate() {

		float lastTickNumber = tickNumber;

		UpdatePing ();

		UpdateTickNumberUpdate();

		// Generally, objects that are updated by interp will not be updated again, so this can just be in Update()

		// This is, in a sense, a global update function for clientside server stuff.

		// .. readMessages()

		// RecievedFullStates should technically be in order:

		tickNumber += (Time.deltaTime / Time.fixedDeltaTime); // This'll probably be from 0.5 <-> 2.0 ish

		lastDeltaTickNumber = tickNumber - lastTickNumber;

		float indexToLoad = getTickToLoad();



		if (indexToLoad != -1) // No state to load if -1
		{

			// "theTick" does exist, so it will end the loop.
			// Loads every tick:
			int theTick = receivedFullStates [Mathf.FloorToInt (indexToLoad)].tickNumber;
			int indexPos = receivedFullStates.Length - 1;
			for (int a = lastTickLoaded + 1; a < theTick;) {
				if (receivedFullStates [indexPos] != null && receivedFullStates [indexPos].tickNumber == a) {
					bool wasThisFirstTick = lastTickLoaded < receivedFullStates [indexPos].tickNumber; // This can be false, of course.
					lastTickLoaded = a;
					GameState.interp (receivedFullStates [indexPos], null, 0, wasThisFirstTick, false);
					// Checks next:
					a++;
					indexPos--;
				} else if (receivedFullStates [indexPos] != null && receivedFullStates [indexPos].tickNumber > a) {
					// a increases
					a++;
				} else {
					// indexPos increases
					indexPos--;
				}
			}

			bool isThisFirstTick = lastTickLoaded < receivedFullStates [Mathf.FloorToInt (indexToLoad)].tickNumber;

			// Sets lastTickLoaded BEFORE in case an error happens, as not to repeat the error & not to repeat other things that shouldn't be. 
			// This can be an issue for visuals in the short term, (<10 seconds)

			lastTickLoaded = receivedFullStates[Mathf.FloorToInt(indexToLoad)].tickNumber;
			if (indexToLoad % 1 == 0)
			{
				// It actually has to, for 2 cases within getTickToLoad() work like this: (first 12 ticks + choke past)
				GameState.interp(receivedFullStates[Mathf.FloorToInt(indexToLoad)], null, 0, isThisFirstTick, false);
			}
			else {
				// "tickToLoad % 1" is actually reversed. So it counts up like this, assuming a fast framerate: 5.6 -> 5.2 -> 6.8 -> 6.4 -> 6.0
				GameState.interp(receivedFullStates[Mathf.FloorToInt(indexToLoad)], receivedFullStates[Mathf.FloorToInt(indexToLoad) - 1], indexToLoad % 1, isThisFirstTick, false);
			}

		}

		
	}

	float getTickToLoad() {
		for (int i = 0; i < receivedFullStates.Length; i++)
		{
			if (receivedFullStates[i] == null)
			{
				// This will only occur on the first 12 ticks currently.
				return i - 1; // This is the last state. It will return -1 if there is no states;
			} else if (tickNumber >= receivedFullStates[i].tickNumber) // This is asking: Is the official tick number after this state
			{
				if (i == 0) {
					// !! Choke Future !!
					return 0;
				} else {
					// returns: (i - 1) + (currentTick - OlderTick) / (NewerTick - OlderTick)
					// So it returns the "i" of the previous tick, plus the % the currentTick has progressed from tick(i) to tick(i - 1).
					return i + (tickNumber - receivedFullStates[i].tickNumber) / (receivedFullStates[i - 1].tickNumber - receivedFullStates[i].tickNumber); 
				}
			}
		}
		// !! Choke Past !!
		return receivedFullStates.Length - 1;
	}

	public static float getLifeTime(int tickSpawnedAt) {
		return ServerState.getLifeTime (tickNumber, tickSpawnedAt);
	}

	// Non-recursive.. but it still can handle more than one tick.
	public int recieveData(byte[] data, bool isMainGS, out bool isInOrder)
	{
		isInOrder = true;


		int index = 0;
		short length = BitConverter.ToInt16(data, 0);
		index += 2;
		if (OperationNetwork.isDemo) {
			OperationNetwork.currentByte += 2;
		}
		// It is assumed to be valid, of course: (as this is server -> player

		if (isMainGS) {
			// We check what the tick number will be:
			int gameStateTickNumber = BitConverter.ToInt32 (data, index);

			bool fullGameSend = false;

			// Negative means that it's a full gamestate send:
			if (gameStateTickNumber < 0) {
				fullGameSend = true;
				gameStateTickNumber = -gameStateTickNumber - 1;
				Debug.Log ("Full resend received: " + gameStateTickNumber);
			}

			// Gamestates that are out of order are ignored..
			if (fullGameSend || (receivedFullStates[0] != null && (gameStateTickNumber > receivedFullStates [0].tickNumber && gameStateTickNumber <= receivedFullStates[0].tickNumber + ServerState.MAX_CHOKE_TICKS))) {
				GameState ngs = new GameState (data, index, length, receivedFullStates [0], true, -1);
				shiftBuffer (receivedFullStates); // Pushes off the oldest one.
				receivedFullStates [0] = ngs;
				if (receivedFullStates [1] == null) {
					// Set initial variables:
					tickNumber = receivedFullStates [0].tickNumber - interp / Time.fixedDeltaTime;
					lastTickLoaded = Mathf.FloorToInt (tickNumber); // This is an initial variable. Last tick loaded is to do with interp, so it starts off based on data recieved time sub interp
					currentTickDelta = 0;
				} else {
					currentTickDelta = (receivedFullStates [0].tickNumber - interp / Time.fixedDeltaTime) - tickNumber;
					// Doesn't edit tickNumber.
				}
				OperationNetwork.lastTickLoaded = Mathf.FloorToInt (receivedFullStates [0].tickNumber);
			} else {
				if (receivedFullStates[0] == null || gameStateTickNumber > receivedFullStates [0].tickNumber + ServerState.MAX_CHOKE_TICKS) {
					Debug.LogError ("Choke is too high! " + gameStateTickNumber);

					// Ask for full resend:
					OperationView.RPC (null, "ResendTicks", OperationNetwork.ToServer);
				}
				isInOrder = false; // Hand this information over so lastReceivedPlayerState doesn't update either
			}
		} else {
			lastReceivedPlayerState = new GameState (data, index, length, lastReceivedPlayerState, false, receivedFullStates[0].tickNumber); // Since main gamestate is always read right before playerstate
			// Instantly run "interp" on this state, even though there it's just the one tick
			GameState.interp (lastReceivedPlayerState, null, 0, true, true);
		}


		index += length; // Includes the length from above already.



		if (OperationNetwork.isDemo) {
			OperationNetwork.currentByte += length;
		}

		return index;
	}

	// Feel free to use this.. it makes the code far more clear.
	public static void shiftBuffer(Array array)
	{
		Array.Copy (array, 0, array, 1, array.Length - 1);
	}
		
	public static void shiftBuffer(float[] array, float newObject) {
		shiftBuffer (array);
		array [0] = newObject;
	}
}
