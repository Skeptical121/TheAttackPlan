using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;

public class GameState
{

	public static int lastChoke = 0;


	public int tickNumber; // This is only referenced outside of GameState in Interp.

	List<short> objectIDs;
	List<byte> objectTypes;
	List<bool> objectExists; // Client side only.
	List<object[]> netInformation;
	List<byte[]> netDataInterp; // Server side only. By saving this, I don't get the information again when doing add stuff. It's eh.
	List<short> netPlayerOwnerIDs;


	GameState[] previousGameState; // These are the states, (in order as follows: {1 tick previous, 2 ticks previous, 3 ticks previous, ...})
	GameState previousGameStateClient = null;

	// Changed information:
	List<object[]> changedInfo = new List<object[]>(); // Server side only. The concept is key to client though
	List<byte[]> changedDataInterp = new List<byte[]>(); // Server side only. The concept is key to client though
	List<short> changedPlayerOwnerIDs = new List<short>();

	List<object[]> destroyedInfoLaterTicks = new List<object[]>();
	List<byte[]> destroyedDataInterpLaterTicks = new List<byte[]>();
	List<short> destroyedLaterTicksPlayerOwnerIDs = new List<short>(); // As the object has been destroyed, we have to get the player owner ID of the destroyed object to determine its sendout

	// Changed information regarding objects that were just destroyed- this data is the only data that is used on next gamestates for the sendout process
	List<object[]> destroyedChangedInfo = new List<object[]>();
	List<byte[]> destroyedDataInterp = new List<byte[]>();
	List<short> destroyedPlayerOwnerIDs = new List<short>(); // This is used for setting destroyedLaterTicksPlayerOwnerIDs

	// On the player, additional "objects" are sent for the syncing process. Player still syncs normally..
	// But, prediction errors will use "Player.thisPlayer" to set such values. They are objects with specific objectType (246)
	// Its sync is different. It sends the object multiple times 

	// Server
	void fullUnchangedGameState(short player) { // Where player is set to -1 if it is the Main GameState
		// It guesses at the size:
		objectIDs = new List<short> (OperationNetwork.operationObjects.Count);
		objectTypes = new List<byte> (OperationNetwork.operationObjects.Count);
		objectExists = new List<bool> (OperationNetwork.operationObjects.Count);
		netInformation = new List<object[]> (OperationNetwork.operationObjects.Count);
		netDataInterp = new List<byte[]> (OperationNetwork.operationObjects.Count);
		netPlayerOwnerIDs = new List<short> (OperationNetwork.operationObjects.Count);

		for (int i = 0; i < OperationNetwork.operationObjects.Count; i++) {
			if (OperationNetwork.operationObjects [i] != null && (player == -1 || (OperationNetwork.operationObjects [i].getPreditivePlayerOwner() == player))) {
				objectIDs.Add (OperationNetwork.operationObjects [i].objectID);
				objectTypes.Add (OperationNetwork.operationObjects [i].objectType);
				objectExists.Add (OperationNetwork.operationObjects [i].exists);
				byte[] dataInterp;
				netInformation.Add (OperationNetwork.operationObjects [i].GetInformation (out dataInterp, player != -1 && (OperationNetwork.operationObjects [i].getPreditivePlayerOwner() == player)));
				netDataInterp.Add (dataInterp);
				netPlayerOwnerIDs.Add(OperationNetwork.operationObjects [i].getPreditivePlayerOwner());
			}
		}
	}

