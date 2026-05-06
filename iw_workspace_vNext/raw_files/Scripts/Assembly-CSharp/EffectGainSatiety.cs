public class EffectGainSatiety : EffectDiminishing
{
	private BigNumber amount;

	private Variable target;

	private Variable income;

	public EffectGainSatiety(int amount = 1)
	{
		this.amount = amount;
	}

	public override void Apply()
	{
		if (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Shapeshifter)
		{
			if (target == null)
			{
				target = GameManager.Instance.CurrentHero.ClassBonusStacks;
			}
			if (income == null)
			{
				income = GameContext.GetResource("Shapeshifter.Income");
			}
			if (target != null && income != null)
			{
				BigNumber addendum = ApplyEfficiency(amount) * income.Value;
				target.Change(addendum, 1.0);
			}
		}
	}

	public override string Preview(string key = "")
	{
		if (target == null)
		{
			target = GameManager.Instance.CurrentHero.ClassBonusStacks;
		}
		if (income == null)
		{
			income = GameContext.GetResource("Shapeshifter.Income");
		}
		if (target == null || income == null)
		{
			return string.Empty;
		}
		return (ApplyEfficiency(amount) * income.Value).ToReadableString();
	}
}
