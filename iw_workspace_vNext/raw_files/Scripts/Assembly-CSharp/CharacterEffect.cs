using System;

[Serializable]
public class CharacterEffect
{
	public float based;

	public Variable target;

	public VariableInt level;

	protected BigNumber applied_mult;

	private VariableBignumber prev_w;

	private bool applyed;

	private bool isDump;

	public CharacterEffect(float b, Variable t, VariableInt l)
	{
		based = b - 1f;
		target = t;
		level = l;
		applied_mult = 1.0;
	}

	public void SetIsDump(bool isDump)
	{
		this.isDump = isDump;
	}

	public void Start()
	{
		if (!applyed)
		{
			Apply();
			VariableInt variableInt = level;
			variableInt.OnChange = (Action)Delegate.Combine(variableInt.OnChange, new Action(Update));
		}
	}

	public void Stop()
	{
		if (applyed)
		{
			Delete();
			VariableInt variableInt = level;
			variableInt.OnChange = (Action)Delegate.Combine(variableInt.OnChange, new Action(Update));
		}
	}

	public void Apply()
	{
		if (!applyed)
		{
			apply();
			applyed = true;
		}
	}

	public BigNumber GetMainBonus()
	{
		if (!isDump)
		{
			return based * GameManager.Instance.AttributeManager.AttributePower.Value;
		}
		return GameManager.Instance.AttributeManager.VersatilityPower.ApplyModOnVar(based);
	}

	private void apply()
	{
		applied_mult = (1.0 + GetMainBonus()).Pow(level.ValueInt);
		target.Change(0.0, applied_mult);
	}

	public void Delete()
	{
		if (applyed)
		{
			target.Change(0.0, 1.0 / applied_mult);
			applyed = false;
		}
	}

	public void Update()
	{
		if (applyed)
		{
			Delete();
			Apply();
		}
	}

	public string Preview()
	{
		if (Settings.ColoredTips)
		{
			return "<color=#e2b018>" + ((applied_mult - 1.0) * 100.0).ToReadableString() + "%</color>";
		}
		return ((applied_mult - 1.0) * 100.0).ToReadableString() + "%";
	}
}
