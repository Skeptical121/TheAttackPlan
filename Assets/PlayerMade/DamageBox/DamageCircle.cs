using UnityEngine;
using System.Collections;

public class DamageCircle : PlayerMade {

	public override short getMaxHealth ()
	{
		return 250; // Damage Circle doesn't have health.. although it could..
	}

	public override float getLifeTime ()
	{
		return 12f;
	}

	public override void setHealthBar() {
		// No health bar.
	}

	// Returns # from 0 - 1.
	// time is "Time since touching damage circle"
	public static float isTouchingDamageCircle(float time)
	{
		if (time < 1f) {
			return 1f;
		} else if (time < 1.5f) {
			return ((1.5f - time) * 2f);
		} else {
			return 0;
		}
	}

	public static float touchingDamageCircle(float time, float damage, float percent)
	{
		damage *= 1 + isTouchingDamageCircle (time) * percent; // Percent is scale.
		return damage;
	}
}
