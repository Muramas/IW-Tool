public class BloodToPetXP : EffectDiminishing
{
	protected float part;

	protected BigNumber xpRate;

	public BloodToPetXP(float part, float xpRate, float pow_diminish)
	{
		this.part = part;
		this.xpRate = xpRate;
		pow_diminishing = pow_diminish;
	}

	public override void Apply()
	{
		if (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Nosferatu && GameManager.Instance.CurrentPet.Pet != null)
		{
			BigNumber bonus = GetBonus();
			Nosferatu obj = GameManager.Instance.CurrentHero.Hero as Nosferatu;
			obj.ConsumeBlood(obj.GetBlood() * part);
			GameManager.Instance.CurrentPet.Pet.AddExpConst(bonus);
		}
	}

	protected virtual BigNumber GetBonus()
	{
		return GameManager.Instance.CurrentPet.ExpBonus.ApplyModOnVar(ApplyEfficiency(GameManager.Instance.CurrentHero.ClassBonusStacks.Value * part * xpRate));
	}

	public override string Preview(string key = "")
	{
		return GetBonus().ToReadableString();
	}
}
