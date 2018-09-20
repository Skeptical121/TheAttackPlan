using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class PlayerMove : MonoBehaviour
{
	public GameObject mainCamera;

	// This is saved on server / player & is used to determine if a damage boost shoud be done. (It won't if it's null, of course)
	public float timeSinceTouchingDamageCircle = 3600f; // <- Arbitrary number that is over about 5 seconds.


	// This is also saved on server / player
	public bool isInSpawnRoom = false;
	public bool isInTrapSelectionZone = false; // This is only saved by player

	Vector3 lastNormal;

	// This is relevant for prediction errors:
	public PredictionErrorTest predictionErrorTest;

	public PlayerView playerView;

	public float inDirtMound = 0;

	// Even smoothing moved axis will be new..

	// The following are covered by lastPlayerRot & lastPlayerPos, and are used by class 4 to consider turn speed.

	public Vector3 lastPlayerPosition = Vector3.zero;
	public float playerSpeed = 0; // Used for class 4.

	public float lastTime = -1; // Set based on Time.time

	// The other equivalents for "current" are handled within the transform
	public float currentPlayerRotUpDown = 0; // CLIENT only
	public bool isGrounded = false;
	public bool isCrouched = false;

	public float isCrouchedVal = 0; // 0 -> 1

	public bool thisIsMine = false; // Same as before.
	public short plyr; // This is on both server & clients. THIS IS DEPRECATED - IssueDesktop

	// Looking:
	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseXAndY;

	// These are constants:
	public static float sensitivity = 4F;
	public const float minimumY = -87.5f;
	public const float maximumY = 87.5f;


	// Phase through: (movement ability)
	public GameObject hudElement;
	public GameObject hudElement2;

	public const float phaseTime = 10f; // Total distance = 15.6

	public float movementAbilityCoolDownStartedAt = -1000f; // This is also used to determine whether phase shifting / speed boosting is happening

	public bool puttingArmorOn; // Completely decided by server
	public float isArmorOn = 0;

	public bool ClientPhasingOrSpeedBoosting // This is used on more than just the client
	{
		set
		{
			if (clientPhasingOrSpeedBoosting != value) {
				if (GetComponent<ClassControl> ().classNum == 0) {
					SetPhaseThroughEffect (value, true);
				}
			}
			clientPhasingOrSpeedBoosting = value;
		}
		get
		{
			return clientPhasingOrSpeedBoosting;
		}
	}
	bool clientPhasingOrSpeedBoosting = false;



	// Physics:
	public bool jumped = false;
	public bool wasGrounded = false;

	public float speed = 5.0F;
	public float jumpSpeed;
	//bool useGravity = true;
	public Vector3 moveDirection = Vector3.zero;
	public Vector3 effectDirection = Vector3.zero;

	public WalkThroughWalls walkThroughWalls = null;


	public Vector3 GetMainCameraLocalPos()
	{
		return Vector3.Lerp(new Vector3(0, 1.27f, 0), new Vector3(0, 1.0f, 0), isCrouchedVal);
	}

	// Player sends movement data to Server. (All "commands" are sent by the original RPC calls)

	// Server replicates this movement & sends out this movement data to the players

	// This doesn't account for playerSpeed yet
	public float getSpeed() {
		if (GetComponent<ClassControl> ().classNum == 0) {
			if (isPhasing ()) {
				return speed * 1.75f; // 200% speed when in "armor" mode. 'robot' as I'd call it.
			} else {
				return speed * (1 - isArmorOn) + speed * 0.5f * isArmorOn; // 50% speed while armor is on, 150% speed while armor is off
			}
		} else {
			return speed;
		}
	}

	public float getCrouchSpeed() {
		return getSpeed () * 0.5f; // Significant slow down
	}
		
	// Sent to server so the server can replicate the commands:

	bool serverAcceptingInputDataForPlayerObject = false;

	public void movementData(short packetID, byte[] dataForPlayerInput)
	{
		if (!thisIsMine)
		{
			PlayerInput pI = new PlayerInput (dataForPlayerInput);

			// Hang on!!
			if (!serverAcceptingInputDataForPlayerObject) {
				if (pI.wasFirstPlayerInputForPlayerObjectOnServer && pI.objectID == GetComponent<SyncPlayer>().objectID) {
					serverAcceptingInputDataForPlayerObject = true; // Good to go
				} else {
					return; // Not good to go
				}
			}

			// SERVER EXECUTION
			playerAndServer(pI, true);

			GetComponent<ClassControl>().PlayerAndServer(pI, true);
		}
	}


	// Called by P & S
	void Jump(bool runEffects, Vector3 moveDir)
	{
		// This if statement is to make this method proper. The server relies on it, and it is simply an extra if statement for player side.
		if (GetComponent<ClassControl> ().classNum != 4) {
			moveDirection.y = 0; // hmm
			effectDirection.y = jumpSpeed + playerSpeed / 10;
		} else {
			movementAbilityCoolDownStartedAt = GetComponent<SyncPlayer>().playerTime;
			Vector3 dir;
			if (moveDir == Vector3.zero)
				dir = Vector3.Normalize (transform.TransformDirection(Vector3.forward));
			else
				dir = Vector3.Normalize (transform.TransformDirection (moveDir));
			Vector3 amount = (dir * 5f * speed + Vector3.up * 1f * jumpSpeed);
			moveDirection = new Vector3 (amount.x, 0, amount.z);
			effectDirection = new Vector3 (0, amount.y, 0);
		}
		JustJumped (runEffects); // mainly for animation
	}

	bool isLastNormalSlide()
	{
		return lastNormal.y > -0.64278760968 && lastNormal.y < -0.01;
	}

	void OnControllerColliderHit(ControllerColliderHit hit)
	{
		lastNormal = Vector3.Normalize(hit.point - transform.TransformPoint(-new Vector3(0, GetComponent<CharacterController>().height / 2.0f, 0) + new Vector3(0, GetComponent<CharacterController>().radius, 0)));
	}

	bool ccIsGrounded(CharacterController cc, Vector3 moveDir, Vector3 effectDir) {
		// We do NOT assume that the last position was on ground.. (?)
		if (effectDir.y < 0.8f) { // Hard transition

			// Welcome to extreme simulation cost!!
			Vector3 lastPosition = transform.position;
			cc.Move (Vector3.down * cc.height * transform.localScale.x * 0.03f);
			bool isGrounded = cc.isGrounded && !isLastNormalSlide(); // isLastNormalSlide must only be used if cc.isGrounded = true, which is the case here
			transform.position = lastPosition;
			return isGrounded;
		} else {
			return false;
		}
	}

	void friction(PlayerInput pI) {
		effectDirection = new Vector3(effectDirection.x * Mathf.Pow(0.15f, pI.frameTime), effectDirection.y, effectDirection.z * Mathf.Pow(0.15f, pI.frameTime));

		float dist = Vector3.Distance(Vector3.zero, effectDirection);
		float constFrict = 10.0f * pI.frameTime;
		if (dist > constFrict)
		{
			effectDirection = Vector3.Normalize(effectDirection) * (dist - constFrict);
		} else
		{
			effectDirection = Vector3.zero;
		}
	}

	void GroundReducePlayerSpeed(PlayerInput pI) {
		if (GetComponent<ClassControl> ().classNum == 3) {
			float mult = 0.15f; // Was 0.333f (July 11)
			float time = 0f; // Was 3f.. 1f still too high
			if (GetComponent<SyncPlayer>().playerTime - movementAbilityCoolDownStartedAt < time) {
				mult = 1 - (1 - mult) * (GetComponent<SyncPlayer>().playerTime - movementAbilityCoolDownStartedAt) / time;
			}
			playerSpeed *= Mathf.Pow (mult, pI.frameTime);
		}
	}

	float AirReducePlayerSpeed(PlayerInput pI, float deltaY, Vector3 oldLastPlayerPosition) {
		float airResistance = 0.13f;
		if (GetComponent<ClassControl> ().classNum == 3) {
			Vector3 lastVelocity = (transform.position - oldLastPlayerPosition) / pI.frameTime;
			if (deltaY > 0) {
				deltaY *= 0.8f; // Only decrease speed by 5/6 what is decreased when going UP in the air. (July 11, changed from 5/6 to 4/5)
			}
			float mult = 0.6f; // Was 0.5f (July 11)
			float time = 5f;
			if (GetComponent<SyncPlayer> ().playerTime - movementAbilityCoolDownStartedAt < time) {
				mult = 1 - (1 - mult) * (GetComponent<SyncPlayer> ().playerTime - movementAbilityCoolDownStartedAt) / time;
			}
			playerSpeed *= Mathf.Pow (mult, pI.frameTime);
			playerSpeed -= deltaY * pI.frameTime * 3f; // Was 1.25f
			if (playerSpeed < 0) {
				playerSpeed = 0;
			}
			airResistance = 0.06f; // Reducing this number INCREASES air resistance
		} else if (GetComponent<ClassControl> ().classNum == 4 && GetComponent<SyncPlayer> ().playerTime - movementAbilityCoolDownStartedAt < getLeapCoolDown()) {
			airResistance *= 0.3f; // Decreasing this INCREASES air resistance
		}
		return airResistance;
	}

	// EVERY FRAME
	void UpdateRotation(PlayerInput pI) {
		transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + pI.rotation, transform.eulerAngles.z);

		float upDownDir = mainCamera.transform.rotation.eulerAngles.x;
		if (upDownDir >= 180) {
			upDownDir -= 360;
		}
		upDownDir -= pI.rotationUpDown;
		upDownDir = Mathf.Clamp (upDownDir, minimumY, maximumY);

		mainCamera.transform.eulerAngles = new Vector3(upDownDir, transform.eulerAngles.y, 0); // Main Camera also gets set on server as empty.
	}
		
	void MitigateEffectDirection() {
		// This is when the ceiling is hit:
		if ((GetComponent<CharacterController>().collisionFlags & CollisionFlags.Above) != 0)
		{
			if (effectDirection.y > 0)
			{
				effectDirection.y = -effectDirection.y * 0.05f; // Almost 0... think portal.
			}
		}

		if ((GetComponent<CharacterController>().collisionFlags & CollisionFlags.Sides) != 0)
		{
			effectDirection = new Vector3(0, effectDirection.y, 0); // hmmm..
		}
	}
		
	public void UpdateCrouchedRep(float deltaTime)
	{
		// Crouched is done through interpolation:
		if (isCrouched) {
			// Takes "0.2s" to crouch
			isCrouchedVal += 5f * deltaTime;
			if (isCrouchedVal > 1)
				isCrouchedVal = 1;
		} else {
			// Takes "0.2s" to uncrouch
			isCrouchedVal -= 5f * deltaTime;
			if (isCrouchedVal < 0)
				isCrouchedVal = 0;
		}
		GetComponent<CharacterController>().height = 2.93f - 0.9f * isCrouchedVal;
		GetComponent<CapsuleCollider>().height = 2.93f - 0.9f * isCrouchedVal;
	}

	public void UpdateArmorRep(float deltaTime) {
		float oldArmorOn = isArmorOn;
		if (puttingArmorOn) {
			isArmorOn = Math.Min (isArmorOn + deltaTime, 1);
		} else {
			isArmorOn = Math.Max (isArmorOn - deltaTime, 0);
		}
		if (!OperationNetwork.isHeadless && oldArmorOn != isArmorOn) {
			GetComponent<PlayerAnimation>().SetArmorRender (isArmorOn);
		}
	}


	public GameObject facadeObject = null; // SERVER ONLY
	void SpawnFacade(GameObject facade) {
		facadeObject = (GameObject)MonoBehaviour.Instantiate (facade, transform.position, transform.rotation);
		facadeObject.GetComponent<SyncFacade> ().playerOwner = plyr; // This is only used server side on SyncFacade
		OperationNetwork.OperationAddSyncState (facadeObject);
	}

	void playerAndServerPreUpdate(PlayerInput pI, bool runEffects) {
		GetComponent<SyncPlayer> ().playerTime += pI.frameTime;

		if (OperationNetwork.isServer && facadeObject != null && !isPhasing ()) {
			transform.position = facadeObject.transform.position;
			transform.rotation = facadeObject.transform.rotation;
			facadeObject.GetComponent<SyncFacade> ().exists = false;
		}

		if (clientPhasingOrSpeedBoosting && !isPhasing()) {
			DisablePhaseThrough (runEffects);
		}

		// This updates the rotation of the object & mainCamera rotation
		if (GetComponent<ClassControl> ().isSwitching() || !(GetComponent<ClassControl> ().getUnlockEquipped () is GolfSwing)) {
			UpdateRotation (pI);
		}

		// This will get sent to server now.. hmm. Note that in the current system, this is only getting called with >600 fps. 
		if (pI.frameTime == 0) {
			Debug.LogError ("Missing input commands");
			return;
		}

		if (GetComponent<ClassControl> ().classNum == 0) {
			if (pI.unlockSwitchTo == OptionsMenu.MAIN_ABILITY) { // This command could get overwritten if done on the same tick as switching weapons!!!!!!!! TODO
				if (isArmorOn == 1 && !isPhasing()) {
					puttingArmorOn = false;
				} else if (isArmorOn == 0) {
					puttingArmorOn = true;
				}
			}
			UpdateArmorRep (pI.frameTime);
		}
			
		isCrouched = pI.crouchKey; // The act of crouching has no delay, you can crouch / decrouch as much as you want
		UpdateCrouchedRep (pI.frameTime); // Of course there is a delay to go from standing fully -> crouching & from crouching -> standing fully.
	}

	void poleVault() {
		// If pole vaulting, we need to snap you to a better position:
		Vector3 pos = GetComponent<ClassControl>().getUnlockEquippedWithType<PoleVault>().poleVaultPosition;

		// The current position, is of course, very relevant
		Quaternion rot = Quaternion.LookRotation(transform.position - pos);

		// Your position is rot * distance

		// N/A currently
	}

	void groundMovement(PlayerInput pI, bool runEffects, Vector3 moveDir, CharacterController controller) {
		float moveSpeed = Mathf.Lerp(getSpeed() + playerSpeed, getCrouchSpeed(), isCrouchedVal);

		GroundReducePlayerSpeed (pI);
		// Ground movement:

		// If, and ONLY if, we remain on a surface that will set ccIsGrounded to true, do we force the player down onto the ground
		moveDirection = transform.TransformDirection(moveDir * moveSpeed);
		// Note that moveDirection.y = 0

		if (pI.jumpKey && isCrouchedVal < 0.333f) { // && (GetComponent<ClassControl>().classNum != 4 || GetComponent<SyncPlayer> ().playerTime - movementAbilityCoolDownStartedAt >= getLeapCoolDown())) {
			Jump(runEffects, moveDir);
		}

		// Friction:
		friction(pI);

		controller.Move ((moveDirection + effectDirection) * pI.frameTime); // moveDirec
		MitigateEffectDirection (); // This is frame rate dependant..

		// This is a state that is "undefined" in its state of being grounded or not.
		// Moving down ramps is only allowed if you are already grounded is the logic here

		Vector3 lastPosition = transform.position;
		// Attempt Move Down:
		controller.Move(Vector3.down * moveSpeed * pI.frameTime); // This allows for the "45 degree" angle
		if (!controller.isGrounded) {
			transform.position = lastPosition;
		} else if (!jumped) { 
			// Reset effect direction:
			effectDirection.y = 0;
		}
	}

	void airMovement(PlayerInput pI, bool runEffects, Vector3 moveDir, CharacterController controller, Vector3 oldLastPlayerPosition, bool wasSliding) {
		// Air movement:
		float airResistance = AirReducePlayerSpeed (pI, effectDirection.y, oldLastPlayerPosition);

		if (wasSliding) {
			moveDirection = transform.TransformDirection(moveDir * (getSpeed() + playerSpeed));
			// Slide on normal:
			Vector3 slideDist = Vector3.RotateTowards (lastNormal, Vector3.up, -(Mathf.PI / 2) * 0.9f, 555555) * pI.frameTime;
			controller.Move (slideDist * 4); // Slide down ramps at a speed of 4.


			if (lastNormal.y > -0.170f && lastNormal.y < -0.090f) {
				slideDist = Vector3.RotateTowards (lastNormal, Vector3.up, -(Mathf.PI / 2) * 1.2f, 555555) * pI.frameTime;
				controller.Move (slideDist * 1);
				effectDirection += slideDist * Physics.gravity.y * 1 * Vector2.Distance (Vector2.zero, new Vector2 (lastNormal.x, lastNormal.z));
			}
		} else {

			// Doesn't matter if crouching in the air
			moveDir = moveDir * getSpeed() * 2.5f * pI.frameTime; // 250% of speed
			moveDirection += transform.TransformDirection(moveDir);

			// Was 0.275f
			effectDirection = new Vector3(effectDirection.x * Mathf.Pow(0.3f, pI.frameTime), effectDirection.y, effectDirection.z * Mathf.Pow(0.3f, pI.frameTime));

			moveDirection = new Vector3(moveDirection.x * Mathf.Pow(airResistance, pI.frameTime), moveDirection.y, moveDirection.z * Mathf.Pow(airResistance, pI.frameTime));

			controller.Move ((moveDirection + effectDirection) * pI.frameTime);
			effectDirection += Physics.gravity * pI.frameTime;
			MitigateEffectDirection (); // This is frame rate dependant..
		}

		// 0.6 seconds
		if (GetComponent<ClassControl> ().classNum == 4 && pI.jumpKey && GetComponent<SyncPlayer> ().playerTime - movementAbilityCoolDownStartedAt >= getLeapCoolDown()) {
			Jump(runEffects, moveDir);
		}
		if (GetComponent<ClassControl> ().classNum == 4 && GetComponent<SyncPlayer> ().playerTime - movementAbilityCoolDownStartedAt < getLeapCoolDown()) {
			effectDirection -= 0.5f * Physics.gravity * pI.frameTime; // Half gravity
		}

		// Becoming grounded:
		if (ccIsGrounded (controller, moveDirection, effectDirection)) {
			effectDirection.y = 0;
		}
	}

	void movementAbilityCheck(PlayerInput pI, bool runEffects) {
		if (pI.movementAbilityKey)
		{
			if (GetComponent<ClassControl> ().classNum == 0 && GetComponent<SyncPlayer> ().playerTime - movementAbilityCoolDownStartedAt >= getPhaseThroughCoolDown () && !isPhasing()) {
				if (canPhaseThrough ()) { // This method is interp'd by server trigger interp
					// Phase through:
					EnablePhaseThrough (runEffects); // This is really irrelivant, actually, as it'll be called next playerAndServer update anyways
					movementAbilityCoolDownStartedAt = GetComponent<SyncPlayer> ().playerTime;
					//moveDirection = transform.forward;

					if (OperationNetwork.isServer && GameManager.PlayerExists(plyr)) {
						SpawnFacade (GameManager.GetPlayer (plyr).facadePlayerObjects [GetComponent<Combat> ().team]);
					}

				}
			} else if (GetComponent<ClassControl> ().classNum == 3 && GetComponent<SyncPlayer> ().playerTime - movementAbilityCoolDownStartedAt >= getSpeedBoostCoolDown ()) {
				SpeedBoost ();
				// Speed boost might stick around, but for now, it's pole vaulting
				// We raycast infront of the player to find where the "pole" will be attached to:
			}
			// Otherwise this key is ignored.
		}
	}

	void hoverMoundCollision(PlayerInput pI, Collider hoverMoundCollider) {
		// Change effect direction accordingly: 
		if (effectDirection.y < -1) {

			effectDirection *= Mathf.Pow (0.02f, pI.frameTime);
		}

		// The model is as follows.
		// If you are not going down, you will hover.
		// If you are going down, you will sink into the hover mound
		float vec2Dist = Vector2.Distance (new Vector2 (transform.position.x, transform.position.z), new Vector2 (hoverMoundCollider.transform.position.x, hoverMoundCollider.transform.position.z));

		Vector3 dir = Vector3.Normalize(new Vector3(moveDirection.x + effectDirection.x, Mathf.Sqrt(Mathf.Abs(moveDirection.y + effectDirection.y)), moveDirection.z + effectDirection.z));

		float ySpeed = moveDirection.y + effectDirection.y;
		float MAX_SPEED = 10f;
		if (ySpeed > MAX_SPEED) {
			ySpeed = MAX_SPEED;
		} else if (ySpeed < -3f) {
			ySpeed = -3f;
		}
		ySpeed = Mathf.Abs (ySpeed);

		// This is good
		effectDirection += pI.frameTime * (Vector3.up * 7f * (12f - vec2Dist) * Mathf.Pow(inDirtMound, 0.5f) * (MAX_SPEED - ySpeed) * (MAX_SPEED - ySpeed) / MAX_SPEED / MAX_SPEED + dir * 12f);
	}

	void areaChecks(PlayerInput pI) {
		Vector3 halfHeight = new Vector3(0, GetComponent<CharacterController>().height / 2 * transform.localScale.x, 0);
		Vector3 center = transform.position + GetComponent<CharacterController>().center;

		Vector3 oldCenter = lastPlayerPosition + GetComponent<CharacterController>().center;


		Collider[] colliders = Physics.OverlapBox(center, halfHeight + new Vector3(GetComponent<CharacterController>().radius * transform.localScale.x, 0, GetComponent<CharacterController>().radius * transform.localScale.x), Quaternion.identity, 1 << 9 | 1 << 24 - GetComponent<Combat>().team);
		// All so it includes where the collider started at. Health packs should probably be picked up like this, but we'll see

		timeSinceTouchingDamageCircle += pI.frameTime;
		isInSpawnRoom = false;
		isInTrapSelectionZone = false;

		// Dirt mound / Force field hit detection: (And spawn room detection)
		bool collided = false;
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders [i].gameObject.GetComponent<HoverMoundCollision> ()) {
				hoverMoundCollision (pI, colliders[i]);
				collided = true;

				// NOT EFFECTED BY enemy team's damage circles: (Thus, it is impossible to get through them w/o phase shift, icicle jump, rocket jump, speed boost.)
			} else if ((colliders [i].gameObject.CompareTag ("DamageBoxBlue") && GetComponent<Combat> ().team == 0) || (colliders [i].gameObject.CompareTag ("DamageBoxRed") && GetComponent<Combat> ().team == 1)) {
				if (DamageCircle.isTouchingDamageCircle (timeSinceTouchingDamageCircle) == 0)
					timeSinceTouchingDamageCircle = Mathf.Max (0, 1.5f - pI.frameTime * 2);
				else
					timeSinceTouchingDamageCircle = Mathf.Max (timeSinceTouchingDamageCircle - pI.frameTime * 2);
			} else if (colliders [i].gameObject.layer == 24 - GetComponent<Combat> ().team && colliders [i].isTrigger && colliders [i].transform.parent.gameObject.layer != 14) { // Might as well check if isTrigger. Checks opposite team notabely.
				isInSpawnRoom = true;
			} else if (colliders [i].gameObject.CompareTag ("SelectTrapZone") && GetComponent<Combat> ().team == 1) {
				isInTrapSelectionZone = true;
			}
		}

		if (OperationNetwork.isServer) {
			RaycastHit[] raycastHit = Physics.CapsuleCastAll(oldCenter - halfHeight, oldCenter + halfHeight, GetComponent<CharacterController>().radius * transform.localScale.x, Vector3.Normalize(center - oldCenter), Vector3.Distance(center, oldCenter), 1 << 9 | 1 << 24 - GetComponent<Combat>().team | 1 << 21 - GetComponent<Combat>().team);
			foreach (RaycastHit rch in raycastHit) {
				if (rch.collider.gameObject.CompareTag ("RocketBullet")) {
					if (rch.collider.gameObject.GetComponent<Bullet> ().exists) {
						rch.collider.gameObject.GetComponent<Bullet> ().BlowUp (transform); // This works because directHit accounts for it, despite the fact that technically this parent object isn't part of the hitbox.
					}
				}
			}
		}

		// Dirt mound history:
		if (collided)
			inDirtMound += pI.frameTime;
		else
			inDirtMound = 0;
	}

	public void playerAndServer(PlayerInput pI, bool runEffects)
	{

		playerAndServerPreUpdate (pI, runEffects);

		// Must go forward, can't rotate

		// P & S. Server gets given initialDirection as it should run the same method.
		if (GetComponent<ClassControl>().classNum == 3 && GetComponent<ClassControl>().getUnlockEquippedWithType<PoleVault>().isPoleVaulting) {
			poleVault ();
		} else {
			
			// Standard movement code:
			CharacterController controller = GetComponent<CharacterController> (); // P & S

			Vector3 moveDir = new Vector3 (pI.dX, 0, pI.dZ).normalized;

			if (walkThroughWalls != null && isPhasing() && walkThroughWalls.RunPlayerAndServer (pI, moveDir, transform)) // Run walkThroughWalls if revelant
				return; // I assume the rest of the code is not relevant

			Vector3 oldLastPlayerPosition = lastPlayerPosition;

			lastPlayerPosition = transform.position; // The character is moved AFTER this method, so this works.
			wasGrounded = ccIsGrounded(controller, moveDirection, effectDirection); // moveDirection and effectDirection are the same from last frame
			isGrounded = wasGrounded; // For animation purposes
			bool wasSliding = false;
			if (wasGrounded)
				wasSliding = isLastNormalSlide ();

			bool didJump = jumped;
			jumped = false;

			if (wasGrounded && !didJump) {
				groundMovement (pI, runEffects, moveDir, controller);
			} else {
				airMovement (pI, runEffects, moveDir, controller, oldLastPlayerPosition, wasSliding);
			}
		}

		// Phasing:
		movementAbilityCheck(pI, runEffects);

		// Hit detection for earth mound / capture points
		areaChecks(pI);
	}

	void Update() {
		if (OperationNetwork.isServer && !GetComponent<PlayerMove>().thisIsMine) {
			PlayerMoveRun ();
		}
	}

	bool hasRunFirstCommand = false; // PLAYER side

	// This should happen after interp probably. 
	public void PlayerMoveRun()
	{
		bool isBot = GetComponent<ClassControl> ().isBot;
		if (((thisIsMine && (GetComponent<SyncPlayer> ().didFirstInterp || OperationNetwork.isServer)) || (OperationNetwork.isServer && isBot))) {

			if (!isBot) {
				playerView.playerOnly (this);
			}


			// Input

			// This is based on the archaic system:
			float deltaRotX = 0;
			float deltaRotY = 0;

			if (!isBot) {
				if (OptionsMenu.IsLockState ()) {
					deltaRotX = Input.GetAxis ("Mouse X") * sensitivity;
					deltaRotY = Input.GetAxis ("Mouse Y") * sensitivity;
				}
			}

			if (lastTime == -1) {
				lastTime = Time.time - Time.deltaTime;
			}

			PlayerInput currentPlayerInput = new PlayerInput (lastTime, deltaRotX, deltaRotY, GetComponent<ClassControl> (), isBot);
			lastTime += currentPlayerInput.frameTime;

			if (isBot) {
				GetComponent<AI> ().runAI (currentPlayerInput);
			}
			// rotation and upDownDir will be updated accordingly in "playerAndServer" to be used in the next frame. These obviously have to be saved for prediction errors.

			// Standardized:

			// PLAYER EXECUTION
			playerAndServer (currentPlayerInput, true); // This doesn't really get called if pI.frameTime = 0, it just updates rotation

			// This literally skips the frame as far as everything goes if frameTime = 0. This'll basically never happen if you have < 1000fps.
			if (currentPlayerInput.frameTime != 0) {

				GetComponent<ClassControl> ().PlayerAndServer (currentPlayerInput, true); // Note this is NOT run for prediction errors.

				// Sends the exact same to the server:
				if (!OperationNetwork.isServer) { // Bots are always on server
					// Saves data here too
					predictionErrorTest.savePlayerData (currentPlayerInput, this);

					RunGame.myClient.addInputCommand (currentPlayerInput.generateByteData (hasRunFirstCommand, GetComponent<SyncPlayer> ().objectID));
					hasRunFirstCommand = true;
					currentPlayerInput.playerInputGroupID = RunGame.myClient.playerInputGroupID; // For saving purposes
				}
			}

			if (isBot) {
				GetComponent<AI> ().afterPlayerInputRun ();
			}
		}

	}

	// Helper method = jump()
	void JustJumped(bool runEffects) {
		jumped = true;
		if (runEffects) {
			GetComponent<PlayerAnimation> ().JustJumped ();
		}
	}

	void grounded(bool grounded) {
		wasGrounded = grounded;
	}

	float getLeapCoolDown() {
		return 1.4f; 
	}

	float getPhaseThroughCoolDown() {
		return 20.0f;
	}

	float getSpeedBoostCoolDown() {
		return 10.0f; // was 7, then was 8. Should be 10 now, is 4 for testing.
	}

	// This is what decides if the "speed boost" effect is displayed:
	public bool isSpeedBoosting() {
		if (OperationNetwork.isServer || thisIsMine) {
			return playerSpeed >= getSpeed() / 2;
		} else {
			return clientPhasingOrSpeedBoosting && GetComponent<ClassControl>().classNum == 3;
		}
	}

	public bool isPhasing() {
		if (OperationNetwork.isServer || thisIsMine) {
			return GetComponent<SyncPlayer>().playerTime - movementAbilityCoolDownStartedAt < phaseTime && GetComponent<ClassControl>().classNum == 0;
		} else {
			return clientPhasingOrSpeedBoosting && GetComponent<ClassControl>().classNum == 0;
		}
	}

	public void setCharge(int classNum)
	{
		float percentCoolDown;
		if (classNum == 0) {
			percentCoolDown = Mathf.Clamp01((GetComponent<SyncPlayer>().playerTime - movementAbilityCoolDownStartedAt) / getPhaseThroughCoolDown ());
		} else {
			percentCoolDown = Mathf.Clamp01((GetComponent<SyncPlayer>().playerTime - movementAbilityCoolDownStartedAt) / getSpeedBoostCoolDown ());
		}

		hudElement.transform.Find ("Charge").GetComponent<Image> ().fillAmount = percentCoolDown;
		hudElement.GetComponent<Image> ().fillAmount = 1 - percentCoolDown;

		if (percentCoolDown == 1 && (classNum != 0 || canPhaseThrough())) {
			hudElement.transform.Find("Charge").GetComponent<Image> ().color = new Color (0, 1f, 0);
		} else {
			hudElement.transform.Find("Charge").GetComponent<Image> ().color = new Color (1f, 1f, 1f);
		}
	}

	void SpeedBoost()
	{
		playerSpeed += 24; // Was 16; // Was 8, then was 6. Now should be among 12
		movementAbilityCoolDownStartedAt = GetComponent<SyncPlayer>().playerTime;
	}
		

	void EnablePhaseThrough(bool runEffects) {
		if (GetComponent<CharacterController> ().enabled) {
			SetPhaseThroughEffect (true, runEffects);
		} else if (runEffects) {
			ClientPhasingOrSpeedBoosting = true; // Note how this is NOT on client
		}
	}

	void DisablePhaseThrough(bool runEffects) {
		if (!GetComponent<CharacterController> ().enabled) {
			SetPhaseThroughEffect (false, runEffects);
		} else if (runEffects) {
			ClientPhasingOrSpeedBoosting = false; // Note how this is NOT on client
		}
	}

	void SetPhaseThroughEffect(bool phasing, bool runEffects) {
		//GetComponent<CharacterController>().enabled = !phasing;
		//GetComponent<CapsuleCollider>().isTrigger = phasing;
		if (runEffects) {
			transform.Find ("PhaseThroughIndicator").gameObject.SetActive (phasing); // .GetComponent<MeshRenderer>().enabled = phasing;
			transform.Find ("PersonModelObject").gameObject.SetActive (!phasing);
			if (thisIsMine) {
				playerView.viewmodelPlayer.transform.Find ("PhaseThroughIndicator").gameObject.SetActive (phasing);
				playerView.viewmodelPlayer.transform.Find ("PersonModelObject").gameObject.SetActive (!phasing);
			}
		}
		// Just in case: (Mainly for player prediction error case)
		clientPhasingOrSpeedBoosting = phasing; // hmm..
	}

	// This should be changed to a capsule sweep (Physics.CapsuleCast) remember 0.499; can use earth mound collision considerations in playerAndServer as well.
	// Also phases through
	public bool canPhaseThrough()
	{

		// First, we make a check that armor is on:
		if (isArmorOn != 1)
			return false;

		return true;
	}
}