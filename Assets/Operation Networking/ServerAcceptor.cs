using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System;
using System.Threading;
using UnityEngine.Networking;
using System.Collections.Generic;

public class ServerAcceptor
{
	public Dictionary<int,ServerPerson> serverClients = new Dictionary<int,ServerPerson> ();

	public HostTopology topology;
	public ConnectionConfig config;
	public int connectionId;
	public int hostId;
	public byte unreliableChannelId;
	public byte reliableSequencedChannelId;

	bool started = false;

	public void StartServer() {
		NetworkTransport.Init();
		config = new ConnectionConfig();

		reliableSequencedChannelId = config.AddChannel(QosType.ReliableSequenced); // Do I even need to add this on server?
		unreliableChannelId = config.AddChannel(QosType.Unreliable); 

		config.SendDelay = 0; // We already combine our messages

		topology = new HostTopology(config, 10);
		hostId = NetworkTransport.AddHost(topology, 26800);

		started = true;
		Debug.LogError("Server started on port" + 26800 + " with id of " + hostId);

		GameManager.InitialConnect ();
	}

	public void ReadMessages() {
		for (int i = 0; i < 5000; i++) {
			try {
				int recHostId;
				int recConnectionId;
				int recChannelId;
				byte[] recBuffer = new byte[2048];
				int bufferSize = 2048;
				int dataSize;
				byte error;
				NetworkEventType networkEvent = NetworkTransport.Receive (out recHostId, out recConnectionId, out recChannelId, recBuffer, bufferSize, out dataSize, out error);

				NetworkError networkError = (NetworkError)error;
				if (networkError != NetworkError.Ok) {
					Debug.LogError (string.Format ("Error recieving event: {0} with recHostId: {1}, recConnectionId: {2}, recChannelId: {3}", networkError, recHostId, recConnectionId, recChannelId));
				}

				switch (networkEvent) {
				case NetworkEventType.Nothing:
					return;
				case NetworkEventType.ConnectEvent:
					Debug.LogError (string.Format ("incoming connection event received with connectionId: {0}, recHostId: {1}, recChannelId: {2}", recConnectionId, recHostId, recChannelId));

					if (!serverClients.ContainsKey (recConnectionId)) {
						// Add server client:
						ServerPerson sP = new ServerPerson (this);
						RunGame.myServerThreads.Add (sP);
						// hmm...  player.players.Add(sP.id, new OtherPlayerInfo()); // Stick to default settings until a RPC is received.
						sP.recConnectionId = recConnectionId;
						serverClients.Add (recConnectionId, sP);
					}
					break;
				case NetworkEventType.DataEvent:
					// Player Input!
					// Delegate this to the "ServerClientData" first:
					if (serverClients.ContainsKey (recConnectionId)) {
						short id = serverClients [recConnectionId].id;

						if (dataSize > 500) {
							Debug.LogError ("Nearing Data limit on server: " + dataSize + ": " + recChannelId + "(" + reliableSequencedChannelId + " / " + unreliableChannelId + ")");
						}

						// todo: When removing RPCs, we're going to make this into a proper ref index sorta thing:
						byte[] rpcData = new byte[dataSize];
						Buffer.BlockCopy (recBuffer, 0, rpcData, 0, dataSize);

						if (recChannelId == reliableSequencedChannelId) {
							OperationNetwork.serverReceivedData (rpcData, id);
						} else {
							short packetID = BitConverter.ToInt16(rpcData, 0);
							byte[] sData = new byte[rpcData.Length - 2];
							// This might be one of the most inefficient things on the server, TODO!! The copy of byte data!!
							Buffer.BlockCopy(rpcData, 2, sData, 0, sData.Length);
							OperationNetwork.serverReceivedInput (sData, id, packetID);
						}
					} else {
						Debug.LogError ("Client not connected: " + recConnectionId);
					}
					break;
				case NetworkEventType.DisconnectEvent:
					Debug.LogError ("remote client " + recConnectionId + " disconnected");
					if (serverClients.ContainsKey (recConnectionId)) {
						RunGame.myServerThreads.Remove (serverClients [recConnectionId]);
						serverClients [recConnectionId].disconnect ();
						serverClients.Remove(recConnectionId);
					}
					break;
				}
			} catch (Exception e) {
				Debug.LogError ("Error on client data receive: " + e.Message);
			}
		}
		Debug.LogError ("Missing Messages on Server!");
	}
}