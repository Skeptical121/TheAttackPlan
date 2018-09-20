using System;
using UnityEngine;
using UnityEngine.Networking;

public class RotateLaser : MonoBehaviour
{

	public float rotX = 0;
	public float rotY = 0;
	public float changeX = 0;
	public float changeY = 0;
	const float Y_ANGLE = 30f;
	const float X_ANGLE = 30f;

	const float MAX_CHANGE = 2f; // Kind of slow because it's more practical to deal with the lasers.


	// Called by server only:
	public void InitStartServer () {
		
		rotX = UnityEngine.Random.Range(-X_ANGLE, X_ANGLE);
		rotY = UnityEngine.Random.Range(-Y_ANGLE, Y_ANGLE);
		changeX = UnityEngine.Random.Range(-MAX_CHANGE, MAX_CHANGE);
		changeY = UnityEngine.Random.Range(-MAX_CHANGE, MAX_CHANGE);

		updateRotation(rotX, rotY); // This might not be necessary.

	}

	public void InitStartClient(float[] randomLaserDefinition, int i) {
		rotX = randomLaserDefinition [i * 4 + 0];
		rotY = randomLaserDefinition [i * 4 + 1];
		changeX = randomLaserDefinition [i * 4 + 2];
		changeY = randomLaserDefinition [i * 4 + 3];
	}

	public void UpdateLaser(float totalDeltaTime)
	{

		if (totalDeltaTime < 12.0f) {
			GetComponent<LaserShoot>().distanceToFire = 0.01f + totalDeltaTime * totalDeltaTime * (200f / 144f);
		} else {
			GetComponent<LaserShoot>().distanceToFire = 200f;
		}

		float thisRotX = rotX;
		float thisRotY = rotY;

		thisRotX += changeX * totalDeltaTime;
		thisRotY += changeY * totalDeltaTime;
		bool changeHappened = true;
		int i = 0;
		while (changeHappened)
		{
			changeHappened = false;
			if (thisRotX < -X_ANGLE)
			{
				thisRotX = 2 * -X_ANGLE - thisRotX; // "flip" it because the deltaTime's can be very large for games that are loading.
				changeHappened = true;
			}
			else if (thisRotX > X_ANGLE)
			{
				thisRotX = 2 * X_ANGLE - thisRotX;
				changeHappened = true;
			}
			if (thisRotY < -Y_ANGLE)
			{
				thisRotY = 2 * -Y_ANGLE - thisRotY;
				changeHappened = true;
			}
			else if (thisRotY > Y_ANGLE)
			{
				thisRotY = 2 * Y_ANGLE - thisRotY;
				changeHappened = true;
			}
			i++;
			if (i >= 200)
			{
				// As to not freeze the game: (todo: just remove, whatever)
				Debug.LogError("Infinite Loop: RotateLaser");
				return;
			}
		}
	
		updateRotation (thisRotX, thisRotY);
	}

	void updateRotation(float rotX, float rotY)
	{
		transform.rotation = (transform.parent.rotation * Quaternion.Euler(new Vector3(rotX, rotY, 0)));
	}
}
