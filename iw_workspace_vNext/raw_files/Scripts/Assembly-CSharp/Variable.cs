using System;

public abstract class Variable
{
	public Action OnChange;

	public Action<BigNumber> OnChangeAdd;

	public abstract BigNumber Value { get; }

	public virtual void SetValue(BigNumber value)
	{
	}

	public abstract void Change(BigNumber addendum, BigNumber multiplier);
}
