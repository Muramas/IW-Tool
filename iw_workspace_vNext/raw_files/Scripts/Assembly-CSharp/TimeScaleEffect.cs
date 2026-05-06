using System.Globalization;
using UnityEngine;

public class TimeScaleEffect : EffectDiminishing
{
	public Effect effect;

	public VariableFloat scale;

	public BigNumber add;

	public BigNumber mult;

	public Variable parameter;

	private float baseScale;

	private float applyedScale;

	public TimeScaleEffect(float t)
	{
		effect = GameContext.GetEffect(EffectNames.Linear.ToString());
		scale = new VariableFloat(t);
		baseScale = t;
		parameter = null;
		add = 0.0;
		mult = 1.0;
		diminishing = 1f;
	}

	public override void Apply()
	{
		scale.SetValue(baseScale * GetEfficiency().Value.ToFloat());
		applyedScale = Time.timeScale + scale.ValueFloat;
		float num = GameContext.GetResource("Chrono.MaxDistortion").Value.ToFloat();
		if (applyedScale > num)
		{
			applyedScale = num;
		}
		Time.timeScale = applyedScale;
	}

	public override string Preview(string key = "")
	{
		scale.SetValue(baseScale * GetEfficiency().Value.ToFloat());
		return scale.ValueFloat.ToString("F2", CultureInfo.InvariantCulture);
	}
}