	// Server
	public GameState (GameState[] pgs, int tickNumber, short player) // Where player is set to -1 if it is the Main GameState
	{
		this.previousGameState = pgs;
		this.tickNumber = tickNumber;


		fullUnchangedGameState (player);

		// Send entire gamestate for the first few ticks: (Remember, missing the appropiate # of ticks to miss something like this will trigger a full resend; so no issue here)
		for (int i = 0; i < previousGameState.Length; i++) {
			if (previousGameState [i] == null) {
				changedInfo = netInformation; // Voila
				changedDataInterp = netDataInterp; // And voila
				changedPlayerOwnerIDs = netPlayerOwnerIDs;

				// This is only used for later ticks:
				for (int t = 0; t < changedDataInterp.Count; t++) {
					if (!OperationNetwork.operationObjects [objectIDs [t]].GetComponent<SyncGameState> ().exists) {
						destroyedChangedInfo.Add (changedInfo[t]);
						destroyedDataInterp.Add (changedDataInterp[t]);
						destroyedPlayerOwnerIDs.Add (OperationNetwork.operationObjects [objectIDs [t]].GetComponent<SyncGameState> ().getPreditivePlayerOwner ());
					}
				}

				// Full gamestate = set gamestate tick number to negative

				return;
			}
		}

		for (int now = 0; now < netInformation.Count; now++) {
			List<object[]> oldInfo = new List<object[]> ();
			for (int pgsIndex = 0; pgsIndex < previousGameState.Length; pgsIndex++) {
				if (previousGameState [pgsIndex] != null) {
					for (int prev = 0; prev < previousGameState [pgsIndex].netInformation.Count; prev++) {
						if (objectIDs [now] == previousGameState [pgsIndex].objectIDs [prev]) {
							oldInfo.Add (previousGameState [pgsIndex].netInformation [prev]);
							break;
						}
					}
				}
			}
			if (oldInfo.Count < previousGameState.Length) {
				// It didn't exist in the previous state, so the object was created:
				// It does this for ALL "MAX_CHOKE_TICKS", since the creation of the object is considered a "difference", and since the object was created, all variables must sync as well. (Note that stuff like rockets don't actually change, so this is VERY important for them)
				changedInfo.Add (netInformation [now]);
				changedDataInterp.Add (netDataInterp [now]);
				changedPlayerOwnerIDs.Add (netPlayerOwnerIDs [now]);
			} else {
				
				byte[] dataInterp;
				object[] theChangedData = OperationNetwork.operationObjects [objectIDs [now]].GetComponent<SyncGameState> ().GetNewInformation (oldInfo, out dataInterp, 
					player == OperationNetwork.operationObjects [objectIDs [now]].GetComponent<SyncGameState> ().getPreditivePlayerOwner());

				if (theChangedData.Length > 0 || !OperationNetwork.operationObjects [objectIDs [now]].GetComponent<SyncGameState> ().exists) {
					// When exists = false, it NEEDS to send the object data!
					changedInfo.Add (theChangedData);
					changedDataInterp.Add (dataInterp);
					changedPlayerOwnerIDs.Add (OperationNetwork.operationObjects [objectIDs [now]].GetComponent<SyncGameState> ().getPreditivePlayerOwner ());
				}

				// This is only used for later ticks:
				if (!OperationNetwork.operationObjects [objectIDs [now]].GetComponent<SyncGameState> ().exists) {
					destroyedChangedInfo.Add (theChangedData);
					destroyedDataInterp.Add (dataInterp);
					destroyedPlayerOwnerIDs.Add (OperationNetwork.operationObjects [objectIDs [now]].GetComponent<SyncGameState> ().getPreditivePlayerOwner ());
				}
				// else it doesn't send anything
			}

		
			
		}
		// There is no reliance on the data being in order, so we're going to take this opportunity to destroy objects that missed their last frame-

		// To do this, we will.. simply send the object in its exists = false state, this works as Interp simply will not create the object once its interping between 2 exists = false states
		for (int pgsIndex = 0; pgsIndex < previousGameState.Length; pgsIndex++) {
			if (previousGameState [pgsIndex] != null) {
				for (int prev = 0; prev < previousGameState [pgsIndex].destroyedChangedInfo.Count; prev++) {
					destroyedInfoLaterTicks.Add (previousGameState [pgsIndex].destroyedChangedInfo [prev]);
					destroyedDataInterpLaterTicks.Add (previousGameState [pgsIndex].destroyedDataInterp [prev]);
					destroyedLaterTicksPlayerOwnerIDs.Add (previousGameState [pgsIndex].destroyedPlayerOwnerIDs [prev]);
				}
			}
		}
	}

