using System;
using UnityEngine;

public class EffectAddProductionPeriodic : EffectAddProduction
{
	public float period = 1f;

	protected float timer;

	public Action<int> OnTick;

	public EffectAddProductionPeriodic(float t, float rate)
		: base(t)
	{
		period = rate;
	}

	public EffectAddProductionPeriodic(float t, float rate, BigNumber _a, BigNumber _m, Variable _w = null)
		: base(t, _a, _m, _w)
	{
		period = rate;
	}

	public override void Apply()
	{
		timer = Time.deltaTime;
	}

	protected virtual void Tick()
	{
		base.Apply();
		if (OnTick != null)
		{
			OnTick(1);
		}
	}

	public override void Update()
	{
		timer += Time.deltaTime;
		if (timer >= period)
		{
			timer -= period;
			Tick();
		}
	}
}
