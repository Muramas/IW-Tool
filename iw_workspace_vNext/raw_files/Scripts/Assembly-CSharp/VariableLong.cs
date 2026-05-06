using System;

public class VariableLong : Variable
{
	protected ulong _value;

	public Action<int> OnChangeInt;

	public override BigNumber Value => new BigNumber(_value);

	public ulong ValueInt
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
		}
	}

	public VariableLong(ulong v)
	{
		_value = v;
	}

	public void SetValue(ulong v)
	{
		_value = v;
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public void Change(int ad)
	{
		if (_value > (ulong)(-1 - (uint)ad))
		{
			_value = ulong.MaxValue;
		}
		else
		{
			_value += (uint)ad;
		}
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeInt != null)
		{
			OnChangeInt(ad);
		}
		if (OnChangeAdd != null)
		{
			OnChangeAdd(ad);
		}
	}

	public void Change(ulong ad)
	{
		if (_value > (ulong)(-1L - (long)ad))
		{
			_value = ulong.MaxValue;
		}
		else
		{
			_value += ad;
		}
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeAdd != null)
		{
			OnChangeAdd(ad);
		}
		if (OnChangeInt != null)
		{
			OnChangeInt((int)ad);
		}
	}

	public void Decrease(int ad)
	{
		if (_value < (uint)ad)
		{
			_value = 0uL;
		}
		else
		{
			_value -= (uint)ad;
		}
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeInt != null)
		{
			OnChangeInt(-ad);
		}
		if (OnChangeAdd != null)
		{
			OnChangeAdd(-ad);
		}
	}

	public void Decrease(ulong ad)
	{
		_value -= ad;
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeAdd != null)
		{
			OnChangeAdd(-new BigNumber(ad));
		}
	}

	public void Change(int ad, int m = 1)
	{
		ulong value = _value;
		_value = (_value + (uint)ad) * (uint)m;
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeInt != null)
		{
			OnChangeInt((int)(_value - value));
		}
		if (OnChangeAdd != null)
		{
			OnChangeAdd(new BigNumber(_value) - new BigNumber(value));
		}
	}

	public override void Change(BigNumber addendum, BigNumber multiplier)
	{
		int ad = (int)addendum.ToDouble();
		int m = (int)multiplier.ToDouble();
		Change(ad, m);
	}
}