	// Clientside:
	public GameState(byte[] data, int index, int length, GameState pgs, bool isMainGS, int tickNumberFromMainGS) {

		int endIndex = index + length;

		this.previousGameStateClient = pgs; // Client just saves the one..
		if (previousGameStateClient != null) {
			objectIDs = new List<short> (previousGameStateClient.objectIDs);
			objectTypes = new List<byte> (previousGameStateClient.objectTypes);
			objectExists = new List<bool> (previousGameStateClient.objectExists);
			netInformation = new List<object[]> (previousGameStateClient.netInformation);

			// Removal from list can be done first:
			for (int i = 0; i < objectExists.Count; i++) {
				if (!objectExists [i]) {
					objectIDs.RemoveAt (i);
					objectTypes.RemoveAt (i);
					objectExists.RemoveAt (i);
					netInformation.RemoveAt (i);
					i--;
				}
			}
		} else {
			objectIDs = new List<short> ();
			objectTypes = new List<byte> ();
			objectExists = new List<bool> ();
			netInformation = new List<object[]> ();
		}

		bool wasFullResend = false;
		if (isMainGS) {
			this.tickNumber = BitConverter.ToInt32 (data, index);
			if (this.tickNumber < 0) {
				wasFullResend = true;
				this.tickNumber = -this.tickNumber - 1;
			}
			index += 4;
		} else {
			this.tickNumber = tickNumberFromMainGS; // Since tickNumber can still be relevant here (used for triggers, for example)
		}

		if (pgs != null && !wasFullResend) {
			lastChoke = (int)(tickNumber - pgs.tickNumber - 1);
			if (tickNumber - pgs.tickNumber > 4) {
				// We still need to implement exists = false + trigger detection for later frames for this to work properly. -note trigger detection might have to be reworked in cases where the trigger is fairly important.
				
				Debug.LogError ("Choke: " + (tickNumber - pgs.tickNumber - 1));
			}
		}

		while (index < endIndex) { // index == endIndex by the end.

			short objectID = BitConverter.ToInt16 (data, index);
			byte objectType = data [index + 2];
			// Note how index is not iterated. This is handled by "getObjects" below.


			// It is notable that, with the exception of return state / not modifying pre state, access to this and previousGameObject should be the same.
			if (previousGameStateClient == null) {
				// dataInterp is not saved on client side as it is unimportant.
				AddObject(objectID, objectType, data, ref index, !isMainGS);
			} else {
				int prevIndex = previousGameStateClient.objectIDs.IndexOf (objectID);
				if (prevIndex == -1) {
					if ((data [index + 3] & (1 << 0)) != 0) {
						// If object didn't exist in the previous tick, then it must be created:
						AddObject (objectID, objectType, data, ref index, !isMainGS); // Note this doesn't actually create the object
					} else {
						// Throwaway (object is trying to be created while exists = false)
					OperationNetwork.instantiatableObjects [objectType].GetComponent<SyncGameState> ().getObjects (data, ref index, null, !isMainGS, this.tickNumber);
					}
				} else {
					// If object did exist in the previous tick, then the object must exist in the following tick. The reason being that removal from the list is done above, and this is the gamestate AFTER exists was set to false- meaning the last data was sent out.
					// The exception to this rule is if the object doesn't exist, in which case the index is simply iterated through in the else statement below, without setting anything
					int newIndex = objectIDs.IndexOf (objectID);
					if (newIndex != -1) { // newIndex == -1 if object was removed just now
						if (objectTypes [newIndex] != objectType) {
							Debug.LogError ("Failure To Destroy! GameState -> new GameState(byte[] data, ...) Old: " + objectTypes[newIndex] + " / New: " + objectType + " at index: " + objectID); // If this happens under normal non-bug cirumstances, the entire gamestate will be resent, so there's not too much of a worry here..
							// We can't exactly add the object back in..

							// What we can do, however, to try to salvage the few ms that the game is in the unstable state is set exists to false:
							if (data [index + 3] % 2 == 1) {
								data [index + 3]--;
							}
							// We DON'T set object type, as the data relies on that -> that will be taken care of next tick, which assuming no packet loss will have all the new changed data
							// We also don't set NetInformation for that same reason- which again, is definitely not a great way of handling it, but these chokes almost never happen even with 30% packet loss
							OperationNetwork.instantiatableObjects [objectType].GetComponent<SyncGameState> ().getObjects (data, 
							ref index, previousGameStateClient.netInformation [prevIndex], !isMainGS, this.tickNumber);
						} else {
							// objectID remains the same.
							// objectType remains the same. (A check is made above.. it is a complete failure of the game protocol.
							objectExists [newIndex] = (data [index + 3] & (1 << 0)) != 0; // Exists changes to false here for interp reasons. 

							//if (Player.thisPlayer != null) {
							netInformation [newIndex] = OperationNetwork.instantiatableObjects [objectType].GetComponent<SyncGameState> ().getObjects (data, 
							ref index, previousGameStateClient.netInformation [prevIndex], !isMainGS, this.tickNumber);
							// netDataInterp obviously stays the same.

							// playerOwner stays the same.
						}
					} else {
						// This will always happen when an object is destroyed as it now sends multiple ticks of its last "changedData"

						// All this will do is simply iterate through the data:

						// Throwaway- object has already been destroyed
						OperationNetwork.instantiatableObjects [objectType].GetComponent<SyncGameState> ().getObjects (data, 
						ref index, previousGameStateClient.netInformation [prevIndex], !isMainGS, this.tickNumber);
					}
				}
			}
		}
		if (index != endIndex) {
			Debug.LogError ("Major Error! Gamestate data length mismatch");
		}
	}

