public abstract class EffectInstant : EffectDiminishing
{
	public float time = 20f;

	public BigNumber a;

	public BigNumber m;

	public Variable w;

	protected BigNumber ApplyW(BigNumber t)
	{
		if (w != null)
		{
			t = (t + a * w.Value) * (1.0 + m * w.Value);
		}
		return ApplyEfficiency(t);
	}
}
