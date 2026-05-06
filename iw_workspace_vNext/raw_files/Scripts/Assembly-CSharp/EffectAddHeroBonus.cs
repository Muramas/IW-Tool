public class EffectAddHeroBonus : EffectInstant
{
	public EffectAddHeroBonus(float t, BigNumber _a, BigNumber _m, Variable _w = null)
	{
		time = t;
		a = _a;
		m = _m;
		w = _w;
	}

	public override void Apply()
	{
		BigNumber bigNumber = ApplyW(time);
		GameManager.Instance.CurrentHero.ClassBonusStacks.Change(bigNumber);
		Statistic.Change(Statistic.CTTotal, bigNumber);
	}

	public override string Preview(string key = "")
	{
		return ApplyW(time).ToReadableString("F0");
	}
}
