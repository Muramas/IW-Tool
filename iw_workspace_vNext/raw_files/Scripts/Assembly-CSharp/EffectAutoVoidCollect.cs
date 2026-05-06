using System;
using System.Globalization;
using UnityEngine;

public class EffectAutoVoidCollect : IEffect
{
	public float t = 5f;

	private float baseTime;

	private float timer;

	private BonusSpawner spawner;

	private bool profit;

	private VariableInt parameter;

	private VariableComplex casts;

	public EffectAutoVoidCollect(float time, VariableInt param, VariableComplex casts, bool _profit = true)
	{
		t = (baseTime = time);
		profit = _profit;
		parameter = param;
		spawner = GameManager.Instance.BonusSpawner;
		this.casts = casts;
		timer = 0f;
	}

	public void Apply()
	{
		RecalcutatePeriod();
		VariableInt variableInt = parameter;
		variableInt.OnChange = (Action)Delegate.Combine(variableInt.OnChange, new Action(RecalcutatePeriod));
	}

	public void Delete()
	{
		VariableInt variableInt = parameter;
		variableInt.OnChange = (Action)Delegate.Remove(variableInt.OnChange, new Action(RecalcutatePeriod));
	}

	public void Update()
	{
		timer += Time.deltaTime;
		if (timer >= t)
		{
			Collect();
			timer -= t;
			RecalcutatePeriod();
		}
	}

	private void Collect()
	{
		BonusClickable[] array = spawner.Clickables.FindAll((BonusClickable x) => x.isSpawned).ToArray();
		if (array.Length == 0)
		{
			return;
		}
		for (int num = 0; num < array.Length; num++)
		{
			if (profit)
			{
				array[num].Activate();
			}
			else
			{
				array[num].Disable();
			}
		}
	}

	public string Preview(string key = "")
	{
		RecalcutatePeriod();
		return t.ToString("F2", CultureInfo.InvariantCulture);
	}

	private void RecalcutatePeriod()
	{
		if (parameter.ValueInt <= 130)
		{
			t = 1f + Mathf.Clamp(baseTime - 1f - 0.3f * (float)parameter.ValueInt, 0f, baseTime - 1f);
		}
		else
		{
			float num = parameter.ValueInt - 130;
			t = Mathf.Clamp(Mathf.Pow(0.95f, num / 20f), 0.05f, 1f);
		}
		if (casts != null)
		{
			t = Mathf.Clamp(t / (1f + (float)(1.0 + casts.Value / 10000.0).Log10()), 0.033f, baseTime);
		}
	}

	public void SetEfficiency(Variable eff)
	{
	}

	public void SetGilding(Variable eff)
	{
	}
}
