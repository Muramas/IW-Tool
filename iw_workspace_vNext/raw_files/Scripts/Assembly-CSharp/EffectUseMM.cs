public class EffectUseMM : EffectDiminishing
{
	public Spell target;

	public int base_count;

	public BigNumber add;

	public float mult;

	public Variable parameter;

	protected double count;

	protected EffectAddProduction ep;

	public EffectUseMM(Spells key = Spells.MagicMissile, float dimisnish = 0f)
	{
		base_count = 1;
		target = GameManager.Instance.SpellBook.GetSpell(key);
		add = 0.0;
		mult = 1f;
		count = 0.0;
		pow_diminishing = dimisnish;
	}

	public override void Apply()
	{
		recalculate_count();
		if (ep == null)
		{
			ep = target.effects[0] as EffectAddProduction;
		}
		ep.k *= count;
		target.Apply();
		ep.k /= count;
		target.IncreaseUses(count);
		target.IncreaseUseThisRun(count);
		if (target.IsAccumulated)
		{
			GameManager.Instance.Scrolls.AccumCastCount.Change(count);
		}
		Statistic.CastSpell.Change(count);
		Statistic.Change(Statistic.CastSpellTotal, count);
		Statistic.Change(Statistic.CastSpellRealm, count);
		if (GameManager.Instance.Scrolls.OnCastAmount != null)
		{
			GameManager.Instance.Scrolls.OnCastAmount(target, count);
		}
	}

	protected void recalculate_count()
	{
		count = base_count;
		if (parameter != null)
		{
			count += (1.0 + parameter.Value / add).Pow(mult).ToDouble();
		}
		else
		{
			count += (1.0 + add).Pow(mult).ToDouble();
		}
		count = ApplyEfficiency(count).ToDouble();
	}

	public override string Preview(string key = "")
	{
		recalculate_count();
		string text = "";
		if (key == "t")
		{
			if (count < 1000000000.0)
			{
				return count.ToString("F0");
			}
			return new BigNumber(count).ToReadableString();
		}
		return ((target.effects[0] as EffectAddProduction).GetValue() * count).ToReadableString();
	}
}
