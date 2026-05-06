public class EffectAutoclickVariable : IEffect
{
	public float add;

	public float mult;

	public Variable parameter;

	public EffectAutoClick autoclick;

	private VariableFloat frequence;

	private bool log;

	private bool fromSpell;

	public EffectAutoclickVariable(float a, float m, Variable param, EffectAutoClick clicks, bool useLog = false, bool fromSpell = false)
	{
		frequence = new VariableFloat(clicks.Frequency);
		add = a;
		mult = m;
		parameter = param;
		autoclick = clicks;
		autoclick.Frequency = getClicks();
		log = useLog;
		this.fromSpell = fromSpell;
	}

	public void Apply()
	{
		autoclick.Frequency = getClicks();
		autoclick.Apply();
	}

	public void SetEfficiency(Variable eff = null)
	{
		autoclick.SetEfficiency(eff);
	}

	public void SetGilding(Variable eff = null)
	{
		autoclick.SetGilding(eff);
	}

	public void Delete()
	{
		autoclick.Delete();
	}

	public void Update()
	{
		autoclick.Frequency = getClicks();
		autoclick.Update();
	}

	public string Preview(string key = "")
	{
		return new BigNumber(getClicks()).ToReadableString("0.##");
	}

	protected float getClicks()
	{
		BigNumber value = add;
		if (log)
		{
			value += (BigNumber)((double)mult * (parameter.Value + 1.0).Log10());
		}
		else
		{
			value += mult * parameter.Value;
		}
		if (fromSpell)
		{
			value *= (BigNumber)GameManager.Instance.Orb.autoclicksFromSpell.ValueFloat;
		}
		if (autoclick.gilding != null)
		{
			value *= (BigNumber)autoclick.gilding.Value.Pow(0.10000000149011612).ToFloat();
		}
		frequence.SetValue(value);
		return frequence.ValueFloat;
	}
}