	void AddObject(short objectID, byte objectType, byte[] data, ref int index, bool isPlayerOwner) {
		objectIDs.Add(objectID); // So we add in this ID to the list. Note that IDs don't necessarily increase, as null references will be overwritten
		objectTypes.Add (objectType);
		objectExists.Add ((data [index + 3] & (1 << 0)) != 0);

		// Types are the same regardless of anything
		object[] objectsForNetInfo = OperationNetwork.instantiatableObjects [objectType].GetComponent<SyncGameState> ().getObjects (data, ref index, null, isPlayerOwner, this.tickNumber);
			netInformation.Add(objectsForNetInfo);
	}

	// INTERP

	// Note that every "GameState" will be a at least once in standard game play. "a, percent=0"
	public static void interp(GameState a, GameState b, float percent, bool isThisFirstTick, bool isThisMine) {

		// Turns out, the most relevant factor is what exists in the world versus what has been removed in a.objectsID. The gameState includes ALL the gameObjects with ALL the objects.

		// "changedInfo" is not used client side.

		// It is assumed the world previous is either correct or empty, (this assumption is valid)
		if (isThisFirstTick) {
			for (int i = 0; i < a.objectIDs.Count; i++) {
				if (a.objectIDs [i] >= OperationNetwork.operationObjects.Count) {
					while (a.objectIDs [i] > OperationNetwork.operationObjects.Count) {
						// This is called when you connect to a server because the IDs have already shifted
						OperationNetwork.operationObjects.Add (null);
					}
					SyncGameState added = MonoBehaviour.Instantiate (OperationNetwork.instantiatableObjects [a.objectTypes [i]]).GetComponent<SyncGameState> ();
					added.InitStart (isThisMine);
					OperationNetwork.operationObjects.Add (added);
				} else if (OperationNetwork.operationObjects [a.objectIDs [i]] == null) {
					SyncGameState added = MonoBehaviour.Instantiate (OperationNetwork.instantiatableObjects [a.objectTypes [i]]).GetComponent<SyncGameState> ();
					added.InitStart (isThisMine);
					OperationNetwork.operationObjects [a.objectIDs [i]] = added;
				} else if (OperationNetwork.operationObjects [a.objectIDs [i]].objectType != a.objectTypes [i]) {
					Debug.LogError ("Failure To Destroy! GameState -> interp");
				}

				SyncGameState sgs = OperationNetwork.operationObjects [a.objectIDs [i]];
				sgs.objectID = a.objectIDs [i];
				//objectType is already set, (and is not really used)
				if (!a.objectExists [i]) {
					try {
						sgs.OnDeath ();
					} catch (Exception e) {
						Debug.LogError ("Error on death: " + e.StackTrace);
					}
					MonoBehaviour.Destroy (sgs.gameObject);
					OperationNetwork.operationObjects [a.objectIDs [i]] = null;
				} else {
					if (b == null || percent == 0) {
						sgs.interp (a.netInformation [i], null, percent, isThisFirstTick, isThisMine);
					} else {
						// Find b: (b must exist as it wasn't destroyed last tick)
						int bI = b.objectIDs.IndexOf (a.objectIDs [i]);
						sgs.interp (a.netInformation [i], b.netInformation [bI], percent, isThisFirstTick, isThisMine);
					}
				}
			}
		} else {

			// No onDestroy or onFirstTick stuff here, as no destruction / instantiation.
			for (int i = 0; i < a.objectIDs.Count; i++) {
				if (OperationNetwork.operationObjects [a.objectIDs [i]]) {
					SyncGameState sgs = OperationNetwork.operationObjects [a.objectIDs [i]];
					if (b == null || percent == 0) {
						sgs.interp (a.netInformation [i], null, percent, isThisFirstTick, isThisMine);
					} else {
						// Find b: (b must exist as it wasn't destroyed last tick)
						int bI = b.objectIDs.IndexOf (a.objectIDs [i]);
						sgs.interp (a.netInformation [i], b.netInformation [bI], percent, isThisFirstTick, isThisMine);
					}
				}
			}
		}



			// Irrelivant if operation
	}

