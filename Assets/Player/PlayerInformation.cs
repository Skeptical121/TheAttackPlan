using UnityEngine;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

public class PlayerInformation : MonoBehaviour {

	public static PlayerName yourInfo = new PlayerName();
	float lastSaveTime;
	public static string steamName;

	// Use this for initialization
	void Start () {
		steamName = "Annonymous" + UnityEngine.Random.Range(0, 100000);
		lastSaveTime = Time.time;
		Load(); // Automatically loads player data. (If it can find it, of course)

		#if !DISABLESTEAMWORKS
		if(SteamManager.Initialized) {
			string name = SteamFriends.GetPersonaName();
			Debug.Log(name);
			steamName = name;
		}
		#endif
	}
	
	// Update is called once per frame
	void Update () {
		// Saves once every 30 seconds:
		if (Time.time - lastSaveTime > 30)
		{
			lastSaveTime = Time.time;
			Save();
		}
	}
	
	public string[] KeyCodesToStringArray(KeyCode[] array)
	{
		string[] stringArray = new string[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			stringArray[i] = array[i].ToString();
		}
		return stringArray;
	}

	public KeyCode[] StringsToKeyCodeArray(string[] array)
	{
		
		KeyCode[] keyCodeArray = new KeyCode[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			print (array[i]);
			keyCodeArray[i] = (KeyCode)Enum.Parse(typeof(KeyCode), array[i]);
		}
		return keyCodeArray;
	}

	void OnApplicationQuit()
	{
		Save();
	}

	public void Save()
	{
		byte[] data = new byte[10];


		BinaryFormatter bf = new BinaryFormatter();
		
		FileStream file = File.Open(Application.persistentDataPath + "/PlayerName.dat", FileMode.OpenOrCreate);
		yourInfo.sensitivity = PlayerMove.sensitivity;
		yourInfo.volume = AudioListener.volume; // The unity editor, onApplicationQuit sets this to 1, but that's okay.
		yourInfo.binds = KeyCodesToStringArray(OptionsMenu.binds);
		bf.Serialize(file, yourInfo);
		file.Close();
	}

	public void Load()
	{
			if (File.Exists(Application.persistentDataPath + "/PlayerName.dat"))
			{
				BinaryFormatter bf = new BinaryFormatter();
				FileStream file = File.Open(Application.persistentDataPath + "/PlayerName.dat", FileMode.Open);
				try
				{
					yourInfo = (PlayerName)bf.Deserialize(file);
				} catch (Exception e)
				{
				// Oh well..
					Debug.LogError("Could not read player information: " + e.Message);
				}
				file.Close();
				PlayerMove.sensitivity = yourInfo.sensitivity;
				AudioListener.volume = yourInfo.volume;
			}
		
	}

	
}

[Serializable]
public class PlayerName
{
	// These are used / set actively.
	public int globalKills = 0;


	// Not synced until save / load
	public float sensitivity = PlayerMove.sensitivity;
	public float volume = 1;
	public string[] binds = new string[4];
}
