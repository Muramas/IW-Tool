using UnityEngine;

public class EffectAddProduction : EffectInstant
{
	public double k = 1.0;

	public float critChance;

	public float critProfit = 1f;

	private bool refreshPps;

	public EffectAddProduction(float t)
	{
		time = t;
		a = 0.0;
		m = 1.0;
		k = 1.0;
		w = null;
	}

	public EffectAddProduction(float t, float pow_diminish)
	{
		time = t;
		a = 0.0;
		m = 1.0;
		k = 1.0;
		w = null;
		pow_diminishing = pow_diminish;
	}

	public EffectAddProduction(float t, BigNumber _a, BigNumber _m, Variable _w = null, float crit = 0f, float crit_profit = 1f, bool refreshPps = false)
	{
		time = t;
		a = _a;
		m = _m;
		k = 1.0;
		w = _w;
		critChance = crit;
		critProfit = crit_profit;
		this.refreshPps = refreshPps;
	}

	public override void Apply()
	{
		if (refreshPps)
		{
			GameManager.Instance.AddProfit(0f);
		}
		GameManager.Instance.ManaChange(GetValue());
	}

	public virtual BigNumber GetValue()
	{
		BigNumber bigNumber = ApplyW(time) * k;
		if (critChance > 0f && Random.Range(0f, 1f) < critChance * GameManager.Instance.Orb.GetCritChange / 100f)
		{
			bigNumber *= 1.0 + critProfit * GameManager.Instance.Orb.crit_profit.Value;
		}
		return GameManager.Instance.PPS.Value * bigNumber;
	}

	public override string Preview(string key = "")
	{
		BigNumber bigNumber = ApplyW(time) * k;
		if (key == "t")
		{
			if (bigNumber > 1.8446744073709552E+19)
			{
				return bigNumber.ToReadableString("F0") + " " + "sec".Translate();
			}
			return Statistic.time_to_string(bigNumber);
		}
		if (key == "m")
		{
			if (critChance > 0f)
			{
				bigNumber *= critProfit * GameManager.Instance.Orb.crit_profit.Value;
			}
			return (GameManager.Instance.PPS.Value * bigNumber).ToReadableString("F0");
		}
		return "+" + (GameManager.Instance.PPS.Value * bigNumber).ToReadableString("F0");
	}
}
