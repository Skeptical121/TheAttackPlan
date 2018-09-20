using UnityEngine;
using System.Collections;
using System;

// An array of these are stored: EVERY server tick these are cleared! (This is how it would work on UDP as well)
public class DamageNumber : SyncableType {
	public Vector3 posData;
	public float damage;

	// Server side:
	public override byte[] getData() {
		byte[] data = new byte[16];
		Buffer.BlockCopy(GameState.getObjectData(posData), 0, data, 0, 12);
		Buffer.BlockCopy(GameState.getObjectData(damage), 0, data, 12, 4);
		return data;
	}

	// Server side:
	public DamageNumber(Vector3 pos, float dmg) {
		posData = pos;
		damage = dmg;
	}

	public override SyncableType createThis (byte[] data, ref int bytePosition, SyncGameState sgg, int tickNumber, bool isPlayerOwner)
	{
		return new DamageNumber (data, ref bytePosition);
	}

	// Client side:
	public DamageNumber(byte[] data, ref int bytePosition) {
		posData = (Vector3)DataInterpret.interpretObject (data, ref bytePosition, Vector3.zero, null, -1, false);
		damage = (float)DataInterpret.interpretObject (data, ref bytePosition, 1f, null, -1, false);
	}

	public override bool isDifferent(SyncableType other) {
		Debug.LogError ("This should never be called! DamageNumber -> isDifferent()");
		return false;
	}
}