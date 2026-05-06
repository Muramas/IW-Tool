using System;
using UnityEngine;

public class EffectAddShardPeriodic : EffectAddShardInstant
{
	public float Frequency;

	public float timer;

	public Action OnApply;

	public EffectAddShardPeriodic(float frequency, BigNumber _v, BigNumber _a, BigNumber _m, Variable _w = null, float diminish = 1f)
		: base(_v, _a, _m, _w, null, diminish)
	{
		Frequency = frequency;
	}

	public EffectAddShardPeriodic(float shard, float frequency)
		: base(shard)
	{
		Frequency = frequency;
	}

	public override void Apply()
	{
		if (OnApply != null)
		{
			OnApply();
		}
	}

	public override void Update()
	{
		if (Frequency == 0f)
		{
			return;
		}
		timer += Time.deltaTime;
		if (!(timer > 1f / Frequency))
		{
			return;
		}
		int num = (int)(timer * Frequency);
		for (int i = 0; i < num; i++)
		{
			base.Apply();
			if (OnApply != null)
			{
				OnApply();
			}
		}
		timer -= (float)num / Frequency;
	}
}
