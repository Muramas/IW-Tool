using System;
using UnityEngine;

public class EffectAddVoidMana : EffectDiminishing
{
	public BigNumber Count;

	public float Frequency;

	public Variable Parameter;

	public Action OnApply;

	public float timer;

	public EffectAddVoidMana(float count, float frequency = 0f, Variable W = null)
	{
		Count = count;
		Frequency = frequency;
		Parameter = W;
	}

	public override void Apply()
	{
		timer = 0f;
		AddVoidMana();
	}

	public override void Update()
	{
		if (Frequency == 0f)
		{
			return;
		}
		timer += Time.deltaTime;
		if (timer > 1f / Frequency)
		{
			int num = (int)(timer * Frequency);
			for (int i = 0; i < num; i++)
			{
				AddVoidMana();
			}
			timer -= (float)num / Frequency;
		}
	}

	public virtual void AddVoidMana()
	{
		BigNumber count = Count;
		if (Parameter != null)
		{
			count *= Parameter.Value;
		}
		count = ApplyEfficiency(count);
		GameManager.Instance.VoidManaChange(count);
		if (OnApply != null)
		{
			OnApply();
		}
	}

	public override string Preview(string key = "")
	{
		BigNumber count = Count;
		if (Parameter != null)
		{
			count *= Parameter.Value;
		}
		count = ApplyEfficiency(count);
		return count.ToReadableString();
	}
}
