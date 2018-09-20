using UnityEngine;
using UnityEngine.Networking;

// Laser Shoot also takes care of networking

	// No longer part of players!

public class LaserShoot : MonoBehaviour
{
	

	int team = 1; // By default.. hmm..

	public GameObject onHitPrefab;

	public GameObject laserPrefab;
	public GameObject[] laser;

	public Vector3 offset;


	float damageDealt = 200f; // Was 150.. DPS was 100 25, (was 20)
	public float distanceToFire = 0.01f; // Increases from 0 -> 200

	// Set on server by LaserField:
	public short playerOwner;

	// This is similar to SawScript in its implementation.

	// This is called by LaserField!
	public void InitStart() {
		for (int i = 0; i < laser.Length; i++) {
			laser [i] = (GameObject)Instantiate (laserPrefab, transform);
		}
	}

	void SetLaserSettings(ParticleSystem ps, float distance) {
		ParticleSystem.ShapeModule sm = ps.shape;
		sm.length = distance - 0.6f; // 0.4 seconds lifetime, speed = 1. Slight extension

		ParticleSystem.EmissionModule em = ps.emission;
		ParticleSystem.MinMaxCurve r = em.rate;
		r.constantMin = distance * 25;
		r.constantMax = distance * 25;
		r.constant = distance * 25;
		em.rate = r;
	}

	// Update is called once per frame
	void FixedUpdate()
	{
		// Laser only "shoots" 17 times per second
		if (OperationNetwork.isServer && ServerState.tickNumber % SawScript.numTicksPerHit != 0)
			return;

		// This is run on BOTH Server and Client, and hitscanShoot has a way to make it only do damage on server

		Vector3 pos = transform.position + transform.right * offset.x + transform.up * offset.y;
		Vector3 forward = -transform.forward;

		Vector3 lastBounce = pos; // This is for damageIndicator.

		bool setLast = true;


		Combat playerHit; // Throwaway
		float playerDamageDone; // Throwaway

		// This also does the damage on server
		Vector3[] laserPos = HitscanGun.hitscanShoot (pos, pos, forward, null, null, null, "", team, damageDealt * Time.fixedDeltaTime * SawScript.numTicksPerHit, 0.1f, 16, distanceToFire, 0, null, out playerHit, out playerDamageDone, playerOwner);

		int index = 1;
		for (; index < laserPos.Length && index <= laser.Length; index++) {
			float distance = Vector3.Distance (laserPos [index - 1], laserPos [index]);
			Vector3 laserForward = Vector3.Normalize(laserPos[index] - laserPos[index - 1]); // This occasionally throws warning for being same point.. (?)

			laser[index - 1].transform.rotation = new Quaternion();
			laser[index - 1].transform.position = laserPos[index - 1] + laserForward * 0.8f;
			laser [index - 1].transform.forward = laserForward;

			SetLaserSettings (laser [index - 1].GetComponent<ParticleSystem> (), distance);
			laser [index - 1].GetComponent<LineRenderer> ().SetPosition (0, laserPos [index - 1]);
			laser [index - 1].GetComponent<LineRenderer> ().SetPosition (1, laserPos [index]);
		}

		GameObject particles = (GameObject)Instantiate(onHitPrefab, laserPos[index - 1], Quaternion.identity);
		Destroy(particles, 0.3f);

		index--;

		// Render the remaining
		for (;index < laser.Length; index++)
		{
			laser[index].transform.position = Vector3.zero;
			SetLaserSettings (laser [index].GetComponent<ParticleSystem> (), 0);
			laser [index].GetComponent<LineRenderer> ().SetPosition (0, Vector3.zero);
			laser [index].GetComponent<LineRenderer> ().SetPosition (0, Vector3.zero);
		}
	}

}
