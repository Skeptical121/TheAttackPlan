using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.Networking;

public class ClientPerson
{

	public bool sentThisTick = false;
	public float lastTimeSentAt = 0;

	public List<byte[]> outgoingMessages = new List<byte[]>(10); // This is the actual "frame per frame" data

	byte[][] previousInputCommands = new byte[7][]; // You have to miss at least (7) input commands in a row to have input commands not go through

	public List<byte[]> outgoingInputCommands = new List<byte[]> (10);

	public int dataInRate = 0;
	public int dataOutRate = 0;
	public int[] previousDataInRate = new int[300]; // Checks for recieving of data time. Obviously the opposite is relevant, but tracking the server is harder to do as its on linux
	public float[] previousDataInTimes = new float[300];

	string ip;
	int port;

	private int hostId;
	private int connectionId;
	private ConnectionConfig config;
	private HostTopology hostTopology;
	public byte reliableSequencedChannelId;
	public byte unreliableChannelId;

	public ClientPerson(string ip, string port) {
		this.ip = ip;
		this.port = int.Parse (port);
	}

	public void StartClient()
	{
		NetworkTransport.Init();
		byte error;
		config = new ConnectionConfig();

		config.SendDelay = 0; // hmm

		reliableSequencedChannelId = config.AddChannel (QosType.ReliableSequenced); // For RPC Input commands like switching teams and of course, PlayerConnect.
		unreliableChannelId = config.AddChannel (QosType.Unreliable); // All input that goes on a frame per frame basis like moving & shooting
		hostTopology = new HostTopology(config, 1);
		hostId = NetworkTransport.AddHost(hostTopology);

		connectionId = NetworkTransport.Connect(hostId, ip, port, 0, out error);

		NetworkError networkError = (NetworkError) error;
		if (networkError != NetworkError.Ok) {
			Debug.LogError(string.Format("Unable to connect to {0}:{1}, Error: {2}", ip, port, networkError));
		} else {
			OperationNetwork.initialConnected = 0;
		}
		Debug.LogError ("Connected initial");
	}

	// This can be called if you are "connected"
	public void SendData(byte[] data)
	{
		if (OperationNetwork.connected)
		{
			outgoingMessages.Add(data);
		}
	}

	public short playerInputGroupID = 0; // This is a looping ID that iterates every time movement data is sent out. Thus, multiple frames will have a certain ID; but this is fine, as server receives data on a per packet basis.
	// Both the client and the server track this number to make sure the input commands are being run- (covered by PredictionError), and to make sure there are no duplicate commands being run on the server

	// Because of this, there will be an overhead of (2) bytes sent to the server
	public void addInputCommand(byte[] data) {
		if (OperationNetwork.connected) {
			
			byte[] outgoingInputCommand = new byte[data.Length + 1];
			if (data.Length > 200) {
				Debug.LogError ("Nearing data limit on client!");
			}
			outgoingInputCommand [0] = (byte)data.Length;
			Buffer.BlockCopy (data, 0, outgoingInputCommand, 1, data.Length);
			outgoingInputCommands.Add (outgoingInputCommand);
		}
	}

