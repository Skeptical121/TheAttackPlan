using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveSpikes : Trap {

	Transform childSpikes;

	public override short getMaxHealth ()
	{
		return 500;
	}

	public override float getLifeTime ()
	{
		return 50f;
	}

	public override void InitStart (bool isThisMine) {
		base.InitStart (isThisMine);
		childSpikes = transform.Find ("ActualSpikes");

		// Obviously no need to update position.
	}

	public override void UpdateServerAndInterp(bool building, bool dieing, float lifeTime) {
		UpdateSpikes (lifeTime);
	}

	public void UpdateSpikes(float netTime)
	{
		
		float position = (netTime + 2f) / 1.5f % 2.0f; // This means 3 second seconds (1.5 * 2) Modify the first value.
		if (position > 1.0f)
		{
			position = 2.0f - position;
		}

		position = Mathf.Pow(position, 1.5f);

		childSpikes.localPosition = new Vector3(0, position * 1 - 0.35f, 0);
	}
}
