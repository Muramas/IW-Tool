using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
	public struct OfflineData
	{
		public int red;

		public int blue;

		public int green;

		public int yellow;

		public BigNumber edust;
	}

	public VariableFloat Enchanting;

	public VariableBignumber Cap;

	public Dictionary<CraftResource, VariableFloat> map;

	private float BaseProgress = 0.022f;

	public VariableComplex EnchantingBonus;

	public VariableComplex EDustIncome;

	public VariableComplex MaxProgress;

	public VariableComplex JarsEff;

	public ResourceBank[] banks;

	public MarketGatherer gatherer;

	public Action<BigNumber> OnUseJars;

	private float timer;

	private const float OfflineGain = 0.6369f;

	public VariableFloat Red => map[CraftResource.Red];

	public VariableFloat Green => map[CraftResource.Green];

	public VariableFloat Blue => map[CraftResource.Blue];

	public VariableFloat Yellow => map[CraftResource.Yellow];

	public void Init()
	{
		JarsEff = new VariableComplex(1.0);
		MaxProgress = new VariableComplex(2000.0);
		EnchantingBonus = new VariableComplex(1.0);
		EDustIncome = new VariableComplex(1.0);
		Cap = new VariableBignumber(100000.0);
		map = new Dictionary<CraftResource, VariableFloat>
		{
			{
				CraftResource.Red,
				new VariableFloat(0f)
			},
			{
				CraftResource.Green,
				new VariableFloat(0f)
			},
			{
				CraftResource.Blue,
				new VariableFloat(0f)
			},
			{
				CraftResource.Yellow,
				new VariableFloat(0f)
			}
		};
		Enchanting = new VariableFloat(0f);
		map.Add(CraftResource.Enchanting, Enchanting);
		GameContext.ContextAddResource("Dust.EnchantingBonus", EnchantingBonus);
		GameContext.ContextAddResource("Dust.EDustIncome", EDustIncome);
		GameContext.ContextAddResource(ResourceType.Items.ToString() + "." + ItemKeys.Jars, JarsEff);
		GameContext.ContextAddResource(ResourceType.Items.ToString() + "." + ItemKeys.Bank, MaxProgress);
		GameContext.ContextAddResource(ResourceType.Items.ToString() + "." + ItemKeys.Cap, Cap);
		gatherer.Init();
		Reset();
	}

	public void Reset()
	{
		ResourceBank[] array = banks;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ResetJar();
		}
	}

	public void Off()
	{
		foreach (KeyValuePair<CraftResource, VariableFloat> item in map)
		{
			item.Value.SetValue(0f);
		}
		ResourceBank[] array = banks;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Off();
		}
	}

	public void RealmReset()
	{
		Off();
		GameManager.Instance.Craft.map[CraftResource.Enchanting].SetValue(0.0);
	}

	public void Update()
	{
		if (GameManager.Instance.Paragon.ItemsIsAvailable && Time.timeScale != 0f)
		{
			if (timer >= 1f)
			{
				int num = (int)timer;
				update_timers(num);
				timer -= num;
			}
			timer += Time.unscaledDeltaTime;
		}
	}

	public void AddResources(CraftResource key)
	{
		if (GameManager.Instance.Paragon.ItemsIsAvailable)
		{
			int num = map[key].Value.ToInt();
			map[key].SetValue(0f);
			GameManager.Instance.Craft.Gathering.AddExp((float)num / 1000f);
			AddResource(key, num);
			OnUseJars?.Invoke(num);
		}
	}

	public float GetDiminish(CraftResource key, float value = 0f)
	{
		BigNumber bigNumber = GameManager.Instance.Craft.map[key].Value + map[key].Value + value;
		if (bigNumber <= Cap.Value)
		{
			return 1f;
		}
		bigNumber = (Cap.Value / bigNumber).Sqrt();
		float num = 1f;
		if (bigNumber < 0.009999999776482582)
		{
			return 0.01f;
		}
		return bigNumber.ToFloat();
	}

	public void AddResource(CraftResource key, int value)
	{
		GameManager.Instance.Craft.map[key].Change(value);
		Statistic.ResourcesRealm[(int)(key - 1)].Change(value);
		Statistic.ResourcesTotal[(int)(key - 1)].Change(value);
		Statistic.ResourcesCollectedTotal.Change(value);
		Statistic.ResourcesCollectedRealm.Change(value);
		Statistic.ResourcesCollected.Change(value);
	}

	public BigNumber GetCraftingDustIncomePure()
	{
		return GameManager.Instance.Craft.Gathering.GetBonus() * JarsEff.GetInternalValue;
	}

	public BigNumber GetCraftingDustIncome(bool isAverage = false)
	{
		if (isAverage)
		{
			return GetProgress() * 0.6369f;
		}
		return GameManager.Instance.Craft.Gathering.GetBonus() * JarsEff.Value;
	}

	public BigNumber GetEnchantingDustIncomePure()
	{
		return GameManager.Instance.Craft.Crafting.GetBonus() * EnchantingBonus.GetInternalValue * EDustIncome.Value * 2.200000047683716;
	}

	public BigNumber GetEnchantingDustIncome()
	{
		return GameManager.Instance.Craft.Crafting.GetBonus() * EnchantingBonus.Value * EDustIncome.Value * 2.200000047683716;
	}

	public BigNumber GetEnchantingBonus()
	{
		return EnchantingBonus.Value * EDustIncome.Value;
	}

	public BigNumber GetEnchantingDustIncomeAverage()
	{
		return BaseProgress * GameManager.Instance.Craft.Gathering.GetBonus() * EnchantingBonus.Value * EDustIncome.Value * 0.4399999976158142;
	}

	private void update_timers(int k = 1)
	{
		float num = MathF.PI * ((float)Statistic.TimeTotal.ValueInt % 9000f) / 9000f;
		float num2 = 0f;
		if (Red.Value < MaxProgress.ValueFloat)
		{
			num2 = (float)k * GetProgress() * Mathf.Abs(Mathf.Sin(num)) * GetDiminish(CraftResource.Red);
			GenerateResource(CraftResource.Red, num2);
		}
		if (Green.Value < MaxProgress.ValueFloat)
		{
			num2 = (float)k * GetProgress() * Mathf.Abs(Mathf.Sin(num + MathF.PI / 4f)) * GetDiminish(CraftResource.Green);
			GenerateResource(CraftResource.Green, num2);
		}
		if (Blue.Value < MaxProgress.ValueFloat)
		{
			num2 = (float)k * GetProgress() * Mathf.Abs(Mathf.Sin(num + MathF.PI / 2f)) * GetDiminish(CraftResource.Blue);
			GenerateResource(CraftResource.Blue, num2);
		}
		if (Yellow.ValueFloat < MaxProgress.ValueFloat)
		{
			num2 = (float)k * GetProgress() * Mathf.Abs(Mathf.Sin(num + MathF.PI * 3f / 4f)) * GetDiminish(CraftResource.Yellow);
			GenerateResource(CraftResource.Yellow, num2);
		}
		if (GameManager.Instance.Paragon.EnchantingIsAvailable)
		{
			Enchanting.Change((k * GetEnchantingDustIncomeAverage()).ToFloat());
			float valueFloat = Enchanting.ValueFloat;
			if (valueFloat >= 1f)
			{
				GameManager.Instance.Craft.GiveEnchantingDust(valueFloat);
				Enchanting.SetValue(0f);
			}
		}
	}

	public void GenerateResource(CraftResource key, float value)
	{
		if (!(map[key].ValueFloat >= MaxProgress.ValueFloat))
		{
			if (value > MaxProgress.ValueFloat - map[key].ValueFloat)
			{
				value = MaxProgress.ValueFloat - map[key].ValueFloat;
			}
			if (value < 0f)
			{
				value = 0f;
			}
			map[key].Change(value);
		}
	}

	private float GetProgress()
	{
		return BaseProgress * GameManager.Instance.Craft.Gathering.GetBonus().ToFloat() * JarsEff.Value.ToFloat() * 2.352f;
	}

	private float GetProgressOffline()
	{
		return BaseProgress * GameManager.Instance.Craft.Gathering.GetBonus().ToFloat() * JarsEff.Value.ToFloat() * 2.352f * 0.6369f;
	}

	public List<SaveData.IntIntPair> SaveJars()
	{
		return new List<SaveData.IntIntPair>
		{
			new SaveData.IntIntPair(1, (int)Red.ValueFloat),
			new SaveData.IntIntPair(2, (int)Green.ValueFloat),
			new SaveData.IntIntPair(3, (int)Blue.ValueFloat),
			new SaveData.IntIntPair(4, (int)Yellow.ValueFloat)
		};
	}

	public void LoadJars(List<SaveData.IntIntPair> jars)
	{
		if (jars == null)
		{
			Red.SetValue(0f);
			Green.SetValue(0f);
			Blue.SetValue(0f);
			Yellow.SetValue(0f);
			return;
		}
		Red.SetValue(jars.Find((SaveData.IntIntPair x) => x.ID == 1).Value);
		Green.SetValue(jars.Find((SaveData.IntIntPair x) => x.ID == 2).Value);
		Blue.SetValue(jars.Find((SaveData.IntIntPair x) => x.ID == 3).Value);
		Yellow.SetValue(jars.Find((SaveData.IntIntPair x) => x.ID == 4).Value);
	}

	public OfflineData OfflineProgress(int sec)
	{
		OfflineData result = new OfflineData
		{
			red = (int)AddOfflineProduction(CraftResource.Red, sec),
			green = (int)AddOfflineProduction(CraftResource.Green, sec),
			yellow = (int)AddOfflineProduction(CraftResource.Yellow, sec),
			blue = (int)AddOfflineProduction(CraftResource.Blue, sec)
		};
		BigNumber bigNumber = 0.0;
		if (GameManager.Instance.Paragon.EnchantingIsAvailable)
		{
			bigNumber = (sec * GetEnchantingDustIncomeAverage()).ToFloat();
			if (bigNumber > 1.0)
			{
				GameManager.Instance.Craft.GiveEnchantingDust(bigNumber);
			}
		}
		result.edust = bigNumber;
		ResourceBank[] array = banks;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].UpdateVisual();
		}
		return result;
	}

	public void TestOffline(float time)
	{
		float num = 0f;
		float num2 = 0f;
		foreach (KeyValuePair<CraftResource, VariableFloat> item in map)
		{
			if (item.Key != CraftResource.Enchanting)
			{
				num = item.Value.ValueFloat;
				num2 = Time.realtimeSinceStartup;
				BigNumber bigNumber = AddOfflineProduction(item.Key, time);
				Debug.Log("1: " + (Time.realtimeSinceStartup - num2));
				item.Value.SetValue(num);
				BigNumber bigNumber2 = GetRawProduction(item.Key, time);
				Debug.Log("2: " + (Time.realtimeSinceStartup - num2));
				Debug.Log(item.Key.ToString() + " " + bigNumber.ToString() + " " + bigNumber2.ToString());
				item.Value.SetValue(num);
			}
		}
	}

	private float GetRawProduction(CraftResource key, float t)
	{
		float num = GetProgress() * 0.6369f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		while (t > 0f)
		{
			num4 = ((!(t > 10f)) ? t : 10f);
			num3 = num * GetDiminish(key) * num4;
			t -= num4;
			map[key].Change(num3);
			num2 += num3;
		}
		return num2;
	}

	private int AddOffline(VariableFloat res, float count)
	{
		int result = 0;
		if (res.ValueFloat < MaxProgress.ValueFloat)
		{
			float valueFloat = res.ValueFloat;
			if (count + res.ValueFloat < MaxProgress.ValueFloat)
			{
				res.Change(count);
			}
			else if (MaxProgress.ValueFloat > count)
			{
				res.SetValue(MaxProgress.ValueFloat - count);
			}
			result = Mathf.FloorToInt(res.ValueFloat - valueFloat);
		}
		return result;
	}

	private BigNumber getResourceSumm(CraftResource key)
	{
		return map[key].Value + GameManager.Instance.Craft.map[key].Value;
	}

	private float AddOfflineProduction(CraftResource key, float time)
	{
		float progressOffline = GetProgressOffline();
		float diminish = GetDiminish(key);
		if (diminish == 1f)
		{
			float value = progressOffline * time;
			value = Mathf.Clamp(value, 0f, MaxProgress.ValueFloat - map[key].ValueFloat);
			if (value < 0f)
			{
				value = 0f;
			}
			diminish = GetDiminish(key, value);
			if (diminish == 1f)
			{
				GenerateResource(key, value);
				return value;
			}
			value = (Cap.Value - getResourceSumm(key)).ToFloat();
			float value2 = value / progressOffline;
			value2 = Mathf.Clamp(value2, 0f, time);
			GenerateResource(key, value);
			float num = time - value2;
			if (num > 0f)
			{
				return value + addProgress(key, num, progressOffline, diminish);
			}
			return value;
		}
		return addProgress(key, time, progressOffline, diminish);
	}

	private float addProgress(CraftResource key, float time, float startSpeed, float diminish)
	{
		float num = Mathf.Abs(MaxProgress.ValueFloat - map[key].ValueFloat);
		if (num < 0.01f)
		{
			return 0f;
		}
		if (diminish <= 0.01f)
		{
			float num2 = startSpeed * 0.01f * time;
			GenerateResource(key, num2);
			return num2;
		}
		float num3 = diminish - 0.01f;
		float num4 = Mathf.Clamp((Cap.Value / (num3 * num3) - getResourceSumm(key)).ToFloat(), 0f, num);
		if (num4 < 0f)
		{
			num4 = 0f;
		}
		float num5 = startSpeed * diminish;
		float num6 = num4 / num5;
		if (num6 > time)
		{
			num4 = num5 * time;
			GenerateResource(key, num4);
			return num4;
		}
		GenerateResource(key, num4);
		return num4 + addProgress(key, time - num6, startSpeed, num3);
	}
}