	public void ReadMessages()
	{
		int numData = 0;
		for (int i = 0; i < 250; i++) { // Up to 250 messages read per tick
			// Receive messages:
			int recHostId;
			int recConnectionId;
			int recChannelId;
			byte[] recBuffer = new byte[1500]; // Prepare for 1500. Warning on server is made at 1000 currently. Probably will have to adjust this one day.
			int bufferSize = 1500;
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
				OperationNetwork.connected = true;
				Debug.LogError ("We have connected! ");
				GameManager.InitialConnect ();
				break;
			case NetworkEventType.DataEvent:

				numData++;

				dataInRate += dataSize;
				Interp.shiftBuffer (previousDataInRate);
				Interp.shiftBuffer (previousDataInTimes);
				previousDataInRate [0] = dataSize;
				previousDataInTimes [0] = Time.time;

				if (OperationNetwork.initialConnected == 0) {
					if (recChannelId == reliableSequencedChannelId) {
						Player.myID = BitConverter.ToInt16 (recBuffer, 0);
						OperationNetwork.initialConnected = 1;
					} else {
						// We send a RPC looking for a full game resend, if this is not a full game state:
						if (BitConverter.ToInt32 (recBuffer, 2) >= 0) {
							OperationView.RPC (null, "ResendTicks", OperationNetwork.ToServer);
						}
					}
				} else {
					OperationNetwork.clientReceivedData (recBuffer); // Note that dataSize could be used as a way of trimming this.
				}

				break;
			case NetworkEventType.DisconnectEvent:
				Debug.LogError ("Server Shut Down");
				break;
			}
		}
		Debug.LogError ("Missing Messages! Num Data Messages (/250): " + numData);
	}

	float lastTimeSentInputData = -1000f;

	public void sendInputData()
	{
		if (outgoingInputCommands.Count > 0 && OperationNetwork.connected && Time.time - lastTimeSentInputData > 0.01f) { // Maximum of 100 sendouts per second- but notice how this can be skewed by consistent 0.011 seconds frames.
			lastTimeSentInputData = Time.time;

			int numPrevious = 0;

			int size = 2;
			for (int i = 0; i < previousInputCommands.Length; i++) {
				if (previousInputCommands [i] != null) {
					size += previousInputCommands [i].Length;

					numPrevious++;
				}
			}
			size += 1;
			for (int i = 0; i < outgoingInputCommands.Count; i++) {
				size += outgoingInputCommands [i].Length;
			}
			byte[] data = new byte[size];
			Buffer.BlockCopy(BitConverter.GetBytes((short)(playerInputGroupID - numPrevious)), 0, data, 0, 2);
			size = 2;
			for (int i = previousInputCommands.Length - 1; i >= 0; i--) {
				if (previousInputCommands [i] != null) {
					Buffer.BlockCopy (previousInputCommands [i], 0, data, size, previousInputCommands [i].Length);
					size += previousInputCommands [i].Length;
				}
			}
			if (outgoingInputCommands.Count > 100) {
				Debug.LogError ("Nearing outgoingInputCommands Max");
			}
			data [size] = (byte)outgoingInputCommands.Count;
			size += 1;
			for (int i = 0; i < outgoingInputCommands.Count; i++) {
				Buffer.BlockCopy (outgoingInputCommands [i], 0, data, size, outgoingInputCommands [i].Length);
				size += outgoingInputCommands [i].Length;
			}
			dataOutRate += data.Length;
			byte error;

			if (data.Length > 600) {
				Debug.LogError ("Nearing Buffer Limit on Client! Count: " + data.Length);
			}

			NetworkTransport.Send (hostId, connectionId, unreliableChannelId, data, data.Length, out error);
			NetworkError networkError = (NetworkError)error;
			if (networkError != NetworkError.Ok) {
				Debug.LogError (string.Format ("Error: {0}, hostId: {1}, connectionId: {2}, channelId: {3}", networkError, hostId, connectionId, unreliableChannelId));
			}

			if (previousInputCommands.Length > 0) {
				Interp.shiftBuffer (previousInputCommands);
			}

			size = 1;
			for (int i = 0; i < outgoingInputCommands.Count; i++) {
				size += outgoingInputCommands [i].Length;
			}
			if (previousInputCommands.Length > 0) {
				previousInputCommands [0] = new byte[size];
				size = 1;
				previousInputCommands [0] [0] = (byte)outgoingInputCommands.Count;
				// Opposite order as that's how shiftBuffer works:
				for (int i = outgoingInputCommands.Count - 1; i >= 0; i--) {
					Buffer.BlockCopy (outgoingInputCommands [i], 0, previousInputCommands [0], size, outgoingInputCommands [i].Length);
					size += outgoingInputCommands [i].Length;
				}
			}

			playerInputGroupID++;
			outgoingInputCommands.Clear();
		}
	}

	public void actuallySendData()
	{
		// count size:
		if (outgoingMessages.Count > 0 && OperationNetwork.connected)
		{
			int size = 0;
			for (int i = 0; i < outgoingMessages.Count; i++)
			{
				size += outgoingMessages[i].Length;
			}
			byte[] sendData = new byte[size];
			size = 0;
			for (int i = 0; i < outgoingMessages.Count; i++)
			{
				Buffer.BlockCopy(outgoingMessages[i], 0, sendData, size, outgoingMessages[i].Length);
				size += outgoingMessages[i].Length;
			}

			dataOutRate += sendData.Length;
			byte error;

			if (sendData.Length > 600) {
				Debug.LogError ("Nearing Buffer Limit on Client! Count: " + sendData.Length);
			}

			NetworkTransport.Send(hostId, connectionId, reliableSequencedChannelId, sendData, sendData.Length, out error);
			NetworkError networkError = (NetworkError) error;
			if (networkError != NetworkError.Ok) {
				Debug.LogError(string.Format("Error: {0}, hostId: {1}, connectionId: {2}, channelId: {3}", networkError, hostId, connectionId, reliableSequencedChannelId));
			}

		}
		outgoingMessages.Clear();
	}

	public void disconnect()
	{
		if (OperationNetwork.connected) {
			OperationNetwork.connected = false;

			byte error;
			NetworkTransport.Disconnect (hostId, connectionId, out error);
			NetworkError networkError = (NetworkError)error;
			if (networkError != NetworkError.Ok) {
				Debug.LogError (string.Format ("Error: {0}, hostId: {1}, connectionId: {2}, channelId: {3}", networkError, hostId, connectionId, reliableSequencedChannelId));
			} else {
				Debug.LogError ("Disconnected!");
			}
		}
	}
}