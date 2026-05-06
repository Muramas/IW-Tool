using UnityEngine;

public class HolyFervorEffect : EffectDiminishing, IOfflineEffect
{
	private int per_cast;

	private int per_crit;

	private float crit_chance;

	private VariableBignumber target;

	private VariableComplex parameter;

	private BigNumber k;

	public HolyFervorEffect(int cast, int critprofit, float chance, VariableComplex variable)
	{
		per_cast = cast;
		per_crit = critprofit;
		crit_chance = chance;
		diminishing = 1.25f;
		parameter = variable;
		k = 1.0;
	}

	public override void Apply()
	{
		HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
		if (nameKey != HeroesNames.Exorcist && nameKey != HeroesNames.Heretic)
		{
			return;
		}
		if (target == null)
		{
			target = GameContext.GetResource(ResourceType.Exorcist.ToString() + ".Charges") as VariableBignumber;
		}
		if (target != null)
		{
			BigNumber bigNumber = ApplyEfficiency(crit_chance);
			VariableBignumber variableBignumber = GameContext.GetResource(ResourceType.Exorcist.ToString() + ".MaxCharges") as VariableBignumber;
			BigNumber bigNumber2 = 0.0;
			bigNumber2 = ((!(bigNumber >= Random.Range(0f, 100f))) ? ((BigNumber)per_cast) : ((BigNumber)per_crit));
			bigNumber2 *= k;
			if (parameter != null)
			{
				bigNumber2 = (bigNumber2 * (1.0 + parameter.Value / 100000.0).Pow(0.4000000059604645)).ToInt();
			}
			if (target.Value + bigNumber2 > variableBignumber.Value)
			{
				GameManager.Instance.CurrentHero.ClassBonusStacks.Change(target.Value + bigNumber2 - variableBignumber.Value);
				target.SetValue(variableBignumber.Value);
			}
			else
			{
				target.Change(bigNumber2);
			}
		}
	}

	public void Offline(BigNumber casts)
	{
		k = casts;
		Apply();
		k = 1.0;
	}

	public override string Preview(string key = "")
	{
		if (key == "a")
		{
			return (per_cast * (1.0 + parameter.Value / 100000.0).Pow(0.4000000059604645)).ToInt().ToString();
		}
		if (key == "m")
		{
			return (per_crit * (1.0 + parameter.Value / 100000.0).Pow(0.4000000059604645)).ToInt().ToString();
		}
		BigNumber bigNumber = ApplyEfficiency(crit_chance);
		if (bigNumber > 100.0)
		{
			bigNumber = 100.0;
		}
		return bigNumber.ToReadableString() + "%";
	}
}
