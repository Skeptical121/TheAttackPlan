using UnityEngine;
using System.Collections;
using System;

// An array of these are stored: EVERY server tick these are cleared! (This is how it would work on UDP as well)
public abstract class SyncableType {

	// Server side:
	public abstract byte[] getData();

	// Server side:
	public abstract bool isDifferent (SyncableType other);

	// Client side: This implementation is to avoid reflection, and to avoid having to write additional code within SyncGameState
	public abstract SyncableType createThis(byte[] data, ref int bytePosition, SyncGameState sgg, int tickNumber, bool isPlayerOwner);

	public virtual SyncableType interp(SyncableType b, float percent) {
		return this;
	}
}
