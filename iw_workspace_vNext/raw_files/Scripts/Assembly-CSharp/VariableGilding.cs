public class VariableGilding : VariableComplex
{
	public override BigNumber Value => _value + add * mult;

	public VariableGilding(int v)
		: base(v)
	{
	}
}
