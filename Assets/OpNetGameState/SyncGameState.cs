using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Collections.Generic;

// There is a requirement that each syncGameState has a getByteChoicesLength() > 0
public abstract class SyncGameState : MonoBehaviour {

	// The following are set on spawn; and only on spawn. (This includes server & client interp)
	public short objectID;
	public byte objectType; // So it knows what to spawn. This is set on the prefab 

	public bool exists = true; // Technically this one always syncs, but it's just through a bit.
	// On Destroy, the object exists is set to false, so it is pending destroy, (then it's destroyed, of course), it is sent to everyone as to account for interp, then it is destroyed.


	// Because multiple ticks can be read at the same time, interp will not always be able to handle the first tick.

	// Extra variables that don't always sync:
	public short playerOwner; // Obviously this requires syncing in most cases
	public int tickSpawnedAt; // Note that this does require syncing, it can't be done on spawn because of UDP & loading in late.

	public bool didFirstInterp = false; // Client only. Server just does this right after InitStart; so its not needed, client has to get the first interp information.

	bool justAdded = true; // Server only. This is to fix how spawning works during serversyncfixedupdate.

	public void didIteration() {
		justAdded = false;
	}

	public bool shouldDoJustAdded() {
		return justAdded;
	}

	public float getLifeTimeInterp() {
		if (OperationNetwork.isServer) {
			Debug.LogError ("Using interp time when this should be using server time!!!!");
			return ServerState.getLifeTime (tickSpawnedAt);
		}
		return Interp.getLifeTime (tickSpawnedAt);
	}

	public float getLifeTimeServer() {
		if (!OperationNetwork.isServer) {
			Debug.LogError ("Using server time when this should be using interp time!!!!");
			return Interp.getLifeTime (tickSpawnedAt);
		}
		return ServerState.getLifeTime (tickSpawnedAt);
	}

	public virtual object getTriggerData(byte index, byte[] data, ref int bytePosition, bool isPlayerOwner) {
		return null; // To be overwritten
	}

	// This just passes the full data set in. CLIENT SIDE
	public object[] getObjects(byte[] data, ref int bytePosition, object[] lastObjects, bool isPlayerOwner, int tickNumber)
	{
		int initialIndex = bytePosition;

		int bitChoicesLength = getBitChoicesLength(isPlayerOwner);

		int byteChoicesByteLength = (bitChoicesLength + 7) / 8;

		// The dataInterp is just part of the data:
		object[] returnObjects = new object[bitChoicesLength - 1];

		
		bytePosition = bytePosition + 3 + byteChoicesByteLength; 

		// Reiterates through the byteChoices:
		for (int i = 1; i < bitChoicesLength; i++)
		{
			if ((data[initialIndex + 3 + i / 8] & (1 << (i % 8))) != 0)
			{
				// Acquire each object from data. New types of objects should be created if, for example, different accuracies of Vector3 are wanted to be written / read.
				returnObjects[i - 1] = DataInterpret.interpretObject(data, ref bytePosition, getObject(i - 1, isPlayerOwner), this, tickNumber, isPlayerOwner); // Last is type. getObject will return the right type.
			} else
			{
				// Adds it anyways:

				if (lastObjects == null || lastObjects[i - 1] == null)
				{
					// If this happens, it's just a throwaway anyway.. should be.. at least.. TODO this is a possible error spot that could happen w/o choke

					// Well. It is ASSUMED that the current state is the old state in this case.
					returnObjects[i - 1] = getObject(i - 1, isPlayerOwner); // getObject is most likely not ready for clientSide use currently. (The type will be right, ofc)
				}
				else {
					returnObjects[i - 1] = lastObjects[i - 1];
				}
			}
		}

		return returnObjects;
	}

	// SERVER
	// The first object is the bytes that contain the "dataToChange"
	public object[] GetInformation(out byte[] dataInterp, bool isPlayerOwner)
	{

		int bitChoicesLength = getBitChoicesLength(isPlayerOwner);

		int byteChoicesByteLength = (bitChoicesLength + 7) / 8;

		dataInterp = new byte[3 + byteChoicesByteLength];

		// Copies in ID / Type:
		Buffer.BlockCopy(BitConverter.GetBytes(objectID), 0, dataInterp, 0, 2);
		dataInterp[2] = objectType;

		// Exists is set manually in dataInterp:
		if (exists)
			dataInterp[3] += (1 << 0);

