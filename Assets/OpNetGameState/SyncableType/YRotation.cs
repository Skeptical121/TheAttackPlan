using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class YRotation : SyncableType {
	byte value;

	public float interpValue;

	public override bool isDifferent (SyncableType other)
	{
		return value != ((YRotation)other).value;
	}

	public override SyncableType interp(SyncableType b, float percent) {
		float newInterpValue;
		int rotA = value;
		int rotB = ((YRotation)b).value;
		if (Math.Abs (rotA - rotB) < 128) {
			newInterpValue = Mathf.Lerp (rotA, rotB, percent);
		} else {
			if (rotB > rotA) {
				rotB -= 256;
			} else {
				rotA -= 256;
			}
			newInterpValue = Mathf.Lerp (rotA, rotB, percent);
			if (newInterpValue < 0) {
				newInterpValue += 256;
			}
		}
		return new YRotation (newInterpValue * 360f / 256);
	}

	public YRotation(float interpValue) {
		this.value = (byte)(interpValue * 256 / 360);
		this.interpValue = interpValue;
	}

	public override SyncableType createThis (byte[] data, ref int bytePosition, SyncGameState sgg, int tickNumber, bool isPlayerOwner)
	{
		return new YRotation (data, ref bytePosition);
	}

	public YRotation(byte[] data, ref int bytePosition) {
		value = data [bytePosition++];
		interpValue = value * 360f / 256;
	}

	public override byte[] getData ()
	{
		return new byte[]{ value };
	}
}