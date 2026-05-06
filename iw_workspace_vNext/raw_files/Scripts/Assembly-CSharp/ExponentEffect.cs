using UnityEngine;

public class ExponentEffect : EffectDiminishing
{
	public Effect effect;

	public Variable target;

	public BigNumber add;

	public BigNumber mult;

	public int level;

	protected BigNumber applied_mult;

	protected VariableBignumber prev_w;

	protected bool applyed;

	public ExponentEffect(Variable t, BigNumber a, BigNumber m)
	{
		effect = GameContext.GetEffect(EffectNames.Linear.ToString());
		target = t;
		add = a;
		mult = m;
		diminishing = 1f;
	}

	public void ChangeLevel(int lvl)
	{
		level = lvl;
		Update();
	}

	public override void Apply()
	{
		if (applyed || level == 0)
		{
			return;
		}
		if (target == null)
		{
			Debug.Log("target is null");
			Debug.Log(add);
			Debug.Log(mult);
			return;
		}
		if (add != 0.0)
		{
			applied_mult = 1.0 + add * level;
		}
		else
		{
			applied_mult = mult.Pow(level);
		}
		if (effect == null)
		{
			Debug.Log("effect null");
		}
		effect.apply(target, 0.0, applied_mult, null);
		applyed = true;
	}

	public override void Delete()
	{
		if (applyed)
		{
			effect.delete(target, 0.0, applied_mult, null);
			applyed = false;
		}
	}

	public override void Update()
	{
		Delete();
		Apply();
	}

	public override string Preview(string key = "t")
	{
		if (key == string.Empty)
		{
			if (add != 0.0)
			{
				return (add * 100.0).ToReadableString("F0") + "%";
			}
			return ((mult - 1.0) * 100.0).ToReadableString("F0") + "%";
		}
		BigNumber bigNumber = ((!(add != 0.0)) ? (mult.Pow(level) - 1.0) : (add * level));
		return (bigNumber * 100.0).ToReadableString() + "%";
	}
}
