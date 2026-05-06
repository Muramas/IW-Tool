using System;

public class EffectFunction : IEffect
{
	private Action action;

	public EffectFunction(Action func)
	{
		action = func;
	}

	public void Apply()
	{
		action();
	}

	public void SetEfficiency(Variable eff = null)
	{
	}

	public void SetGilding(Variable eff = null)
	{
	}

	public void Delete()
	{
	}

	public void Update()
	{
	}

	public string Preview(string key = "")
	{
		return "";
	}
}
