using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GolfBallHoming : Homing {

	public override void InitStart (bool isThisMine)
	{
		base.InitStart (isThisMine);
		homingCapabilities = 60;
		homingCapabilitiesLossPerSecond = 0f; // Could actually gain, to be honest..

		expDmg = 100;
		maxDist = 4f; // Massive range
		initDmgPercent = 0.75f;
		leastDmgPercent = 0.55f;

		knockBackMult = 1.7f;
	}

	public override float getMaxHomingDistance() {
		return 30f;
	}

	// This should be equal to the launch speed for this, I think that works best.
	public override float getMaxSpeed() {
		return 60f;
	}
}
