using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuyNewTrap : MonoBehaviour {

	public const int OBSTACLE = 0;
	public const int SHOOTER = 1;
	public const int ENEMY_TRIGGER_TRAP = 2;
	public const int YOU_TRIGGER_TRAP = 3;
	public static string[] GROUP_NAMES = { "Obstacle", "Shooter", "Enemy Trigger Trap", "You Trigger Trap" };

	public static Dictionary<byte,int> trapIndecies; // = new Dictionary<byte,int> (); // Dictionaries are terribly difficult to iterate through properly

	public static short[] baseTrapCosts;
	public static int[] maxTrapsLoaded;
	public static int[] trapGroupType; // = new Dictionary<byte,int>();
	public static string[] trapNames; // = new Dictionary<byte,string>(); // The cost of a trap will be determined by match time probably. Or levelsync

	public const float costMult = 0.1f;

	public byte buyId;

	public byte rowID = 255;

	bool renderingRowNotPicked = true;

	public void buy() {
		if (buyId == 255) {
			// These aren't run
			if (PlayerHud.whichGroupTypeSelecting != -1) {
				PlayerHud.whichGroupTypeSelecting = -1;
			} else {
				PlayerHud.whichGroupTypeSelecting = -1;
				PlayerHud.isTrapSelectionMenuOpen = false;
			}
		} else if (buyId >= 200) {
			PlayerHud.whichGroupTypeSelecting = buyId - 200;
		} else if (Player.thisPlayer != null && rowID != 255) {
			// Pre-checks:
			Player.thisPlayer.attemptBuyTrap (rowID, buyId);
		}
	}

	void Update() {
		if (Player.thisPlayer && rowID != 255) {
			bool shouldRenderRowNotPicked = Player.thisPlayer.trapTypes [rowID] != 255;
			if (!renderingRowNotPicked && shouldRenderRowNotPicked) {
				// In order to make the selection, this transfer must be done.. so this is fine..
				if (Player.thisPlayer.trapTypes [rowID] == buyId) {

				} else {

				}
			} else if (renderingRowNotPicked && !shouldRenderRowNotPicked) {

			}
		}
	}

	public static void init() {
		int NUM_TRAPS = 8;

		baseTrapCosts = new short[NUM_TRAPS];
		maxTrapsLoaded = new int[NUM_TRAPS];
		trapGroupType = new int[NUM_TRAPS];
		trapNames = new string[NUM_TRAPS];
		trapIndecies = new Dictionary<byte,int> ();

		trapIndecies.Add (1, 0);
		baseTrapCosts[0] = 350;
		trapGroupType[0] = ENEMY_TRIGGER_TRAP;
		trapNames[0] = "Rocket Trap";
		maxTrapsLoaded [0] = 1;

		trapIndecies.Add (3, 1);
		baseTrapCosts[1] = 500;
		trapGroupType[1] = SHOOTER;
		trapNames[1] =  "Homing Energy Ball Gun";
		maxTrapsLoaded [1] = 1;

		trapIndecies.Add (6, 2);
		baseTrapCosts[2] = 250;
		trapGroupType[2] = SHOOTER;
		trapNames[2] = "Laser Field";
		maxTrapsLoaded [2] = 2;

		trapIndecies.Add (8, 3);
		baseTrapCosts[3] = 350;
		trapGroupType[3] = OBSTACLE;
		trapNames[3] = "Saw Blades";
		maxTrapsLoaded [3] = 1;

		trapIndecies.Add (11, 4);
		baseTrapCosts[4] = 500;
		trapGroupType[4] = OBSTACLE;
		trapNames[4] = "Spikes Platform";
		maxTrapsLoaded [4] = 2;

		trapIndecies.Add (12, 5);
		baseTrapCosts[5] = 300;
		trapGroupType[5] = OBSTACLE;
		trapNames[5] = "Swinging Spikes";
		maxTrapsLoaded [5] = 3;

		trapIndecies.Add (13, 6);
		baseTrapCosts[6] = 200;
		trapGroupType[6] = OBSTACLE;
		trapNames[6] = "Windmill";
		maxTrapsLoaded [6] = 3;

		trapIndecies.Add (9, 7);
		baseTrapCosts[7] = 600;
		trapGroupType[7] = SHOOTER;
		trapNames[7] = "Sentry";
		maxTrapsLoaded [7] = 1;
	}
}
