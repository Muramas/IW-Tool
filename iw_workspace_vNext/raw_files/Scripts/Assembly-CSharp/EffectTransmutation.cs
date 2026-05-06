using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectTransmutation : EffectDiminishing
{
	public int baseAmount;

	private List<Building> buildings;

	public EffectTransmutation(int _base, List<int> except, float diminish)
	{
		baseAmount = _base;
		List<BuildingVisual> list = GameManager.Instance.Buildings;
		buildings = new List<Building>();
		for (int i = 0; i < list.Count; i++)
		{
			if (except == null || !except.Contains(list[i].building.Tier))
			{
				buildings.Add(list[i].building);
			}
		}
		diminishing = diminish;
	}

	public override void Apply()
	{
		int baseA = ApplyEfficiency(baseAmount).ToInt();
		List<Building> list = buildings.FindAll((Building x) => x.cost_growth.Value.Pow(x.Level.ValueInt + baseA) * x.base_cost.Value < Statistic.ManaRealm.Value);
		if (list.Count != 0)
		{
			Building building = list[UnityEngine.Random.Range(0, list.Count)];
			int num = 0;
			BigNumber bigNumber = building.cost_growth.Value.Pow(building.Level.ValueInt + baseA) * building.base_cost.Value;
			num = baseA + Convert.ToInt32((1.0 + Statistic.ManaRealm.Value / (bigNumber * 100.0)).Log10());
			if (num > 0)
			{
				building.Level.Change(num);
				Statistic.TotalBuildings.Change(num);
			}
		}
	}

	public override string Preview(string key = "")
	{
		return string.Empty;
	}
}
