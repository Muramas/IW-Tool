public class SpellShards
{
	public BigNumber Pool;

	public VariableComplex Capacity;

	public VariableComplex Efficiency;

	public CounterAverageBigNumber Average;

	public SpellShards()
	{
		Capacity = new VariableComplex("1e3");
		Efficiency = new VariableComplex(1.0);
		Average = new CounterAverageBigNumber();
	}

	public void Add(BigNumber shards)
	{
		shards *= Efficiency.Value;
		Average.Add(shards);
		Insert(shards);
	}

	public void Insert(BigNumber shards)
	{
		Pool += shards;
		if (Pool > Capacity.Value)
		{
			Pool = Capacity.Value;
		}
	}

	public BigNumber GetShards()
	{
		if (Pool < 1.0)
		{
			return 0.0;
		}
		BigNumber bigNumber = Pool * 0.10000000149011612;
		if (bigNumber < 1.0)
		{
			bigNumber = 1.0;
		}
		Pool -= bigNumber;
		return bigNumber;
	}

	public float GetFillPercent()
	{
		return (Pool / Capacity.Value).ToFloat();
	}
}
