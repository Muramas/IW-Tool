public class EffectAddOfflineProduction : EffectInstant
{
	public float diminishing_param;

	public EffectAddOfflineProduction(float t)
	{
		time = t;
		a = 0.0;
		m = 1.0;
		w = null;
	}

	public EffectAddOfflineProduction(float t, BigNumber _a, BigNumber _m, Variable _w = null, float diminish = 1f, float diminish_p = 1f)
	{
		time = t;
		a = _a;
		m = _m;
		w = _w;
		diminishing = diminish;
		diminishing_param = diminish_p;
	}

	public override void Apply()
	{
		BigNumber bigNumber = ApplyW(time);
		BigNumber bigNumber2 = GameManager.Instance.OfflineProduction.Value;
		if (diminishing_param != 1f)
		{
			bigNumber2 = bigNumber2.Pow(diminishing_param);
		}
		bigNumber *= bigNumber2;
		GameManager.Instance.ManaChange(GameManager.Instance.PPS.Value * bigNumber);
	}

	public override string Preview(string key = "")
	{
		BigNumber bigNumber = ApplyW(time);
		if (key == "t")
		{
			if (bigNumber > 1.8446744073709552E+19)
			{
				return bigNumber.ToReadableString("F0");
			}
			return Statistic.time_to_string(bigNumber);
		}
		BigNumber bigNumber2 = GameManager.Instance.OfflineProduction.Value;
		if (diminishing_param != 1f)
		{
			bigNumber2 = bigNumber2.Pow(diminishing_param);
		}
		bigNumber *= bigNumber2;
		return "+" + (GameManager.Instance.PPS.Value * bigNumber).ToReadableString("F0");
	}
}
