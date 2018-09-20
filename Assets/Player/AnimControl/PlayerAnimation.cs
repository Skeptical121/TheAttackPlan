using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class PlayerAnimation : MonoBehaviour
{
	int jumpHash = Animator.StringToHash("JustJumped");

	public Animator anim = null;

	Animator viewmodelAnim = null;
	PlayerMove playerMove = null;

	Vector3 lastPosition;
	Vector3 deltaPosition;

	int fixedUpdateTicks = 0;
	bool isWalking = false;
	bool isJumping = false;
	float startedPlayingAt = 0;


	// On server, the rotations of colliders are saved:
	float[] playerTickTimes = new float[30];
	List<Vector3>[] playerBodyHitBoxPositions = new List<Vector3>[30]; // Can only go back up to 600ms. It runs interp between the positions given for each animation
	List<Quaternion>[] playerBodyHitBoxRotations = new List<Quaternion>[30]; // Can only go back up to 600ms. It runs interp between the positions given for each animation


	List<Vector3> toRevertToPlayerBodyHitBoxPositions = new List<Vector3>();
	List<Quaternion> toRevertToPlayerBodyHitBoxRotations = new List<Quaternion>();

	Material[] armorMatsScientist = new Material[2];

	// Just rotations, as those are the only ones that change

	public void InitStartAnimation() // This is actually called by ClassControl because ClassControl needs it for it's PreStart,
	{
		anim = GetComponent<Animator>();

		//anim.speed = 0;

		playerMove = GetComponent<PlayerMove>();

		lastPosition = transform.position;

		if (GetComponent<ClassControl> ().classNum == 0) {
			if (GetComponent<Combat> ().team == 0) {
				armorMatsScientist [0] = (Material)Resources.Load ("BlueMetal");
				armorMatsScientist [1] = (Material)Resources.Load ("BlueMetalTransparent");
			} else {
				armorMatsScientist [0] = (Material)Resources.Load ("RedMetal");
				armorMatsScientist [1] = (Material)Resources.Load ("RedMetalTransparent");
			}
		}

		// setLayerWeight is done by class1 because at this point classNum is not known.
	}

	public void AfterFirstInterpAnimation() {
		if (playerMove.thisIsMine)
		{
			viewmodelAnim = playerMove.playerView.viewmodelPlayer.GetComponent<Animator>();
		}
	}

	public void goBackToTick(float tick) {
		toRevertToPlayerBodyHitBoxPositions.Clear ();
		toRevertToPlayerBodyHitBoxRotations.Clear ();
		// Save:
		savePositionsRecursively(transform.Find ("Armature"), ref toRevertToPlayerBodyHitBoxPositions, ref toRevertToPlayerBodyHitBoxRotations);

		for (int i = 0; i < playerTickTimes.Length - 1; i++) {
			if (tick <= playerTickTimes [i] && tick > playerTickTimes [i + 1]) {
				if (playerBodyHitBoxPositions [i] != null && playerBodyHitBoxPositions [i + 1] != null) {
					loadTick (i + 1, i, (tick - playerTickTimes [i + 1]) / 1); // It's the server; so each tick is 1 tick..
					return;
				} else if (playerBodyHitBoxPositions [i] != null) {
					loadTick (i, i, 0);
					return;
				}
				break;
			}
		}
		// else: go back as far as possible:
		for (int i = playerTickTimes.Length - 1; i >= 0; i--) {
			if (playerBodyHitBoxPositions [i] != null) {
				loadTick (i, i, 0);
				return;
			}
		}
		Debug.LogError ("Couldn't load tick! PlayerAnimation -> goBackToTick"); // Probably because player has really high ping
	}

	public void loadTick(int a, int b, float percent) {
		List<Vector3> positions = new List<Vector3> ();
		List<Quaternion> rotations = new List<Quaternion> ();
		for (int i = 0; i < playerBodyHitBoxPositions[a].Count; i++) {
			positions.Add(Vector3.Lerp (playerBodyHitBoxPositions [a] [i], playerBodyHitBoxPositions [b] [i], percent));
			rotations.Add(Quaternion.Lerp (playerBodyHitBoxRotations [a] [i], playerBodyHitBoxRotations [b] [i], percent));
		}
		int index = 0;
		setPositionsRecursively (ref index, transform.Find ("Armature"), ref positions, ref rotations);
	}

	public void revert() {
		int index = 0;
		setPositionsRecursively (ref index, transform.Find ("Armature"), ref toRevertToPlayerBodyHitBoxPositions, ref toRevertToPlayerBodyHitBoxRotations);
	}

	public void savePositionsRecursively(Transform boneParent, ref List<Vector3> positions, ref List<Quaternion> rotations) {
		rotations.Add(boneParent.rotation);
		positions.Add(boneParent.position);
		if (boneParent.childCount > 0)
		{
			foreach (Transform child in boneParent)
			{
				if (child.gameObject.layer == 16 || child.gameObject.layer == 17)
				{
					savePositionsRecursively(child, ref positions, ref rotations);
				}
			}
		}
	}

	public void setPositionsRecursively(ref int index, Transform boneParent, ref List<Vector3> positions, ref List<Quaternion> rotations) {
		boneParent.rotation = rotations[index];
		boneParent.position = positions[index];
		if (boneParent.childCount > 0)
		{
			foreach (Transform child in boneParent)
			{
				if (child.gameObject.layer == 16 || child.gameObject.layer == 17)
				{
					index++;
					setPositionsRecursively(ref index, child, ref positions, ref rotations);
				}
			}
		}
	}

	public Color getTrapezoidColor(float armor) {
		Color teamColor;
		if (GetComponent<Combat> ().team == 0)
			teamColor = new Color (0.3f, 0.5f, 0.7f);
		else
			teamColor = new Color (0.7f, 0.5f, 0.3f);

		return teamColor * Mathf.LinearToGammaSpace (armor * 1.3f);
	}

	public void SetArmorRender(float armor) {
		foreach (Transform child in transform) {
			if (child.name.Contains ("Armor") && !child.name.Contains("Helmet")) { // Helmet stays on
				
				child.gameObject.GetComponent<Renderer>().enabled = armor != 0;
				if (armor == 1)
					child.gameObject.GetComponent<Renderer> ().material = armorMatsScientist[0];
				else
					child.gameObject.GetComponent<Renderer> ().material = armorMatsScientist[1];

				float[] mult = { 1f, 0.25f };
				for (int i = 0; i < 2; i++) {
					Color c = child.gameObject.GetComponent<Renderer> ().materials [i].color;
					c.a = armor * mult[i];
					child.gameObject.GetComponent<Renderer> ().materials [i].color = c;
				}

				Material mat = child.gameObject.GetComponent<Renderer>().materials[1];
				mat.SetColor ("_EmissionColor", getTrapezoidColor(armor));
			}
		}
		if (GetComponent<PlayerMove> ().thisIsMine) {
			foreach (Transform child in GetComponent<PlayerMove> ().playerView.viewmodelPlayer.transform) {
				if (child.name.Contains ("Armor") && !child.name.Contains("Helmet")) { // Helmet stays on
					child.gameObject.GetComponent<Renderer>().enabled = armor != 0;
					if (armor == 1)
						child.gameObject.GetComponent<Renderer> ().material = armorMatsScientist[0];
					else
						child.gameObject.GetComponent<Renderer> ().material = armorMatsScientist[1];

					float[] mult = { 1f, 0.25f };
					for (int i = 0; i < 2; i++) {
						Color c = child.gameObject.GetComponent<Renderer> ().materials [i].color;
						c.a = armor * mult[i];
						child.gameObject.GetComponent<Renderer> ().materials [i].color = c;
					}

					Material mat = child.gameObject.GetComponent<Renderer>().materials[1];
					mat.SetColor ("_EmissionColor", getTrapezoidColor(armor));
				}
			}
		}
	}

	public void JustJumped()
	{
		isJumping = true;
		anim.SetTrigger(jumpHash);
		if (viewmodelAnim != null && viewmodelAnim.gameObject.activeInHierarchy) {
			viewmodelAnim.SetTrigger (jumpHash);
		}
	}

	void setLayerWeights()
	{
		float yDir;
		if (playerMove.thisIsMine || OperationNetwork.isServer)
		{
			yDir = playerMove.mainCamera.transform.eulerAngles.x;
		}
		else {
			yDir = -playerMove.currentPlayerRotUpDown;
		}
		if (yDir >= 180)
		{
			yDir -= 360;
		}
		yDir /= -90f;
		if (yDir >= 0)
		{
			anim.SetLayerWeight(2, yDir);
			anim.SetLayerWeight(3, 0);
		}
		else
		{
			anim.SetLayerWeight(2, 0);
			anim.SetLayerWeight(3, -yDir);
		}
		anim.SetLayerWeight(1, playerMove.isCrouchedVal);
		if (viewmodelAnim != null && viewmodelAnim.gameObject.activeInHierarchy) {
			viewmodelAnim.SetLayerWeight (1, playerMove.isCrouchedVal);
		}
	}

	// Set animation layer weights for [Right Arm]
	public void setArmLayerWeights(int classNum)
	{
		for (int i = 4; i <= 7; i++)
		{
			anim.SetLayerWeight(i, 0);
		}
		anim.SetLayerWeight(4 + classNum, 1);

		if (viewmodelAnim != null && viewmodelAnim.gameObject.activeInHierarchy)
		{
			for (int i = 4; i <= 7; i++)
			{
				viewmodelAnim.SetLayerWeight(i, 0);
			}
			viewmodelAnim.SetLayerWeight(4 + classNum, 1);
		}
	}

	void FixedUpdate()
	{
		// Because this needs to update at a fairly consistent rate:
		fixedUpdateTicks++;

		if (fixedUpdateTicks == 3)
		{
			fixedUpdateTicks = 0;
			deltaPosition = transform.position - lastPosition;
			lastPosition = transform.position; // The interp is taken care of by PlayerMove
		}
	}

	public void animTrigger(string triggerName)
	{
		if (anim != null) {
			anim.SetTrigger (triggerName);
		} else {
			GetComponent<Animator>().SetTrigger (triggerName);
		}
		if (viewmodelAnim != null && viewmodelAnim.gameObject.activeInHierarchy)
		{
			viewmodelAnim.SetTrigger(triggerName);
		}
	}

	public void savePlayerHitBoxPositionsOnServer() {
		Interp.shiftBuffer (playerTickTimes);
		Interp.shiftBuffer (playerBodyHitBoxPositions);
		Interp.shiftBuffer (playerBodyHitBoxRotations);
		playerTickTimes[0] = ServerState.tickNumber;
		playerBodyHitBoxPositions [0] = new List<Vector3> ();
		playerBodyHitBoxRotations [0] = new List<Quaternion> ();
		savePositionsRecursively (transform.Find ("Armature"), ref playerBodyHitBoxPositions [0], ref playerBodyHitBoxRotations [0]);
	}

	void Update()
	{
		AnimationUpdate ();
	}

	void AnimationUpdate() {
		if (playerMove.isGrounded && !playerMove.jumped)
		{
			isJumping = false;
		}

		anim.SetBool("isGrounded", playerMove.isGrounded);
		if (viewmodelAnim != null && viewmodelAnim.gameObject.activeInHierarchy) {
			viewmodelAnim.SetBool ("isGrounded", playerMove.isGrounded);
		}

		Vector3 forwardDelta = transform.InverseTransformDirection(deltaPosition); // Use proper rotation
		float forward = forwardDelta.z / Time.fixedDeltaTime / playerMove.getSpeed();
		float side = forwardDelta.x / Time.fixedDeltaTime / playerMove.getSpeed();
		float up = forwardDelta.y / Time.fixedDeltaTime / playerMove.getSpeed();

		
		// Rule of (todo) 0.2f / 0.3f
		if (forward < -0.3f && side < 0.3f && side > -0.3f)
		{
			side = 1;
		}

		setLayerWeights();
		bool walking = false;
		if (playerMove.isPhasing())
		{
			anim.SetFloat("VelocityForward", 0);
			anim.SetFloat("VelocityRight", 0);
		}
		else {

			anim.SetFloat("VelocityForward", forward);
			anim.SetFloat("VelocityRight", side);
			if ((Mathf.Abs(forward) > 0.3 || Mathf.Abs(side) > 0.3) && playerMove.isGrounded)
			{
				walking = true;
			}
		}
		if (walking && !isWalking)
		{
			isWalking = true;
			startedPlayingAt = Time.time;
			GetComponent<AudioSource>().Play();
		} else if (!walking && isWalking && Time.time - startedPlayingAt > 0.1f)
		{
			isWalking = false;
			GetComponent<AudioSource>().Stop();
		}

		

	}
}