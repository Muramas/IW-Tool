using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResourceBank : MonoBehaviour
{
	public CraftResource Key;

	public SpriteRenderer LowSprite;

	public SpriteRenderer FullSptire;

	public TextMeshPro Label;

	public ParticleSystem particleH;

	public ParticleSystem particleV;

	[SerializeField]
	private AudioClip gather;

	[SerializeField]
	private AudioClip full;

	[SerializeField]
	private float sfxVolume = 0.25f;

	[SerializeField]
	private SpriteRenderer diminish;

	[SerializeField]
	private List<ParticleSystem> gatherer;

	private VariableFloat resource;

	private Color initH;

	private Color initV;

	private int prev_value = -1;

	private bool inited;

	private bool off;

	private void Init()
	{
		initV = particleV.main.startColor.color;
		initH = particleH.main.startColor.color;
		inited = true;
		resource = GameManager.Instance.Resources.map[Key];
		GameManager instance = GameManager.Instance;
		instance.OnPostLoad = (Action)Delegate.Combine(instance.OnPostLoad, new Action(OnLoad));
		VariableBignumber cap = GameManager.Instance.Resources.Cap;
		cap.OnChange = (Action)Delegate.Combine(cap.OnChange, new Action(UpdateDiminish));
	}

	public void SetStateGatherer(bool state)
	{
		foreach (ParticleSystem item in gatherer)
		{
			item.gameObject.SetActive(state);
		}
		UpdateGathererParticles(Settings.Particles);
	}

	private void Update()
	{
		if (!inited)
		{
			Init();
		}
		else if (GameManager.Instance.Paragon.ItemsIsAvailable)
		{
			if (off)
			{
				Empty();
			}
			if (prev_value != GetResource())
			{
				UpdateVisual();
			}
		}
		else if (!off)
		{
			Off();
		}
	}

	public void OnLoad()
	{
		if (!inited)
		{
			Init();
		}
		prev_value = 0;
		UpdateVisual();
	}

	public int GetResource()
	{
		return Mathf.FloorToInt(resource.ValueFloat);
	}

	public void Take(int amount)
	{
		if (GameManager.Instance.Paragon.ItemsIsAvailable && amount > 0 && !(resource.ValueFloat < 1f))
		{
			if ((float)amount > resource.ValueFloat)
			{
				amount = Mathf.FloorToInt(resource.ValueFloat);
			}
			resource.Change(-amount);
			GameManager.Instance.Craft.Gathering.AddExp((float)amount / 1000f);
			GameManager.Instance.Resources.AddResource(Key, amount);
		}
	}

	public bool IsFull()
	{
		return (float)GetResource() >= GameManager.Instance.Resources.MaxProgress.ValueFloat;
	}

	public void UpdateDiminish()
	{
		float num = GameManager.Instance.Resources.GetDiminish(Key);
		if (num < 1f)
		{
			diminish.enabled = true;
			diminish.color = new Color(1f, 1f, 1f, 0.25f + (1f - num) * 0.75f);
		}
		else
		{
			diminish.enabled = false;
		}
	}

	public void UpdateVisual()
	{
		int num = GetResource();
		if (num != prev_value)
		{
			UpdateDiminish();
			Label.text = num.ToString("F0");
			Label.enabled = num > 0;
			off = num == 0;
			prev_value = num;
			bool flag = IsFull();
			LowSprite.enabled = num >= 1 && !flag;
			FullSptire.enabled = flag;
			if (Settings.Particles)
			{
				float num2 = 0.1f + 0.9f * ((float)num / GameManager.Instance.Resources.MaxProgress.ValueFloat);
				ParticleSystem.MainModule main = particleV.main;
				main.startColor = new ParticleSystem.MinMaxGradient(new Color(initV.r, initV.g, initV.b, initV.a * num2));
				main = particleH.main;
				main.startColor = new ParticleSystem.MinMaxGradient(new Color(initH.r, initH.g, initH.b, initH.a * num2));
			}
		}
	}

	public void ParticlesTurnOn(bool on)
	{
		if (!inited)
		{
			Init();
		}
		if (on)
		{
			particleH.Play();
			particleV.Play();
			UpdateVisual();
		}
		else
		{
			particleH.Stop();
			particleV.Stop();
		}
		UpdateGathererParticles(on);
	}

	private void UpdateGathererParticles(bool on)
	{
		if (on)
		{
			foreach (ParticleSystem item in gatherer)
			{
				item.Play();
			}
			return;
		}
		foreach (ParticleSystem item2 in gatherer)
		{
			item2.Stop();
		}
	}

	public void ResetJar()
	{
		Empty();
	}

	public void Off()
	{
		UpdateVisual();
		StopAllCoroutines();
		FullSptire.enabled = false;
		LowSprite.enabled = false;
		particleH.gameObject.SetActive(value: false);
		particleV.gameObject.SetActive(value: false);
		off = true;
	}

	private void OnMouseDown()
	{
		Collect();
		SoundManager.Instance.PlaySound(gather, sfxVolume);
	}

	public void Collect()
	{
		GameManager.Instance.Resources.AddResources(Key);
		Empty();
	}

	public void Empty()
	{
		particleH.gameObject.SetActive(value: true);
		particleV.gameObject.SetActive(value: true);
		off = false;
		Label.enabled = false;
		LowSprite.enabled = false;
		FullSptire.enabled = false;
		if (resource != null)
		{
			resource.SetValue(0f);
		}
	}
}
