using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameTrigger {

	int triggeredAt = -1; // Server / Client representation.
	float playerTriggeredAt = -1; // Player / Server representation.

	public float getTriggerTime(PlayerMove parent) {
		if (OperationNetwork.isServer || parent.thisIsMine) {
			return playerTriggeredAt;
		} else {
			return triggeredAt;
		}
	}

	// Player and Server
	public void trigger(float netTime) {
		playerTriggeredAt = netTime;
		if (OperationNetwork.isServer)
			triggeredAt = ServerState.tickNumber;
	}

	// Player and Server
	public void reset() {
		playerTriggeredAt = -1;
		if (OperationNetwork.isServer)
			triggeredAt = -1;
	}

	// Server
	public bool triggeredThisFrame() {
		return triggeredAt == ServerState.tickNumber;
	}

	// Server for use with SYNCING, typically within = MAX_CHOKE_TICKS
	public bool triggeredThisFrame(int within) {
		return ServerState.tickNumber >= triggeredAt && ServerState.tickNumber <= triggeredAt + within;
	}

	// CLient
	public void triggerClient(int tickNumber) {
		triggeredAt = tickNumber;
	}

	// Client
	public bool triggeredThisTick() {
		return triggeredAt == Interp.getTickNumber ();
	}

	// Client
	public void interpTrigger(bool b) {
		if (b) {
			triggeredAt = Interp.getTickNumber ();
		}
	}

	public float getPlayerTriggerTime() {
		return playerTriggeredAt;
	}

	// For prediction error use only:
	public void setPlayerTriggerTime(float time) {
		playerTriggeredAt = time;
	}
}