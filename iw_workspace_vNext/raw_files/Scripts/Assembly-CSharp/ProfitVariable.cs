public class ProfitVariable : VariableComplex
{
	public ProfitVariable(BigNumber v)
		: base(v)
	{
	}

	public ProfitVariable(string v)
		: base(v)
	{
	}

	public override void Change(BigNumber addendum, BigNumber multiplier)
	{
		base.Change(addendum, multiplier);
	}

	public new void SetAdd(BigNumber a)
	{
		base.SetAdd(a);
	}

	public new void SetMult(BigNumber m)
	{
		base.SetMult(m);
	}

	public void DumpRemaining()
	{
	}

	public void ResetTracking()
	{
	}
}
