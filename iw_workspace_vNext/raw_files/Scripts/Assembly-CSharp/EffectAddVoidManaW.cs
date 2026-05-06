public class EffectAddVoidManaW : EffectAddVoidMana
{
	public BigNumber A;

	public float M;

	public Variable W;

	public EffectAddVoidManaW(float count, BigNumber a, float m, Variable W, float frequency = 0f, float dimish = 1f, float pow_dimish = 1f)
		: base(count, frequency)
	{
		this.W = W;
		A = a;
		M = m;
		diminishing = dimish;
		pow_diminishing = pow_dimish;
		Parameter = new VariableBignumber(0.0);
	}

	public override void AddVoidMana()
	{
		Parameter.SetValue(1.0 + A * W.Value.Pow(M));
		base.AddVoidMana();
	}

	public override string Preview(string key = "")
	{
		Parameter.SetValue(1.0 + A * W.Value.Pow(M));
		return base.Preview(key);
	}
}
