using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class PlaceIcicle : PlacePlayerMade
{

	public override int getUnlockPosition() {
		return 2;
	}

	float ammoStored;
	public float AmmoStored
	{
		set
		{
			ammoStored = value;
			if (ammoStored > maxAmmoStored)
				ammoStored = maxAmmoStored;
			if (parentPlayerMove.thisIsMine && hudElement != null) {
				for (int i = 0; i < ammoStored - 1; i++) {
					fillIcicle (i, 1);
				}
				if (ammoStored < maxAmmoStored) {
					fillIcicle ((int)(ammoStored), ammoStored % 1f);
					for (int j = (int)ammoStored + 1; j < maxAmmoStored; j++) {
						fillIcicle (j, 0);
					}
				}
			}
		}
		get
		{
			return ammoStored;
		}
	}

	const float recoilTime = 0.5f;
	public const int maxAmmoStored = 3;

	void fillIcicle(int num, float percent) {
		hudElement.transform.Find("Icicle" + (num + 1)).GetComponent<Image> ().fillAmount = 1 - percent;
		hudElement.transform.Find("Icicle" + (num + 1)).Find("IcicleCharge").GetComponent<Image> ().fillAmount = percent;
	}

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		cutOut = Resources.Load ("IciclePlacement") as GameObject;
	}

	public override void setPlacingRotation(Vector3 normal)
	{
		placing.transform.position += normal * 1.6f;
		placing.transform.rotation = Quaternion.LookRotation(normal);
	}

	public override string getHudType () {
		return "Icicle";
	}

	public override bool normalIsValid(RaycastHit hit, Vector3 normal)
	{
		return true;
	}

	public override bool canEnable ()
	{
		return true;
	}

	public override bool canPlace() {
		return AmmoStored >= 1 && parentPlayerMove.GetComponent<SyncPlayer> ().getTotalTimeSince(coolDownStartedAt) > recoilTime;
	}

	// PlaceIcicle's ammoPanel remains active at all times. It should also contain an icicle graphic to explain this, plus a reference to the bind, much like cooldown's are shown.
	// Infact, it should be shown more like a cooldown type, rather than a gun type

	public override void setCoolDown()
	{
		AmmoStored--;
		coolDownStartedAt = parentPlayerMove.GetComponent<SyncPlayer> ().getTime ();
	}

	public override void PlayerAndServerAlways(PlayerInput pI, bool equipped) {
		AmmoStored += pI.frameTime / getCoolDown();
		base.PlayerAndServerAlways (pI, equipped);
	}

	public override void UpdateServerAndPlayer(PlayerInput pI, bool runEffects)
	{
		base.UpdateServerAndPlayer (pI, runEffects);
	}

	// Not applicable for Icicle currently: (Its infinite right now)
	public override int getPlacingLength()
	{
		return 200; // Standard max distance.
	}

	public override float getSwitchToTime()
	{
		return 0.5f;
	}

	public override float getSwitchFromTime()
	{
		return 0.3f;
	}

	public override string GetUnlockName() {
		return "Icicle";
	}

	public override float getCoolDown()
	{
		return 3.0f;
	}
}
