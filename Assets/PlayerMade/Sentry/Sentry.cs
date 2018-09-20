using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Sentry : Trap
{

	int firedAt = -1;

	float pos = 0; // From -1 to 1

	bool goingLeft = false;

	// CONSTANTS:
	const float angleMax = 1.38f;//1.66f;
	const float distFromMid = 1f; // "radius"
	const float offSet = -0.3333f;
	const float maxYAngle = 0.55f;


	const float sentryFireHeight = 0.82f;
	const float sentryFireInfront = 0.76f;

	// Set in InitStart:
	Vector3 init; // For AI only
	Transform sentryObj;

	const float shootInterval = 0.1f; // Automatic gun

	// Targeting:
	public GameObject target = null;
	public float canRetarget = 0f;

	// Muzzle Flash
	public int lastMFlash = 0;
	public GameObject[] mFlashes;
	public Light lightObject;
	public bool muzzleEnabled = false;

	public GameObject soundEffect;

	Vector3[] lastHitscanData = new Vector3[0];

	// Up down Angle is only saved on the server because it does not effect visuals.

	public float upDownAngle = 0f;

	float damage = 3.5f;
	float maxDistance = 15f;

	bool disabledConstructionRenderer = false;

	public override short getMaxHealth () {
		return 400;
	}

	public override float getLifeTime () {
		return 60f;
	}

	public override void setHealthBar() {
		// This should call after building animation, that goes for a lot of traps..
		PlayerHud.addHealthBar(sentryObj.transform.Find("HealthBar"), this, team);
	}

	// Use this for initialization
	public override void InitStart(bool isThisMine) {
		sentryObj = transform.Find("SentryMain");

		sentryObj.GetComponent<Renderer> ().enabled = false;
		sentryObj.GetComponent<Collider> ().enabled = false;

		base.InitStart (isThisMine);


		init = transform.position + transform.TransformDirection(new Vector3(0, 0, 1) * offSet); // For AI only
	}

	void updateSentryTransform()
	{
		if (!disabledConstructionRenderer && (OperationNetwork.isServer && getLifeTimeServer() > 5f) || (!OperationNetwork.isServer && getLifeTimeInterp() > 5f)) {
			disabledConstructionRenderer = true;
			transform.Find ("Construction").GetComponent<Renderer> ().enabled = false;
		}

		float angle = pos * angleMax;
		Vector3 newPos = new Vector3(0, 0, offSet) + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * distFromMid;
		sentryObj.position = transform.TransformPoint(newPos);
		

		Vector3 newRot = new Vector3(0, angle * 180f / Mathf.PI, 0);
		sentryObj.localRotation = Quaternion.Euler(newRot);
	}

	public override float getBuildTime() {
		return 3f;
	}

	public override void endBuildAnimation ()
	{
		sentryObj.GetComponent<Renderer> ().enabled = true;
		sentryObj.GetComponent<Collider> ().enabled = true;
	}

	void updateSentry(float deltaTime)
	{


		// Server only for now.
		if (!OperationNetwork.isServer) {
			Debug.LogError ("Server only for now! Sentry -> updateSentry");
		}

		if (getLifeTimeServer() > getBuildTime()) {

			if (!ai ()) {
				if (goingLeft) {
					pos += 0.6f * deltaTime;
					if (pos > 1) {
						pos = 2 - pos;
						goingLeft = false;
					}
				} else {
					pos -= 0.6f * deltaTime;
					if (pos < -1) {
						pos = -2 - pos;
						goingLeft = true;
					}
				}
			}
		}
		interpMuzzle ();
	}

	public void FireTriggered()
	{
		if (OperationNetwork.isServer) {
			firedAt = ServerState.tickNumber;
		} else {
			firedAt = Interp.getTickNumber ();
			createBullet ();
		}

	}

	void interpMuzzle() {
		float timeSinceFire;
		if (OperationNetwork.isServer) {
			timeSinceFire = ServerState.getLifeTime (firedAt);
		} else {
			timeSinceFire = Interp.getLifeTime (firedAt);
		}
		if (timeSinceFire < 0.025f) {
			enableMuzzle ();
		} else {
			disableMuzzle ();
		}
	}

	void enableMuzzle()
	{
		if (!muzzleEnabled) {
			lastMFlash = (lastMFlash + 1) % mFlashes.Length;
			mFlashes [lastMFlash].GetComponent<Renderer> ().enabled = true;
			lightObject.enabled = true;
			muzzleEnabled = true;
		}
	}

	void disableMuzzle()
	{
		if (muzzleEnabled) {
			mFlashes [lastMFlash].GetComponent<Renderer> ().enabled = false;
			lightObject.enabled = false;
			muzzleEnabled = false;
		}
	}




	void shoot()
	{


		Vector3 fireFromPos = sentryObj.position + sentryObj.TransformDirection(new Vector3(0, sentryFireHeight, sentryFireInfront)); //new Vector3(0, 1.113f, 1.1f));
		// Sentries have no lies:
		Combat playerHit;
		float playerDamageDone;
		// Note that this is technically hitscan, but it's not "Hitscan" because it does not come from a player.
		Vector3[] posToSend = HitscanGun.hitscanShoot(fireFromPos, fireFromPos, sentryObj.forward * Mathf.Cos(upDownAngle) + sentryObj.up * Mathf.Sin(upDownAngle), 
			sentryObj.gameObject, null, soundEffect, "BulletDefault", team, damage, 2.0f, 3, 200, 0, null, out playerHit, out playerDamageDone, playerOwner);

		lastHitscanData = posToSend;
		FireTriggered ();
	}

	void createBullet() {
		HitscanGun.createBullet (lastHitscanData, sentryObj.gameObject, null, soundEffect, "BulletDefault");
	}

	bool ai()
	{
		if (canRetarget > 0)
		{
			if (target == null)
			{
				canRetarget = 0;
			}
			else
			{
				canRetarget -= Time.deltaTime; // This doesn't use "deltaTime" because it actually needs to be in sync with the client
			}
		}
		else
		{
			target = null;
		}

		if (target == null)
		{
			// This is why the players layer is important.
			Transform iterater = GameObject.Find("Players").transform;
			// Players are effected by projectiles:
			float range = maxDistance; // Max range
			for (int i = 0; i < iterater.childCount; i++)
			{
				// Check for team
				Transform t = iterater.GetChild(i);
				if (t.GetComponent<Combat>().team != team)
				{
					Vector3 properPlayerPos = t.position + new Vector3(0, 0.5f, 0); // Has to aim slightly higher to not miss.
					Vector3 improperInit = transform.position + transform.InverseTransformPoint(init);
					Vector3 playerPos = transform.position + transform.InverseTransformPoint(properPlayerPos);



					// Check for distance
					float distance = Vector3.Distance(improperInit, playerPos);
					if (distance > distFromMid - 0.4f && distance < range)
					{
						// Check for angle
						float angle = Mathf.Atan2(playerPos.x - improperInit.x, playerPos.z - improperInit.z);
						//angle -= transform.eulerAngles.y * (Mathf.PI / 180f); Not important with proper.
						angle = (angle + 5 * Mathf.PI) % (Mathf.PI * 2) - Mathf.PI;
						if (angle > -angleMax && angle < angleMax)
						{
							Vector3 simulatedFireFromPos = transform.position + new Vector3(0, 0, offSet) + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * distFromMid + new Vector3(0, sentryFireHeight, sentryFireInfront);
							// Check for max y angle (upDown angle)
							float yAngle = Mathf.Atan2(playerPos.y - simulatedFireFromPos.y, Vector2.Distance(new Vector2(playerPos.x, playerPos.z), new Vector2(simulatedFireFromPos.x, simulatedFireFromPos.z)));
							if (yAngle >= -maxYAngle && yAngle <= maxYAngle)
							{

								// ALL PROPER NOW:
								// Test for raycast:
								RaycastHit hit;
								Vector3 forward = Vector3.Normalize(properPlayerPos - simulatedFireFromPos); // Note how this is using proper coords
								// Can only hit "default", "phase through", and "players" (as in it tries to shoot through other objects). Can't hit allies of course
								if (Physics.Raycast(simulatedFireFromPos, forward, out hit, maxDistance, (1 << 13 | 1 << 0 | 1 << (22 - 14 * team) | 1 << 11)))
								{
									// Check if hit the right player
									if (hit.transform == t)
									{
										// Can see player!
										distance = range;
										target = t.gameObject;
										upDownAngle = yAngle;
									}
								}
							}
						}
					}
				}
			}
		}




		if (target != null)
		{
			// Rotate towards target: (And fire if rotated to target)
			Vector3 improperInit = transform.InverseTransformPoint(init);
			Vector3 improperPlayerPos = transform.InverseTransformPoint(target.transform.position + new Vector3(0, 0.4f, 0));

			Vector3 improperFireFromPos = transform.InverseTransformPoint(sentryObj.TransformPoint(new Vector3(0, sentryFireHeight, sentryFireInfront)));


			upDownAngle = Mathf.Atan2(improperPlayerPos.y - improperFireFromPos.y, Vector2.Distance(new Vector2(improperPlayerPos.x, improperPlayerPos.z), new Vector2(improperFireFromPos.x, improperFireFromPos.z)));
			float angle = Mathf.Atan2(improperPlayerPos.x - improperInit.x, improperPlayerPos.z - improperInit.z); // This is from init.
			angle = (angle + 5 * Mathf.PI) % (Mathf.PI * 2) - Mathf.PI;
			if (angle > pos * angleMax)
			{
				pos += 1.4f * Time.deltaTime;
				if (angle < pos * angleMax)
				{
					pos = angle / angleMax;
					if (ServerState.getLifeTime(firedAt) >= shootInterval)
					{
						shoot();
					}
				}
			}
			else if (angle < pos * angleMax)
			{
				pos -= 1.4f * Time.deltaTime;
				if (angle > pos * angleMax)
				{
					pos = angle / angleMax;
					if (ServerState.getLifeTime(firedAt) >= shootInterval)
					{
						shoot();
					}
				}
			}
			if (pos > 1)
			{
				pos = 1;
			}
			else if (pos < -1)
			{
				pos = -1;
			}
			return true;
		}
		else
		{
			return false;
		}
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner) {
		return base.getBitChoicesLengthThis (isPlayerOwner) + 3;
	}

	public override object getObjectThis(int num, bool isPlayerOwner) {
		int childNum = num - base.getBitChoicesLengthThis (isPlayerOwner);

		switch (childNum)
		{
		case 0: return pos;
		case 1: return (firedAt == ServerState.tickNumber) ? (byte) 1 : (byte) 0;
		case 2: return lastHitscanData;

		default: return base.getObjectThis(num, isPlayerOwner);
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		int previousTotal = base.SetInformation (data, a, b, isThisFirstTick, isThisMine);

		pos = (float)data[previousTotal];
		bool firedThisFrame = ((byte)data [previousTotal + 1]) == 1;
		lastHitscanData = (Vector3[])data[previousTotal + 2];

		if (firedThisFrame && isThisFirstTick) {
			FireTriggered ();
		}

		updateSentryTransform();
		interpMuzzle ();

		return previousTotal + 3;
	}

	public override void ServerSyncFixedUpdate() {
		base.ServerSyncFixedUpdate ();
		updateSentry (Time.fixedDeltaTime);
		updateSentryTransform ();
	}
}
