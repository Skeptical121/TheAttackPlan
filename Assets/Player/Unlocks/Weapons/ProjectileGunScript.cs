using System;
using UnityEngine;
using UnityEngine.Networking;

// of Gun
public class ProjectileGunScript : GunScript
{
	public override int getUnlockPosition() {
		return 0;
	}

	public GameObject bulletPrefab;

	public GameObject soundEffect;
	
	public override void LoadUnlockObject() {
		unlockObject = Resources.Load ("ProjectileGun") as GameObject;

		// Also can load other things here:
		if (parentPlayerMove.GetComponent<Combat> ().team == 0) {
			bulletPrefab = Resources.Load ("BulletBlue") as GameObject;
		} else {
			bulletPrefab = Resources.Load ("BulletRed") as GameObject;
		}
	}

	public void FireBullet(Vector3 cameraForward)
	{
		GameObject bullet = (GameObject)MonoBehaviour.Instantiate(
			bulletPrefab, parentPlayerMove.transform.position + parentPlayerMove.GetMainCameraLocalPos() * parentPlayerMove.transform.localScale.x + 
			cameraForward * 0.12f + Quaternion.LookRotation(cameraForward) * Vector3.up * -0.1f + Quaternion.LookRotation(cameraForward) * Vector3.right * 0.11f, 
			Quaternion.LookRotation(cameraForward));
		bullet.GetComponent<SyncGameState> ().playerOwner = parentPlayerMove.plyr;
		bullet.GetComponent<Projectile>().SetInitialVelocity(cameraForward * GetSpeed());
		OperationNetwork.OperationAddSyncState (bullet);
	}

	// Projectiles don't miss currently.
	public override void fire(PlayerInput pI, float missDistance, float missAngle, int isThisShotgun, int shotIndex, out Combat playerHit, out float playerDamageDone)
	{

		if (OperationNetwork.isServer)
		{
			FireBullet (parentPlayerMove.mainCamera.transform.forward); // MainCamera on server side is just an empty transform.
		}

		playerHit = null;
		playerDamageDone = 0;
	}

	public override string GetUnlockName() {
		return "Rocket Gun";
	}

	public virtual float GetSpeed()
	{
		return 25f * (1f + DamageCircle.isTouchingDamageCircle(parentPlayerMove.timeSinceTouchingDamageCircle) * 1f); // Was 20f
	}

	public override int getTotalShots()
	{
		return 5;
	}

	public override float getReloadTime()
	{
		return 1.9f;
	}

	public override float getFireTime()
	{
		return 0.625f;
	}
}