using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class PlayerState {

	// Note that not everything here needs to be synced! However, we MIGHT AS WELL SYNC EVERYTHING!!
	// Why? Because these things will NEVER fail Prediction Error (assuming they are sent)
	// The assumption that things are sent will stand in cases of subtle movement; however it still can't be done in ClassControl because of stuff like weapon switching and cooldowns. <-- Also, of course, Phase Shift / Firing is the primary reason

	// Only the first (5) variables here need to be saved.. hmm..

	// Largely for simplicity, but with the consideration that with packet loss OR "unexpected" situations like phase shift / shooting, 
	// All variables will be sent through the server for syncing

	// Therefore, PlayerState works with UnlockSnapshot, (and is based off the UnlockSnapshot system)

	// All these objects are only set if there is a significant difference between the saved state and the new state.

	// Enum, essentially:
	public const int POSITION = 0;
	public const int JUMPED = 1;
	public const int IN_DIRT_MOUND = 2;
	public const int PLAYER_TIME = 3;
	public const int MOVE_DIRECTION = 4;
	public const int EFFECT_DIRECTION = 5;
	public const int IS_CROUCHED_VAL = 6;
	public const int LAST_PLAYER_POSITION = 7;
	public const int PLAYER_SPEED = 8;
	public const int TIME_SINCE_TOUCHING_DAMAGE_CIRCLE = 9; // This is important for prediction as accuracy of hitscan relies on it; and thus the effect relies on it
	public const int IS_ARMOR_ON = 10;
	public const int ROTATION = 11;
	public const int ROTATION_UP_DOWN = 12;

	const int PLAYER_STATE_TYPES = 13;

	// The saved state is simply saved how the server saves it- with the objects. Each object is then compared in this class!
	public static object getObject(int num, SyncPlayer sp, PlayerMove pMove) {
		if (num == POSITION)
			return sp.transform.position;
		if (num == JUMPED)
			return Convert.ToByte(pMove.jumped);
		if (num == IN_DIRT_MOUND)
			return pMove.inDirtMound;
		if (num == PLAYER_TIME)
			return sp.playerTime;
		if (num == MOVE_DIRECTION)
			return pMove.moveDirection;
		if (num == EFFECT_DIRECTION)
			return pMove.effectDirection;
		if (num == IS_CROUCHED_VAL)
			return pMove.isCrouchedVal;
		if (num == LAST_PLAYER_POSITION)
			return pMove.lastPlayerPosition;
		if (num == PLAYER_SPEED)
			return pMove.playerSpeed;
		if (num == TIME_SINCE_TOUCHING_DAMAGE_CIRCLE)
			return pMove.timeSinceTouchingDamageCircle;
		if (num == IS_ARMOR_ON) {
			if (pMove.puttingArmorOn)
				return pMove.isArmorOn;
			else
				return pMove.isArmorOn + 2;
		}
		if (num == ROTATION)
			return pMove.transform.eulerAngles.y;
		if (num == ROTATION_UP_DOWN)
			return pMove.mainCamera.transform.eulerAngles.x;
		return UnlockSnapshot.getObject (num - PLAYER_STATE_TYPES, sp);
	}

	public static int getBitChoicesLength(byte team, int classNum) {
		return PLAYER_STATE_TYPES + UnlockSnapshot.getBitChoicesLength(team, classNum);
	}

	public static object[] createObjectList(SyncPlayer sp, PlayerMove pMove) {
		int total = getBitChoicesLength(sp.GetComponent<Combat>().team, sp.GetComponent<ClassControl>().classNum);
		object[] objects = new object[total];
		for (int i = 0; i < total; i++) {
			objects [i] = getObject (i, sp, pMove);
		}
		return objects;
	}

	// lps = LastPlayerPosition, sps = ServerPlayerPosition
	public static bool isDifferent(object[] lps, object[] sps) {

		// Obviously everything needs to be checked in player state, and if ANYTHING is different, full resimulation is done.

		// Note that movement resimulation technically is almost entirely separate from unlock simulation, however because of key exceptions (notabely phase shift / shooting),
		// Doing it like this is how it's done in all cases. If this becomes a FPS issue, this will definitely be looked into
		return Vector3.Distance (Vector3.zero, (Vector3)sps[POSITION] - (Vector3)lps[POSITION]) > 0.03f ||
			(Convert.ToBoolean ((byte)sps [JUMPED]) != Convert.ToBoolean ((byte)lps [JUMPED])) ||
			Mathf.Abs((float)sps[IN_DIRT_MOUND] - (float)lps[IN_DIRT_MOUND]) > 0.03f ||
			Mathf.Abs((float)sps[PLAYER_TIME] - (float)lps[PLAYER_TIME]) > 0.03f ||
			Vector3.Distance (Vector3.zero, (Vector3)sps[MOVE_DIRECTION] - (Vector3)lps[MOVE_DIRECTION]) > 0.03f ||
			Vector3.Distance (Vector3.zero, (Vector3)sps[EFFECT_DIRECTION] - (Vector3)lps[EFFECT_DIRECTION]) > 0.03f ||
			Mathf.Abs((float)sps[IS_CROUCHED_VAL] - (float)lps[IS_CROUCHED_VAL]) > 0.03f ||
			Vector3.Distance (Vector3.zero, (Vector3)sps[LAST_PLAYER_POSITION] - (Vector3)lps[LAST_PLAYER_POSITION]) > 0.03f ||
			Mathf.Abs((float)sps[PLAYER_SPEED] - (float)lps[PLAYER_SPEED]) > 0.03f ||
			Mathf.Abs((float)sps[TIME_SINCE_TOUCHING_DAMAGE_CIRCLE] - (float)lps[TIME_SINCE_TOUCHING_DAMAGE_CIRCLE]) > 0.03f ||
			Mathf.Abs((float)sps[IS_ARMOR_ON] - (float)lps[IS_ARMOR_ON]) > 0.03f ||
			Mathf.Abs((float)sps[ROTATION] - (float)lps[ROTATION]) > 0.03f ||
			Mathf.Abs((float)sps[ROTATION_UP_DOWN] - (float)lps[ROTATION_UP_DOWN]) > 0.03f ||
			UnlockSnapshot.isDifferent(PLAYER_STATE_TYPES, lps, sps);
	}

	public static void setObjects(object[] objects, SyncPlayer sp, PlayerMove pMove) {
		sp.transform.position = (Vector3)objects [POSITION];
		pMove.jumped = Convert.ToBoolean ((byte)objects [JUMPED]);
		pMove.inDirtMound = (float)objects [IN_DIRT_MOUND];
		sp.playerTime = (float)objects [PLAYER_TIME];
		pMove.moveDirection = (Vector3)objects [MOVE_DIRECTION];
		pMove.effectDirection = (Vector3)objects [EFFECT_DIRECTION];
		pMove.isCrouchedVal = (float)objects [IS_CROUCHED_VAL];
		pMove.lastPlayerPosition = (Vector3)objects [LAST_PLAYER_POSITION];
		pMove.playerSpeed = (float)objects [PLAYER_SPEED];
		pMove.timeSinceTouchingDamageCircle = (float)objects [TIME_SINCE_TOUCHING_DAMAGE_CIRCLE]; // timeSinceTouchingDamageCircle will cause A LOT of prediction errors currently- as the object spawns / despawns & moves
		if ((float)objects [IS_ARMOR_ON] >= 2) {
			pMove.isArmorOn = (float)objects [IS_ARMOR_ON] - 2;
			pMove.puttingArmorOn = false;
		} else {
			pMove.isArmorOn = (float)objects [IS_ARMOR_ON];
			pMove.puttingArmorOn = true;
		}
		pMove.transform.eulerAngles = new Vector3(pMove.transform.eulerAngles.x, (float)objects[ROTATION], pMove.transform.eulerAngles.z);
		pMove.mainCamera.transform.eulerAngles = new Vector3((float)objects[ROTATION_UP_DOWN], pMove.transform.eulerAngles.y, 0);
		UnlockSnapshot.setObjects (PLAYER_STATE_TYPES, objects, sp);
	}

}
