using UnityEngine;
using System.Collections;

public class WindmillCode : Trap {

	Transform windmillSpokes;

	public override void InitStart(bool isThisMine) {
		base.InitStart (isThisMine);

		windmillSpokes = transform.Find("WindMillSpokes");
	}

	public override short getMaxHealth ()
	{
		return 600;
	}

	public override float getLifeTime() {
		return 80.0f;
	}

	public override void UpdateServerAndInterp(bool building, bool dieing, float lifeTime) {

		float rot = lifeTime * 100f;
		windmillSpokes.localRotation = Quaternion.Euler(new Vector3 (0, 0, rot));
	}
}
