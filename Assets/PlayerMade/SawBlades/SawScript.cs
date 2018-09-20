using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SawScript : MonoBehaviour
{
	public static int numTicksPerHit = 3;

	// Should be placed on the actual Saw because it uses OnTriggerStay.

	List<GameObject> playersHitLastTick = new List<GameObject>();
	List<float> lastHitTime = new List<float>();
	List<int> numTicksHit = new List<int>();

	// Remember, these values aren't used (Set them to appropiate values in inspector)
	public float minDPS = 100;
	public float maxDPS = 500;
	public Vector3 localOffset = new Vector3(0, 0, 0);

	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}

	void FixedUpdate()
	{
		if (!OperationNetwork.isServer)
			return;
		
		for (int i = 0; i < playersHitLastTick.Count; i++)
		{
			if (Time.time - lastHitTime[i] > 0.1f)
			{
				playersHitLastTick.RemoveAt(i);
				lastHitTime.RemoveAt(i);
				numTicksHit.RemoveAt(i);
				i--;
			}
		}
	}

	// DUPLICATE CODE TO "MoveSpikes":
	void OnTriggerStay(Collider hit)
	{

		if (!OperationNetwork.isServer || ServerState.tickNumber % numTicksPerHit != 0)
			return;

		if (hit.gameObject.layer == 8 || hit.gameObject.layer == 22)
		{
			float dmg = maxDPS * Time.fixedDeltaTime * numTicksPerHit;
			Combat combat = hit.GetComponent<Combat>();
			int indexOfPlayer = playersHitLastTick.IndexOf(hit.gameObject);
			if (indexOfPlayer != -1)
			{
				dmg *= Mathf.Pow(0.6f, numTicksHit[indexOfPlayer] * 0.3f);
				if (dmg < minDPS * Time.fixedDeltaTime * numTicksPerHit) // Minimum of 100dps.
				{
					dmg = minDPS * Time.fixedDeltaTime * numTicksPerHit;
				}
				lastHitTime[indexOfPlayer] = Time.time;
				numTicksHit[indexOfPlayer]++;
				
			} else
			{
				playersHitLastTick.Add(hit.gameObject);
				lastHitTime.Add(Time.time);
				numTicksHit.Add(1);
			}



			if (combat.GetComponent<Combat>().team == (gameObject.layer - 10) / 5) {
				combat.TakeDamageMove(dmg, 1.0f, Vector3.Normalize(hit.transform.position - transform.TransformPoint(localOffset)));
			} else {
				PlayerMade playerMade;
				if (transform.parent.GetComponent<PlayerMade> ()) {
					playerMade = transform.parent.GetComponent<PlayerMade> ();
				} else {
					playerMade = transform.parent.parent.GetComponent<PlayerMade> ();
				}
				combat.TakeDamage(dmg, 1.0f, Vector3.Normalize(hit.transform.position - transform.TransformPoint(localOffset)), false, Combat.SAW, false, transform.position, playerMade.playerOwner);
			}
		}
	}
}