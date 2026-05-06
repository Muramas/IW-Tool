public class RelentlessnessEffect : EffectDiminishing
{
	private BigNumber speed;

	private bool applyed;

	private Variable chargeSpeed;

	private BigNumber applied_speed;

	public RelentlessnessEffect(BigNumber _speed)
	{
		speed = _speed;
		pow_diminishing = 0.5f;
	}

	public override void Apply()
	{
		if (applyed)
		{
			return;
		}
		HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
		if (nameKey != HeroesNames.Exorcist && nameKey != HeroesNames.Heretic)
		{
			return;
		}
		applyed = true;
		if (chargeSpeed == null)
		{
			chargeSpeed = GameContext.GetResource(ResourceType.Exorcist.ToString() + ".ChargeSpeed");
		}
		if (chargeSpeed != null)
		{
			applied_speed = speed;
			if (efficiency != null)
			{
				applied_speed = ApplyEfficiency(applied_speed);
			}
			chargeSpeed.Change(0.0, applied_speed);
		}
	}

	public override void Update()
	{
		if (applyed)
		{
			Delete();
			Apply();
		}
	}

	public override void Delete()
	{
		if (applyed)
		{
			if (chargeSpeed != null)
			{
				chargeSpeed.Change(0.0, 1.0 / applied_speed);
			}
			applyed = false;
		}
	}

	public override string Preview(string key = "")
	{
		BigNumber bigNumber = speed;
		if (efficiency != null)
		{
			bigNumber = ApplyEfficiency(bigNumber);
		}
		return ((bigNumber - 1.0) * 100.0).ToReadableString() + "%";
	}
}
