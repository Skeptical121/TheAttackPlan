using UnityEngine;
using System.Collections;

public class HealthPack : MonoBehaviour {

	int respawnTime = 10; // 10 seconds to respawn is pretty standard.

	public float timeSinceTake = 500f;
	public float healthGive = 50f;

	// issueDesktop healthPacks have to be redone with LevelLogic.

	public byte healthPackID; // Assigned by mainCode. Should be a max of 255 healthPacks.

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (timeSinceTake < respawnTime)
		{
			timeSinceTake += Time.deltaTime;
			if (timeSinceTake >= respawnTime)
			{
				GetComponent<Renderer>().enabled = true;
			}
		} else
		{

			// Rotate health pack:
			transform.Rotate(Vector3.right, Time.deltaTime * 50f);
		}
	}

	public void TakeHealthPack()
	{
		timeSinceTake = 0f;
		GetComponent<Renderer>().enabled = false;

		// TODO: health packs need to be synced.
	}

	// Server only
	public bool exists() {
		return timeSinceTake >= respawnTime;
	}

	// Interestingly, someone with bad connection will not pickup the health pack if they teleport past it.. same applies to traps though..
	void OnTriggerStay(Collider hit)
	{

		if (!OperationNetwork.isServer)
			return;

		if (timeSinceTake >= respawnTime && (hit.gameObject.layer == 8 || hit.gameObject.layer == 22))
		{
			float healthDiff = hit.gameObject.GetComponent<Combat>().maxHealth - hit.gameObject.GetComponent<Combat>().health;
			if (healthDiff > 0) {
				TakeHealthPack();
				// Give health:
				hit.gameObject.GetComponent<Combat>().TakeDamage(Mathf.Max(-healthDiff, -healthGive), Combat.HEALING); // Heal that health Difference at most.
			}
		}
	}
}
