public class PowerOfSacrificeEffect : EffectDiminishing
{
	protected BigNumber per_mega;

	protected VariableBignumber target;

	protected BigNumber bonus;

	private bool isActive;

	public PowerOfSacrificeEffect(float bonus_per_mega)
	{
		per_mega = bonus_per_mega;
	}

	public override void Apply()
	{
		if (!isActive)
		{
			if (target == null)
			{
				target = GameContext.GetResource(ResourceType.Exorcist.ToString() + ".Charges") as VariableBignumber;
			}
			if (target != null)
			{
				bonus = GetBonus();
				GameManager.Instance.Orb.click_profit.Change(0.0, bonus);
				target.SetValue(0.0);
			}
			isActive = true;
		}
	}

	protected virtual BigNumber GetBonus()
	{
		return 1.0 + ApplyEfficiency(per_mega.Pow(target.Value.ToInt()));
	}

	public override void Delete()
	{
		if (isActive)
		{
			GameManager.Instance.Orb.click_profit.Change(0.0, 1.0 / bonus);
			isActive = false;
		}
	}

	public override string Preview(string key = "")
	{
		if (target == null)
		{
			target = GameContext.GetResource(ResourceType.Exorcist.ToString() + ".Charges") as VariableBignumber;
		}
		BigNumber bigNumber = (isActive ? (bonus - 1.0) : (GetBonus() - 1.0));
		return (bigNumber * 100.0).ToReadableString() + "%";
	}
}
