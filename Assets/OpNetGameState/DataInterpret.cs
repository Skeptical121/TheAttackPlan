using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

public static class DataInterpret {

	// Static class devoted to methods that were originally in SyncGameState that involve the interpreting & representing of byte data into objects.


	public static object interpretObject(byte[] data, ref int bytePosition, object type, SyncGameState sgg, int tickNumber, bool isPlayerOwner)
	{
		object returnObject = null;
		if (type is float) {
			returnObject = BitConverter.ToSingle (data, bytePosition);
			bytePosition += 4;
		} else if (type is int) {
			returnObject = BitConverter.ToInt32 (data, bytePosition);
			bytePosition += 4;
		} else if (type is short) {
			returnObject = BitConverter.ToInt16 (data, bytePosition);
			bytePosition += 2;
		} else if (type is byte) {
			returnObject = data [bytePosition];
			bytePosition += 1;
		} else if (type is Vector3) { // Equals feels like good usage here.
			returnObject = OperationNetwork.getVector3 (data, bytePosition);
			bytePosition += 12;
		} else if (type is bool) {
			returnObject = BitConverter.ToBoolean (data, bytePosition);
			bytePosition += 1;
		} else if (type is Quaternion) {
			returnObject = OperationNetwork.getQuaternion (data, bytePosition);
			bytePosition += 16;
		} else if (type is string) {
			// All strings have max 255 length.
			// Just because.
			byte length = data [bytePosition];
			bytePosition += 1;

			returnObject = Encoding.ASCII.GetString (data, bytePosition, length);
			bytePosition += length;
		} else if (type is float[]) {
			byte length = data [bytePosition]; // Max 255 length
			bytePosition += 1;

			returnObject = new float[length];
			for (int i = 0; i < length; i++) {
				((float[])returnObject) [i] = BitConverter.ToSingle (data, bytePosition);
				bytePosition += 4;
			}
		} else if (type is Vector3[]) {
			byte length = data [bytePosition]; // Max 255 length
			bytePosition += 1;

			returnObject = new Vector3[length];
			for (int i = 0; i < length; i++) {
				((Vector3[])returnObject) [i] = OperationNetwork.getVector3 (data, bytePosition);
				bytePosition += 12;
			}
		} else if (type is Vector3[][]) {
			// Hitscan data:
			byte length = data [bytePosition];
			bytePosition += 1;
			returnObject = new Vector3[length][];
			for (int i = 0; i < length; i++) {
				((Vector3[][])returnObject) [i] = (Vector3[])interpretObject (data, ref bytePosition, new Vector3[0], null, -1, false);
			}
		} else if (type is SyncableType) {
			returnObject = ((SyncableType)type).createThis (data, ref bytePosition, sgg, tickNumber, isPlayerOwner);
		} else if (type is DamageNumber[]) {
			byte length = data [bytePosition]; // Max 255 length
			bytePosition += 1;

			returnObject = new DamageNumber[length];
			for (int i = 0; i < length; i++) {
				((DamageNumber[])returnObject) [i] = new DamageNumber (data, ref bytePosition);
			}
		} else if (type is byte[]) {
			byte length = data [bytePosition]; // Max 255 length
			bytePosition += 1;
			returnObject = new byte[length];
			for (int i = 0; i < length; i++) {
				((byte[])returnObject) [i] = data[bytePosition];
				bytePosition += 1;
			}
		}
		return returnObject;
	}


	// b is the OLDER one; a is the NEWER one.
	public static bool isObjectDifferent(object a, object b)
	{
		if (a is Vector3[][]) {
			// Special case; reserved for hitscan:
			return ((Vector3[][])a).Length > 0;
		} else if (b is DamageNumber[]) {
			return ((DamageNumber[])a).Length > 0 || ((DamageNumber[])b).Length > 0; // The server sends to the client the information that a tick has no damage numbers.
		} else if (a is float) {
			return Mathf.Abs ((float)a - (float)b) > 0.001f;
		} else if (a is Vector3) {
			Vector3 a1 = (Vector3)a;
			Vector3 b1 = (Vector3)b;
			return Mathf.Abs (a1.x - b1.x) > 0.001f || Mathf.Abs (a1.y - b1.y) > 0.001f || Mathf.Abs (a1.z - b1.z) > 0.001f;
		} else if (a is object[]) { // Does float[], Vector3[], does NOT do DamageNumber[] because that works differently
			object[] a1 = (object[])a;
			object[] b1 = (object[])b;
			if (a1.Length == b1.Length) {
				for (int i = 0; i < a1.Length; i++)
					if (isObjectDifferent (a1 [i], b1 [i]))
						return false;
				return true;
			}
			return false;
		} else if (a is Quaternion) {
			return Quaternion.Angle ((Quaternion)a, (Quaternion)b) > 0.01f; // 1 degree is pretty small.
		} else if (a is SyncableType) {
			return ((SyncableType)a).isDifferent ((SyncableType)b);
		}

		return !a.Equals(b); // This should be confirmed for all types.
	}

	public static object interp(object a, object b, float percent)
	{
		// Because of, and only because of playerOwner changing, a & b must be the same type to check for interp. (And obviously to do interp)
		// This means that the player will not appear to move on the first frame because of interp. (Interp should run last issueDesktop)

		if (a is Vector3[][]) {
			return a;
		}

		if (a is float && b is float) {
			return Mathf.Lerp ((float)a, (float)b, percent);
		} else if (a is Vector3 && b is Vector3) {
			return Vector3.Lerp ((Vector3)a, (Vector3)b, percent);
		} else if (a is Quaternion && b is Quaternion) {
			return Quaternion.Lerp ((Quaternion)a, (Quaternion)b, percent);
		} else if (a is object[] && b is object[]) {
			// Interping this might be a terrible idea in some, (or perhaps all) cases. Think of hitscanShoot.

			object[] a1 = (object[])a;
			object[] b1 = (object[])b;

			if (a1.Length == b1.Length) {
				object[] c1 = new object[a1.Length]; // For obvious reference reasons, a new object array must be created for the return variable.
				for (int i = 0; i < a1.Length; i++) {
					c1 [i] = interp (a1 [i], b1 [i], percent); // TODO get rid of this in case of HitscanShoot
				}
				return b1;
			}
			return a1; // This object doesn't have interp.
		} else if (a is SyncableType && b is SyncableType) {
			return ((SyncableType)a).interp ((SyncableType)b, percent);
		}
		return a; // This object doesn't have interp.
	}
}
