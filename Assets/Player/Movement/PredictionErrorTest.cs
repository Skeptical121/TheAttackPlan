using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredictionErrorTest {

	PlayerInput[] lastPlayerInput;
	object[][] lastPlayerState; // As PlayerMove handles the saving of PlayerState

	public PredictionErrorTest() {
		lastPlayerInput = new PlayerInput[500]; // 500..
		lastPlayerState = new object[500][];
	}


	void shiftPlayerBufferState()
	{
		// Player Input is not subject to prediction errors:
		for (int i = lastPlayerInput.Length - 1; i >= 1; i--)
		{
			lastPlayerInput[i] = lastPlayerInput[i - 1];
		}

		// Everything else is:
		for (int i = lastPlayerState.Length - 1; i >= 1; i--)
		{
			lastPlayerState[i] = lastPlayerState[i - 1];
		}
	}

	// Position is saved to compare it with the server data!!


	// The position that the server has is final.

	// However, initial states + all the Player Inputs need to be saved to recreate all the commands from that time.

	// ONLY PlayerMove needs to be updated, as that is the only thing that relies on position.
	// Everything else actually specifically should NOT be updated, because it relies on things not running more than once.
	public void savePlayerData(PlayerInput pI, PlayerMove parent)
	{
		shiftPlayerBufferState();
		lastPlayerInput[0] = pI;
		lastPlayerState [0] = PlayerState.createObjectList (parent.GetComponent<SyncPlayer>(), parent);
	}

	// isCrouchedVal is needed because you can't un-crouch when there is a ceiling above you
	// Phase distance used var is needed in case phase shift doesn't go through
	// Also player only. Not executed by server
	public void testForPredictionError(short origPacketID, object[] playerState, PlayerMove parent) // Its not that isCrouchedVal is expected to be wrong
	{
		for (int i = 0; i < lastPlayerInput.Length; i++)
		{
			if (lastPlayerInput[i] != null && lastPlayerInput[i].playerInputGroupID == origPacketID)
			{
				// Update ping time: (issueDesktop - this should be changed to tickNumber implementation) - While it could be, there is actually no data that is sent right now that would update this properly.
				Interp.actualPing = Time.time - lastPlayerInput[i].gameTime; // Large framerates actually mean that RTT is shorter than this value produces

				if (PlayerState.isDifferent (lastPlayerState [i], playerState)) {
					PlayerState.setObjects (playerState, parent.GetComponent<SyncPlayer> (), parent);
					// Prediction error.
					// Fully correct using RESIMULATION:
					lastPlayerState [i] = PlayerState.createObjectList (parent.GetComponent<SyncPlayer> (), parent); // lastPlayerInput doesn't have to be changed, of course.

					for (int n = i - 1; n >= 0; n--) { // n = i - 1 because this data is saved after the simulation is done.
						// PLAYER EXECUTION
						parent.playerAndServer (lastPlayerInput [n], false);
						parent.GetComponent<ClassControl> ().PlayerAndServer (lastPlayerInput [n], false);

						// Overwrites the data:
						lastPlayerState [n] = PlayerState.createObjectList (parent.GetComponent<SyncPlayer> (), parent);
					}
				}
				return;
			}
		}


		// Else run all input this player has; just match.. This is what happens when you spawn:
		// Ping not calculated here; nor is isDifferent
		PlayerState.setObjects (playerState, parent.GetComponent<SyncPlayer> (), parent);
		// lastPlayerState is not updated either; as their is no playerInput to match it with. (and is unimportant, as isDifferent isn't used for this; as said above)
		for (int n = lastPlayerInput.Length - 1; n >= 0; n--) { // n = i - 1 because this data is saved after the simulation is done.
			// PLAYER EXECUTION

			// ALL frames are run.
			if (lastPlayerInput [n] != null) {
				parent.playerAndServer (lastPlayerInput [n], false);
				parent.GetComponent<ClassControl> ().PlayerAndServer (lastPlayerInput [n], false);

				// Overwrites the data:
				lastPlayerState [n] = PlayerState.createObjectList (parent.GetComponent<SyncPlayer> (), parent);
			}
		}
	}
}
