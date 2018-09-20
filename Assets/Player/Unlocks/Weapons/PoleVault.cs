using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoleVault : Unlock {
	// NOT YET IMPLEMENTED FULLY

	public Vector3 poleVaultPosition;
	public bool isPoleVaulting; // Determines whether poleVaultPosition is used for rendering..

	Transform unlockInside = null;

	public const float POLE_VAULT_LENGTH = 6f * 0.7f; // This is model length * transform.localScale.x of the personModel!

	// When the pole is pulled out, it will look awkward, as it defaults to having TENSION = 0.

	// The pole only bends when it's in use, in which case TENSION will increase. It only bends forward, regardless.

	// When the pole is not in use, it will.. hmm

	public override void LoadUnlockObject() {
		unlockObject = Resources.Load ("PoleVaultPole") as GameObject;
	}

	// poleVaultPosition is synced to everyone, the pole is then interpolated based on the player position.

	// Pole Vault's hud element should be generic

	// Much like throwable:
	public override void interp ()
	{
		Update ();
	}

	public override void PlayerAndServerAlways (PlayerInput pI, bool equipped)
	{
		base.PlayerAndServerAlways (pI, equipped);
		if (!equipped) {
			isPoleVaulting = false;
		}
	}

	public override void midEnable()
	{
		unlockObject.SetActive (true);
	}

	public override void midDisable()
	{
		unlockObject.SetActive (false);
	}

	public override void UpdateServerAndPlayer (PlayerInput pI, bool runEffects)
	{
		if (isPoleVaulting != pI.fireKey) {
			if (pI.fireKey) {
				// Set pole vault position:
				RaycastHit hitInfo;
				if (Physics.Raycast (unlockObject.transform.parent.position, parentPlayerMove.transform.TransformDirection (Vector3.forward + Vector3.down), out hitInfo, POLE_VAULT_LENGTH, 1 << 0 | 1 << 14 | 1 << 10 | 1 << 15)) {
					Debug.Log (hitInfo.point);
					isPoleVaulting = true;
					poleVaultPosition = hitInfo.point;
				} else {
					isPoleVaulting = false;
				}
			} else {
				isPoleVaulting = false;
			}
		}
		if (runEffects) {
			// The player needs to run this a little bit differently. The "pole" needs to be apart of either the world or the viewmodel.
			Update ();
		}
	}
		
	void Update() {
		Transform mainBone = unlockObject.transform.Find ("Armature").Find ("Bone");
		Transform secondaryBone = unlockObject.transform.Find ("Armature").Find ("Bone").Find ("Bone_001").Find ("Bone_002");
		if (isPoleVaulting) {
			// Update unlockObject:


			// Point secondary bone in the right direction:

			float distance = Vector3.Distance (mainBone.position, poleVaultPosition);

			Vector3 midPosition = (mainBone.position + poleVaultPosition) / 2;

			// Pole Vault Len
			if (distance <= POLE_VAULT_LENGTH) {
				// The hypotenuse length needs to equal POLE_VAULT_LENGTH / 2
				midPosition += Quaternion.LookRotation (poleVaultPosition - mainBone.position) * Vector3.up * Mathf.Sqrt (POLE_VAULT_LENGTH * POLE_VAULT_LENGTH - distance * distance) / 2;
				//Vector3 midPosition += (POLE_VAULT_LENGTH - distance);
			} else {
				// Otherwise we actually have to extend the pole.. but maybe not?
			}

			mainBone.right = -Vector3.Normalize (midPosition - mainBone.position);

			secondaryBone.position = midPosition;
			secondaryBone.right = -Vector3.Normalize (poleVaultPosition - midPosition);
		} else {
			mainBone.localRotation = Quaternion.LookRotation(Vector3.down);
			secondaryBone.localRotation = Quaternion.identity;
			secondaryBone.localPosition = new Vector3 (0, -4, 0); // This could need to be a different # depending on the model
		}

		// Constantly update the viewmodel object based on how much the player is looking up or down: (And update positions / rotations)
		viewmodelUnlockObject.transform.Find ("Armature").Find ("Bone").localRotation = unlockObject.transform.Find("Armature").Find("Bone").localRotation;

		viewmodelUnlockObject.transform.Find ("Armature").Find ("Bone").Find ("Bone_001").Find ("Bone_002").localPosition = 
			unlockObject.transform.Find("Armature").Find("Bone").Find("Bone_001").Find("Bone_002").localPosition;
		
		viewmodelUnlockObject.transform.Find ("Armature").Find ("Bone").Find ("Bone_001").Find ("Bone_002").localRotation = 
			unlockObject.transform.Find ("Armature").Find ("Bone").Find ("Bone_001").Find ("Bone_002").localRotation;
	}

	public override string getHudType() {
		return "Generic";
	}

	public override string getAnimationType() {
		return "PoleVault";
	}

	public override string GetUnlockName() {
		return "Pole Vault (N/A)";
	}

	public override int getUnlockPosition ()
	{
		return 2;
	}

	public override bool canDisable ()
	{
		return false; // Probably can't be put away through normal means.. must be cancelled?
	}

	public override bool canEnable ()
	{
		return true;
	}

	public override void disable ()
	{
	}

	public override void enable ()
	{
	}
}
