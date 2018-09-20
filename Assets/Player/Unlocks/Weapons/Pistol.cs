using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Pistol : HitscanGun
{

	const byte SHORT_FAST_FIRE = 0;
	const byte LONG_SLOW_FIRE = 1;
	const byte CONDUCTIVE = 2;

	// When switching modes, the firedAt will be changed on the pistol to account for the changed fireTime
	public byte mode = SHORT_FAST_FIRE;

	public override int getUnlockPosition() {
		return 1;
	}

	public override string getBulletType() {
		if (mode == SHORT_FAST_FIRE) {
			return "BulletPistolFastFire";
		} else if (mode == LONG_SLOW_FIRE) {
			return "BulletPistolSlowFire";
		}
		return "BulletDefault"; // N/A right now
	}

	// Hitscan weapons have no limit to how many mirror bounces they have

	public void attemptConductiveExplode(Vector3 pos) {
		if (mode == CONDUCTIVE) {

		}
	}

	public override void LoadUnlockObject() {
		unlockObject = Resources.Load ("LaserPistol") as GameObject;
	}

	public override void StartUnlock()
	{
		base.StartUnlock ();
		setMode(SHORT_FAST_FIRE);
	}

	public override string getHudType () {
		return "Gun3Modes";
	}
		
	public void setMode(byte mode) {
		this.mode = mode;
		// No need to mess with firedAt time unless we want it to work the other way in terms of recoil time.
		if (mode == SHORT_FAST_FIRE) {
			damageDealt = 12f;
			maxMissAngle = 6f;
		} else if (mode == LONG_SLOW_FIRE) {
			damageDealt = 27f;
			maxMissAngle = 0.3f;
		} else if (mode == CONDUCTIVE) {
			damageDealt = 18f;
			maxMissAngle = 0.7f;
		}

		parentPlayerMove.GetComponent<Animator>().SetFloat("LaserGunFireSpeed", 1f / getFireTime());
		if (parentPlayerMove.thisIsMine) {
			parentPlayerMove.playerView.viewmodelPlayer.GetComponent<Animator> ().SetFloat ("LaserGunFireSpeed", 1f / getFireTime ());
		}
	}

	public void UpdatePistolMode(PlayerInput pI) {
		if (pI.unlockSwitchTo == OptionsMenu.PRIMARY_1) {
			setMode(SHORT_FAST_FIRE);
		} else if (pI.unlockSwitchTo == OptionsMenu.PRIMARY_2) {
			setMode(LONG_SLOW_FIRE);
		} else if (pI.unlockSwitchTo == OptionsMenu.PRIMARY_3) {
			setMode(CONDUCTIVE);
		}
	}

	public override Transform getGunBone(Transform playerObjTransform) {
		return playerObjTransform.Find ("Armature").Find ("Pelvis").
			Find ("Stomach").Find ("Chest").Find ("UpperArm_L").Find ("LowerArm_L").Find ("Wrist_L");
	}

	public override string GetUnlockName() {
		return "Laser Gun";
	}

	public override int getTotalShots()
	{
		return 16;
	}

	public override float getReloadTime()
	{
		return 1.15f;
	}

	public override float getFireTime()
	{
		// 0.17f was the old time
		if (mode == SHORT_FAST_FIRE) {
			return 0.13f;
		} else if (mode == LONG_SLOW_FIRE) {
			return 0.4f;
		} else if (mode == CONDUCTIVE) {
			return 0.3f;
		}
		// doesn't happen..
		return 10.17f;
	}
}
