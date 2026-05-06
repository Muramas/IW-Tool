using System;
using UnityEngine;

public class EffectDancingFlame : EffectAutoClick
{
	public BigNumber Shadows;

	private float scale;

	public EffectDancingFlame(float frequency, float profit = 1f, float chance = 1f, float crit_profit = 1f, bool crit_const = false, float diminishing = 1f, Action _onClick = null, float shadows = 0f, bool fromSpell = true, float effScale = 0f)
		: base(frequency, profit, chance, crit_profit, crit_const, diminishing, fromSpell, _onClick)
	{
		Shadows = shadows;
		scale = effScale;
	}

	public override void Update()
	{
		if (Frequency == 0f)
		{
			return;
		}
		timer += Time.deltaTime;
		float clicks = getClicks();
		if (timer > 1f / clicks)
		{
			int num = (int)(timer * clicks);
			for (int i = 0; i < num; i++)
			{
				click();
			}
			ShadowEnergyManager.instance.AddShadow(Shadows * ShadowEnergyManager.instance.IncomeMod.Value * num * GetEfficiency().Value.Pow(scale));
			timer -= (float)num / clicks;
		}
	}

	public override string Preview(string key = "")
	{
		if (key == string.Empty)
		{
			return (Shadows * ShadowEnergyManager.instance.IncomeMod.Value * GetEfficiency().Value.Pow(scale)).ToReadableString();
		}
		return new BigNumber(getClicks()).ToReadableString("0.##");
	}
}
