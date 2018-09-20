using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class SoundHandler : MonoBehaviour {

	public static SoundHandler soundHandler = null;

	// VoiceLines:
	const byte HEALING_CALL1 = 0;
	const byte HEALING_CALL2 = 1;
	const byte HEALING_CALL_CANCER = 2;
	const byte HEALING_CALL_ENCOURAGING = 3;


	public const byte BEEN_HEALED = 4;
	public const byte GRUNT = 5;

	// This can be used for many things..
	public GameObject emptySoundGameObject;


	// In order of VoiceLines:
	public AudioClip[] voiceLines;

	public AudioClip[] deathSounds; // Same death sound plays every time for now for each class

	public AudioClip[] fireSounds;
	public AudioClip[] secondaryFireSounds;
	public AudioClip[] reloadSounds;

	public AudioClip healthPackTakenSound;

	// Use this for initialization
	void Start () {
		soundHandler = this;
	}

	public void PlayFireSound(Type type, Transform parent) {
		int index;
		if (type == typeof(ShotGun)) {
			index = 0;
		} else if (type == typeof(Pistol)) {
			index = 1;
		} else if (type == typeof(ProjectileGunScript)) {
			index = 2;
		} else if (type == typeof(MeeleeWeapon)) {
			index = 3;
		} else if (type == typeof(TakeHealthWeapon)) {
			index = 4;
		} else {
			return; // No sound
		}
		GameObject sound = (GameObject) Instantiate(emptySoundGameObject, parent);
		sound.transform.position = parent.position;
		sound.GetComponent<AudioSource>().clip = fireSounds[index];
		sound.GetComponent<AudioSource>().Play();
		Destroy(sound, 10.0f);
	}

	public void PlaySecondaryFireSound(Type type, Transform parent) {
		int index;
		if (type == typeof(TakeHealthWeapon)) {
			index = 0;
		} else {
			return;
		}
		GameObject sound = (GameObject) Instantiate(emptySoundGameObject, parent);
		sound.transform.position = parent.position;
		sound.GetComponent<AudioSource>().clip = secondaryFireSounds[index];
		sound.GetComponent<AudioSource>().Play();
		Destroy(sound, 10.0f);
	}

	public void PlayReloadSound(Type type, Transform parent) {
		int index;
		if (type == typeof(ShotGun)) {
			index = 0;
		} else {
			return; // No sound
		}
		GameObject sound = (GameObject) Instantiate(emptySoundGameObject, parent);
		sound.transform.position = parent.position;
		sound.GetComponent<AudioSource>().clip = reloadSounds[index];
		sound.GetComponent<AudioSource>().Play();
		Destroy(sound, 10.0f);
	}

	public void PlayDeathSound(int classNum, Transform parent) {
		GameObject sound = (GameObject) Instantiate(emptySoundGameObject, parent);
		sound.transform.position = parent.position;
		sound.GetComponent<AudioSource>().clip = deathSounds[classNum];
		sound.GetComponent<AudioSource>().Play();
		Destroy(sound, 10.0f);
	}
	
	public void PlayVoiceLine(byte clipNum, byte volume, Transform parent)
	{
		GameObject sound = (GameObject) Instantiate(emptySoundGameObject, parent);
		sound.transform.position = parent.position;
		sound.GetComponent<AudioSource> ().volume = volume / 255.0f;
		sound.GetComponent<AudioSource>().clip = voiceLines[clipNum];
		sound.GetComponent<AudioSource>().Play();
		Destroy(sound, 10.0f); // Assumed to be at the VERY least, shorter than 10 seconds.. Probably they all don't go past 5 seconds.
	}

	public void PlayHealthPackTakenSound(Transform parent) {
		GameObject sound = (GameObject) Instantiate(emptySoundGameObject, parent);
		sound.transform.position = parent.position;
		sound.GetComponent<AudioSource>().clip = healthPackTakenSound;
		sound.GetComponent<AudioSource>().Play();
		Destroy(sound, 10.0f);
	}
}
