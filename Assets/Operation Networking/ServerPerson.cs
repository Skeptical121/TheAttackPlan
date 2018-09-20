using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

public class ServerPerson
{

	public static short addID = 10; // -9 to 9 is reserved. Once ~32757 users have connected.. this is an issue, of course. (Can just reuse IDs later on) Note that these do not need to be in order.

	ServerAcceptor parent;

	public static string demoName = "TestDemo.dem";

	public bool connected = false;
	public short id;

	public int recConnectionId;

	// Incoming messages from each client is needed for the server to insure that the order is maintained:
	byte[][] messages = new byte[200][]; // Incoming messages. ServerPerson / ClientPerson adds to this // Incoming messages. ONLY clientPerson uses this to prevent order issues, ServerPerson has its own implementation.

	// We now do a derpy way of doing this: We should change it do isDifferent timers, so we don't have to recreate game states for each and every player using multiple isDifferent checks PER client.
	// Or, a simpler way, just move the gamestate update to server.. although.. that doesn't work very well.. because player owned objects.. but that will be changed soon enough..
	public GameState[] lastSentGameState = new GameState[ServerState.MAX_CHOKE_TICKS]; // Chokes of up to (3) are acceptable. The game will literally pause and wait for the previously sent state at that point- or it could not pause; and just send you the entire gamestate. Either seems fine.

	public int dataSent = 0; // Counts the rate for every second.

	public short lastPlayerInputGroupID = -1; // The reasoning for this is that the client starts the input at frame 0

	// Player Input is cleared after about 10-20ish, and the player input will simply be skipped by that point
	public Dictionary<short,byte[]> inputCommands = new Dictionary<short,byte[]>();

	public bool shouldNextSendOutBeFullSendOut = false; // The first few ticks doing full sendouts are not controlled by this; this is just for high choke situations

	public ServerPerson(ServerAcceptor parent)
	{
		this.parent = parent;
		id = addID++;
		demoName = "TestDemo" + DateTime.UtcNow.Ticks + ".dem";
	}


	public FileStream file;
	public void setDemo() {
		// Write outgoingMessages to file:
		id = 32767;
		file = File.Open(demoName, FileMode.Append);
	}

	public void SendData(byte[] data, bool reliableChannel)
	{
		if (connected) {
			if (id == 32767) {
				try {
						

					file.Write(data, 0, data.Length);
				} catch (Exception e) {
					Debug.LogError (e.Message);
				}

			} else {

				byte error;
				if (data.Length > 1000) {
					Debug.LogError ("NEARING DATA LIMIT of 1400- this can be increased!");
				}

				byte channel = parent.unreliableChannelId;
				if (reliableChannel)
					channel = parent.reliableSequencedChannelId;

				NetworkTransport.Send(parent.hostId, recConnectionId, channel, data, data.Length, out error);
				NetworkError networkError = (NetworkError) error;
				if (networkError != NetworkError.Ok) {
					Debug.LogError(string.Format("Error: {0}, hostId: {1}, connectionId: {2}, channelId: {3}", networkError, parent.hostId, this.recConnectionId, channel));
				}
			}
		}
			
	}

	public void disconnect()
	{
		GameManager.DestroyPlayerRelated(id);
		connected = false;
	}
}