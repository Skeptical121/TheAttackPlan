using UnityEngine;
using System.Collections;

public class LaunchIcicle : PlayerMade
{

	float healthToLaunch = 100f; // Fairly minimal health. (multiplied by 10) Could just be instant launch? (Not synced)

	public GameObject icicleDet;
	public GameObject icicle;

	bool launched = false;

	public override short getMaxHealth () {
		return 200;
	}

	public override float getLifeTime () {
		return 24f;
	}
		
	// Server
	public void Launch()
	{
		if (!launched) {
			launched = true;

			icicle = (GameObject)Instantiate (icicle, transform.position, transform.rotation);
			icicle.GetComponent<Rigidbody> ().velocity = transform.forward * 15f;
			icicle.GetComponent<SyncGameState> ().playerOwner = playerOwner;
			OperationNetwork.OperationAddSyncState (icicle);

			// Destroy this: (No need to do alive set)
			exists = false;
		}
	}

	public override void OnDeath ()
	{
		GameObject det = (GameObject)Instantiate (icicleDet, transform.position - transform.forward * 0.761f, transform.rotation);
		Destroy (det, 5.0f);
	}

	public override void TakeDamageObject (float amount, Vector3 fromWhere, bool isHitscan, short playerSender)
	{
		// Intercept it if it cames from an allied player:
		if (GameManager.PlayerExists (playerSender) && GameManager.GetPlayer(playerSender) && GameManager.GetPlayer(playerSender).team == team && OperationNetwork.isServer) {
			TakeDamageIcicle (amount);
		} else {
			base.TakeDamageObject (amount, fromWhere, isHitscan, playerSender);
		}
	}
		
	public void TakeDamageIcicle(float amount)
	{

		if (!OperationNetwork.isServer)
		{
			Debug.LogError ("Issue! LaunchIcicle -> TakeDamageIcicle");
			return;
		}

		if (!launched)
		{
			healthToLaunch -= amount; // No need to update this on other clients

			if (healthToLaunch <= 0)
			{
				Launch ();
			}
		}
	}
}
