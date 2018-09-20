using UnityEngine;
using System.Collections;

public class ArrowTrigger : Trap {

	float lastHighlightedAt = 0; // For highlighting your own trigger.

	// Arrow Trigger is now a tripwire

	// Use this for initialization
	void Start () {
	
	}

	public void Highlight()
	{
		Transform highlight = transform.Find("Highlight");
		highlight.GetComponent<Renderer>().enabled = true;
		lastHighlightedAt = Time.time;
	}

	public override short getMaxHealth () {
		return 200; // Not used.
	}

	public override float getLifeTime () {
		return 90.0f;
	}

	public override void setHealthBar() {
	}

	public override void InitStart (bool isThisMine)
	{
		base.InitStart (isThisMine);
		// Note that this would HAVE to be "afterFirstInterp" for non-server:

		if (GameManager.PlayerExists(playerOwner)) {
			if (GameManager.GetPlayer(playerOwner).arrowTrigger) {
				GameManager.GetPlayer(playerOwner).arrowTrigger.exists = false;
			}
			GameManager.GetPlayer(playerOwner).arrowTrigger = this;
		}
	}

	public override void OnDeath ()
	{
		base.OnDeath ();
		if (GameManager.PlayerExists(playerOwner) && this == GameManager.GetPlayer (playerOwner).arrowTrigger)
			GameManager.GetPlayer (playerOwner).arrowTrigger = null;
	}

	void Update()
	{
		if (Time.time - lastHighlightedAt > 0.1f)
		{
			Transform highlight = transform.Find("Highlight");
			highlight.GetComponent<Renderer>().enabled = false;
		}
	}

	void OnTriggerStay(Collider hit)
	{

		if (!OperationNetwork.isServer)
			return;

		if (!alive || !exists)
			return;

		if (hit.gameObject.layer == 8) // Talk about hard coding it in. Only effects team Attackers
		{
			// No damage. It simply triggers the trap:
			if (GameManager.PlayerExists(playerOwner) && GameManager.GetPlayer(playerOwner).arrowLauncher != null)
			{
					
				GameManager.GetPlayer(playerOwner).arrowLauncher.Launch();

				alive = false;
				diedAt = ServerState.tickNumber;
			}
			//combat.TakeDamage(dmg, 1.0f, Vector3.Normalize(hit.transform.position - transform.position), false, false, transform.position, OperationNetwork.ServerObject);
		}
	}
}
