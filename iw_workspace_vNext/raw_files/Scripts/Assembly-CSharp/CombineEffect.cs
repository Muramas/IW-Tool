using System.Collections.Generic;

public class CombineEffect : IEffect
{
	public Variable target;

	public List<SimpleEffect> simple_effects;

	private List<SimpleEffect> with_efficienty;

	private Variable efficiency;

	private Variable gilding;

	public CombineEffect(Variable _target)
	{
		target = _target;
		simple_effects = new List<SimpleEffect>();
		with_efficienty = new List<SimpleEffect>();
	}

	public void AddEffect(BigNumber a, BigNumber m, Variable w = null, Effect effect = null, bool eff = true, float dimishing = 1f, float pow_diminish = 1f)
	{
		SimpleEffect simpleEffect = new SimpleEffect();
		simpleEffect.target = target;
		simpleEffect.add = a;
		simpleEffect.mult = m;
		simpleEffect.parameter = w;
		simpleEffect.diminishing = dimishing;
		simpleEffect.pow_diminishing = pow_diminish;
		if (effect == null)
		{
			simpleEffect.effect = GameContext.GetEffect(EffectNames.Linear.ToString());
		}
		else
		{
			simpleEffect.effect = effect;
		}
		simple_effects.Add(simpleEffect);
		if (eff)
		{
			with_efficienty.Add(simpleEffect);
		}
	}

	public void SetEfficiency(Variable eff)
	{
		efficiency = eff;
		foreach (SimpleEffect item in with_efficienty)
		{
			item.SetEfficiency(efficiency);
		}
	}

	public void SetGilding(Variable eff)
	{
		gilding = eff;
		foreach (SimpleEffect item in with_efficienty)
		{
			item.SetGilding(gilding);
		}
	}

	public void Apply()
	{
		foreach (SimpleEffect simple_effect in simple_effects)
		{
			simple_effect.Apply();
		}
	}

	public void Delete()
	{
		foreach (SimpleEffect simple_effect in simple_effects)
		{
			simple_effect.Delete();
		}
	}

	public void Update()
	{
		Delete();
		Apply();
	}

	public string Preview(string key = "")
	{
		BigNumber bigNumber = 0.0;
		BigNumber bigNumber2 = 1.0;
		foreach (SimpleEffect simple_effect in simple_effects)
		{
			string[] array = simple_effect.effect.preview(simple_effect.add, simple_effect.mult, simple_effect.parameter, with_efficienty.Contains(simple_effect) ? simple_effect.GetEfficiency() : null, asnumber: true).Split('@');
			bigNumber += new BigNumber(array[0]);
			bigNumber2 *= new BigNumber(array[1]);
		}
		string text = "";
		if (key == "")
		{
			if (bigNumber != 0.0)
			{
				text += bigNumber.ToReadableString();
			}
			if (bigNumber2 != 1.0)
			{
				text += ((bigNumber2 - 1.0) * 100.0).ToReadableString();
				text += "%";
			}
		}
		if (key == "a")
		{
			text = bigNumber.ToReadableString();
		}
		if (key == "m")
		{
			text += ((bigNumber2 - 1.0) * 100.0).ToReadableString();
		}
		if (key == "t")
		{
			text = (bigNumber * 100.0).ToReadableString() + "%";
		}
		return text;
	}
}
