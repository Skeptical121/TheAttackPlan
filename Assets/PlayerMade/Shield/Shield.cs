using UnityEngine;
using System.Collections;

public class Shield : PlayerMade {

	float[] initialAlpha;

	float gridWidth = 1.4f;

	const int HALF_WIDTH = 2;
	const int HEIGHT = 3;

	public GameObject shieldObject;
	GameObject[,] shieldObjects = new GameObject[HALF_WIDTH * 2 + 1,HEIGHT];

	// 1.7 * 5

	// The first one is placed on the ground.
	// 2 can go left and right for the first 2 rows.
	// last row, 1 can go left & right

	public override short getMaxHealth() {
		return 300;
	}

	public override float getLifeTime() {
		return 16.0f;
	}

	void testSpawnShield(int x, int y, int dX, int dY, int dir) {
		if (!Physics.Raycast (transform.TransformPoint (new Vector3 ((x - HALF_WIDTH) * gridWidth, (y + 0.5f) * gridWidth, 0)), transform.TransformDirection(new Vector3(dX, dY, 0)), gridWidth * 0.6f, 1 << 0)) {
			spawnShields (x + dX, y + dY, dir);
		}
	}

	void spawnShields(int x, int y, int dir) {
		if (x >= 0 && x < HALF_WIDTH * 2 + 1 && y >= 0 && y < HEIGHT) {
			shieldObjects [x, y] = Instantiate (shieldObject, transform.TransformPoint(new Vector3((x - HALF_WIDTH) * gridWidth, (y + 0.5f) * gridWidth, 0)), transform.rotation, transform);
			if (dir == 0) {
				testSpawnShield (x, y, 0, 1, 0);
			}
			if (dir <= 0) {
				testSpawnShield (x, y, -1, 0, -1);
			}
			if (dir >= 0) {
				testSpawnShield (x, y, 1, 0, 1);
			}
		}
	}

	public override void InitStart (bool isThisMine)
	{
		base.InitStart (isThisMine);

		if (OperationNetwork.isServer) {
			spawnShields (2, 0, 0);
		}
	}

	public override void AfterFirstInterp ()
	{
		base.AfterFirstInterp ();
		if (!OperationNetwork.isServer) {
			spawnShields (2, 0, 0);
		}
	}
}