	// dataInterp is simply added to the changed object data here, it is just here to add to the byte data.
	public static byte[] getObjectData(byte[] dataInterp, object[] o) {
		byte[] returnData = new byte[1200]; // Buffer of 1200 bytes. That's a lot.
		int bytePosition = 0;
		if (dataInterp != null) {
			Buffer.BlockCopy (dataInterp, 0, returnData, bytePosition, dataInterp.Length);
			bytePosition += dataInterp.Length;
		}
		for (int i = 0; i < o.Length; i++) {
			byte[] objData = getObjectData (o [i]);
			Buffer.BlockCopy (objData, 0, returnData, bytePosition, objData.Length);
			bytePosition += objData.Length;
		}
		Array.Resize (ref returnData, bytePosition);
		return returnData;
	}

	public static byte[] getObjectData(object o) {
		if (o is byte) {
			return new byte[]{ (byte)o };
		} else if (o is short) {
			return BitConverter.GetBytes ((short)o);
		} else if (o is int) {
			return BitConverter.GetBytes ((int)o);
		} else if (o is long) {
			return BitConverter.GetBytes ((long)o);
		} else if (o is float) {
			return BitConverter.GetBytes ((float)o);
		} else if (o is double) {
			return BitConverter.GetBytes ((double)o);
		} else if (o is Vector3) {
			Vector3 v = (Vector3)o;
			byte[] returnData = new byte[12];
			Buffer.BlockCopy (BitConverter.GetBytes ((float)v.x), 0, returnData, 0, 4);
			Buffer.BlockCopy (BitConverter.GetBytes ((float)v.y), 0, returnData, 4, 4);
			Buffer.BlockCopy (BitConverter.GetBytes ((float)v.z), 0, returnData, 8, 4);
			return returnData;
		} else if (o is Quaternion) {
			Quaternion q = (Quaternion)o;
			byte[] returnData = new byte[16];
			Buffer.BlockCopy (BitConverter.GetBytes ((float)q.x), 0, returnData, 0, 4);
			Buffer.BlockCopy (BitConverter.GetBytes ((float)q.y), 0, returnData, 4, 4);
			Buffer.BlockCopy (BitConverter.GetBytes ((float)q.z), 0, returnData, 8, 4);
			Buffer.BlockCopy (BitConverter.GetBytes ((float)q.w), 0, returnData, 12, 4);
			return returnData;
		} else if (o is float[]) {
			byte[] returnData = new byte[1 + ((float[])o).Length * 4];
			returnData [0] = (byte)((float[])o).Length;
			for (int i = 0; i < ((float[])o).Length; i++) {
				Buffer.BlockCopy (BitConverter.GetBytes (((float[])o) [i]), 0, returnData, 1 + i * 4, 4);
			}
			return returnData;
		} else if (o is Vector3[]) {
			byte[] returnData = new byte[1 + ((Vector3[])o).Length * 12];
			returnData [0] = (byte)((Vector3[])o).Length;
			for (int i = 0; i < ((Vector3[])o).Length; i++) {
				Buffer.BlockCopy (getObjectData (((Vector3[])o) [i]), 0, returnData, 1 + i * 12, 12);
			}
			return returnData;
		} else if (o is Vector3[][]) {
			Vector3[][] v = (Vector3[][])o;
			int netLength = 0;
			for (int i = 0; i < v.Length; i++) {
				netLength += v [i].Length;
			}
			byte[] returnData = new byte[1 + v.Length + netLength * 12];
			int bytePosition = 0;
			returnData [bytePosition] = (byte)v.Length;
			bytePosition += 1;
			for (int i = 0; i < v.Length; i++) {
				returnData [bytePosition] = (byte)v [i].Length;
				bytePosition += 1;
				for (int j = 0; j < v [i].Length; j++) {
					Buffer.BlockCopy (getObjectData (v [i] [j]), 0, returnData, bytePosition, 12);
					bytePosition += 12;
				}
			}
			return returnData;
		} else if (o is string) {
			byte[] returnData = new byte[Encoding.ASCII.GetBytes ((string)o).Length + 1];
			returnData [0] = (byte)(returnData.Length - 1);
			Buffer.BlockCopy (Encoding.ASCII.GetBytes ((string)o), 0, returnData, 1, returnData.Length - 1);
			return returnData;
		} else if (o is DamageNumber[]) {
			byte[] returnData = new byte[1 + ((DamageNumber[])o).Length * 16];
			returnData [0] = (byte)((DamageNumber[])o).Length;
			for (int i = 0; i < ((DamageNumber[])o).Length; i++) {
				Buffer.BlockCopy (getObjectData (((DamageNumber[])o) [i]), 0, returnData, 1 + i * 16, 16);
			}
			return returnData;
		} else if (o is SyncableType) {
			return ((SyncableType)o).getData ();
		} else if (o is byte[]) {
			byte[] returnData = new byte[1 + ((byte[])o).Length];
			returnData [0] = (byte)((byte[])o).Length;
			for (int i = 0; i < ((byte[])o).Length; i++) {
				returnData [1 + i] = ((byte[])o) [i];
			}
			return returnData;
		}
		Debug.LogError ("getObjectData in GameState FAILURE for type! " + o);
		return null;
	}

