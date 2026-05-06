using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectBuyManaSource : EffectDiminishing
{
	public BigNumber Count;

	public float Frequency;

	public Variable Parameter;

	public Action OnApply;

	public float timer;

	public float scale;

	public EffectBuyManaSource(float count, float frequency = 0f, Variable W = null, float scale = 0f)
	{
		Count = count;
		Frequency = frequency;
		Parameter = W;
		this.scale = scale;
	}

	public override void Apply()
	{
		timer = 0f;
	}

	public override void Update()
	{
		if (Frequency != 0f)
		{
			timer += Time.deltaTime;
			if (timer > 1f / Frequency)
			{
				int num = (int)(timer * Frequency);
				buySource(num);
				timer -= (float)num / Frequency;
			}
		}
	}

	protected void buySource(int times)
	{
		List<BuildingVisual> list = new List<BuildingVisual>();
		list.AddRange(GameManager.Instance.Buildings);
		GameManager.Instance.BuildingManager.UpBuilding(times, null, list, getManaCap(), null);
	}

	protected BigNumber getManaCap()
	{
		BigNumber result = 1.0;
		if (Parameter != null)
		{
			result = (1.0 + Parameter.Value).Pow(scale);
		}
		return result;
	}

	public override string Preview(string key = "")
	{
		return (Statistic.ManaRealm.Value * getManaCap()).ToReadableString();
	}
}
