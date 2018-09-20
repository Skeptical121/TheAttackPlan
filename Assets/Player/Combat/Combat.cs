using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Combat : MonoBehaviour
{






	public GameObject ragdoll;

	public GameObject damageIndicatorPrefab;

	// Set in prefab:
	public short maxHealth = 125; // Max health doesn't sync
	public byte team = 0;

	// Health is synced with a short: Rounded up. Thus, on Player Side, health will always be a short, essentially.
	public float health;

	string displayName = ""; // No string = no name displayed. This is when you look at a teammate.


	public Transform healthBar = null; // The "green" part (the health part). Only used by class 4 for friendlies currently.
	bool renderHealthBar = true;

	bool isOverHealEffectDisplayed = false;

	List<GameObject> damageIndicators = new List<GameObject>(); // The scale is set on spawn, so the damage doesn't need to be saved.
	List<Vector3> damageIndicatorsFrom = new List<Vector3>();
	List<float> damageIndicatorsTime = new List<float>();

	// Types of damage: Some types, (like projectile,) don't make any sound with Combat. If these damages cause death, they also determine the icon displayed. (TODO)

	public const byte SINGLE_BULLET = 0;
	public const byte SHOTGUN = 1; 
	public const byte MEELEE = 2;
	public const byte SAW = 3;
	public const byte SPIKES = 4;
	public const byte ARROW = 5;
	public const byte LASER = 6;
	public const byte HEALING = 7;

	// No sound, still pain sound:
	public const byte PAIN = 100;
	public const byte PROJECTILE = 101;
	public const byte BLOW_UP = 102;

	public const byte OTHER = 200; // Other does not make sound, nor show up in the kill feed. This can be anything from losing health from overheal

	// On death, the same death sound(s) are played every time. (todo)

	float timeSinceLastVoiceQueue = 5f;


	public GameObject[] tookDamageFromSounds; // Doesn't include the negative types.

	void Start()
	{
		health = maxHealth;
	}

	//OperationRPC
	public void VoiceLine(byte whichVoiceLine, byte volume, bool continueSend) // Basically set continueSend to true unless it's from the RPC.
	{
		// Some Voice Lines produce effects, this is determined here!
		// It is notable that visual / sound effects like these needn't be made on the server.
		if (OperationNetwork.isServer)
		{
			SoundHandler.soundHandler.PlayVoiceLine(whichVoiceLine, volume, transform);
			if (continueSend) {
				GetComponent<OperationView> ().RPC ("VoiceLine", OperationNetwork.ClientsOnly, whichVoiceLine, volume);
			} else {
				GetComponent<OperationView> ().RPC ("VoiceLine", -GetComponent<PlayerMove> ().plyr, whichVoiceLine, volume);
			}
		} else if (GetComponent<PlayerMove>().thisIsMine)
		{
			SoundHandler.soundHandler.PlayVoiceLine(whichVoiceLine, volume, GetComponent<PlayerMove>().mainCamera.transform);
			if (continueSend) {
				GetComponent<OperationView> ().RPC ("VoiceLine", OperationNetwork.ToServer, whichVoiceLine, volume);
			}
		} else
		{
			SoundHandler.soundHandler.PlayVoiceLine(whichVoiceLine, volume, transform); // The sounds are coming from the chests of the people currently.. could be a bit weird.
		}

	}

	// Standard way of causing damage:
	public void TakeDamage(float amount, float knockBackMult, Vector3 dir, bool isMine, byte damageType, bool isHitscan, Vector3 fromWhere, short playerSender)
	{
		if (OperationNetwork.isServer)
		{
			TakeDamageProper(amount, knockBackMult, dir, isMine, damageType, isHitscan, fromWhere, playerSender); // isHitscan should be false is this is a dedicated server.
		}
		else {
			// We still make it so a blood effect appears.. but that's it..
		}
	}

	//OperationRPC. fromWhere is NOT used in the rpc, it is only used when this is called manually; otherwise it's just set to Vector3.zero.
	public void TakeDamageProper(float amount, float knockBackMult, Vector3 dir, bool isMine, byte damageType, bool isHitscan, Vector3 fromWhere, short playerSender)
	{ // PlayerSender int is "info". fromWhere is used by server only to indicate the location this damage was sent from. (This will be sent to this player's owner)
		if (!OperationNetwork.isServer) {
			print("[--ERROR--]");
			// Must call this on the server!
			return;
		}

		// If the sender is not alive, the damage is not done.
		if (isHitscan && GameManager.PlayerExists(playerSender) && GameManager.GetPlayer(playerSender).playerObject == null)
			return;

		if (!GetComponent<PlayerMove> ()) {
			if (GameManager.PlayerExists (GetComponent<SyncFacade>().playerOwner) && GameManager.GetPlayer (GetComponent<SyncFacade>().playerOwner).playerObject != null) {
				// This is a "way" of doing things. An alternative way is let the facade be able to die, but this way it's.. hmm.. obviously subject to change.

				// Note that this assumes that armor is on & is phasing
				GameManager.GetPlayer (GetComponent<SyncFacade> ().playerOwner).playerObject.GetComponent<Combat> ().TakeDamageProper (amount * 2 * 2, knockBackMult, dir, isMine, damageType, isHitscan, fromWhere, playerSender);
			
				// Note that if/when we implement facade dieing, the PLAYER will still be teleported over to create the ragdoll
			}
			return;
		}

			// Phase Shift = not take damage. EVEN for hitscan. (phase is synonymous with "invincible") It is OKAY to do this check to prevent damage (?) Maybe not for hitscan though!!
		if (GetComponent<PlayerMove> ().isPhasing () && amount > 0) {
			amount *= 0.5f; // Half damage taken AND with armor on, that's almost no damage taken.
			knockBackMult = 0;
		}

		// 150% damage taken while armor is off
		// 50% damage taken while armor is on
		if (GetComponent<ClassControl> ().classNum == 0 && amount > 0)
			amount = amount * 1.5f - amount * 1f * GetComponent<PlayerMove> ().isArmorOn;

		// For stuff that does constant damage, it only triggers like 10 times per second, so using a short should be fine.

		health -= amount;

		// Play sound on server: (grunt sound)
		if (amount > 0 && damageType < 200) // Minimum damage.
		{
			VoiceLine(SoundHandler.GRUNT, (byte)(Mathf.Clamp(amount * 5, 2, 255)), true); // Minimum volume of 2/255. Note that ~50 damage is full volume.
		} else if (amount < -8 && damageType == HEALING)
		{
			VoiceLine(SoundHandler.BEEN_HEALED, 255, true); // Full sound for now.
		}
			

		if (dir != Vector3.zero && !isMine)
		{
			TakeDamageMove(amount, knockBackMult, dir);
		}

		// Damage went through. (Currently no checks are made for this, (isDead check) but is "todo")

		// Death / Health situation:
		if (health <= 0)
		{
			health = 0;
			Kill(playerSender, true);
		} else
		{
			// Just a health change. Sends this to everyone but the player.
			if (isHitscan)
			{
				// Comes from player:
				if (GameManager.PlayerExists(playerSender)) {
					fromWhere = GameManager.GetPlayer (playerSender).playerObject.transform.position;
				}
			}
		}

		if (damageType < 200) {
			GetComponent<SyncPlayer> ().playerTriggerSet.trigger (SyncPlayer.DAMAGE_TAKEN, new DamageNumber (fromWhere, amount));

			if (playerSender != GetComponent<PlayerMove> ().plyr) {
				// Damage went through callback is now implemented like this:
				if (GameManager.PlayerExists (playerSender) && GameManager.GetPlayer (playerSender).playerObject != null) {
					GameManager.GetPlayer (playerSender).playerObject.GetComponent<SyncPlayer> ().playerTriggerSet.trigger(SyncPlayer.DAMAGE_NUMBER, new DamageNumber(transform.position, amount));
				}
			}
		}
	}

	// For damageMove, this can be called manually on the server rather than from takeDamageProper.
	public void TakeDamageMove(float amount, float knockBackMult, Vector3 dir) // clientMovement used for rocket jumping so the event can be recorded.
	{
		short shortAmount = (short)Mathf.RoundToInt (amount);

		if (shortAmount == 0)
		{
			shortAmount = 1; // Default for 0 dmg.
		}
		// Also knockback:
		Vector3 effDir = dir * knockBackMult * shortAmount / 5.0f;//(Vector3.Normalize(transform.position - pos) * (maxDist - distance) / maxDist) * 10f * knockBackMult; // In 1 second
		UpdateMoveDirection(effDir);
		// This will essentially make it so a prediction error call is made to clients.

	}

	//OperationRPC
	public void UpdateMoveDirection(Vector3 change)
	{
		GetComponent<PlayerMove>().effectDirection += change;
	}

	public void OnGUI()
	{
		if (displayName != "")
		{
			GUI.Box(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 200, 200, 24), displayName);
		}
	}

	void Update()
	{

		bool renderHealthBar = false;

		if (!GetComponent<PlayerMove>() || !GetComponent<PlayerMove>().thisIsMine)
		{
			if (Player.thisPlayer != null && Player.thisPlayer.playerObject != null)
			{
				if (Player.thisPlayer.playerObject.GetComponent<ClassControl>().classNum == 4 && Player.thisPlayer.team == team)
				{
					renderHealthBar = true;
					if (!this.renderHealthBar)
					{
						this.renderHealthBar = true;
						healthBar.GetComponent<Renderer>().enabled = true;
						healthBar.parent.GetComponent<Renderer>().enabled = true;
					}
					// Display health bar:
					if (healthBar != null)
					{
						healthBar.localScale = new Vector3(1.01f * health / maxHealth, 1.01f, 1.01f);
						healthBar.localPosition = new Vector3(-0.505f + 0.505f * health / maxHealth, 0, -0.01f);
						healthBar.parent.rotation = Quaternion.LookRotation(healthBar.parent.position - Player.thisPlayer.playerCamera.transform.position);
					}
				}
			}
		}
		else {
			// Kill bind
			if (Input.GetKeyDown(KeyCode.K))
			{
				GetComponent<OperationView> ().RPC ("KillSelf", OperationNetwork.ToServer);
			}

			// SOUND QUEUES:
			timeSinceLastVoiceQueue += Time.deltaTime;

			if (timeSinceLastVoiceQueue > 0.7f) {
				// Call for medic:
				if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X))
				{
					VoiceLine((byte)Random.Range(0, 2), 255, true); // Full volume for now..
					timeSinceLastVoiceQueue = 0f; 
				}
			}
			

			while (damageIndicatorsTime.Count > 0 && Time.time - damageIndicatorsTime[0] > 2.0f)
			{
				Destroy(damageIndicators[0]);
				damageIndicators.RemoveAt(0);
				damageIndicatorsFrom.RemoveAt(0);
				damageIndicatorsTime.RemoveAt(0);
			}

			for (int i = 0; i < damageIndicators.Count; i++)
			{

				Vector3 loc = Vector3.ProjectOnPlane(GetComponent<PlayerMove>().mainCamera.transform.forward, damageIndicatorsFrom[i] - GetComponent<PlayerMove>().mainCamera.transform.position);
				Quaternion rotation = Quaternion.LookRotation(damageIndicatorsFrom[i] - GetComponent<PlayerMove>().mainCamera.transform.position) * Quaternion.Inverse(GetComponent<PlayerMove>().mainCamera.transform.rotation);

				float angleUsed = -rotation.eulerAngles.y + 90f;

				damageIndicators[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(0 + 1.6f * Mathf.Cos(angleUsed * Mathf.PI / 180f) * 200, 0 + Mathf.Sin(angleUsed * Mathf.PI / 180f) * 200);
				damageIndicators[i].GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, angleUsed);
				if (Time.time - damageIndicatorsTime[i] > 1f)
				{
					Color color = damageIndicators[i].GetComponent<RawImage>().color;
					damageIndicators[i].GetComponent<RawImage>().color = new Color(color.r, color.g, color.b, damageIndicatorsTime[i] - Time.time + 2.0f);
				}
			}

			// See player name & your trigger / trap.
			PlayerMove pMove = GetComponent<PlayerMove>();
			displayName = "";
			RaycastHit hit;
			float dist = 50;

			// It should be noted that it has to hit the hitboxes, not the player right now because otherwise it would just the raycast would always hit your own player.. (Easy fix is just to move the raycast forward)
			if (Physics.Raycast(pMove.mainCamera.transform.position, pMove.mainCamera.transform.forward, out hit, dist, LayerLogic.HitscanShootLayer()))
			{
				if (hit.transform.gameObject.layer == 16 + pMove.GetComponent<Combat>().team || hit.transform.gameObject.layer == 17 - pMove.GetComponent<Combat>().team) // Can see both allies and enemies right now.
				{
					// Finds the actual player object
					Transform plyr = hit.transform;
					do
					{
						plyr = plyr.parent;
					} while (plyr.GetComponent<Combat>() == null);

					if (plyr.GetComponent<PlayerMove> ()) { // facades don't have names for now..
						short playerOwner = plyr.GetComponent<PlayerMove> ().plyr;
						if (GameManager.PlayerExists (playerOwner)) {
							displayName = GameManager.GetPlayer (playerOwner).playerName;
						}
					}
				}

				if (hit.transform.gameObject.layer == 14) {
					if (hit.transform.GetComponent<ArrowLauncher>()) {
						if (hit.transform.GetComponent<ArrowLauncher>().playerOwner == pMove.plyr) // "myArrowTrap"
						{
							hit.transform.GetComponent<ArrowLauncher>().Highlight();
						}
					}
					if (hit.transform.GetComponent<ArrowTrigger>())
					{
						if (hit.transform.GetComponent<ArrowTrigger>().playerOwner == pMove.plyr) // "myArrowTrap"
						{
							hit.transform.GetComponent<ArrowTrigger>().Highlight();
						}
					}
				}
			}
		}

		if (OperationNetwork.isServer)
		{
			// Loss of overheal over time:
			if (health > maxHealth && (int)(Time.time) > (int)(Time.time - Time.deltaTime)) // This = every second.
			{
				TakeDamage(Mathf.Min(5, health - maxHealth), OTHER); // Every second, lose 5 of the buff.
			}

			// You get healed within your respawn room:
			if (GetComponent<PlayerMove>() && GetComponent<PlayerMove>().isInSpawnRoom && health < maxHealth && (int)(Time.time) > (int)(Time.time - Time.deltaTime))
			{
				TakeDamage(Mathf.Max(-50, health - maxHealth), HEALING); // 50 heal per second in spawn.
			}
		}

		if (!renderHealthBar && this.renderHealthBar)
		{
			this.renderHealthBar = false;
			if (healthBar != null)
			{
				healthBar.GetComponent<Renderer>().enabled = false;
				healthBar.parent.GetComponent<Renderer>().enabled = false;
			}
		}

		// Assign to its own game object:

		// Needs to be at least 10 buffed to get particle effect:
		if (health <= maxHealth + 10 && isOverHealEffectDisplayed)
		{
			isOverHealEffectDisplayed = false;
			transform.Find("OverHealParticles").gameObject.SetActive(false);
		} else if (health > maxHealth + 10 && !isOverHealEffectDisplayed)
		{
			isOverHealEffectDisplayed = true;
			transform.Find("OverHealParticles").gameObject.SetActive(true);
		}
	}

	// Helper method:
	// This should only be used for self damage.
	public void TakeDamage(float amount, byte damageType)
	{
		TakeDamage(amount, 0, Vector3.zero, false, damageType, false, Vector3.zero, OperationNetwork.ServerObject); // Since it is only used for self damage, hitscan can be true / false, it makes no difference.
		// --> This last variable is only used if this is being sent from the server; and if it is, that is indeed the playerSender, (the server)
	}

	// Overrides phase shift, must be called by server. Also called when damage is taken to death.
	// Server only!
	public void Kill(short playerSender, bool recordStat)
	{
		// We get this chance to teleport the player to the facade player:
		if (GetComponent<PlayerMove> ().facadeObject != null) {
			// This is for ragdoll:
			transform.position = GetComponent<PlayerMove> ().facadeObject.transform.position;
			transform.rotation = GetComponent<PlayerMove> ().facadeObject.transform.rotation;
			GetComponent<PlayerMove> ().facadeObject.GetComponent<SyncFacade> ().exists = false; // Destroy the facade
		}
		GetComponent<SyncPlayer> ().exists = false;
		if (GameManager.PlayerExists(GetComponent<PlayerMove>().plyr)) {
			GameManager.GetPlayer (GetComponent<PlayerMove>().plyr).killedByPlayer = playerSender;
		}
		if (recordStat)
			GameManager.PlayerStat(playerSender, GetComponent<PlayerMove>().plyr); // Kill feed..
	}

	public void addDamageIndicator(DamageNumber dn) {
		if (dn.damage > 0.01f) // Minimum damage
		{
			// NOTE THAT DAMAGE INDICATORS DON'T WORK ON SERVER!
			GameObject dmgIndicator = Instantiate(damageIndicatorPrefab);
			dmgIndicator.transform.SetParent(GameObject.Find("PlayerHud").transform); // hmm
			//dmgIndicator.GetComponent<RectTransform>().anchoredPosition = new Vector2((health - newHealth + 50) * 2, (health - newHealth + 50) * 2);
			dmgIndicator.GetComponent<RectTransform>().sizeDelta = new Vector2((dn.damage + 200) * 0.5f, (dn.damage + 7) * 3.5f);
			damageIndicators.Add(dmgIndicator);
			damageIndicatorsFrom.Add(dn.posData);
			damageIndicatorsTime.Add(Time.time);
		}
	}
		
	public void die(Vector3 deathVelocity)
	{
		for (int i = 0; i < transform.childCount; i++)
		{
			if (transform.GetChild(i).GetComponent<AudioSource>())
			{
				// Set parent to world
				Transform child = transform.GetChild(i);
				child.parent = null;
				i--;
			}
		}

		if (!OperationNetwork.isHeadless) { // Might as well create the ragdoll if you are the server; but obviously it is completely useless on linux headless
			// Spawn ragdoll:
			GameObject ragdollObject = (GameObject)Instantiate (ragdoll, transform.position, transform.rotation);
			// Death sound:
			SoundHandler.soundHandler.PlayDeathSound (GetComponent<ClassControl> ().classNum, ragdollObject.transform);

			if (GetComponent<PlayerMove> ().thisIsMine) {
				Destroy (GetComponent<PlayerMove> ().playerView.viewmodelPlayer);
				for (int i = 0; i < damageIndicators.Count; i++) {
					Destroy (damageIndicators [i]);
				}
				// There's no need to clear the damage indicators.
			}

			List<Quaternion> rotations = new List<Quaternion> ();
			List<Vector3> positions = new List<Vector3> ();

			Transform mB1 = transform.Find ("Armature"); //.FindChild("Pelvis");
			Transform mB2 = ragdollObject.transform.Find ("Armature"); //.FindChild("Pelvis");

			int index = 0;
			saveBoneTransformRecursive (ref index, mB1, ref rotations, ref positions);
			index = 0;

			Vector3 velocity = deathVelocity;
			if (GetComponent<PlayerMove> ().isGrounded) { // The isGrounded variable is synced with the server
				velocity = new Vector3 (velocity.x, 0, velocity.z);
			}
			setBoneTransformRecursive (ref index, mB2, ref rotations, ref positions, velocity);

			Destroy (ragdollObject, 60.0f);
		}
	}

	void saveBoneTransformRecursive(ref int index, Transform boneParent, ref List<Quaternion> rotations, ref List<Vector3> positions)
	{
		rotations.Add(boneParent.localRotation);
		positions.Add(boneParent.localPosition);
		if (boneParent.childCount > 0)
		{
			foreach (Transform child in boneParent)
			{
				if (child.gameObject.layer == 16 || child.gameObject.layer == 17)
				{
					index++;
					saveBoneTransformRecursive(ref index, child, ref rotations, ref positions);
				}
			}
		}
	}

	void setBoneTransformRecursive(ref int index, Transform boneParent, ref List<Quaternion> rotations, ref List<Vector3> positions, Vector3 velocity) // Just the one velocity for now
	{
		boneParent.localRotation = rotations[index];
		boneParent.localPosition = positions[index];
		if (boneParent.GetComponent<Rigidbody>())
		{
			boneParent.GetComponent<Rigidbody>().velocity = velocity;
		}
		if (boneParent.childCount > 0)
		{
			foreach (Transform child in boneParent)
			{
				index++;
				setBoneTransformRecursive(ref index, child, ref rotations, ref positions, velocity);
			}
		}
	}

	// Death. NOT respawn, respawn is a terrible name, its called that because it is what initiates the respawn sequence.
}