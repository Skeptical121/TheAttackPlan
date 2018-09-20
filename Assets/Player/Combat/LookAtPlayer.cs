using UnityEngine;
using System.Collections;

public class LookAtPlayer : MonoBehaviour {

	float aliveTime;
	float initialHeight;
	float movementAngle;
	float movementSpeed;

	// Use this for initialization
	void Start () {
		aliveTime = 0f;
		initialHeight = transform.position.y;
		movementAngle = Random.Range(0, Mathf.PI * 2);
		movementSpeed = Random.Range(0, 1f);
		Update();
	}

	public void DamageDone(short dmg)
	{
		if (dmg > 0) {
			GetComponent<TextMesh>().text = "" + (int)dmg;
			// Hitsound:
			GetComponent<AudioSource>().volume = Mathf.Clamp01(0.3f * (0.5f + dmg / 50.0f));
			GetComponent<AudioSource>().pitch = Mathf.Clamp(1.8f - dmg / 65.0f, 0.3f, 3);
		} else if (dmg < 0)
		{
			GetComponent<TextMesh>().text = "" + (int)(-dmg);
			GetComponent<TextMesh>().color = new Color(0, 0, 255);
			// Hitsound:
			GetComponent<AudioSource>().volume = Mathf.Clamp01(0.3f * (0.5f + -dmg / 50.0f));
			GetComponent<AudioSource>().pitch = Mathf.Clamp(1.8f - -dmg / 65.0f, 0.3f, 3);
		}
	}
	
	// Update is called once per frame
	void Update () {
		aliveTime += Time.deltaTime;
		transform.position = new Vector3(transform.position.x + Mathf.Cos(movementAngle) * movementSpeed * Time.deltaTime, initialHeight - (aliveTime * aliveTime + 0.7f * aliveTime) * 2f, transform.position.z + Mathf.Cos(movementAngle) * movementSpeed * Time.deltaTime);
		if (Player.thisPlayer.playerObject == null)
		{
			Destroy(gameObject);
			return;
		}
		if (Player.thisPlayer.playerCamera != null) {
			GameObject mainCam = Player.thisPlayer.playerCamera;
			transform.rotation = Quaternion.LookRotation (transform.position - mainCam.transform.position);
			GetComponent<TextMesh> ().characterSize = 0.025f + Vector3.Distance (mainCam.transform.position, transform.position) / 25.0f;
		}
		
		if (aliveTime > 0.2f)
		{
			Renderer currRenderer = GetComponent<Renderer>();
			Color color;
			foreach (Material material in currRenderer.materials)
			{
				color = material.color;
				color.a = (0.5f - aliveTime) / 0.3f;
				material.color = color;
			}
		}
	}
}
