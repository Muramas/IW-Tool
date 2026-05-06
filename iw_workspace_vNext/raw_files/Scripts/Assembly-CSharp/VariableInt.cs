using System;

public class VariableInt : Variable
{
	protected int _value;

	public Action<int> OnChangeInt;

	public override BigNumber Value => new BigNumber(_value);

	public virtual int ValueInt
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

	public VariableInt(int v)
	{
		_value = v;
	}

	public void Reset(int v)
	{
		_value = v;
	}

	public void SetValue(int v)
	{
		if (OnChangeAdd != null)
		{
			OnChangeAdd(MathF.Max(0f, v - _value));
		}
		_value = v;
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public virtual void Change(int ad, int m = 1, bool isCallback = true)
	{
		int value = _value;
		_value = (_value + ad) * m;
		if (isCallback)
		{
			if (OnChange != null)
			{
				OnChange();
			}
			if (OnChangeInt != null)
			{
				OnChangeInt(_value - value);
			}
			if (OnChangeAdd != null)
			{
				OnChangeAdd(_value - value);
			}
		}
	}

	public override void Change(BigNumber addendum, BigNumber multiplier)
	{
		int ad = addendum.ToInt();
		int m = multiplier.ToInt();
		Change(ad, m);
	}
}
