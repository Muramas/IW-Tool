public class VariableIntVector : VariableInt
{
	protected int add;

	protected int mult;

	public override int ValueInt => (_value + add) * mult;

	public VariableIntVector(int v)
		: base(v)
	{
		add = 0;
		mult = 1;
	}

	public override void Change(int ad, int m = 1, bool isCallback = true)
	{
		int valueInt = ValueInt;
		add += ad;
		mult *= m;
		if (isCallback)
		{
			if (OnChange != null)
			{
				OnChange();
			}
			if (OnChangeInt != null)
			{
				OnChangeInt(ValueInt - valueInt);
			}
			if (OnChangeAdd != null && ad != 0)
			{
				OnChangeAdd(ad);
			}
		}
	}

	public string debug()
	{
		return _value + " " + add + " " + mult;
	}
}
