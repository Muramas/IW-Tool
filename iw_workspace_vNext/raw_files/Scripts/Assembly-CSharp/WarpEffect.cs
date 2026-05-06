public class WarpEffect : EffectDiminishing, IOfflineEffect
{
	public Effect effect;

	public VariableBignumber time;

	public BigNumber add;

	public BigNumber mult;

	public Variable parameter;

	public BigNumber bonus;

	public BigNumber k = 1.0;

	public bool StopSpells = true;

	private int baseTime = 1;

	public WarpEffect(int t, bool stopSpells = true)
	{
		effect = GameContext.GetEffect(EffectNames.Linear.ToString());
		time = new VariableBignumber(t);
		baseTime = t;
		parameter = null;
		add = 0.0;
		mult = 1.0;
		diminishing = 1f;
		bonus = 1.0;
		StopSpells = stopSpells;
	}

	public override void Apply()
	{
		time.SetValue(baseTime);
		effect.apply(time, add, mult, parameter, GetEfficiency());
		GameManager.Instance.SkipTime(time.Value * bonus * k, insec: true, real: false, showMessage: false, tw: false, StopSpells);
	}

	public void Offline(BigNumber casts)
	{
		time.SetValue(baseTime);
		effect.apply(time, add, mult, parameter, GetEfficiency());
		GameManager.Instance.SkipTime(time.Value * bonus * k * casts, insec: true, real: false, showMessage: false, tw: false, StopSpells);
	}

	public override string Preview(string key = "")
	{
		time.SetValue(baseTime);
		effect.apply(time, add, mult, parameter, GetEfficiency());
		return Statistic.time_to_string(time.Value * bonus * k);
	}
}
