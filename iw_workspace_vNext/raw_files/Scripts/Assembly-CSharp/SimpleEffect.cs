using UnityEngine;

public class SimpleEffect : EffectDiminishing
{
	public Effect effect;

	public Variable target;

	public BigNumber add;

	public BigNumber mult;

	public Variable parameter;

	protected BigNumber applied_add;

	protected BigNumber applied_mult;

	protected VariableBignumber prev_w;

	protected bool applied;

	public bool IsActive => applied;

	public BigNumber GetAdd()
	{
		return applied_add;
	}

	public SimpleEffect()
	{
		effect = GameContext.GetEffect(EffectNames.Linear.ToString());
		target = (parameter = null);
		add = 0.0;
		mult = 1.0;
		diminishing = 1f;
	}

	public SimpleEffect(Variable t)
	{
		effect = GameContext.GetEffect(EffectNames.Linear.ToString());
		target = t;
		parameter = null;
		add = 0.0;
		mult = 1.0;
		diminishing = 1f;
	}

	public SimpleEffect(Variable t, BigNumber a, BigNumber m, EffectNames effName = EffectNames.Linear, float pow_diminish = 1f)
	{
		effect = GameContext.GetEffect(effName.ToString());
		target = t;
		parameter = null;
		add = a;
		mult = m;
		diminishing = 1f;
		pow_diminishing = pow_diminish;
	}

	public SimpleEffect(Variable t, BigNumber a, BigNumber m, Variable param, EffectNames effName = EffectNames.Linear, float pow_diminish = 1f)
	{
		effect = GameContext.GetEffect(effName.ToString());
		target = t;
		parameter = param;
		add = a;
		mult = m;
		diminishing = 1f;
		pow_diminishing = pow_diminish;
	}

	public SimpleEffect(Variable t, BigNumber a, BigNumber m, Variable param, Variable efficiency, EffectNames effName = EffectNames.Linear, float pow_diminish = 1f)
	{
		effect = GameContext.GetEffect(effName.ToString());
		target = t;
		parameter = param;
		add = a;
		mult = m;
		diminishing = 1f;
		pow_diminishing = pow_diminish;
		SetEfficiency(efficiency);
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
		effect.apply(target, applied_add, applied_mult, parameter, prev_e);
		applied = true;
	}

	public override void Delete()
	{
		if (applied)
		{
			effect.delete(target, applied_add, applied_mult, prev_w, prev_e);
			applied = false;
		}
	}

	public override void Update()
	{
		if (applied && (!(prev_e.Value == GetEfficiency().Value) || (prev_w != null && !(prev_w.Value == parameter.Value)) || !(applied_add == add) || !(applied_mult == mult)))
		{
			Delete();
			Apply();
		}
	}

	public string GetParameterDebugString()
	{
		if (parameter == null)
		{
			return "parameter=null";
		}
		string text = ((prev_w != null) ? prev_w.Value.ToReadableString() : "null");
		string text2 = parameter.Value.ToReadableString();
		return "prev_w=" + text + " parameter.Value=" + text2 + " match=" + (prev_w != null && prev_w.Value == parameter.Value);
	}

	public string GetDebugCheck()
	{
		string text = "";
		text = ((prev_e == null) ? (text + "e is null / ") : (text + prev_e.Value.ToReadableString() + " " + GetEfficiency().Value.ToReadableString() + " " + (prev_e.Value == GetEfficiency().Value) + " / "));
		text = ((prev_w == null) ? (text + "w is null / ") : (text + prev_w.Value.ToReadableString() + " " + parameter.Value.ToReadableString() + " " + (prev_w.Value == parameter.Value) + " / "));
		return text + applied_add.ToReadableString() + " " + add.ToReadableString() + " " + (applied_add == add) + " / " + applied_mult.ToReadableString() + " " + mult.ToReadableString() + " " + (applied_mult == mult);
	}

	public string Preview(BigNumber a, BigNumber m, Variable parameter)
	{
		GetEfficiency();
		return effect.preview(add, mult, parameter, preview_eff);
	}

	public string Preview(string key, bool isIgnoreEff)
	{
		if (isIgnoreEff)
		{
			return effect.preview(add, mult, parameter);
		}
		return Preview(key);
	}

	public override string Preview(string key = "")
	{
		string result = string.Empty;
		if (key == "clean")
		{
			return effect.preview(add, mult, parameter);
		}
		GetEfficiency();
		switch (key)
		{
		case "":
			result = effect.preview(add, mult, parameter, preview_eff);
			break;
		case "t":
			result = ((mult * preview_eff.Value - 1.0) * 100.0).ToReadableString() + "%";
			break;
		case "k":
			result = (new BigNumber(effect.preview(add, mult, parameter, preview_eff, asnumber: true).Split('@')[0]) * 100.0).ToReadableString() + "%";
			break;
		case "p":
			if (parameter != null)
			{
				result = parameter.Value.ToReadableString("F0");
			}
			break;
		case "q":
		{
			string[] array3 = effect.preview(add, mult, parameter, preview_eff, asnumber: true).Split("@");
			BigNumber bigNumber4 = array3[0];
			result = ((!(bigNumber4 >= 1.0)) ? ((((BigNumber)array3[1] - (BigNumber)1.0) * 100.0).ToReadableString() + "%") : (((bigNumber4 - 1.0) * 100.0).ToReadableString() + "%"));
			break;
		}
		case "c":
			result = BigNumberExt.ToReadableString(effect.preview(add, mult, parameter, preview_eff, asnumber: true).Split("@")[0]) + "%";
			break;
		case "%":
		{
			string[] array2 = effect.preview(add, mult, parameter, preview_eff, asnumber: true).Split("@");
			BigNumber bigNumber = array2[0];
			BigNumber bigNumber2 = array2[1];
			result = ((!(bigNumber != 0.0)) ? ((bigNumber2 * 100.0).ToReadableString() + "%") : ((!(bigNumber < 0.0)) ? ((bigNumber * 100.0).ToReadableString() + "%") : ((bigNumber * 100.0).Abs().ToReadableString() + "%")));
			break;
		}
		case "@":
		{
			BigNumber bigNumber3 = mult * preview_eff.Value;
			result = ((!(bigNumber3 < 1.0)) ? (((bigNumber3 - 1.0) * 100.0).ToReadableString() + "%") : (((1.0 - bigNumber3) * 100.0).ToReadableString() + "%"));
			break;
		}
		case "e":
			return ((preview_eff.Value - 1.0) * 100.0).ToReadableString() + "%";
		case "&":
			result = (mult * preview_eff.Value * 100.0).ToReadableString() + "%";
			break;
		case "int":
			result = (add * preview_eff.Value).ToInt().ToString("F0");
			break;
		default:
		{
			string[] array = effect.preview(add, mult, parameter, preview_eff, asnumber: true).Split('@');
			if (key == "a")
			{
				result = new BigNumber(array[0]).ToReadableString("0.##");
			}
			if (key == "m")
			{
				result = ((new BigNumber(array[1]) - 1.0) * 100.0).ToReadableString("0.00#");
			}
			if (key == "w")
			{
				result = new BigNumber(array[1]).ToReadableString("0.00#");
			}
			break;
		}
		}
		return result;
	}
}
