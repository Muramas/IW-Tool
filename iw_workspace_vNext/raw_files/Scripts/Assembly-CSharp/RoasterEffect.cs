using System;
using System.Collections.Generic;
using UnityEngine;

public class RoasterEffect : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem red;

	[SerializeField]
	private ParticleSystem green;

	[SerializeField]
	private ParticleSystem blue;

	[SerializeField]
	private ParticleSystem yellow;

	private float timer;

	private Dictionary<string, float> map;

	private void Awake()
	{
		SettingMenu settingMenu = GameManager.Instance.SettingMenu;
		settingMenu.OnParticleChange = (Action)Delegate.Combine(settingMenu.OnParticleChange, new Action(ChangeState));
	}

	private void OnEnable()
	{
		map = GameManager.Instance.Event.solsticeBonuses;
		ChangeState();
	}

	private void Update()
	{
		timer += Time.unscaledDeltaTime;
		if (timer > 0.2f)
		{
			timer = 0f;
			map = GameManager.Instance.Event.solsticeBonuses;
			ParticleSystem.MainModule main = red.main;
			main.startColor = new Color(main.startColor.color.r, main.startColor.color.g, main.startColor.color.b, GetAlpha(Solstice.Keys.dust));
			main = green.main;
			main.startColor = new Color(main.startColor.color.r, main.startColor.color.g, main.startColor.color.b, GetAlpha(Solstice.Keys.profit));
			main = blue.main;
			main.startColor = new Color(main.startColor.color.r, main.startColor.color.g, main.startColor.color.b, GetAlpha(Solstice.Keys.ench));
			main = yellow.main;
			main.startColor = new Color(main.startColor.color.r, main.startColor.color.g, main.startColor.color.b, GetAlpha(Solstice.Keys.splinter));
		}
	}

	private float GetAlpha(Solstice.Keys key)
	{
		if (map == null || !map.ContainsKey(key.ToString()))
		{
			return 0f;
		}
		return Mathf.Pow(Mathf.Clamp01(map[key.ToString()] / 2000f), 2f);
	}

	private void ChangeState()
	{
		SetActive(Settings.Particles);
	}

	private void SetActive(bool state)
	{
		red.gameObject.SetActive(state);
		green.gameObject.SetActive(state);
		blue.gameObject.SetActive(state);
		yellow.gameObject.SetActive(state);
		base.enabled = state;
	}
}
