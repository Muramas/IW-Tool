public class VariableFloat : Variable
{
	protected float _value;

	public override BigNumber Value => new BigNumber(_value);

	public float ValueFloat
	{
		get
		{
			return _value;
		}
		private set
		{
			_value = value;
		}
	}

	public VariableFloat(float v)
	{
		_value = v;
	}

	public override void SetValue(BigNumber value)
	{
		SetValue(value.ToFloat());
	}

	public void SetValue(float v)
	{
		_value = v;
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public void Change(float ad)
	{
		_value += ad;
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeAdd != null)
		{
			OnChangeAdd(ad);
		}
	}

	public void Change(float ad, float m = 1f)
	{
		float value = _value;
		_value = (_value + ad) * m;
		if (OnChange != null)
		{
			OnChange();
		}
		if (OnChangeAdd != null)
		{
			OnChangeAdd(_value - value);
		}
	}

	public override void Change(BigNumber addendum, BigNumber multiplier)
	{
		float ad = (float)addendum.ToDouble();
		float m = (float)multiplier.ToDouble();
		Change(ad, m);
	}
}
