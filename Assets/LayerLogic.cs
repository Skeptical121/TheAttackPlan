using UnityEngine;
using System.Collections;

public class LayerLogic : MonoBehaviour {

	// Team should be the allied team.
	public static int HitscanShootLayer(int team)
	{
		return ~(1 << 2 | 1 << 12 | 1 << 9 | 1 << 8 | 1 << 22 | 1 << 13 | 1 << (16 + team) | 1 << 18 | 1 << 19 | 1 << 23 | 1 << 24); // Note that this is NOT 16 + team.
	}

	// Can hit both teams:
	public static int HitscanShootLayer()
	{
		return ~(1 << 2 | 1 << 12 | 1 << 9 | 1 << 8 | 1 << 22 | 1 << 13 | 1 << 18 | 1 << 19 | 1 << 23 | 1 << 24);
	}

	public static int BlowUpLayer() {
		return (1 << 8 | 1 << 10 | 1 << 15 | 1 << 20 | 1 << 21 | 1 << 22); // Note how default / decorCollide are not included
	}


	public static int BlowUpSeeIfLineOfSightLayer(int team)
	{
		return (1 << 14 | 1 << 0 | 1 << 15 - team * 5); // Perhaps decorCollide shouldn't be included here.
	}

	// To do projectile collisions, we could just make an additional collision sphere that is larger that only collides with players.. (TODO!)
	public static int ProjectileCollision(int team) {
		return (1 << 0 | (1 << 17 - team) | 1 << 14 | (1 << 15 - team * 5));
	}

	// Team is irrelivant here
	public static int PlacePlayerMadeCollision() {
		// Collides with default, (the map), decorCollide- which is essentially the props of the map that traps can't be attached to; and 1 << 19
		return (1 << 0 | 1 << 14 | 1 << 19 | 1 << 23 | 1 << 24); // Because of this, the layer "Placement" (layer 18) is officially deprecated. AntiPlacement still stays around though
	}

	public static int WorldColliders() {
		return (1 << 0 | 1 << 14);
	}

	// The following is a comprehensive explanation of the layers:

}
