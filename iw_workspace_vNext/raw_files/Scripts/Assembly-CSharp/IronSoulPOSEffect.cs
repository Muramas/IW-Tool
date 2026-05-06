public class IronSoulPOSEffect : PowerOfSacrificeEffect
{
	public IronSoulPOSEffect(float bonus_per_mega)
		: base(bonus_per_mega)
	{
	}

	protected override BigNumber GetBonus()
	{
		return 1.0 + ApplyEfficiency(target.Value.Pow(per_mega.ToFloat()));
	}
}
