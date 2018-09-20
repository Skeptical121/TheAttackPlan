using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TriggerSet : SyncableType {
	
	public List<byte> triggerIndecies = new List<byte>();
	public List<int> triggerTickNumbers = new List<int>();
	public List<object> triggerData = new List<object>();

	int lastTickReceived = -1;

	public TriggerSet() {

	}

	public void trigger(byte index) {
		trigger(index, null);
	}

	// Server
	public void trigger(byte index, object data) {
		triggerIndecies.Add (index);
		triggerTickNumbers.Add (ServerState.tickNumber);
		triggerData.Add (data);
	}

	// sgg is merely used for type here
	public override SyncableType createThis (byte[] data, ref int bytePosition, SyncGameState sgg, int tickNumber, bool isPlayerOwner)
	{
		return new TriggerSet (data, ref bytePosition, sgg, tickNumber, isPlayerOwner);
	}

	public void removeOld() {
		for (int i = triggerTickNumbers.Count - 1; i >= 0; i--) {
			if (ServerState.tickNumber - triggerTickNumbers [i] >= ServerState.MAX_CHOKE_TICKS) {
				triggerIndecies.RemoveAt (i);
				triggerTickNumbers.RemoveAt (i);
				triggerData.RemoveAt (i);
			}
		}
	}

	// Client
	public TriggerSet(byte[] data, ref int bytePosition, SyncGameState sgg, int tickNumber, bool isPlayerOwner) {
		short numTriggers = data [bytePosition++];
		// It's actually not unviable for it to go >255 because triggers sync for multiple ticks
		if (numTriggers == 255) {
			numTriggers = BitConverter.ToInt16 (data, bytePosition);
			bytePosition += 2;
		}

		for (int i = 0; i < numTriggers; i++) {
			byte index = (byte)(data[bytePosition] / 16);
			int triggerTick = tickNumber - data[bytePosition] % 16;
			triggerIndecies.Add (index);
			triggerTickNumbers.Add (triggerTick);
			bytePosition++;
			triggerData.Add (sgg.getTriggerData (index, data, ref bytePosition, isPlayerOwner));
		}
		lastTickReceived = tickNumber;
	}

	public void AddTriggerSet(TriggerSet ts) {

		// ALL old triggers get cleared
		triggerIndecies.Clear();
		triggerTickNumbers.Clear ();
		triggerData.Clear ();

		if (ts.lastTickReceived > lastTickReceived) {
			for (int i = 0; i < ts.triggerIndecies.Count; i++) {
				if (ts.triggerTickNumbers [i] > lastTickReceived) {
					triggerIndecies.Add (ts.triggerIndecies[i]);
					triggerTickNumbers.Add (ts.triggerTickNumbers[i]);
					triggerData.Add (ts.triggerData[i]);
				}
			}

			lastTickReceived = ts.lastTickReceived;
		}
	}

	public override bool isDifferent (SyncableType other)
	{
		removeOld ();
		return triggerIndecies.Count > 0;
	}

	// Data sendout is retrieved on the tick the gamestate is created; there is never any delay for the sendout.. Past ticks' objects are not sent, a new gamestate would simply be retrieved instead.
	public override byte[] getData() {

		removeOld ();

		List<byte> data = new List<byte>();

		if (triggerTickNumbers.Count < 255) {
			data.Add ((byte)triggerTickNumbers.Count);
		} else {
			data.Add ((byte)255);
			data.AddRange(BitConverter.GetBytes((short)triggerTickNumbers.Count));
		}
		for (int i = 0; i < triggerTickNumbers.Count; i++) {
			if (ServerState.tickNumber - triggerTickNumbers [i] < ServerState.MAX_CHOKE_TICKS) {
				byte timeSince = (byte)(ServerState.tickNumber - triggerTickNumbers [i]);
				data.Add ((byte)(triggerIndecies [i] * 16 + timeSince));
				if (triggerData[i] != null)
					data.AddRange (GameState.getObjectData(triggerData [i]));
			}
		}

		return data.ToArray ();
	}
}
