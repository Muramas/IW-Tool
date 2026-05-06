public class EffectAddShadows : EffectInstant, IOfflineEffect
{
	private ShadowEnergyManager shadows;

	public EffectAddShadows(float t, BigNumber _a, BigNumber _m, Variable _w = null, float diminish = 1f)
	{
		time = t;
		a = _a;
		m = _m;
		w = _w;
		diminishing = diminish;
	}

	public override void Apply()
	{
		CheckShadow();
		if (!(shadows == null))
		{
			shadows.AddShadow(ApplyW(time) * shadows.IncomeMod.Value);
		}
	}

	public void Offline(BigNumber casts)
	{
		CheckShadow();
		if (!(shadows == null))
		{
			shadows.AddShadow(ApplyW(time) * shadows.IncomeMod.Value * casts);
		}
	}

	public override string Preview(string key = "")
	{
		CheckShadow();
		if (shadows == null)
		{
			ApplyW(time).ToReadableString("F0");
		}
		return (ApplyW(time) * shadows.IncomeMod.Value).ToReadableString("F0");
	}

	private void CheckShadow()
	{
		if (shadows == null && ShadowEnergyManager.instance != null && ShadowEnergyManager.instance.isActivated())
		{
			shadows = ShadowEnergyManager.instance;
		}
	}
}
