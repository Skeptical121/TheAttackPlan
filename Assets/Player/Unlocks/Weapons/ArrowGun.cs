using UnityEngine;
using System.Collections;

public class ArrowGun : ProjectileGunScript {

	public override int getUnlockPosition() {
		return 0;
	}

	public override void LoadUnlockObject() {
		unlockObject = Resources.Load ("LaserGun") as GameObject;

		// Also can load other things here:
		if (parentPlayerMove.GetComponent<Combat> ().team == 0) {
			bulletPrefab = Resources.Load ("BlueArrow") as GameObject;
		} else {
			bulletPrefab = Resources.Load ("RedArrow") as GameObject;
		}
	}

	public override string GetUnlockName() {
		return "Arrow Gun";
	}

	public override float GetSpeed()
	{
		return 35f;
	}

	public override int getTotalShots()
	{
		return 7;
	}

	public override float getReloadTime()
	{
		return 1.3f;
	}

	public override float getFireTime()
	{
		return 0.625f * 2;
	}
}
