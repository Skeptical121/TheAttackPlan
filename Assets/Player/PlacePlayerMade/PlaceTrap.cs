using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceTrap : PlacePlayerMade {

	public override void LoadUnlockObject() {
		base.LoadUnlockObject ();
		// This can be changed whenever:
		cutOut = null;
	}
		
	public int trapPlacing = 0; // 0 or 1.. this is ENTIRELY predicted..

	public override void UpdateServerAndPlayer(PlayerInput pI, bool runEffects)
	{
		if (parentPlayerMove.thisIsMine && runEffects) {
			if (Player.thisPlayer != null && Player.thisPlayer.trapTypes[trapPlacing] != 255 && (cutOut == null || cutOut.GetComponent<CollideCheck>().placeType != Player.thisPlayer.trapTypes[trapPlacing])) {
				cutOut = PlacePlayerMade.cutOuts [Player.thisPlayer.trapTypes [trapPlacing]];
				if (placing != null)
					MonoBehaviour.Destroy (placing);
				instantiateObject ();
			} else if (Player.thisPlayer != null && Player.thisPlayer.trapTypes[trapPlacing] == 255 && cutOut != null) {
				// Equivalent to, might be an issue:
				disable ();
				cutOut = null;
			}
		}
		base.UpdateServerAndPlayer (pI, runEffects);
	}

	public override void setPlacingRotation(Vector3 normal)
	{
		if (cutOut.GetComponent<CollideCheck> ().canPlaceOn == CollideCheck.canPlaceAnywhere) {
			placing.transform.rotation = Quaternion.LookRotation (normal);
			placing.transform.position += normal * 0.12f;
		} else if (cutOut.GetComponent<CollideCheck> ().canPlaceOn == CollideCheck.canPlaceAnywhere2) {
			if (normal.y <= 0.5f) {
				placing.transform.rotation = Quaternion.LookRotation (normal) * Quaternion.Euler (90, 0, 0);
			} else {
				base.setPlacingRotation (normal);
			}
		} else if (cutOut.GetComponent<CollideCheck> ().canPlaceOn == CollideCheck.canPlaceOnWalls) {
			placing.transform.rotation = Quaternion.Euler (0, Mathf.Atan2 (normal.x, normal.z) * 180 / Mathf.PI + 180f, 0);
			placing.transform.position += normal * 0.14f;
		} else {
			base.setPlacingRotation (normal);
		}
	}

	public override bool normalIsValid(RaycastHit hit, Vector3 normal)
	{
		if (cutOut.GetComponent<CollideCheck> ().canPlaceOn == CollideCheck.canPlaceAnywhere || cutOut.GetComponent<CollideCheck> ().canPlaceOn == CollideCheck.canPlaceAnywhere2)
			return true;
		else if (cutOut.GetComponent<CollideCheck> ().canPlaceOn == CollideCheck.canPlaceOnWalls)
			return normal.y <= 0.5f;
		else
			return normal.y > 0.5f;
	}
		
	public void setTrapCoolDown() {

		float playerTime = parentPlayerMove.GetComponent<SyncPlayer> ().playerTime;

		Player player = GameManager.GetPlayer (parentPlayerMove.plyr);

		int trapTypeIndex = BuyNewTrap.trapIndecies[player.trapTypes [trapPlacing]];

		if (OperationNetwork.isServer) {
			player.resetTrapCoolDownsTo [trapPlacing] = player.trapCoolDownsStartedAt [trapPlacing];
		}

		float currentTime = player.trapCoolDownsStartedAt [trapPlacing];
		float deltaTime = (playerTime - currentTime);
		if (deltaTime > BuyNewTrap.maxTrapsLoaded [trapTypeIndex] * BuyNewTrap.baseTrapCosts [trapTypeIndex] * Time.fixedDeltaTime) {
			player.trapCoolDownsStartedAt [trapPlacing] = playerTime - (BuyNewTrap.maxTrapsLoaded [trapTypeIndex] - 1) * BuyNewTrap.baseTrapCosts [trapTypeIndex] * Time.fixedDeltaTime;
		} else {
			player.trapCoolDownsStartedAt [trapPlacing] = currentTime + BuyNewTrap.baseTrapCosts [trapTypeIndex] * Time.fixedDeltaTime; // Approximation; of course
		}
		lastTimePlacedTrap = Time.time;
	}

	public static float lastTimePlacedTrap = -1; // In "Time.time" units

	public const float TIME_BETWEEN_PLACING_TRAPS = 0.6f;

	// This is PLAYER only.
	public bool canPlaceTrap() {
		Player player = GameManager.GetPlayer (parentPlayerMove.plyr);

		if (player.trapTypes[trapPlacing] == 255)
			return false;
		float playerTime = parentPlayerMove.GetComponent<SyncPlayer> ().playerTime;
		return Time.time - lastTimePlacedTrap > TIME_BETWEEN_PLACING_TRAPS && (playerTime - player.trapCoolDownsStartedAt [trapPlacing]) >= BuyNewTrap.baseTrapCosts [BuyNewTrap.trapIndecies[player.trapTypes [trapPlacing]]] * Time.fixedDeltaTime;
	}

	public override int getUnlockPosition() {
		return 3;
	}

	public override int getPlacingLength()
	{
		return 14;
	}

	public override string GetUnlockName() {
		return "Trap or Obstacle";
	}

	public override float getCoolDown()
	{
		return 162500f; // Large #, above 125000, (referenced in ClassControl -> switchCheck)
	}

}
