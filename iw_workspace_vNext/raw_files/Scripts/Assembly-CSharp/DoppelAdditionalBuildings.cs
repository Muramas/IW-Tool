public class DoppelAdditionalBuildings : DoppelAbility
{
	private EffectAddTemporaryBuildings effect;

	public DoppelAdditionalBuildings(int lvl, getValue updateValue, EffectAddTemporaryBuildings effect)
		: base(lvl, updateValue)
	{
		this.effect = effect;
	}

	public override void Apply()
	{
		if (!active)
		{
			effect.a = get();
			effect.Apply();
			active = true;
		}
	}

	public override void Delete()
	{
		if (active)
		{
			effect.Delete();
			active = false;
		}
	}

	public override void Update()
	{
		if (active)
		{
			BigNumber bigNumber = get();
			if (effect.a != bigNumber)
			{
				effect.a = get();
				effect.Delete();
				effect.Apply();
			}
		}
	}
}
