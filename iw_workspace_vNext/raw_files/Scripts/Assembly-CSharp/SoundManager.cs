using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public enum ModeList
	{
		MainGame = 0,
		CardGame = 1,
		All = 10
	}

	public static SoundManager Instance;

	[SerializeField]
	private AudioSource Music;

	[SerializeField]
	private AudioClip mainTheme;

	[SerializeField]
	private SoundSource SoundSource;

	[SerializeField]
	private List<SoundSource> pool;

	public Action EventSoundChange;

	public ModeList Mode { get; private set; }

	public void Init()
	{
		Instance = this;
		Mode = ModeList.MainGame;
	}

	public void OnChangeMusicVolume()
	{
		float musicVolume = Settings.MusicVolume;
		Music.volume = musicVolume;
	}

	public void OnChangeSoundVolume()
	{
		if (EventSoundChange != null)
		{
			EventSoundChange();
		}
	}

	public void MusicPlay()
	{
		if (!Settings.Mute && !Music.isPlaying)
		{
			Music.Play();
		}
	}

	public void MusicStop()
	{
		if (Music.isPlaying)
		{
			Music.Stop();
		}
	}

	public void PlayMainTheme()
	{
		PlayMusicTrack(mainTheme);
	}

	public void PlayMusicTrack(AudioClip clip)
	{
		if (!Settings.Mute)
		{
			StopAllCoroutines();
			StartCoroutine(musicSwitch(clip));
		}
	}

	public void PlayBonusSpawn(AudioClip clip, float volume = 1f)
	{
		if (!Settings.Mute && Settings.VoidSFX && Settings.SoundVolume != 0f && Mode == ModeList.MainGame)
		{
			GetSource().Play(clip, volume);
		}
	}

	public void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f, ModeList mode = ModeList.MainGame)
	{
		if (!Settings.Mute && Settings.SoundOn && Settings.SoundVolume != 0f && (mode == ModeList.All || Mode == mode))
		{
			GetSource().Play(clip, volume, pitch);
		}
	}

	public void PlaySound(AudioSource source, float volume = 1f, ModeList mode = ModeList.MainGame)
	{
		if (!Settings.Mute && Settings.SoundOn && Settings.SoundVolume != 0f && (mode == ModeList.All || Mode == mode))
		{
			volume *= Settings.SoundVolume;
			source.volume = volume;
			source.Play();
		}
	}

	public void Deactivate(SoundSource source)
	{
		source.gameObject.SetActive(value: false);
		pool.Add(source);
	}

	public void ChangeMode(ModeList mode)
	{
		Mode = mode;
	}

	private SoundSource GetSource()
	{
		SoundSource result;
		if (pool.Count > 0)
		{
			result = pool[0];
			pool.RemoveAt(0);
		}
		else
		{
			result = UnityEngine.Object.Instantiate(SoundSource, base.transform);
		}
		return result;
	}

	private IEnumerator musicSwitch(AudioClip clip)
	{
		if (!Settings.MusicOn || Settings.Mute)
		{
			yield return null;
			Music.clip = clip;
			yield break;
		}
		float dt = 0.5f;
		float timer = 0.5f;
		while (dt > 0f)
		{
			dt -= Time.unscaledDeltaTime;
			if (dt < 0f)
			{
				dt = 0f;
			}
			float volume = Settings.MusicVolume * dt / timer;
			Music.volume = volume;
			yield return null;
		}
		Music.clip = clip;
		Music.Play();
		while (dt < timer)
		{
			dt += Time.unscaledDeltaTime;
			if (dt > timer)
			{
				dt = timer;
			}
			float volume2 = Settings.MusicVolume * dt / timer;
			Music.volume = volume2;
			yield return null;
		}
	}
}