		// Everything must be changed:
		for (int i = 1; i < bitChoicesLength; i++)
		{
			// No if statement needed:
			dataInterp[3 + i / 8] += (byte)(1 << (i % 8));
		}

		// It just adds all the objects:
		object[] returnObjects = new object[bitChoicesLength - 1];
		for (int i = 1; i < bitChoicesLength; i++)
		{
			returnObjects[i - 1] = getObject(i - 1, isPlayerOwner);
		}
			

		return returnObjects;
	}

	public virtual short getPreditivePlayerOwner() {
		return -2; // Not Applicable
	}

	// Server
	public object[] GetNewInformation(List<object[]> oldInformation, out byte[] dataInterp, bool isPlayerOwner)
	{

		int bitChoicesLength = getBitChoicesLength(isPlayerOwner);

		int byteChoicesByteLength = (bitChoicesLength + 7) / 8;

		int count = 0;
		dataInterp = new byte[byteChoicesByteLength + 3];
		
		// Copies in ID / Type:
		Buffer.BlockCopy(BitConverter.GetBytes(objectID), 0, dataInterp, 0, 2);
		dataInterp[2] = objectType;

		// Exists is set manually in dataInterp:
		if (exists)
			dataInterp[3] += (1 << 0);

		// Sets what needs to be changed:
		for (int i = 1; i < bitChoicesLength; i++)
		{
			for (int x = 0; x < oldInformation.Count; x++) {
				if (DataInterpret.isObjectDifferent (getObject (i - 1, isPlayerOwner), oldInformation [x] [i - 1])) {
					count++;
					dataInterp [3 + i / 8] += (byte)(1 << (i % 8));
					break;
				}
			}
		}

		// It only adds the neccessary objects:
		object[] returnObjects = new object[count];
		count = 0;
		for (int i = 1; i < bitChoicesLength; i++)
		{
			if ((dataInterp[3 + i / 8] & (1 << (i % 8))) != 0)
			{
				returnObjects[count++] = getObject(i - 1, isPlayerOwner);
			}
		}

		return returnObjects;
	}

	// This sets all information in the same order as getObject would suggest. (As in, getBitChoicesLenght = data.Length, for example)
	// data is the interped data according to standard rules. Extra interp rules can be done with a & b, in a similar way to how standard interp is done here. (Basically, a & b are not used for almost all SetInformation calls)
	public abstract int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine);

	// Unlike InitStart, this is only used in cases that RELY on stuff like "playerOwner".
	// This is Client AND Server. This is identical to InitStart for server, but is essential on client for using interp variables in the start code.. like position?
	public virtual void AfterFirstInterp () {
		// n/a
	}

	// Client AND Server. This HAS to be used instead of Start(). Stuff like parenting should be set here.
	public abstract void InitStart(bool isThisMine);

	// Server:
	public abstract void ServerSyncFixedUpdate();

	// This is simply the length of the objects as can be got through "getObject"
	public abstract int getBitChoicesLengthThis(bool isPlayerOwner);

	int getBitChoicesLength(bool isPlayerOwner)
	{
		return getBitChoicesLengthThis(isPlayerOwner) + 1;
	}

	// Server gets object by index. On client, the type must be accessable in prefab. bool isPlayerOwner might be paired with bool isTeam in the future.
	public abstract object getObjectThis(int num, bool isPlayerOwner);

	object getObject(int num, bool isPlayerOwner)
	{
		return getObjectThis(num, isPlayerOwner);
	}

	// Client AND Server.
	public abstract void OnDeath();


	// INTERP

	// Interps between ALL objects: 0 <= percent < 1.
	// b can be null if percent == 0. b could be null in any case, actually.

	// a.Length should equal b.Length, of course.
	public void interp(object[] a, object[] b, float percent, bool isThisFirstTick, bool isThisMine)
	{
		// Interp is not used for the first "frame" because interpolation can't be done on "SyncPlayer" because it switches what data it uses..
		if (b == null || percent == 0 || !didFirstInterp)
		{
			SetInformation(a, a, b, isThisFirstTick, isThisMine);
			if (!didFirstInterp) {
				didFirstInterp = true;
				AfterFirstInterp ();
			}
			return;
		}

		object[] c = new object[a.Length];
		for (int i = 0; i < a.Length; i++)
		{
			c[i] = DataInterpret.interp(a[i], b[i], percent);
		}
		SetInformation(c, a, b, isThisFirstTick, isThisMine);
	}

}
