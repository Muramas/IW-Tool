using System.Collections.Generic;
using UnityEngine;

public class EffectJMS : EffectDiminishing
{
	public Effect e;

	public float c;

	public float a;

	public float m;

	public float k;

	private Variable param;

	public EffectJMS(float _a, float _m, float _c, float _k, Spells key)
	{
		a = _a;
		m = _m;
		c = _c;
		k = _k;
		if (key != Spells.None)
		{
			param = GameManager.Instance.SpellBook.GetSpell(key).Use;
		}
		else
		{
			param = null;
		}
	}

	public override void Apply()
	{
	}

	public void UpdateTimes(int times)
	{
		List<Scroll> list = GameManager.Instance.Scrolls.Scrolls.FindAll((Scroll x) => x.spell != null && x.spell.ResetUses);
		if (list.Count > 0)
		{
			double casts = GetChargeProgress() * (double)times;
			list[Random.Range(0, list.Count)].IncreaseFakeCasts(casts);
		}
	}

	public override void Update()
	{
		UpdateTimes(1);
	}

	public override string Preview(string key = "")
	{
		return BigNumberExt.ToReadableString(GetChargeProgress(), "F0");
	}

	private double GetChargeProgress()
	{
		BigNumber bigNumber = ApplyEfficiency(param.Value);
		return (double)c + (double)k * (a * bigNumber).Pow(m).ToDouble();
	}
}
