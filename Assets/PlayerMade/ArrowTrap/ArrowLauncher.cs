using UnityEngine;
using System.Collections;

public class ArrowLauncher : Trap {

	public const float delayTime = 0.5f;

	// Set In Inspector
	public GameObject arrow;

	// To be instantiated:
	public GameObject arrowTrigger;

	// Server only
	int ticksSinceLaunch = 0;

	float lastTickHighlightedAtCLIENT_ONLY = 0; // For highlighting your own trap.


	public override short getMaxHealth () {
		return 200; // Not used.
	}

	public override float getLifeTime () {
		return 90.0f;
	}

	public override void InitStart (bool isThisMine)
	{
		base.InitStart (isThisMine);
		if (OperationNetwork.isServer) {
			// Note that this would HAVE to be "afterFirstInterp" for non-server:
			if (GameManager.PlayerExists (playerOwner)) {
				if (GameManager.GetPlayer(playerOwner).arrowLauncher) {
					GameManager.GetPlayer(playerOwner).arrowLauncher.exists = false;
				}

				GameManager.GetPlayer(playerOwner).arrowLauncher = this;

				// Server is responsible for creating arrow trigger as well.

				// One of the traps we make will have a tripwire for the trigger.

				// We need to look for the current player position, and place the trigger there.. for now..
			}
		}
	}

	bool firstUpdateDone = false;

	public override void OnDeath ()
	{
		base.OnDeath ();
		if (GameManager.PlayerExists(playerOwner) && this == GameManager.GetPlayer(playerOwner).arrowLauncher)
			GameManager.GetPlayer(playerOwner).arrowLauncher = null;
	}

	// Alpha is between 0 -> 1.
	void SetTransparency(float alpha)
	{
		Renderer curRenderer = GetComponent<Renderer>();
		Color color;
		foreach (Material material in curRenderer.materials)
		{
			color = material.color;
			color.a = alpha;
			material.color = color;
		}
	}

	public override void setHealthBar() {
	}

	public void Highlight()
	{
		Transform highlight = transform.Find("Highlight");
		highlight.GetComponent<Renderer>().enabled = true;
		lastTickHighlightedAtCLIENT_ONLY = Time.time;
	}

	void GeneralUpdate() {
		// Render stuff:
		if (Time.time - lastTickHighlightedAtCLIENT_ONLY > 0.1f)
		{
			Transform highlight = transform.Find("Highlight");
			highlight.GetComponent<Renderer>().enabled = false;
		}
	}

	// Only called on server!!
	public void Launch() 
	{
		if (alive) {
			alive = false;
			diedAt = ServerState.tickNumber;
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		int previousTotal = base.SetInformation (data, a, b, isThisFirstTick, isThisMine);

		GeneralUpdate ();

		return previousTotal;
	}

	public override void ServerSyncFixedUpdate() {
		base.ServerSyncFixedUpdate ();

		// It seems like "OperationAddSyncState" shouldn't be done in InitStart..
		if (!firstUpdateDone) {
			firstUpdateDone = true;
			if (GameManager.GetPlayer (playerOwner).playerObject != null) {
				// trip wire won't go up or down, it'll stay straight:
				Vector3 pos = GameManager.GetPlayer(playerOwner).playerObject.transform.position;
				//pos.y = transform.position.y - 1.1f;

				RaycastHit hit;
				if (Physics.Raycast (transform.position + Vector3.down * 1.5f, Vector3.Normalize (pos - (transform.position + Vector3.down * 1.5f)), out hit, 12f, LayerLogic.WorldColliders())) {
					// hmm.. 
				}

				GameObject obj = (GameObject)Instantiate (arrowTrigger, pos + Vector3.down, Quaternion.identity);
				obj.GetComponent<PlayerMade> ().playerOwner = playerOwner;
				OperationNetwork.OperationAddSyncState (obj);
			}
		}

		GeneralUpdate();
	}

	public override float getDeathTime() {
		return delayTime + 3.1f;
	}
	public override float getBuildTime () {
		return 2f;
	}

	public override void buildAnimation (float lifeTime) {
		SetTransparency(1.0f - lifeTime / getBuildTime());
	}

	public override void endBuildAnimation() {
		GetComponent<Renderer>().enabled = false;
	}

	bool playedLaunchSound = false;

	public override void deathAnimation(float deathTime) {
		// Reveals the trap over the initial time:
		if (!playedLaunchSound) {
			SoundHandler.soundHandler.PlayVoiceLine ((byte)4, (byte)255, transform);
			playedLaunchSound = true;
		}
		if (deathTime < delayTime) {
			if (!GetComponent<Renderer> ().enabled) {
				GetComponent<Renderer> ().enabled = true;
			}
			SetTransparency ((delayTime - deathTime) / delayTime);
		} else {


			if (OperationNetwork.isServer) {
				ticksSinceLaunch++;
				if (ticksSinceLaunch <= 150 && ticksSinceLaunch % 5 == 0) { // 150 ticks is 3 seconds.
					float maxMissAngle = 25f;
					// Launch arrow:
					Vector2 randInUnitCircle = UnityEngine.Random.insideUnitCircle;
					Vector3 direction = randInUnitCircle * maxMissAngle * Mathf.PI / 180.0f; // This is here because the accuracy, is on average less when using a cone to do inaccuracy. This increases the amount of shots that are more accurate. In the future it could just use tan, or maybe tan with this.
					direction.z = 1.0f;
					//direction = transform.TransformDirection(direction.normalized);
					Quaternion fireRot = Quaternion.LookRotation(transform.forward) * Quaternion.LookRotation(direction.normalized);
					Vector3 fireDir = fireRot * Vector3.forward;

					GameObject arrowObj = (GameObject)Instantiate(arrow, transform.position + transform.forward * 0.6f + transform.right * 1f * randInUnitCircle.x + transform.up * 1f * randInUnitCircle.y, fireRot);
					arrowObj.GetComponent<SyncGameState> ().playerOwner = playerOwner;
					arrowObj.GetComponent<Projectile>().SetInitialVelocity(fireDir * Random.Range(24, 32));
					OperationNetwork.OperationAddSyncState (arrowObj);
				}
			}
		}
	}
}
