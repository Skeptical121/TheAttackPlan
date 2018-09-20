using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public abstract class GunScript : Unlock {


	public bool isEnabled = false;

	GameObject ammoPanel;

	int numShots;
	public int NumShots
	{
		set
		{
			numShots = value;
			if (parentPlayerMove.thisIsMine && ammoPanel != null)
				ammoPanel.GetComponent<Text>().text = "" + value;
		}
		get
		{
			return numShots;
		}
	}

	// Player & Server only:
	public override void StartUnlock () {
		base.StartUnlock ();

		if (isReloadableType ()) {
			NumShots = getTotalShots ();
		}
	}

	public override void setHudElement (Transform t, float origNum)
	{
		base.setHudElement (t, origNum);
		if (getHudType () == "Gun") {
			ammoPanel = setHudElement (t.transform.Find("AdjacentClassHud"), "AmmoPanel", GetUnlockName (), whichBind, hudElement.GetComponent<RectTransform>(), origNum).transform.Find ("Ammo").gameObject;
			ammoPanel.transform.parent.gameObject.SetActive (false);
			NumShots = NumShots;
		}
	}

	// Standard variables; set by type.
	bool recharge = false;

	// Client, Player and Server
	bool isReloading()
	{
		return parentPlayerMove.GetComponent<SyncPlayer> ().getTotalTimeSince (reloadedAt.getTriggerTime (parentPlayerMove)) < getReloadTime ();
	}

	// Player & Server
	bool justFinishedReloading(float frameTime) {
		return !isReloading () && parentPlayerMove.GetComponent<SyncPlayer> ().getTotalTimeSince (reloadedAt.getTriggerTime (parentPlayerMove)) - frameTime < getReloadTime ();
	}

	bool isFiring() {
		return parentPlayerMove.GetComponent<SyncPlayer> ().getTotalTimeSince (firedAt.getTriggerTime (parentPlayerMove)) < getFireTime ();
	}

	bool isSecondaryFiring() {
		return parentPlayerMove.GetComponent<SyncPlayer> ().getTotalTimeSince (secondaryFiredAt.getTriggerTime (parentPlayerMove)) < getSecondaryFireTime ();
	}

	public virtual float getSecondaryFireTime() {
		return 0f; // N/A unless isSecondaryFire() == true
	}

	// Player and Server
	bool canFire()
	{
		return !isFiring() && !isReloading();
	}

	public override string getHudType () {
		return "Gun";
	}

	public override void UpdateServerAndPlayer(PlayerInput pI, bool runEffects) {
		if (isEnabled)
		{
			if (justFinishedReloading(pI.frameTime)) {
				reloadEnd();
			}
			else if (pI.fireKey && canFire() && (!isReloadableType() || NumShots > 0)) // Can't shoot while phasing..
			{
				gunFire (runEffects);
				if (runEffects) {
					Combat playerHit;
					float playerDamageDone;

					// The relevance of playerHit and playerDamageDone are found in shotgun. (To count up the damage against each player it hits)
					fire (pI, -1, -1, 0, 0, out playerHit, out playerDamageDone);
				}
			}
			else if (hasSecondaryFire() && pI.secondaryFireKey && !isSecondaryFiring()) // Can't shoot while phasing..
			{
				secondaryGunFire (runEffects);
				if (runEffects) {
					Combat playerHit;
					float playerDamageDone;

					// The relevance of playerHit and playerDamageDone are found in shotgun. (To count up the damage against each player it hits)
					secondaryFire (pI);
				}
			}


			else if (isReloadableType() && !isFiring() && !isReloading() && (NumShots <= 0 || (pI.reloadKey && NumShots < getTotalShots())))
			{
				reload (runEffects);
			}
		}
	}

	public override void PlayerAndServerAlways(PlayerInput pI, bool equipped) {
		base.PlayerAndServerAlways (pI, equipped);

		if (!equipped) {
			reloadedAt.reset();

			// Now, weapons shouldn't be able to be switched right after being fired - (for reload it'll just stop the reload)
		}
	}

	public virtual bool hasSecondaryFire() {
		return false;
	}

	public virtual bool isReloadableType() {
		return true;
	}

	public abstract int getTotalShots();

	public abstract float getReloadTime();
	public abstract float getFireTime();
	public abstract void fire(PlayerInput pI, float missDistance, float missAngle, int isThisShotgun, int shotIndex, out Combat playerHit, out float playerDamageDone); // -1 missDistance to specify random missDistance & random missAngle

	// Secondary fire is generally very simple, and is NEVER shotgun, (which the last 4 arguments above are devoted to)
	public virtual void secondaryFire(PlayerInput pI) {

	}

	void gunFire(bool runEffects)
	{
		if (isReloadableType ()) {
			NumShots--;
		}
		FireTriggered (runEffects, -1); // tickNumber is not used for this
	}

	void secondaryGunFire(bool runEffects) {
	// Secondary gun fire is generally a cooldown thing
		SecondaryFireTriggered (runEffects, -1);
	}

	// tickNumber used on CLIENT ONLY
	public void ReloadTriggered(bool runEffects, int tickNumber) // reloadedAt has passed. (Or player / server has reloaded)
	{
		if (OperationNetwork.isServer || parentPlayerMove.thisIsMine) {
			reloadedAt.trigger (parentPlayerMove.GetComponent<SyncPlayer> ().getTime ());
			parentPlayerMove.GetComponent<SyncPlayer> ().currentTriggerSet.trigger (SyncPlayer.RELOAD_TRIGGER);
		} else {
			reloadedAt.triggerClient (tickNumber);
		}
		if (runEffects) {
			SoundHandler.soundHandler.PlayReloadSound(GetType(), unlockObject.transform);
			parentPlayerMove.GetComponent<PlayerAnimation>().animTrigger("ReloadTrigger");
		}
	}

	void reload(bool runEffects)
	{
		ReloadTriggered (runEffects, -1); // tickNumber is not used for this
	}

	void reloadEnd()
	{
		NumShots = getTotalShots();
	}

	public override bool canEnable()
	{
		return true;
	}

	public override bool canDisable()
	{
		return true;
	}

	public override void midEnable ()
	{
		if (parentPlayerMove.thisIsMine && hudElement != null) {
			hudElement.GetComponent<Image> ().color = new Color (1f, 1f, 1f); // Technically this turns white a little bit too late
		}
		base.midEnable ();
	}

	public override void midDisable ()
	{
		if (parentPlayerMove.thisIsMine && hudElement != null) {
			hudElement.GetComponent<Image> ().color = new Color (0, 1f, 0); // Technically this turns green a little bit too early
		}
		base.midDisable ();
	}

	public override void enable()
	{
		isEnabled = true;
		firedAt.reset ();
		secondaryFiredAt.reset ();
		reloadedAt.reset ();
		if (ammoPanel != null) {
			ammoPanel.transform.parent.gameObject.SetActive (true);
		}
	}

	public override void disable()
	{
		isEnabled = false;
		if (ammoPanel != null) {
			ammoPanel.transform.parent.gameObject.SetActive (false);
		}
	}

	public override string getAnimationType()
	{
		return "Gun";
	}

	public virtual void SetCharge()
	{


	}
}
