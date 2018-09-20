using UnityEngine;
using System.Collections;

public class Arrow : InterpRotate
{

	// The clients use this form of interpolation to determine the rotation; but the server uses velocity still.

	// Use this for initialization
	public override void InitStart(bool isThisMine) // Only called on server.
	{
		base.InitStart (isThisMine);

		if (ignoreTeam == 1)
		{
			expDmg = 70;
			maxDist = 3.7f; // Was 3.3f.  .. Was 2.7f, Note bullet is (at least was) 2.7f
			initDmgPercent = 0.8f;
			leastDmgPercent = 0.25f;
		} else
		{
			// Arrow takes on 2 states:
			expDmg = 50;
			maxDist = 2.1f;
			initDmgPercent = 0.5f;
			leastDmgPercent = 0.4f;
		}

		knockBackMult = 0.5f;
	}

	public override float getExpDamage()
	{
		if (ignoreTeam == 0)
		{
			// Blue arrows have falloff: (Fairly minimal) todo: arrows don't need damage falloff.
			return expDmg * Mathf.Pow(0.8f, getLifeTimeServer());
		}
		else {
			return expDmg;
		}
	}
}
