using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class OperationView : MonoBehaviour {



	// Since no new items are added to this dictionary, the order is maintained, and therefore can automatically be assigned byte values.
	static Dictionary<string, byte> rpcIDList = new Dictionary<string, byte>(); // String converted to byte so it is prepared for delivery.
	static Dictionary<byte, OperationRPC> rpcList = new Dictionary<byte, OperationRPC>(); // Once arrived, the byte needs to be converted into a Method Call.

	public delegate int OperationRPC(GameObject obj, byte[] data, int fromWho);

	public void RPC(string methodName, int sendToSelf, params object[] objects)
	{
		RPC (this, methodName, sendToSelf, objects);
	}

	// sendToSelf is only for server use.
	public static void RPC(OperationView oV, string methodName, int sendToSelf, params object[] objects)
	{
		// The server doesn't send RPCs: (However the Server can send itself RPCs as if it was a client)
		if (OperationNetwork.isServer && sendToSelf != OperationNetwork.ToServer)
			return;

		// Demos aren't actually connected so they can't send RPCs.
		if (OperationNetwork.isDemo)
			return;

		// Needs to turn the data into bytes:
		byte[] data = new byte[300];
		int writeLocation = 3;
		for (int i = 0; i < objects.Length; i++)
		{
			if (objects[i] is float)
			{
				Buffer.BlockCopy(BitConverter.GetBytes((float)objects[i]), 0, data, writeLocation, 4);
				writeLocation += 4;
			}
			else if (objects[i] is int)
			{
				Buffer.BlockCopy(BitConverter.GetBytes((int)objects[i]), 0, data, writeLocation, 4);
				writeLocation += 4;
			}
			else if (objects[i] is bool)
			{
				data[writeLocation] = Convert.ToByte((bool)objects[i]);
				writeLocation += 1;
			}
			else if (objects[i] is byte)
			{
				data[writeLocation] = (byte)objects[i];
				writeLocation += 1;
			}
			else if (objects[i] is Vector3)
			{
				Buffer.BlockCopy(BitConverter.GetBytes(((Vector3)objects[i]).x), 0, data, writeLocation, 4);
				writeLocation += 4;
				Buffer.BlockCopy(BitConverter.GetBytes(((Vector3)objects[i]).y), 0, data, writeLocation, 4);
				writeLocation += 4;
				Buffer.BlockCopy(BitConverter.GetBytes(((Vector3)objects[i]).z), 0, data, writeLocation, 4);
				writeLocation += 4;
			}
			else if (objects[i] is Vector3[])
			{
				byte length = (byte)((Vector3[])objects[i]).Length; // Maximum length of 255
				data[writeLocation] = length;
				writeLocation += 1;
				for (int sk = 0; sk < length; sk++)
				{
					Buffer.BlockCopy(BitConverter.GetBytes(((Vector3[])objects[i])[sk].x), 0, data, writeLocation, 4);
					writeLocation += 4;
					Buffer.BlockCopy(BitConverter.GetBytes(((Vector3[])objects[i])[sk].y), 0, data, writeLocation, 4);
					writeLocation += 4;
					Buffer.BlockCopy(BitConverter.GetBytes(((Vector3[])objects[i])[sk].z), 0, data, writeLocation, 4);
					writeLocation += 4;
				}
			}
			else if (objects[i] is Quaternion)
			{
				Buffer.BlockCopy(BitConverter.GetBytes(((Quaternion)objects[i]).x), 0, data, writeLocation, 4);
				writeLocation += 4;
				Buffer.BlockCopy(BitConverter.GetBytes(((Quaternion)objects[i]).y), 0, data, writeLocation, 4);
				writeLocation += 4;
				Buffer.BlockCopy(BitConverter.GetBytes(((Quaternion)objects[i]).z), 0, data, writeLocation, 4);
				writeLocation += 4;
				Buffer.BlockCopy(BitConverter.GetBytes(((Quaternion)objects[i]).w), 0, data, writeLocation, 4);
				writeLocation += 4;
			}
			else if (objects[i] is float[])
			{
				byte length = (byte)((float[])objects[i]).Length; // Maximum length of 255
				data[writeLocation] = length;
				writeLocation += 1;
				Buffer.BlockCopy((float[])objects[i], 0, data, writeLocation, ((float[])objects[i]).Length * 4);
				writeLocation += ((float[])objects[i]).Length * 4;
			}
			else if (objects[i] is byte[])
			{
				if (((byte[])objects [i]).Length > 255) {
					Debug.LogError ("BYTE DATA OVERLOAD- Probably mirrors for playerInput hitscanData");
				}
				byte length = (byte)((byte[])objects[i]).Length; // Maximum length of 255
				data[writeLocation] = length;
				writeLocation += 1;
				Buffer.BlockCopy((byte[])objects[i], 0, data, writeLocation, ((byte[])objects[i]).Length);
				writeLocation += ((byte[])objects[i]).Length;
			}
			else if (objects[i] is string)
			{
				byte[] stringBytes = Encoding.ASCII.GetBytes((string)objects[i]);
				if (stringBytes.Length > 255) {
					// Cut it off:
					byte[] extraStringBytes = stringBytes;
					stringBytes = new byte[255];
					Buffer.BlockCopy (extraStringBytes, 0, stringBytes, 0, 255); 
				}
				data [writeLocation] = (byte)(stringBytes.Length);
				writeLocation += 1;
				Buffer.BlockCopy(stringBytes, 0, data, writeLocation, stringBytes.Length);
				writeLocation += stringBytes.Length;
			} else if (objects[i] is short)
			{
				Buffer.BlockCopy(BitConverter.GetBytes((short)objects[i]), 0, data, writeLocation, 2);
				writeLocation += 2;
			}
		}
		// Trim data: (Probably should just use writeLocation in "sendData" instead)
		Array.Resize(ref data, writeLocation);

		// All OperationViews come with a SyncGameState.
		if (oV != null) {
			Buffer.BlockCopy (BitConverter.GetBytes (oV.GetComponent<SyncGameState> ().objectID), 0, data, 0, 2);
		} else {
			Buffer.BlockCopy (BitConverter.GetBytes ((short)0), 0, data, 0, 2);
		}

		data[2] = rpcIDList[methodName]; // Which RPC call

		if (OperationNetwork.isServer)
		{
			// This is no longer how the server interacts with the players
			if (sendToSelf == OperationNetwork.ToServer) {
				byte[] sendData = new byte[data.Length - 3];
				Buffer.BlockCopy (data, 3, sendData, 0, data.Length - 3);
				oV.ReceiveRPC (data [2], OperationNetwork.FromServerClient, sendData);
			}
			return;
		} else
		{
			// Just send to server. (Client can't send anywhere else)
			OperationNetwork.sendDataToServer(data);
		}

	}

	public int ReceiveRPC(byte methodID, int fromWho, byte[] data)
	{
		// Instantly call it:
		return rpcList[methodID](gameObject, data, fromWho);
	}

	public static int ReceiveFakeRPC(byte methodID, int fromWho, byte[] data)
	{
		return rpcList[methodID](null, data, fromWho);
	}

	// List of all RPCs:

	// Your credit count is not predicted whatsoever- so your buying time is increased by ping time.
	static int BuyTrap(GameObject obj, byte[] data, int fromWho) {
		if (obj) obj.GetComponent<Player> ().BuyTrap (data [0], data[1]);
		return 2;
	}
		
	static int AddPlayerMade(GameObject obj, byte[] data, int fromWho)
	{
		if (obj) {
			// Must be alive
			byte placePlayerMadeType = data [0];
			PlacePlayerMade.AddObject(obj.GetComponent<PlayerMove>(), OperationNetwork.getVector3(data, 1), OperationNetwork.getQuaternion(data, 13), placePlayerMadeType, obj.GetComponent<Combat>().team, (short)fromWho);
		}
		return 29;
	}
		
	// (byte, byte)
	static int switchTeam(GameObject obj, byte[] data, int fromWho)
	{
		if (obj) obj.GetComponent<Player>().SwitchTeam(data[0], data[1]);
		return 2;
	}
	// ()
	static int IsSpectating(GameObject obj, byte[] data, int fromWho)
	{
		// TODO: Implement somewhere!
		return 0;
	}
	// (byte)
	static int VoiceLine(GameObject obj, byte[] data, int fromWho)
	{
		if (obj) obj.GetComponent<Combat>().VoiceLine(data[0], data[1], false);
		return 2;
	}
	// (string)
	static int Chat(GameObject obj, byte[] data, int fromWho)
	{
		byte count = data [0];
		string stringData = Encoding.ASCII.GetString(data, 1, count);
		if (OperationNetwork.isServer)
		{
			if (obj) obj.GetComponent<Communication>().Chat(stringData, (short)fromWho);
			return count + 1;
		} else
		{
			if (obj) obj.GetComponent<Communication>().Chat(stringData, BitConverter.ToInt16(data, count + 1));
			return count + 3;
		}
	}
	// (string)
	static int PlayerConnect(GameObject obj, byte[] data, int fromWho)
	{
		byte count = data[0];
		string stringData = Encoding.ASCII.GetString (data, 1, count);
		// obj is actually irrelivant here.
		GameManager.PlayerConnect(stringData, (short)fromWho);
		return count + 1;
	}
	//
	static int KillSelf(GameObject obj, byte[] data, int fromWho)
	{
		if (GameManager.PlayerExists ((short)fromWho) && GameManager.GetPlayer((short)fromWho).playerObject) {
			GameManager.GetPlayer ((short)fromWho).playerObject.GetComponent<Combat> ().TakeDamage (5000f, Combat.OTHER);
		}
		return 0;
	}
	//
	static int ResendTicks(GameObject obj, byte[] data, int fromWho) {
		// Full Resend is done..
		if (GameManager.PlayerExists ((short)fromWho)) {
			OperationNetwork.getClient ((short)fromWho).shouldNextSendOutBeFullSendOut = true;
		} else {
			// hmm
		}
		return 0;
	}
	// End of list of all RPCs

	static void addRPC(string value, OperationRPC methodCall)
	{
		int numAdded = rpcIDList.Count;
		rpcIDList.Add(value, (byte)numAdded);
		rpcList.Add((byte)numAdded, methodCall);
	}

	void Start()
	{

	}

	public static void initRPCs()
	{
		if (rpcList.Count > 0)
			return;

		// Create rpcList:
		addRPC("BuyTrap", BuyTrap);
		addRPC("AddPlayerMade", AddPlayerMade);
		addRPC("switchTeam", switchTeam);
		addRPC("IsSpectating", IsSpectating); // This is an RPC that is sent to keep the spectator player connected.
		addRPC("VoiceLine", VoiceLine);
		addRPC("Chat", Chat);
		addRPC ("PlayerConnect", PlayerConnect);
		addRPC ("KillSelf", KillSelf);
		addRPC ("ResendTicks", ResendTicks);
	}
	
}
