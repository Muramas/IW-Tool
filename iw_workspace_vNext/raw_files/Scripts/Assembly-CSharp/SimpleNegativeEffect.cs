using UnityEngine;

public class SimpleNegativeEffect : SimpleEffect
{
	public SimpleNegativeEffect(Variable t, BigNumber a, BigNumber m, Variable param, EffectNames effName = EffectNames.Linear, float pow_diminish = 1f)
		: base(t, a, m, param, effName, pow_diminish)
	{
	}

	public override void Apply()
	{
		if (applied)
		{
			return;
		}
		if (target == null)
		{
			Debug.Log("target is null");
			Debug.Log(add);
			Debug.Log(mult);
			Debug.Log(parameter);
			if (parameter != null)
			{
				Debug.Log(parameter.Value);
			}
			return;
		}
		if (parameter != null)
		{
			if (prev_w == null)
			{
				prev_w = new VariableBignumber(parameter.Value);
			}
			else
			{
				prev_w.SetValue(parameter.Value);
			}
		}
		applied_add = add;
		applied_mult = mult;
		RecalculateEff();
		if (effect == null)
		{
			Debug.Log("effect null");
		}
		effect.delete(target, applied_add, applied_mult, parameter, prev_e);
		applied = true;
	}

	public override void Delete()
	{
		if (applied)
		{
			effect.apply(target, applied_add, applied_mult, prev_w, prev_e);
			applied = false;
		}
	}
}
