using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class ExplodeDetection {

	// Explosion functionality is fairly simple.

	static float getDamage (float damage, float distance, float range, float minPercentDamage) {
		return ((range - distance) / range) * damage * (1 - minPercentDamage) + damage * minPercentDamage;
	}

	// It includes render functionality, but this is optional.

	// Blow up check occurs on server, of course.
	// All functionality should be in the method parameters:
	// "healthTaken" is set to -1 if it is NOT use healthTaken exploding functinality - this functionality is currently hard coded into this method for simplicity.
	// fromWhere should generally be set to position, as it is where the blast supposedly comes from.
	public static void BlowUp(Vector3 position, Vector3 fromWhere, float damage, float range, float minPercentDamage, float knockBackMult, GameObject exception, int team, ref float healthTaken, short playerSender) {
		// Set position to fromWhere to have the explosion be "standard"

		Collider[] colliders = Physics.OverlapSphere(position, range, LayerLogic.BlowUpLayer());

		List<PlayerMade> playerMadeObjectsHit = new List<PlayerMade> ();
		List<float> playerMadeObjectsDamageDone = new List<float> ();
		for (int i = 0; i < colliders.Length; i++)
		{
			Vector3 ccp = colliders [i].ClosestPointOnBounds (position);
			float distance = Vector3.Distance (position, ccp);
			if (distance > range) {
				Debug.LogError ("Object out of range! ExplodeDetection -> BlowUp");
				damage = 0f;
				continue;
			} else {
				damage = getDamage (damage, distance, range, minPercentDamage); //(range - distance) / range) * damage * (1 - minPercentDamage) + damage * minPercentDamage;
			}

			// Line of sight, essentially
			RaycastHit hit;
			if (Physics.Raycast(position, Vector3.Normalize(ccp - position), out hit, distance, LayerLogic.BlowUpSeeIfLineOfSightLayer(team)))
			{
				// invalid..
				continue;
			}

			// Reverse check, (very important; this one is really the required one.)
			if (Physics.Raycast(ccp, -Vector3.Normalize(ccp - position), out hit, distance, LayerLogic.BlowUpSeeIfLineOfSightLayer(team)))
			{
				// invalid..
				continue;
			}
				
			if (colliders[i].gameObject.layer == 8 + team * 14 && !(colliders[i] is CharacterController))
			{
				Combat hitPlayer = colliders [i].gameObject.GetComponent<Combat> ();
				if (hitPlayer.GetComponent<SyncPlayer> ().playerOwner == playerSender) {

					if (healthTaken == -1) {
						// EXACT same code as is done to enemies: NOTICE how instead of ccp, hitPlayer.transform.position is used to calculate the direction in which the force takes place
						hitPlayer.TakeDamage (damage, knockBackMult, Vector3.Normalize (hitPlayer.transform.position - fromWhere), false, Combat.BLOW_UP, false, fromWhere, playerSender);
					}
				} else if (healthTaken != -1) { // Can't heal yourself
					// HEALING:

					if (hitPlayer.gameObject != exception && hitPlayer.health < hitPlayer.maxHealth) {
						float maxHealthTaken = Mathf.Min(healthTaken, 50);
						healthTaken += (-Mathf.Min (hitPlayer.maxHealth - hitPlayer.health, maxHealthTaken));
						hitPlayer.TakeDamage (Mathf.Max (hitPlayer.health - hitPlayer.maxHealth, -maxHealthTaken), 0, Vector3.zero, false, Combat.HEALING, true, fromWhere, playerSender);
					}
				}
			} else if (colliders[i].gameObject.layer == 22 - team * 14 && !(colliders[i] is CharacterController))
			{
				Combat hitPlayer = colliders[i].gameObject.GetComponent<Combat>();
				if (hitPlayer.gameObject != exception) {
					if (healthTaken != -1) {
						// "TAKE HEALTH"

						// This is completely based on player's health, rather than damage done:
						healthTaken += (Mathf.Min ((25 + hitPlayer.health * 0.5f), 80));
					}
					// NOTICE how instead of ccp, hitPlayer.transform.position is used to calculate the direction in which the force takes place
					hitPlayer.TakeDamage (damage, knockBackMult, Vector3.Normalize (hitPlayer.transform.position - fromWhere), false, Combat.BLOW_UP, false, fromWhere, playerSender);
				}
			}
			else if (colliders[i].gameObject.layer == 10 + team * 5)
			{
				if (healthTaken != -1 && PlayerMade.IsFriendlyBuilding(colliders[i].transform, team))
				{
					// HEALING:

					// Heal this building:
					PlayerMade pMade = PlayerMade.GetPlayerMade(colliders[i].transform);
					if (!playerMadeObjectsHit.Contains(pMade) && pMade.gameObject != exception && pMade.getHealth() < pMade.getMaxHealth())
					{
						// Note how damage is not used, range doesn't matter here!
						playerMadeObjectsHit.Add (pMade); // This is important, to prevent multiple hits
						playerMadeObjectsDamageDone.Add (0); // NOT USED for healthTaken
						float maxHealthTaken = Mathf.Min(healthTaken, 50); // Can only heal 50 in one burst. (Note: you can technically hit more than one player at once)
						healthTaken += (-Mathf.Min(pMade.getMaxHealth() - pMade.getHealth(), maxHealthTaken));
						pMade.TakeDamageObject((short)Mathf.RoundToInt(Mathf.Max(pMade.getHealth() - pMade.getMaxHealth(), -maxHealthTaken)), fromWhere, false, playerSender);
					}
				}
			} else if (colliders[i].gameObject.layer == 15 - team * 5)
			{
				if (PlayerMade.IsEnemy(colliders[i].transform, team))
				{
					PlayerMade pMade = PlayerMade.GetPlayerMade(colliders[i].transform);
					// Kind of redundant because TakeDamageObjectG finds pMade, but it's generalized.
					if (pMade.gameObject != exception) {
						int indexOfPMO = playerMadeObjectsHit.IndexOf (pMade);
						if (indexOfPMO != -1) {
							if (damage > playerMadeObjectsDamageDone [indexOfPMO]) {
								playerMadeObjectsDamageDone [indexOfPMO] = damage;
							}
						} else {
							playerMadeObjectsHit.Add (pMade);
							playerMadeObjectsDamageDone.Add (damage);
						}
					}
				}
			}
		}

		for (int i = 0; i < playerMadeObjectsHit.Count; i++) {
			PlayerMade.TakeDamageObjectG (playerMadeObjectsHit[i].transform, playerMadeObjectsDamageDone[i], fromWhere, false, playerSender);
		}
	}
}
