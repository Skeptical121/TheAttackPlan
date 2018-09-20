using UnityEngine;
using System.Collections;

public class SelectClass : MonoBehaviour {

	public byte num; // Set on init by PlayerHud

	public void SelectClassNum() {
		if (OperationNetwork.connected && Player.thisPlayer != null) {
			OperationView opView = Player.thisPlayer.GetComponent<OperationView> ();
			if (opView != null && num != Player.thisPlayer.classNum) {
				opView.RPC("switchTeam", OperationNetwork.ToServer, Player.thisPlayer.team, num);
			}
		}
	}

	public void SelectTeam() {
		if (OperationNetwork.connected && Player.thisPlayer != null) {
			OperationView opView = Player.thisPlayer.GetComponent<OperationView> ();
			if (opView != null && num != Player.thisPlayer.team) {
				opView.RPC ("switchTeam", OperationNetwork.ToServer, num, Player.thisPlayer.classNum);
				if (num == 2) {
					OptionsMenu.classSelectionMenuOpen = false;
					OptionsMenu.ChangeLockState();
				}
			}
		}
	}
}
