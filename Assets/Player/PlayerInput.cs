using UnityEngine;
using System;
using System.Collections;

public class PlayerInput {

	// This is the PlayerInput generated from the player's input commands based on their keybinds.

	// dX & dY can be -1, 0, or 1. All combinations of WASD, essentially.
	public int dX = 0;
	public int dZ = 0;

	public bool jumpKey = false; // Only true on the first "frame" of jump press. "SPACE"
	public bool crouchKey = false; // "CTRL"
	public bool movementAbilityKey = false; // LeftShift
	public bool ultimateAbilityKey = false;

	public bool fireKey = false; // Mouse1.
	public bool reloadKey = false; // "R"
	public bool cancelKey = false;

	public bool secondaryFireKey = false;

	public byte unlockSwitchTo = 255; // This is NOT by the unlock ID, this is by unlock slot ID

	short shortRotation = 0;
	public float rotation = 0;

	short shortRotationUpDown = 0;
	public float rotationUpDown = 0;

	byte byteFrameTime = 0;
	public float frameTime = 0; // This is generated as the ACTUAL time, and takes the place of Time.deltaTime. (It will be different!)

	public float tickNumber = 0; // This is ONLY used on server to do lag compensation

	public short playerInputGroupID = 0; 

	public float gameTime; // For determining ping on player

	public bool wasFirstPlayerInputForPlayerObjectOnServer = false; // Use on server only
	public short objectID = -1; // For use on server only

	// Player side.
	public PlayerInput(float lastTime, float lastRotation, float lastRotationUpDown, ClassControl cc, bool isBot) {
		// Generates PlayerInput right now.

		byteFrameTime = timeToByte (Time.time - lastTime);
		frameTime = byteToTime (byteFrameTime);

		// rotation is interpolated on PLAYER SIDE rather than SERVER SIDE, as it doesn't matter what the lastRotation was on the server.
		// Thus, lastRotation is NOT set based on playerInput.

		// I believe modulus works correctly here, doesn't really matter though, that would only matter if in one frame you rotated more than 360 degrees
		shortRotation = (short)((lastRotation % 360) * (32767 / 360.0f));
		// ARCHAIC SYSTEM: (short) Mathf.Clamp (Mathf.Floor ((float)(lastRotation / 360.0 * 65536)) - 32768, -32768, 32767);

		shortRotationUpDown = (short)(Mathf.Clamp(lastRotationUpDown, -90f, 90f) * (32767 / 90.0f));
		// ARCHAIC SYSTEM: (short)Mathf.Clamp (Mathf.Floor ((float)(lastRotationUpDown / 90.0 * 32768)), -32768, 32767); // Clamping could actually be done here.

		rotation = shortToRotation (shortRotation);
		rotationUpDown = shortToRotationUpDown (shortRotationUpDown);

		// lastTime needs to be updated as to simulate a consistent amount of frames. >1000FPS will result in you going faster.. (0 rounds up to 1)

		if (lastTime > Time.time + 5) {
			// More than 5 seconds "faster" = KICK.
			Debug.LogError ("You should be kicked. (Ahead)");
		} else if (lastTime < Time.time - 5) {
			// More than 5 seconds "slower" actually doesn't mean kick..
			Debug.LogError ("You should be kicked. (Behind)");
		}

		// The following should not be relied on to be called consistently: (They are "optional")
		if (OptionsMenu.IsLockState () && !isBot) {

			// Default keys for now:
			dX = (int)Input.GetAxisRaw ("Horizontal");
			dZ = (int)Input.GetAxisRaw ("Vertical");

			jumpKey = Input.GetKeyDown (KeyCode.Space);
			crouchKey = Input.GetKey (KeyCode.LeftControl);

			fireKey = Input.GetKey (KeyCode.Mouse0);
			cancelKey = Input.GetKeyDown(KeyCode.Mouse1);
			secondaryFireKey = Input.GetKey (KeyCode.Mouse1); // Obviously most unlocks don't trigger this

			reloadKey = Input.GetKey (KeyCode.R);

			movementAbilityKey = Input.GetKey (OptionsMenu.binds [OptionsMenu.MOVEMENT_ABILITY_BIND]);

			ultimateAbilityKey = Input.GetKeyDown (OptionsMenu.binds[OptionsMenu.ULTIMATE_ABILITY]);

			// Priority goes to the 1st unlock:

			// This is ONLY for switching to unlocks that currently exist:
			if (!PlayerHud.isTrapSelectionMenuOpen) {
				for (int i = 0; i < OptionsMenu.NUM_SWITCH_TO_BINDS; i++) {
					if (Input.GetKeyDown (OptionsMenu.binds [i])) {
						unlockSwitchTo = (byte)i;
						break;
					}
					// Unlocks can not be created here.
				}
			}

		}

		gameTime = Time.time; // Player only
		
	}

