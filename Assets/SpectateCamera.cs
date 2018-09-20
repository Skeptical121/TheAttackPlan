using UnityEngine;
using System.Collections;

public class SpectateCamera : MonoBehaviour {

	string displayName = "";

	void OnGUI()
	{
		if (displayName != "")
		{
			GUI.Box(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 200, 200, 24), displayName);
		}
	}

	// Update is called once per frame
	void Update () {
		if (GetComponent<Camera>().enabled && OptionsMenu.IsLockState()) {
			// Controls for SPECTATOR CAMERA!!
			float v = Input.GetAxis("Vertical");
			float h = Input.GetAxis("Horizontal");
			transform.position += transform.TransformDirection(new Vector3(h * 5f * Time.deltaTime, 0, v * 5f * Time.deltaTime));
			float xRot = transform.eulerAngles.x - Input.GetAxis("Mouse Y") * PlayerMove.sensitivity;
			if ((xRot < PlayerMove.minimumY) || (xRot >= 180 && xRot < 360 + PlayerMove.minimumY))
			{
				xRot = PlayerMove.minimumY;
			}
			if ((xRot <= 180 && xRot > PlayerMove.maximumY) || (xRot > 360 + PlayerMove.maximumY))
			{
				xRot = PlayerMove.maximumY;
			}
			transform.eulerAngles = new Vector3(xRot,
				transform.eulerAngles.y + Input.GetAxis("Mouse X") * PlayerMove.sensitivity,
				transform.eulerAngles.z);

			// See player names:

			// Almost identical to the implementation in Combat. (Should be a method, todo)
			PlayerMove pMove = GetComponent<PlayerMove>();
			displayName = "";
			RaycastHit hit;
			float dist = 50;

			// It should be noted that it has to hit the hitboxes, not the player right now because otherwise it would just the raycast would always hit your own player.. (Easy fix is just to move the raycast forward)
			if (Physics.Raycast (transform.position, transform.forward, out hit, dist, LayerLogic.HitscanShootLayer ())) {
				if (hit.transform.gameObject.layer == 16 || hit.transform.gameObject.layer == 17) {
					// Finds the actual player object
					Transform plyr = hit.transform;
					do {
						plyr = plyr.parent;
					} while (plyr.GetComponent<Combat> () == null);

					if (plyr.GetComponent<PlayerMove> ()) {
						short id = plyr.GetComponent<PlayerMove> ().plyr;
						if (GameManager.PlayerExists (id)) {
							displayName = GameManager.GetPlayer (id).name;
						}
					}
				}
			}
		}
	}
}
