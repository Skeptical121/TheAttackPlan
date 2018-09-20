using UnityEngine;
using System.Collections;

public class SwingCode : Trap {


	float swingTime = 0.45f;

	Transform sphere;

	public override void InitStart(bool isThisMine) {
		base.InitStart (isThisMine);

		sphere = transform.Find("SwingingSpikeBall");
	}

	public override short getMaxHealth ()
	{
		return 600;
	}

	public override float getLifeTime() {
		return 80.0f;
	}


	public override void UpdateServerAndInterp(bool building, bool dieing, float lifeTime) {

		float rot = Mathf.Sin(lifeTime / swingTime) * 0.65f * Mathf.Rad2Deg;
		sphere.localRotation = Quaternion.Euler(new Vector3 (rot - 90, 90, 0));
	}


}
