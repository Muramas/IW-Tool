using System.Collections.Generic;

public class EffectAddShardInstant : EffectInstant
{
	private List<Spells> except;

	public EffectAddShardInstant(BigNumber _v, List<Spells> _exept = null)
	{
		time = _v.ToFloat();
		a = 0.0;
		m = 1.0;
		w = null;
		except = _exept;
	}

	public EffectAddShardInstant(BigNumber _v, BigNumber _a, BigNumber _m, Variable _w = null, List<Spells> _exept = null, float diminish = 1f)
	{
		time = _v.ToFloat();
		a = _a;
		m = _m;
		w = _w;
		except = _exept;
		diminishing = diminish;
	}

	public override void Apply()
	{
		BigNumber bigNumber = ApplyW(time);
		if (except != null)
		{
			if (bigNumber > 1.7014117331926443E+38)
			{
				bigNumber = 1.7014117331926443E+38;
			}
			GameManager.Instance.Scrolls.AddShardsRandom(bigNumber.ToFloat(), except);
		}
		else
		{
			GameManager.Instance.Scrolls.ShardsPool.Add(bigNumber);
		}
	}

	public override string Preview(string key = "")
	{
		BigNumber number = ApplyW(time);
		return "+" + number.ToReadableString("F0");
	}
}
