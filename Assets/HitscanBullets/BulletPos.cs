using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BulletPos : MonoBehaviour {

	float timeSinceShot = 0f;
	public Vector3[] shotPositions; // Set on spawn

	bool doneFiring = false;

	float maxDistance;

	// DEFAULT:
	float velocity = 400f;
	float lengthOfBullet = 18f;
	float deathTime = 0.04f;

	float timeOfDeath;

	// Called after shotPositions are set:
	public void init() {
		if (name.Contains ("BulletPistolSlowFire")) {
			timeSinceShot = 100;
			velocity = 2500f; // Almost instant
			lengthOfBullet = 200f;
			deathTime = 0.09f;
		} else if (name.Contains ("BulletPistolFastFire")) {
			velocity = 1000f; 
			lengthOfBullet = 200f;
			deathTime = 0.06f;
		}

		for (int i = 1; i < shotPositions.Length; i++) {
			maxDistance += Vector3.Distance (shotPositions [i - 1], shotPositions [i]);
		}
	}
	
	// Update is called once per frame
	void Update () {
		timeSinceShot += Time.deltaTime;
		setBulletPos();
	}

	void SetLaserGunPositions(ParticleSystem ps, float distance) {
		ParticleSystem.ShapeModule sm = ps.shape;
		sm.length = distance - 0.6f; // 0.4 seconds lifetime, speed = 1. Slight extension

		ParticleSystem.EmissionModule em = ps.emission;
		em.SetBursts (new ParticleSystem.Burst[]{ new ParticleSystem.Burst (0f, (short)(distance * 2)) });

		ps.Play ();
	}

	void setBulletPos()
	{
		int posStartDefined = -1;
		List<Vector3> lineRenderPositions = new List<Vector3> ();

		float endDist = Mathf.Min(maxDistance, timeSinceShot * velocity);
		if (endDist == maxDistance && !doneFiring) {
			doneFiring = true;
			timeOfDeath = Time.time;

			if (name.Contains ("BulletPistolSlowFire")) {
				transform.rotation = Quaternion.LookRotation (shotPositions [1] - shotPositions [0]);
				SetLaserGunPositions (GetComponent<ParticleSystem> (), Vector3.Distance (shotPositions [0], shotPositions [1]));

				Destroy(gameObject, deathTime + 5f); // Extra time for the particles
			} else {
				Destroy(gameObject, deathTime);
			}
		}

		if (doneFiring) {
			if (name.Contains ("BulletPistolSlowFire") || name.Contains ("BulletPistolFastFire")) {
				float initAlpha = 1f;
				if (name.Contains ("BulletPistolFastFire"))
					initAlpha = 60f / 255f;

				float percent = Mathf.Max (0, (deathTime - (Time.time - timeOfDeath)) / deathTime);

				Gradient g = new Gradient ();
				g.mode = GradientMode.Blend;
				g.SetKeys(GetComponent<LineRenderer> ().colorGradient.colorKeys, 
				new GradientAlphaKey[]{new GradientAlphaKey(initAlpha * percent, 0),
					new GradientAlphaKey(percent, 1)});

				GetComponent<LineRenderer> ().colorGradient = g;

			}
		}

		float startDist = Mathf.Max (0, endDist - lengthOfBullet); // 18f = length of bullet



		float prevNetDistance = 0;
		float netDistance = 0;
		for (int i = 1; i < shotPositions.Length; i++) {
			if (posStartDefined >= 0 && i != 1) {
				posStartDefined++;
				lineRenderPositions.Add(shotPositions [i - 1]);
			}
			netDistance += Vector3.Distance (shotPositions [i - 1], shotPositions [i]); // Whatever..
			if (posStartDefined == -1 && netDistance > startDist) {
				posStartDefined = 0;
				lineRenderPositions.Add(Vector3.Lerp (shotPositions [i - 1], shotPositions [i], (startDist - prevNetDistance) / (netDistance - prevNetDistance)));
			}
			if (netDistance >= endDist) {
				// Set Position:
				transform.position = Vector3.Lerp (shotPositions [i - 1], shotPositions [i], (endDist - prevNetDistance) / (netDistance - prevNetDistance));
				transform.rotation = Quaternion.LookRotation (shotPositions [i - 1] - shotPositions [i]);

				// Set Line renderer:
				lineRenderPositions.Add(transform.position);
				GetComponent<LineRenderer> ().SetVertexCount (lineRenderPositions.Count);
				GetComponent<LineRenderer> ().SetPositions (lineRenderPositions.ToArray ());
				break;
			}
			prevNetDistance = netDistance;
		}
	}
}
