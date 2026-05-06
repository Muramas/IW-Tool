public class AffixData
{
	public string Id;

	public string Target;

	public float Min;

	public float Max;

	public float Diminish;

	public string Description;

	public string Int;

	public bool IsInt()
	{
		return !string.IsNullOrEmpty(Int);
	}

	public BigNumber GetMinValue(Variable efficiency, Variable gilding, bool isInt)
	{
		BigNumber bigNumber = 1.0;
		if (gilding != null)
		{
			bigNumber = gilding.Value;
		}
		if (efficiency != null)
		{
			bigNumber *= efficiency.Value;
		}
		if (Diminish != 1f)
		{
			bigNumber = bigNumber.Pow(Diminish);
		}
		if (isInt)
		{
			return Min * bigNumber;
		}
		return 1.0 + (Min - 1f) * bigNumber;
	}

	public BigNumber GetMaxValue(Variable efficiency, Variable gilding, bool isInt)
	{
		BigNumber bigNumber = 1.0;
		if (gilding != null)
		{
			bigNumber = gilding.Value;
		}
		if (efficiency != null)
		{
			bigNumber *= efficiency.Value;
		}
		if (Diminish != 1f)
		{
			bigNumber = bigNumber.Pow(Diminish);
		}
		if (isInt)
		{
			return Max * bigNumber;
		}
		return 1.0 + (Max - 1f) * bigNumber;
	}
}
