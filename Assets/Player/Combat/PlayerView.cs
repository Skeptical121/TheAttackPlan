using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerView {
	public GameObject viewmodelCamera;

	public GameObject viewmodelPlayer; // The player instantiates this.

	public void playerOnly(PlayerMove parent)
	{
		//viewmodelPlayer:
		viewmodelPlayer.transform.position = new Vector3(0, 10, 0); // The way this would become an issue is if teleporting became a thing. This should then be moved to lateUpdate.

		// Main camera with considerations to crouching:
		parent.mainCamera.transform.localPosition = parent.GetMainCameraLocalPos();
	}

	public void createViewModelPlayer(PlayerMove parent)
	{
		viewmodelPlayer = (GameObject)MonoBehaviour.Instantiate(parent.gameObject, new Vector3(0, 10, 0), Quaternion.identity);

		viewmodelPlayer.transform.parent = parent.transform;

		Transform mB1 = viewmodelPlayer.transform.Find("Armature").Find("Pelvis");
		setBoneLayersTransformRecursive(mB1);
		viewmodelPlayer.transform.Find("PersonModelObject").gameObject.layer = 12; // Viewmodel
		foreach (Transform child in viewmodelPlayer.transform) {
			if (child.name.Contains ("Armor") || child.name.Contains("PhaseThroughIndicator")) {
				child.gameObject.layer = 12; // Viewmodel
				if (child.name.Contains ("Helmet")) {
					child.gameObject.GetComponent<Renderer> ().enabled = false;
				}
			}
		}

		// Disable all scripts but "PlayerViewmodels"
		foreach (MonoBehaviour script in viewmodelPlayer.GetComponents<MonoBehaviour>())
		{
			script.enabled = false;
		}

		viewmodelCamera = Resources.Load ("ViewmodelCamera") as GameObject;
		viewmodelCamera = (GameObject)MonoBehaviour.Instantiate(viewmodelCamera, Vector3.zero, Quaternion.identity);
		viewmodelCamera.transform.parent = viewmodelPlayer.transform;
		viewmodelCamera.transform.localPosition = new Vector3(0, 1.10f, 0.14f);

	}

	public PlayerView(PlayerMove parent, bool setOwner)
	{
		if (setOwner) {
			// Set some static variables. IMPORTANT!
			OptionsMenu.classSelectionMenuOpen = false;
			OptionsMenu.ChangeLockState ();

			createViewModelPlayer (parent);

			parent.predictionErrorTest = new PredictionErrorTest ();

			GameObject.Find ("DeathCamera").GetComponent<Camera> ().enabled = false;
			GameObject.Find ("DeathCamera").GetComponent<AudioListener> ().enabled = false;

			parent.mainCamera = (GameObject)MonoBehaviour.Instantiate (parent.mainCamera, Vector3.zero, Quaternion.identity);
			parent.mainCamera.transform.parent = parent.transform;
			parent.mainCamera.transform.localPosition = parent.GetMainCameraLocalPos ();

			// Don't render the world player to the main camera:
			setGunsToNotRender (parent.transform.Find ("Armature").Find ("Pelvis"));

			parent.transform.Find ("PersonModelObject").gameObject.layer = 11; // Mirror see only
			foreach (Transform child in parent.transform) {
				if (child.name.Contains ("Armor") || child.name.Contains ("Cube") || child.name.Contains ("Cylinder") || child.name.Contains ("Curve") || child.name.Contains ("PhaseThroughIndicator")) {
					child.gameObject.layer = 11; // Mirror see only
				}
			}

			parent.lastPlayerPosition = parent.transform.position;
		}
	}

	// This is for the viewmodels:
	static void setBoneLayersTransformRecursive(Transform boneParent)
	{
		// This sets layer to viewmodel for guns:
		if (boneParent.GetComponent<Renderer> ())
			boneParent.gameObject.layer = 12;

		if (boneParent.GetComponent<Collider>())
			boneParent.GetComponent<Collider>().enabled = false;

		if (boneParent.childCount > 0)
		{
			foreach (Transform child in boneParent)
			{
				setBoneLayersTransformRecursive(child);
			}
		}
	}

	// This is so stuff like guns don't render either: (It's a little inefficient, but it's only called once on spawn).
	// This is for the actual player model
	public void setGunsToNotRender(Transform boneParent)
	{
		if (boneParent.GetComponent<Renderer> () && !boneParent.GetComponent<ParticleSystemRenderer> ()) // Only the guns will have renderers, not the bones. (It is important that the bones remain as the hitbox layers)
			boneParent.gameObject.layer = 11;

		// If server wants to play as a player, they have to deal with issues regarding not being able to place down, seeing player name when looking down, etc.
		// This is because the server needs the hitbox of their own player to do calculations. HealingShotgun will hit themselves in this case as well.
		if (boneParent.GetComponent<Collider>() && !OperationNetwork.isServer)
			boneParent.GetComponent<Collider>().enabled = false;

		if (boneParent.childCount > 0)
		{
			foreach (Transform child in boneParent)
			{
				setGunsToNotRender(child);
			}
		}
	}
}
