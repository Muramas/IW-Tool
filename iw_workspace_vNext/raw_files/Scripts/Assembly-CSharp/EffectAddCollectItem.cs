using System;
using UnityEngine;

public class EffectAddCollectItem : EffectInstant
{
	public float Frequency;

	private float timer;

	public EffectAddCollectItem(int count, float frequency = 0f, float diminish = 2f)
	{
		time = count;
		Frequency = frequency;
		diminishing = diminish;
	}

	public override void Apply()
	{
		timer = Time.timeSinceLevelLoad;
		add_collect(1f);
	}

	public override void Update()
	{
		if (Frequency != 0f)
		{
			float num = Time.timeSinceLevelLoad - timer;
			if (num > 1f / Frequency)
			{
				int num2 = (int)(num * Frequency);
				add_collect(num2);
				timer += num;
			}
		}
	}

	public int GetCollected()
	{
		return Mathf.Max(1, Convert.ToInt32(Math.Floor(ApplyW(time).ToDouble())));
	}

	private void add_collect(float k)
	{
		int count = GetCollected() * (int)k;
		GameManager.Instance.BonusSpawner.AddItems(count);
	}

	public override string Preview(string key = "")
	{
		return GetCollected().ToString();
	}
}