	// Other GameState.

	public byte[] getDataWithoutPlayerData(bool changedData, short player, bool shouldSendFullGSRegardless) { // changedData is set to false for newly connecting players (could also be used for choke)
		List<byte> data = new List<byte> (100); // 100 is a reasonable guess, probably too high most of the time, but often significantly too low

		// Makes room for length var: (short)
		data.Add ((byte)0);
		data.Add ((byte)0);
		
		if (!changedData || shouldSendFullGSRegardless) 
			data.AddRange(BitConverter.GetBytes (-tickNumber - 1));
		else
			data.AddRange(BitConverter.GetBytes (tickNumber));
		// Curiously, since PlayerObject is the only thing that is predicted right now, that is the only data that is specific to the player- and the player input can be completely delegated to the PlayerObject- including the clientPackId

		if (!changedData || shouldSendFullGSRegardless) {
			for (int i = 0; i < netInformation.Count; i++) {
				if (netPlayerOwnerIDs [i] != player) {
					data.AddRange (getObjectData (netDataInterp [i], netInformation [i]));
				}
			}
		} else {
			for (int i = 0; i < changedInfo.Count; i++) {
				if (changedPlayerOwnerIDs[i] != player) {
					data.AddRange (getObjectData (changedDataInterp [i], changedInfo [i]));
				}
			}
		}
		for (int i = 0; i < destroyedInfoLaterTicks.Count; i++) {
			if (destroyedLaterTicksPlayerOwnerIDs [i] != player) {
				data.AddRange (getObjectData (destroyedDataInterpLaterTicks [i], destroyedInfoLaterTicks [i]));
			}
		}

		byte[] dArray = data.ToArray ();

		Buffer.BlockCopy (BitConverter.GetBytes ((short)(data.Count - 2)), 0, dArray, 0, 2);
		return dArray;
	}

	// For use with Player GameState -  player predictionerror state..
	public byte[] getChangedData() {

		List<byte> data = new List<byte> (100); // 100 is a reasonable guess, probably too high most of the time, but often significantly too low

		// Makes room for length var: (short)
		data.Add ((byte)0);
		data.Add ((byte)0);


		for (int i = 0; i < changedInfo.Count; i++) {
			data.AddRange(getObjectData(changedDataInterp[i], changedInfo[i]));
		}

		for (int i = 0; i < destroyedInfoLaterTicks.Count; i++) {
			data.AddRange(getObjectData(destroyedDataInterpLaterTicks[i], destroyedInfoLaterTicks[i]));
		}

		byte[] dArray = data.ToArray ();

		Buffer.BlockCopy (BitConverter.GetBytes ((short)(data.Count - 2)), 0, dArray, 0, 2);
		return dArray;
	}


}


