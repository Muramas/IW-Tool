public class EffectOrnaments : EffectDiminishing
{
	private int bonus;

	private bool applyed;

	private Variable hc;

	public EffectOrnaments()
	{
		hc = GameContext.GetResource("Exorcist.MaxCharges");
	}

	public override void Apply()
	{
		if (!applyed)
		{
			bonus = getBonus();
			hc.Change(bonus, 1.0);
			applyed = true;
		}
	}

	public override void Delete()
	{
		if (applyed)
		{
			hc.Change(-bonus, 1.0);
			applyed = false;
		}
	}

	private int getBonus()
	{
		return (int)((GameManager.Instance.VoidMana.Value + 1.0).Log10() + GameManager.Instance.Scrolls.IncantationEfficiency.Value.Log10());
	}

	public override string Preview(string key = "")
	{
		if (applyed)
		{
			return bonus.ToString();
		}
		return getBonus().ToString();
	}
}
