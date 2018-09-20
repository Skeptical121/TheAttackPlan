using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Communication : MonoBehaviour {

	List<string> chatMessages = new List<string>();
	List<float> chatAge = new List<float>(); // Based on time recieved of chat.

	const int maxMessages = 10;

	public static bool typing = false;
	string message = "";

	float timeSinceLastMessageSent = 100f;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		for (int i = chatAge.Count - 1; i >= 0; i--)
		{
			chatAge[i] += Time.deltaTime;
			if (chatAge[i] > 15)
			{
				chatMessages.RemoveAt(i);
				chatAge.RemoveAt(i);
			}
		}

		timeSinceLastMessageSent += Time.deltaTime;

		if (!typing && Input.GetKeyUp(KeyCode.Y))
		{
			typing = true;
			message = "";
			OptionsMenu.ChangeLockState();
		}
	}

	void OnGUI()
	{
		for (int i = chatAge.Count - 1; i >= 0; i--)
		{
			if (i >= chatAge.Count - maxMessages)
			{
				int height = Screen.height - 300 + (chatAge.Count - i - 1) * 15;
				GUI.color = Color.black;
				GUI.Label(new Rect(Screen.width - 400, height, 390, 24), chatMessages[i]);
			}
			
		}

		if (typing)
		{
			Event e = Event.current;
			if (e.keyCode == KeyCode.Return)
			{
				// Send message:
				typing = false;

				if (message.Length > 0 && timeSinceLastMessageSent >= 0.3f) // Up to ~3 messages per second max.
				{
					GetComponent<OperationView>().RPC("Chat", OperationNetwork.ToServer, message);
					timeSinceLastMessageSent = 0;
				}

				message = "";
				OptionsMenu.ChangeLockState();
				return;
			}
			GUI.color = Color.white;
			GUI.SetNextControlName("ChatBox");
			message = GUI.TextField(new Rect(Screen.width - 400, Screen.height - 130, 200, 24), message, 26);

			// Constantly set control:
			GUI.FocusControl("ChatBox");
		}
	}

	//OperationRPC
	public void Chat(string message, short fromWho)
	{
		if (OperationNetwork.isServer)
		{
			// This needs to be done in the OpNetGS way.
			//GetComponent<OperationView>().RPC("Chat", -fromWho, message, fromWho);
		}
		if (GameManager.PlayerExists (fromWho)) {
			chatMessages.Add (GameManager.GetPlayer(fromWho).playerName + ": " + message);
			chatAge.Add (0);
		}
	}
}
