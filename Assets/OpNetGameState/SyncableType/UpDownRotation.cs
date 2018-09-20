using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpDownRotation : SyncableType {
	byte value;

	public float interpValue;

	public override bool isDifferent (SyncableType other)
	{
		return value != ((UpDownRotation)other).value;
	}

	// b is a byte, but it is in float form because that's how interp works
	public float getInterpValueFromByte(float b) {
		float newInterpValue = b * 180f / 256;
		if (newInterpValue < 90f) {
			newInterpValue = -270 - newInterpValue;
		} else {
			newInterpValue = 90 - newInterpValue;
		}
		return newInterpValue;
	}

	public override SyncableType interp(SyncableType b, float percent) {
		int rotA = value;
		int rotB = ((UpDownRotation)b).value;
		float newInterpValue = getInterpValueFromByte (Mathf.Lerp (rotA, rotB, percent));
		return new UpDownRotation (newInterpValue);
	}

	public UpDownRotation(float interpValue) {
		if (interpValue < -180) {
			// From -270 to -360:
			this.value = (byte)((270 - interpValue) * 256 / 180);
		} else {
			// From 0 to -90:
			this.value = (byte)((-90 - interpValue) * 256 / 180);
		}
		this.interpValue = interpValue;
	}

	public override SyncableType createThis (byte[] data, ref int bytePosition, SyncGameState sgg, int tickNumber, bool isPlayerOwner)
	{
		return new UpDownRotation (data, ref bytePosition);
	}

	public UpDownRotation(byte[] data, ref int bytePosition) {
		value = data [bytePosition++];
		interpValue = getInterpValueFromByte(value);
	}

	public override byte[] getData ()
	{
		return new byte[]{ value };
	}
}