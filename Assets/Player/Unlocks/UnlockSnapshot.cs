using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class UnlockSnapshot {
	
	public static object getObject(int num, SyncPlayer sp) {
		// Every variable gets synced on every frame, but obviously only ones that are being used will change

		ClassControl cc = sp.GetComponent<ClassControl> ();

		Player player = null;
		if (GameManager.PlayerExists(sp.playerOwner)) {
			player = GameManager.GetPlayer (sp.playerOwner);
		}

		if (num == 0) 
			return cc.whichUnlock;
		if (num == 1)
			return cc.nextUnlock;
		if (num == 2)
			return cc.switchedAt.getPlayerTriggerTime ();

		// Sync todo with currently equipped stuff:
		if (num >= 3 && num <= 7) {
			if (!cc.isSwitching ()) {
				Unlock unlock = cc.getUnlockEquipped ();
				if (unlock != null) {
					if (num == 3)
						return unlock.firedAt.getPlayerTriggerTime ();
					if (num == 4)
						return unlock.secondaryFiredAt.getPlayerTriggerTime ();
					if (num == 5)
						return unlock.reloadedAt.getPlayerTriggerTime ();
					if (num == 6)
						return unlock.equippedAt.getPlayerTriggerTime ();
					if (num == 7) {
						if (unlock is GunScript && ((GunScript)unlock).isReloadableType())
							return (byte)((GunScript)unlock).NumShots;
						else
							return (byte)0;
					}
				}
			}
			if (num == 7)
				return (byte)0;
			else
				return -1f;
			// Class specific stuff that syncs at all times:
		} else if (cc.classNum == 0) {
			if (num == 8)
				return sp.GetComponent<PlayerMove> ().movementAbilityCoolDownStartedAt;
			if (sp.GetComponent<Combat> ().team == 0) {
				if (num == 9)
					return cc.getUnlockEquippedWithType<PlaceShield> ().coolDownStartedAt;
			} else if (player != null) {
				if (num == 9)
					return player.trapCoolDownsStartedAt [0];
				if (num == 10)
					return player.trapCoolDownsStartedAt [1];
			} else {
				return 0f;
			}
		} else if (cc.classNum == 1) {
			if (num == 8)
				return cc.getUnlockEquippedWithType<PlaceIcicle> ().coolDownStartedAt;
			if (num == 9)
				return cc.getUnlockEquippedWithType<PlaceIcicle> ().AmmoStored;
			if (num == 10)
				return cc.getUnlockEquippedWithType<Pistol> ().mode;
			if (sp.GetComponent<Combat>().team == 0) {
				if (num == 11)
					return cc.getUnlockEquippedWithType<Throwable>().coolDownStartedAt;
			} else if (player != null) {
				if (num == 11)
					return player.trapCoolDownsStartedAt [0];
				if (num == 12)
					return player.trapCoolDownsStartedAt [1];
			} else {
				return 0f;
			}
		} else if (cc.classNum == 2) {
			if (num == 8)
				return cc.getUnlockEquippedWithType<PlaceEarthMound> ().coolDownStartedAt;
			if (sp.GetComponent<Combat>().team == 0) {
				if (num == 9)
					return cc.getUnlockEquippedWithType<PlaceMirror>().coolDownStartedAt;
			} else if (player != null) {
				if (num == 9)
					return player.trapCoolDownsStartedAt [0];
				if (num == 10)
					return player.trapCoolDownsStartedAt [1];
			} else {
				return 0f;
			}
		} else if (cc.classNum == 3) {
			if (num == 8)
				return sp.GetComponent<PlayerMove> ().movementAbilityCoolDownStartedAt;
			if (num == 9)
				return cc.getUnlockEquippedWithType<ThrowableFootball> ().coolDownStartedAt;
			if (sp.GetComponent<Combat> ().team == 0) {
				if (num == 10)
					return cc.getUnlockEquippedWithType<GolfSwing>().coolDownStartedAt;
			} else if (player != null) {
				if (num == 10)
					return player.trapCoolDownsStartedAt [0];
				if (num == 11)
					return player.trapCoolDownsStartedAt [1];
			} else {
				return 0f;
			}
		} else if (cc.classNum == 4) {
			if (num == 8)
				return sp.GetComponent<PlayerMove> ().movementAbilityCoolDownStartedAt;

			if (sp.GetComponent<Combat> ().team == 0) {

			} else if (player != null) {
				if (num == 9)
					return player.trapCoolDownsStartedAt [0];
				if (num == 10)
					return player.trapCoolDownsStartedAt [1];
			} else {
				return 0f;
			}
		}

		Debug.LogError ("Attempting to get: " + num + " when it doesn't exist on class num: " + sp.GetComponent<ClassControl> ().classNum + " / " + sp.GetComponent<Combat> ().team + " <- team");
		return null;
	}

	public static int getBitChoicesLength(byte team, int classNum) {
		if (classNum == 0) {
			if (team == 0)
				return 10;
			else
				return 11;
		} else if (classNum == 1) {
			if (team == 0)
				return 12;
			else
				return 13;
		} else if (classNum == 2) {
			if (team == 0)
				return 10;
			else
				return 11;
		} else if (classNum == 3) {
			if (team == 0)
				return 11;
			else
				return 12;
		} else if (classNum == 4) {
			if (team == 0)
				return 9;
			else
				return 11;
		}
		Debug.LogError ("No bit choices- " + team + ", " + classNum + " in: getBitChoicesLength of UnlockSnapshot");
		return 0;
	}

	public static bool isDifferent(int start, object[] lps, object[] sps) {
		return false;
	}

	public static void setObjects(object[] objects, SyncPlayer sp) {
		ClassControl cc = sp.GetComponent<ClassControl> ();

		cc.switchedAt.setPlayerTriggerTime ((float)objects [2]);

		if (cc.whichUnlock != (byte)objects [0]) {

			cc.enableUnlockEquippedRegardlessOfSwitching (false);
			cc.whichUnlock = (byte)objects [0];
			if (cc.whichUnlock == (byte)objects [1]) { // This is because of SetNextUnlock
				cc.enableUnlockEquippedRegardlessOfSwitching (true);
			}
		}
			
		if (cc.nextUnlock != (byte)objects [1]) {
			cc.SetNextUnlock((byte)objects [1], false, false);
		}

		if (!cc.isSwitching ()) {
			Unlock unlock = cc.getUnlockEquipped ();
			if (unlock != null) {
				unlock.firedAt.setPlayerTriggerTime ((float)objects [3]);
				unlock.secondaryFiredAt.setPlayerTriggerTime ((float)objects [4]);
				unlock.reloadedAt.setPlayerTriggerTime ((float)objects [5]);
				unlock.equippedAt.setPlayerTriggerTime ((float)objects [6]);
				if (unlock is GunScript && ((GunScript)unlock).isReloadableType())
					((GunScript)unlock).NumShots = (byte)objects [7];
			}
		}
		int team = sp.GetComponent<Combat> ().team;
		int classNum = sp.GetComponent<ClassControl> ().classNum;
		if (classNum == 0) {
			sp.GetComponent<PlayerMove> ().movementAbilityCoolDownStartedAt = (float)objects[8];
			if (team == 0) {
				cc.getUnlockEquippedWithType<PlaceShield> ().coolDownStartedAt = (float)objects [9];
			} else if (Player.thisPlayer) {
				Player.thisPlayer.trapCoolDownsStartedAt [0] = (float)objects [9];
				Player.thisPlayer.trapCoolDownsStartedAt [1] = (float)objects [10];
			}
		} else if (classNum == 1) {
			cc.getUnlockEquippedWithType<PlaceIcicle> ().coolDownStartedAt = (float)objects[8];
			cc.getUnlockEquippedWithType<PlaceIcicle> ().AmmoStored = (float)objects[9];
			cc.getUnlockEquippedWithType<Pistol> ().setMode ((byte)objects [10]);
			if (team == 0) {
				cc.getUnlockEquippedWithType<Throwable> ().coolDownStartedAt = (float)objects [11];
			} else if (Player.thisPlayer) {
				Player.thisPlayer.trapCoolDownsStartedAt [0] = (float)objects [11];
				Player.thisPlayer.trapCoolDownsStartedAt [1] = (float)objects [12];
			}
		} else if (classNum == 2) {
			cc.getUnlockEquippedWithType<PlaceEarthMound> ().coolDownStartedAt = (float)objects[8];
			if (team == 0) {
				cc.getUnlockEquippedWithType<PlaceMirror> ().coolDownStartedAt = (float)objects [9];
			} else if (Player.thisPlayer) {
				Player.thisPlayer.trapCoolDownsStartedAt [0] = (float)objects [9];
				Player.thisPlayer.trapCoolDownsStartedAt [1] = (float)objects [10];
			}
		} else if (classNum == 3) {
			sp.GetComponent<PlayerMove> ().movementAbilityCoolDownStartedAt = (float)objects[8];
			cc.getUnlockEquippedWithType<ThrowableFootball> ().coolDownStartedAt = (float)objects[9];
			if (team == 0) {
				cc.getUnlockEquippedWithType<GolfSwing> ().coolDownStartedAt = (float)objects [10];
			} else if (Player.thisPlayer) {
				Player.thisPlayer.trapCoolDownsStartedAt [0] = (float)objects [10];
				Player.thisPlayer.trapCoolDownsStartedAt [1] = (float)objects [11];
			}
		} else if (classNum == 4) {
			cc.GetComponent<PlayerMove> ().movementAbilityCoolDownStartedAt = (float)objects [8];
			if (team == 0) {

			} else if (Player.thisPlayer) {
				Player.thisPlayer.trapCoolDownsStartedAt [0] = (float)objects [9];
				Player.thisPlayer.trapCoolDownsStartedAt [1] = (float)objects [10];
			}
		}
	}

	public static void setObjects(int startIndex, object[] objects, SyncPlayer sp) {
		object[] finalObjects = new object[objects.Length - startIndex];
		Array.Copy (objects, startIndex, finalObjects, 0, objects.Length - startIndex);
		setObjects (finalObjects, sp);
	}
}
