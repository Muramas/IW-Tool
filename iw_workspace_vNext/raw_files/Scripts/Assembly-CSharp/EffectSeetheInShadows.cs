public class EffectSeetheInShadows : EffectDiminishing
{
	public BigNumber a;

	public BigNumber m;

	public BigNumber aShadows;

	public BigNumber mShadows;

	private ShadowEnergyManager shadows;

	private Variable param;

	public EffectSeetheInShadows(BigNumber _a, BigNumber _m, BigNumber _a2, BigNumber _m2)
	{
		a = _a;
		m = _m;
		aShadows = _a2;
		mShadows = _m2;
		param = GameContext.GetResource("Building.1.Level");
	}

	public override void Apply()
	{
		GameManager.Instance.ManaChange(GetProfitModOnClick() * GameManager.Instance.Orb.click_profit.Value * GameManager.Instance.Orb.autoclick_profit.Value);
	}

	public override string Preview(string key = "")
	{
		BigNumber profitModOnClick = GetProfitModOnClick();
		if (key == "")
		{
			profitModOnClick *= GameManager.Instance.Orb.click_profit.Value * GameManager.Instance.Orb.autoclick_profit.Value;
		}
		return profitModOnClick.ToReadableString("F0");
	}

	private BigNumber GetProfitModOnClick()
	{
		if (shadows == null && ShadowEnergyManager.instance != null && ShadowEnergyManager.instance.isActivated())
		{
			shadows = ShadowEnergyManager.instance;
		}
		if (shadows == null)
		{
			return 0.0;
		}
		BigNumber t = 20.0 * (1.0 + a * param.Value.Pow(m.ToDouble())) * (1.0 + aShadows * shadows.ShadowEnergy.Value.Pow(mShadows.ToDouble()));
		return ApplyEfficiency(t);
	}
}
