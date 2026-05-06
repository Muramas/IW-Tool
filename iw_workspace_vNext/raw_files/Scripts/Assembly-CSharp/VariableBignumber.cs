public class VariableBignumber : Variable
{
	protected BigNumber _value;

	public override BigNumber Value => _value;

	public VariableBignumber()
	{
		_value = new BigNumber(0.0);
	}

	public VariableBignumber(string v)
	{
		_value = new BigNumber(v);
	}

	public VariableBignumber(BigNumber v)
	{
		_value = v;
	}

	public override void SetValue(BigNumber v)
	{
		_value = v;
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public void Reset(BigNumber v)
	{
		_value = v;
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public override void Change(BigNumber addendum, BigNumber multiplier)
	{
		BigNumber value = _value;
		_value = (_value + addendum) * multiplier;
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeAdd != null)
		{
			OnChangeAdd(_value - value);
		}
	}

	public void Change(BigNumber addendum)
	{
		_value += addendum;
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeAdd != null)
		{
			OnChangeAdd(addendum);
		}
	}

	public VariableBignumber Clone()
	{
		return new VariableBignumber(_value);
	}
}
