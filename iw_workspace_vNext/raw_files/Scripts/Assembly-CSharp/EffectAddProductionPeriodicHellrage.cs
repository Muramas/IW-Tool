using UnityEngine;

public class EffectAddProductionPeriodicHellrage : EffectAddProductionPeriodic
{
	private int b = 725;

	public EffectAddProductionPeriodicHellrage(float t, float rate)
		: base(t, rate)
	{
	}

	public override void Apply()
	{
		timer = Time.deltaTime;
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

	protected override void Tick()
	{
		int times = GetTimes();
		GameManager.Instance.ManaChange(GetValue() * times);
		if (OnTick != null)
		{
			OnTick(times);
		}
	}

	private int GetTimes()
	{
		if ((float)Statistic.ManaSession.Value.Exponent / 2f < (float)b)
		{
			return 1;
		}
		return 1 + Mathf.FloorToInt(((float)Statistic.ManaSession.Value.Exponent / 2f - (float)b) / 13.1356f);
	}

	public override string Preview(string key = "")
	{
		if (key == "a")
		{
			return GetTimes().ToString();
		}
		if (key == "t")
		{
			return base.Preview(key);
		}
		return "+" + (GetValue() * GetTimes()).ToReadableString("F0");
	}
}
