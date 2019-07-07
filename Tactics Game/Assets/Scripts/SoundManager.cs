using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour {

	// De SoundManager class regelt de achtergrond geluiden binnen het spel, waaronder achtergrond geluid en volumes.

	private AudioSource [] backgroundMusicSource;
	private int activeMusicSourceIndex;

	[Tooltip ("soundFadeSpeed is de snelheid waarmee geluid fade (hoger getal betekend kortere fade in/out tijd).")]
	public float soundFadeTime = 1f;

	private float masterVolume = .4f;                               //The master volume.
	private float effectsVolume = 1;                                //The volume used for sound effects.
	private float musicVolume = .2f;                                //The volume used for the background music.

	private bool currentASource;                                    //Saves which of the two AudioSources is currently being used. 


	void Awake () {
		//Spawning of the two music AudioSources
		backgroundMusicSource = new AudioSource [2];
		for (int i = 0; i < 2; i++) {
			GameObject newMusicSource = new GameObject ("MusicSource" + (i + 1));
			backgroundMusicSource [i] = newMusicSource.AddComponent<AudioSource> ();
			backgroundMusicSource [i].loop = true;
			backgroundMusicSource [i].mute = true;
			backgroundMusicSource [i].volume = 0;
			backgroundMusicSource [i].playOnAwake = false;
			newMusicSource.transform.parent = transform;
		}
	}

	public void ChangeSpecificVolume (float volume, int channel) {
		if (channel == 0)
			ChangeVolume (volume, effectsVolume, musicVolume);
		if (channel == 1)
			ChangeVolume (masterVolume, volume, musicVolume);
		if (channel == 2)
			ChangeVolume (masterVolume, effectsVolume, volume);
	}

	public void ChangeVolume (float master, float effects, float music) {
		masterVolume = master;
		effectsVolume = effects;
		musicVolume = music;

		if ((masterVolume == 0 && musicVolume == 0) && backgroundMusicSource [0].volume != 0) {
			MuteAudioSource (backgroundMusicSource [0], 1);
			MuteAudioSource (backgroundMusicSource [1], 1);
		}
		else if ((masterVolume != 0 && musicVolume != 0) && backgroundMusicSource [0].volume == 0) {
			ResumeAudioSource (backgroundMusicSource [0], 1);
		}
		else if (backgroundMusicSource [0].volume != masterVolume * musicVolume) {
			backgroundMusicSource [0].volume = masterVolume * musicVolume;
			backgroundMusicSource [1].volume = masterVolume * musicVolume;
		}
	}

	//MuteAudioSource mutes a specific AudioSource.
	public void MuteAudioSource (AudioSource a, float fadeDuration = 1) {
		StartCoroutine (FadeSound (a, fadeDuration));
	}

	//ResumeMusic causes the currently active audioSource to start playing its sound again.
	public void ResumeAudioSource (AudioSource a, float fadeDuration = 1) {
		StartCoroutine (StartSound (a, fadeDuration));
	}

	//StartMusic takes an AudioSource and plays it, while slowly increasing the volume to the desired amount.
	IEnumerator StartSound (AudioSource a, float fadeDuration) {
		float percent = 0;
		float fadeSpeed = 1 / fadeDuration;

		a.mute = false;
		a.Play ();

		while (a.volume < masterVolume * musicVolume) {
			percent += Time.deltaTime * fadeSpeed;
			a.volume = percent;

			yield return null;
		}

		a.volume = masterVolume * musicVolume;
	}

	//FadeMusic takes an AudioSource and stops it from playing, after slowly decreasing the volume to zero.
	IEnumerator FadeSound (AudioSource a, float fadeDuration) {
		float percent = 0;
		float fadeSpeed = 1 / fadeDuration;

		while (a.volume > 0) {
			percent -= Time.deltaTime * fadeSpeed;
			a.volume = percent;

			yield return null;
		}

		a.volume = 0;
		a.Stop ();
	}

	public void PlaySound (AudioClip clip, Vector3 pos) {
		if (clip != null) {
			AudioSource.PlayClipAtPoint (clip, pos, masterVolume * effectsVolume);
		}
	}

	//SwapBackgroundSound takes an AudioClip and plays this in one of the two AudioSources, while the other is being faded out.
	public void PlayBackgroundSound (AudioClip clip, float fadeDuration) {
		MuteAudioSource (backgroundMusicSource [activeMusicSourceIndex], 1);
		activeMusicSourceIndex = 1 - activeMusicSourceIndex;

		backgroundMusicSource [activeMusicSourceIndex].clip = clip;
		ResumeAudioSource (backgroundMusicSource [activeMusicSourceIndex], 1);
		backgroundMusicSource [activeMusicSourceIndex].Play ();
	}

}
