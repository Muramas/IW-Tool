using UnityEngine;

public class EffectStabilizeTheFlow : EffectDiminishing
{
	private SimpleEffect e;

	private VariableBignumber v;

	public EffectStabilizeTheFlow(SimpleEffect effect, float pow_diminish = 1f)
	{
		e = effect;
		v = new VariableBignumber();
		pow_diminishing = pow_diminish;
	}

	public override void Apply()
	{
		v.SetValue(GetPower());
		Time.timeScale = 1f;
		e.parameter = v;
		e.Apply();
	}

	public override void Delete()
	{
		e.Delete();
		v.SetValue(0.0);
	}

	public override void Update()
	{
	}

	private BigNumber GetPower()
	{
		float timeScale = Time.timeScale;
		BigNumber t = new BigNumber(timeScale).Pow(1f + timeScale / 10f);
		return ApplyEfficiency(t);
	}

	public override string Preview(string key = "")
	{
		if (e.IsActive)
		{
			return e.Preview(key);
		}
		if (v.Value == 0.0)
		{
			VariableBignumber parameter = new VariableBignumber(GetPower());
			e.parameter = parameter;
		}
		return e.Preview(key);
	}
}
