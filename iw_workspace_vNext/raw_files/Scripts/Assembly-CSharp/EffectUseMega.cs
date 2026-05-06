using UnityEngine;

public class EffectUseMega : EffectDiminishing, IOfflineEffect
{
	public float uses = 1f;

	public float k = 1f;

	private MegaClick mega;

	public EffectUseMega(float t, float dimish = 1f)
	{
		uses = t;
		pow_diminishing = dimish;
	}

	public override void Apply()
	{
		if (mega == null)
		{
			mega = (GameManager.Instance.CurrentHero.HeroPanel.HeroMap[HeroesNames.Exorcist] as Exorcist).mc;
		}
		if (mega != null)
		{
			BigNumber bigNumber = uses * k;
			bigNumber *= GetEfficiency().Value;
			mega.Performed.Change(bigNumber);
			Statistic.Change(Statistic.HCTotal, bigNumber);
			bigNumber *= GameManager.Instance.Orb.click_profit.Value;
			if (Random.Range(0f, 100f) < GameManager.Instance.Orb.GetCritChange)
			{
				bigNumber *= 1.0 + GameManager.Instance.Orb.crit_profit.Value;
			}
			GameManager.Instance.ManaChange(bigNumber * mega.megaProfit.Value);
		}
	}

	public void Offline(BigNumber casts)
	{
		k = casts.ToFloat();
		Apply();
		k = 1f;
	}

	public override string Preview(string key = "")
	{
		BigNumber bigNumber = uses * k;
		bigNumber *= GetEfficiency().Value;
		if (key == "a")
		{
			if (mega != null)
			{
				return (bigNumber * GameManager.Instance.Orb.click_profit.Value * mega.megaProfit.Value).ToReadableString();
			}
			return "";
		}
		return bigNumber.ToReadableString("F0");
	}
}
