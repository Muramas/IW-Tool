public class TradeVoidToShardsEffect : IEffect
{
	public float mod;

	public float k;

	private SimpleEffect e;

	private EffectAddShardPeriodic esp;

	private VariableBignumber v;

	private Variable efficiency;

	private Variable gilding;

	private float rate;

	public BigNumber reap;

	private bool activate;

	public TradeVoidToShardsEffect(float _mod, float _k, float ratio = 0f)
	{
		v = new VariableBignumber();
		mod = _mod;
		k = _k;
		rate = ratio;
		esp = new EffectAddShardPeriodic((float)v.Value.ToDouble(), 1f);
	}

	public void Apply()
	{
		BigNumber value = GameManager.Instance.VoidMana.Value;
		if (!(value < 1.0))
		{
			value = value.Pow(mod);
			reap = GameManager.Instance.VoidMana.Value - value;
			BigNumber value2 = efficiency.Value;
			if (gilding != null)
			{
				value2 *= gilding.Value;
			}
			v.SetValue(reap.Pow(k) * (1.0 + value2.Pow(rate)));
			GameManager.Instance.VoidMana.SetValue(value);
			esp.time = v.Value.ToFloat();
			esp.Apply();
			activate = true;
		}
	}

	public void SetEfficiency(Variable eff = null)
	{
		efficiency = eff;
	}

	public void SetGilding(Variable eff = null)
	{
		gilding = eff;
	}

	public void Delete()
	{
		esp.Delete();
		activate = false;
	}

	public void Update()
	{
		if (activate)
		{
			esp.Update();
		}
	}

	public string Preview(string key = "")
	{
		BigNumber bigNumber = GameManager.Instance.VoidMana.Value.Pow(mod);
		if (key == "t")
		{
			return bigNumber.ToReadableString();
		}
		if (activate)
		{
			return v.Value.ToReadableString();
		}
		BigNumber bigNumber2 = GameManager.Instance.VoidMana.Value - bigNumber;
		if (bigNumber2 < 1.0)
		{
			v.SetValue(0.0);
		}
		else
		{
			BigNumber value = efficiency.Value;
			if (gilding != null)
			{
				value *= gilding.Value;
			}
			v.SetValue(bigNumber2.Pow(k) * (1.0 + value.Pow(rate)));
		}
		return v.Value.ToReadableString();
	}
}
