using System;

public class VariableComplex : Variable
{
	protected BigNumber _value;

	public BigNumber add;

	public BigNumber mult;

	public Action<BigNumber, BigNumber> OnChangeVector;

	public BigNumber GetInternalValue => _value;

	public float ValueFloat => Value.ToFloat();

	public override BigNumber Value => (_value + add) * mult;

	public void ChangeValue(BigNumber a, BigNumber m)
	{
		_value += a;
		_value *= m;
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeAdd != null)
		{
			OnChangeAdd(a);
		}
		if (OnChangeVector != null)
		{
			OnChangeVector(a, m);
		}
	}

	public override void SetValue(BigNumber v)
	{
		_value = v;
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public string debug()
	{
		return _value.CheckAndReturn().ToReadableString() + " " + add.CheckAndReturn().ToReadableString() + " " + mult.CheckAndReturn().ToReadableString("F4");
	}

	public void Reset(BigNumber _v)
	{
		_value = _v;
		add = 0.0;
		mult = 1.0;
	}

	public void Reset()
	{
		_value = 0.0;
		add = 0.0;
		mult = 1.0;
	}

	public VariableComplex(string v)
	{
		_value = new BigNumber(v);
		add = 0.0;
		mult = 1.0;
	}

	public VariableComplex(BigNumber v)
	{
		_value = v;
		add = 0.0;
		mult = 1.0;
	}

	public override void Change(BigNumber addendum, BigNumber multiplier)
	{
		add += addendum;
		mult *= multiplier;
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeAdd != null)
		{
			OnChangeAdd(addendum);
		}
		if (OnChangeVector != null)
		{
			OnChangeVector(addendum, multiplier);
		}
	}

	public void SetAdd(BigNumber a)
	{
		add = a;
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeVector != null)
		{
			OnChangeVector(a, 1.0);
		}
	}

	public void SetMult(BigNumber m)
	{
		mult = m;
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeVector != null)
		{
			OnChangeVector(0.0, m);
		}
	}

	public BigNumber ApplyModOnVar(BigNumber target)
	{
		return (target + add) * mult;
	}

	public BigNumber ApplyComponents(BigNumber component)
	{
		return (component + _value + add) * mult;
	}

	public VariableComplex Clone()
	{
		return new VariableComplex(_value)
		{
			add = add,
			mult = mult
		};
	}
}
