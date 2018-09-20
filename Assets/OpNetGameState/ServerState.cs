using UnityEngine;
using System;
using System.Collections;

// ServerState is one of the many scripts contained within the master object.
public class ServerState : MonoBehaviour {

	public const int MAX_CHOKE_TICKS = 7; // This MUST be <16 because of how TriggerSet works right now


	public static int tickNumber; // Tick number goes hand in hand with serverState.


	// 10 = MAX ticks until choke is too many. This is 200ms.
	public static GameState[] previousGameStates = new GameState[MAX_CHOKE_TICKS]; // With the new revamped isDifferent system, having so many previous game states will be totally okay for the data sendout


	void Start () {
		
	}
	
	// This NEEDS to be the last FixedUpdate that runs.
	void FixedUpdate()
	{
		// This class is only for server use
		if (!OperationNetwork.isServer)
			return;
	}

	public static void iterateTickNumber() {
		tickNumber++;
	}

	public static float getLifeTime(int tickSpawnedAt) {
		if (!OperationNetwork.isServer) {
			Debug.LogError ("This must not be used on client! ServerState -> getLifeTime");
		}
		return getLifeTime (tickNumber, tickSpawnedAt);
	}

	// getLifeTime, UNLIKE getDeltaTime, is done in seconds. This is "tickNumber - tickSpawnedAt" (so reversed from getDeltaTime)
	public static float getLifeTime(float tickNumber, int tickSpawnedAt)
	{
		return (tickNumber - tickSpawnedAt) * Time.fixedDeltaTime;
	}
}
