public abstract class EffectDiminishing : IEffect
{
	public Variable efficiency;

	public Variable gilding;

	public float diminishing = 1f;

	public float pow_diminishing = 1f;

	protected VariableBignumber preview_eff;

	protected VariableBignumber prev_e;

	public abstract void Apply();

	public virtual void Delete()
	{
	}

	public virtual void Update()
	{
	}

	public void SetEfficiency(Variable eff = null)
	{
		if (diminishing != 0f)
		{
			efficiency = eff;
		}
	}

	public void SetGilding(Variable eff = null)
	{
		if (diminishing != 0f)
		{
			gilding = eff;
		}
	}

	public void RecalculateEff()
	{
		prev_e = new VariableBignumber(1.0);
		if (efficiency != null || gilding != null)
		{
			prev_e.SetValue(GetEfficiency().Value);
		}
	}

	public virtual VariableBignumber GetEfficiency()
	{
		if (preview_eff == null)
		{
			preview_eff = new VariableBignumber(1.0);
		}
		BigNumber bigNumber = 1.0;
		if (gilding != null)
		{
			bigNumber = gilding.Value;
		}
		if (efficiency != null)
		{
			bigNumber *= efficiency.Value;
		}
		if (diminishing != 1f)
		{
			preview_eff.SetValue(1.0 + (double)diminishing * (1.0 + bigNumber).Log10());
		}
		else if (pow_diminishing != 1f)
		{
			preview_eff.SetValue(bigNumber.Pow(pow_diminishing));
		}
		else
		{
			preview_eff.SetValue(bigNumber);
		}
		return preview_eff;
	}

	public BigNumber ApplyEfficiency(BigNumber t)
	{
		if (efficiency != null || gilding != null)
		{
			t *= GetEfficiency().Value;
		}
		return t;
	}

	public abstract string Preview(string key);
}