	public PlayerInput(byte[] data) {
		jumpKey = (data [0] & (1 << 0)) != 0;
		crouchKey = (data[0] & (1 << 1)) != 0;
		fireKey = (data [0] & (1 << 2)) != 0;
		reloadKey = (data [0] & (1 << 3)) != 0;
		movementAbilityKey = (data [0] & (1 << 4)) != 0;
		if ((data [0] & (1 << 5)) != 0)
			dX = -1;
		if ((data [0] & (1 << 6)) != 0)
			dX = 1;
		if ((data [0] & (1 << 7)) != 0)
			dZ = -1;
		if ((data [1] & (1 << 0)) != 0)
			dZ = 1;
		ultimateAbilityKey = (data [1] & (1 << 1)) != 0;
		wasFirstPlayerInputForPlayerObjectOnServer = (data [1] & (1 << 2)) != 0;
		cancelKey = (data [1] & (1 << 3)) != 0;
		secondaryFireKey = (data [1] & (1 << 4)) != 0;

		unlockSwitchTo = data[2];
		frameTime = byteToTime (data [3]);
		rotation = shortToRotation (BitConverter.ToInt16 (data, 4));
		rotationUpDown = shortToRotationUpDown (BitConverter.ToInt16 (data, 6));

		if (fireKey) {
			tickNumber = BitConverter.ToSingle (data, 8);
			if (wasFirstPlayerInputForPlayerObjectOnServer) {
				objectID = BitConverter.ToInt16 (data, 12);
			}
		}
		if (wasFirstPlayerInputForPlayerObjectOnServer) {
			objectID = BitConverter.ToInt16 (data, 8);
		}
	}

	byte timeToByte(float time) {
		byte bFT;

		// Minimum of 4fps before you start going slower.. hmm..
		if (time > 0.255f) {
			bFT = (byte)255;
		} else if (time < -0.050f) {
			// Skipping frames can cause input commands to be missed, so it is to be avoided at almost all costs
			bFT = 0; // 50ms = at LEAST 50 frames ahead
		} else if (time < 0f) {
			bFT = 1;
		} else {
			bFT = (byte)(((double)time * 1000.0));

			// 0 = this frame gets skipped
			if (bFT == 0)
				bFT = 1;
		}
		return bFT;
	}

	float byteToTime(byte byteTime) {
		return (byteTime / 1000.0f);
	}

	float shortToRotation(short rot) {
		return (float)(rot * 360.0f / 32768); //(float)((rot + 32768) / 65536.0 * 360.0);
	}

	float shortToRotationUpDown(short rotUpDown) {
		return (float)(rotUpDown * 90.0f / 32768); //(float)(rotUpDown / 32768.0 * 90.0);
	}

	// Player side.
	public byte[] generateByteData(bool hasRunFirstCommand, short playerObjectID) {
		int length = 8;
		if (fireKey)
			length += 4;
		if (!hasRunFirstCommand)
			length += 2; // For objectID
		byte[] data = new byte[length];

		if (jumpKey)
			data[0] += (1 << 0);
		if (crouchKey)
			data[0] += (1 << 1);
		if (fireKey)
			data[0] += (1 << 2);
		if (reloadKey)
			data[0] += (1 << 3);
		if (movementAbilityKey)
			data[0] += (1 << 4);
		if (dX == -1)
			data[0] += (1 << 5);
		if (dX == 1)
			data[0] += (1 << 6);
		if (dZ == -1)
			data[0] += (1 << 7);
		if (dZ == 1)
			data[1] += (1 << 0);
		if (ultimateAbilityKey)
			data[1] += (1 << 1);
		if (!hasRunFirstCommand)
			data [1] += (1 << 2);
		if (cancelKey)
			data [1] += (1 << 3);
		if (secondaryFireKey)
			data [1] += (1 << 4);
		
		data[2] = unlockSwitchTo; // We are saving the traps that we get on player only right now. This will change with the new trap system.
		data[3] = byteFrameTime; // For the time being net sending is based on framerate largely due to the escessive amount of work that it would take to iterate on this system- as shown by previous attempts.
		Buffer.BlockCopy (BitConverter.GetBytes (shortRotation), 0, data, 4, 2);
		Buffer.BlockCopy (BitConverter.GetBytes (shortRotationUpDown), 0, data, 6, 2);

		// Only needs to send tick number if there's a chance hitscan might be fired:
		if (fireKey) {
			Buffer.BlockCopy (BitConverter.GetBytes (Interp.getRepTickNumber ()), 0, data, 8, 4);
		}
		if (!hasRunFirstCommand) {
			int addAt = 8;
			if (fireKey) {
				addAt = 12;
			}
			Buffer.BlockCopy (BitConverter.GetBytes (playerObjectID), 0, data, addAt, 2);
		}
		return data;
	}
}
