using UnityEngine;
using System.Collections;

public class MirrorLogic : PlayerMade {

	public Material materialNonMirror;
	public Material materialMirror;

	public Material[] tintColors;

	bool isThisOurTeamsMirror = false;

	public override void InitStart (bool isThisMine)
	{
		base.InitStart (isThisMine);

		// Set layer for collider:
		transform.Find("MirrorBack").gameObject.layer = 10 + team * 5;  // MAKE it playermade! So it can be destroyed from the back. Note that it can only be destroyed with direct damage due to this being an object within. But this actually seems perfectly fine.


		// This finds if your team has an existing mirror and destroys it.
		// It also sets this mirror for the given team.
		if (OperationNetwork.isServer) {
			mirrorCheck ();
		}

		SetMirrorCamera(team);
	}

	public override short getMaxHealth() {
		return 300;
	}

	public override float getLifeTime ()
	{
		return 16f; // Just above half the cooldown
	}

	// SERVER
	void mirrorCheck() {

		int otherTeam = (team + 1) % 2; // Assumed to be 2 teams!

		if (Mirrors.mirrors[team])
		{
			GameObject mir = Mirrors.mirrors[team];
			mir.GetComponent<SyncGameState> ().exists = false;
		}

		Mirrors.mirrors[team] = gameObject; // This overwrites the mirror reference. (If there is one)
	}

	// This no longer sets the mirror camera, but the team that the mirror is being used by.. hmm.. todo: deprecate it.
	public void SetMirrorCamera(byte teamNum)
	{
		team = teamNum;
		isThisOurTeamsMirror = true;

		// Set shader:
		transform.Find("ReflectPlane").GetComponent<Renderer>().material = materialMirror;
	}

	// This might as well be Update..
	void Update()
	{
		// Might as well do a simple if statement every frame to see if the player's team has changed:
		if (isThisOurTeamsMirror != (Player.thisPlayer != null && Player.thisPlayer.team == team))
		{
			SetMirrorCamera(team);
		}
	}
}
