using System;
using UnityEngine;

[Serializable]
public class Building : Buyable
{
	public bool Available = true;

	[Header("  Building")]
	public VariableInt Level;

	public VariableInt TotalLevel;

	public VariableInt TemporalyLevel;

	public int levelsToBuy;

	public BigNumber cost_current = 0.0;

	public float base_cost_growth = 1.1f;

	public int Tier;

	public ulong ACatalyst;

	public ulong MCatalyst;

	public ulong RCatalyst;

	public string NameStr;

	public string base_pps;

	public VariableComplex pps_per_building;

	public VariableComplex Pps;

	public BigNumber pps_multiplicator = 1.0;

	public VariableComplex cost_growth;

	public int Upgrade;

	public int NextGoal;

	public float NextGoalBonus;

	private int oldRed;

	private int oldTempRed;

	private bool redIsOff;

	public string Description = "";

	public string Name => NameStr.Translate();

	public BuildingSpecBase spec { get; private set; }

	public BigNumber cost_one => cost_growth.Value.Pow(Level.ValueInt) * base_cost.Value;

	public BigNumber GetBasePPS
	{
		get
		{
			if (pps_per_building == null || GameManager.Instance.Profit == null)
			{
				Debug.LogError("PPS is null " + Tier);
				return 0.0;
			}
			if (pps_per_building.GetInternalValue.Mantissa == 0.0)
			{
				return 0.0;
			}
			return GameManager.Instance.Profit.ApplyModOnVar(pps_per_building.Value) * pps_multiplicator * (1.0 + ACatalyst * GetGreenCataPower()) * (1.0 + GameManager.Instance.BuildingManager.CatalystMultPower.Value).Pow(MCatalyst);
		}
	}

	public void Init()
	{
		Level = new VariableInt(0);
		TemporalyLevel = new VariableInt(0);
		TotalLevel = new VariableInt(0);
		pps_per_building = new VariableComplex(base_pps);
		Pps = new VariableComplex(0.0);
		base_cost = new VariableComplex(base_cost_string);
		cost_growth = new VariableComplex(base_cost_growth);
		GameContext.ContextAddBuilding(this);
	}

	public void ReApply()
	{
		if (spec != null)
		{
			if (spec is BuildingSpecProduction)
			{
				BuildingSpecProduction obj = spec as BuildingSpecProduction;
				obj.UpdatePPS();
				obj.UpdateCostGrowthRate();
				recalculate_pps_multiplier();
				CalculateCost(GameManager.Instance.BuyPack);
			}
		}
		else
		{
			base_cost.Reset(base_cost_string);
			cost_growth.SetValue(base_cost_growth);
			pps_per_building.SetValue(base_pps);
		}
	}

	public void Acitvate()
	{
		VariableInt level = Level;
		level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(recalculateTotalLevel));
		VariableInt temporalyLevel = TemporalyLevel;
		temporalyLevel.OnChange = (Action)Delegate.Combine(temporalyLevel.OnChange, new Action(recalculateTotalLevel));
		VariableComplex allBuildings = GameManager.Instance.BuildingManager.AllBuildings;
		allBuildings.OnChange = (Action)Delegate.Combine(allBuildings.OnChange, new Action(recalculateTotalLevel));
	}

	public void Deactivate()
	{
		VariableInt level = Level;
		level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(recalculateTotalLevel));
		VariableInt temporalyLevel = TemporalyLevel;
		temporalyLevel.OnChange = (Action)Delegate.Remove(temporalyLevel.OnChange, new Action(recalculateTotalLevel));
		VariableComplex allBuildings = GameManager.Instance.BuildingManager.AllBuildings;
		allBuildings.OnChange = (Action)Delegate.Remove(allBuildings.OnChange, new Action(recalculateTotalLevel));
	}

	public void Restart()
	{
		Pps.Reset();
		if (spec != null)
		{
			base_cost.Reset(spec.BuildingCost);
			cost_growth.SetValue(spec.GetBuildingGrowthRate());
			pps_per_building.SetValue(spec.BuildingProfit);
		}
		else
		{
			base_cost.Reset(base_cost_string);
			cost_growth.SetValue(base_cost_growth);
			pps_per_building.SetValue(base_pps);
		}
		int valueInt = Level.ValueInt;
		Level.SetValue(0);
		TemporalyLevel.SetValue(RCatalyst);
		recalculate_pps_multiplier();
		if (Statistic.TotalBuildings.ValueInt >= valueInt)
		{
			Statistic.TotalBuildings.Change(-valueInt);
		}
		else
		{
			Statistic.TotalBuildings.SetValue(0);
		}
	}

	public void ResetSpec()
	{
		if (spec != null)
		{
			if (spec.building != this)
			{
				Debug.Log("wrong assign spec");
			}
			spec.TurnOff();
			spec = null;
		}
		Restart();
	}

	public void SetSpecialization(BuildingSpecBase spec, bool reset = false)
	{
		if (reset)
		{
			ResetSpec();
		}
		this.spec = spec;
		base_cost.SetValue(spec.BuildingCost);
		cost_growth.SetValue(spec.GetBuildingGrowthRate());
		pps_per_building.SetValue(spec.GetPPS());
		recalculate_pps_multiplier();
		CalculatePps();
		CalculateCost(GameManager.Instance.BuyPack);
	}

	public void RecalculateTemp()
	{
		if (!redIsOff)
		{
			int num = (int)RCatalyst;
			int num2 = (int)((float)num * GameManager.Instance.BuildingManager.CatalystTempPower.Value.ToFloat());
			if (oldRed != num || num2 != oldTempRed)
			{
				int ad = num2 - oldTempRed;
				TemporalyLevel.Change(ad);
				oldRed = num;
				oldTempRed = num2;
			}
		}
	}

	public void TurnOffRed()
	{
		redIsOff = true;
		TemporalyLevel.Change(-oldTempRed);
		oldTempRed = 0;
	}

	public void TurnOnRed()
	{
		redIsOff = false;
		RecalculateTemp();
	}

	public void SetRedCatalysts(ulong red)
	{
		RCatalyst = red;
		RecalculateTemp();
		oldRed = (int)RCatalyst;
		if (!redIsOff)
		{
			oldTempRed = (int)((float)oldRed * GameManager.Instance.BuildingManager.CatalystTempPower.Value.ToFloat());
		}
	}

	public void LoadRedCatalysts(ulong red)
	{
		RCatalyst = red;
		oldRed = (int)RCatalyst;
		oldTempRed = (int)((float)oldRed * GameManager.Instance.BuildingManager.CatalystTempPower.Value.ToFloat());
		TemporalyLevel.SetValue(oldTempRed);
	}

	public BigNumber GetGreenCataPower()
	{
		BigNumber value = GameManager.Instance.BuildingManager.CatalystAddPower.Value;
		if (GameManager.Instance.BuildingManager.CatalystAddPowerScale.ValueFloat > 1f)
		{
			value *= 1.0 + new BigNumber(ACatalyst).Pow(GameManager.Instance.BuildingManager.CatalystAddPowerScale.ValueFloat - 1f);
		}
		return value;
	}

	public void CalculatePps()
	{
		Pps.SetValue(TotalLevel.ValueInt * GetBasePPS);
	}

	protected override void OnBuy()
	{
		int num = levelsToBuy;
		Statistic.TotalBuildings.Change(num);
		Level.SetValue(Level.ValueInt + num);
	}

	private void recalculateTotalLevel()
	{
		TotalLevel.SetValue(Level.ValueInt + TemporalyLevel.ValueInt + GameManager.Instance.BuildingManager.AllBuildings.Value.ToInt());
		recalculate_pps_multiplier();
	}

	private void recalculate_pps_multiplier()
	{
		int num = GameManager.Instance.BuildingLeveling.Length;
		int num2 = -1;
		pps_multiplicator = 1.0;
		if (!IsEnableGoals())
		{
			return;
		}
		for (int i = 0; i < num - 1 && TotalLevel.ValueInt >= GameManager.Instance.BuildingLeveling[i].level; i++)
		{
			num2 = i;
		}
		NextGoal = GameManager.Instance.BuildingLeveling[num2 + 1].level;
		NextGoalBonus = GameManager.Instance.BuildingLeveling[num2 + 1].increace;
		if (num2 < 0)
		{
			return;
		}
		for (int j = 0; j <= num2; j++)
		{
			pps_multiplicator *= (BigNumber)GameManager.Instance.BuildingLeveling[j].increace;
		}
		if (num2 == num - 2)
		{
			int num3 = (TotalLevel.ValueInt - GameManager.Instance.BuildingLeveling[num - 2].level) / GameManager.Instance.BuildingLeveling[num - 1].level;
			if (num3 > 0)
			{
				pps_multiplicator *= new BigNumber(GameManager.Instance.BuildingLeveling[num - 1].increace).Pow(num3);
			}
			NextGoal = GameManager.Instance.BuildingLeveling[num - 2].level + GameManager.Instance.BuildingLeveling[num - 1].level * (1 + num3);
			NextGoalBonus = GameManager.Instance.BuildingLeveling[num - 1].increace;
		}
	}

	public bool IsEnableGoals()
	{
		if (spec != null)
		{
			return spec.Key == BuildingGilding.SourceType.Production;
		}
		return true;
	}

	private BigNumber getCostCurrent()
	{
		return getCostFor(Level.ValueInt);
	}

	private BigNumber getCostFor(int level)
	{
		return cost_growth.Value.Pow(level) * base_cost.Value / GameManager.Instance.BuildingManager.CostReduction.Value;
	}

	public BigNumber GetCost(int additionalLevels)
	{
		return getCostCurrent() * cost_growth.Value.Pow(additionalLevels);
	}

	public void CalculateCost(int n = 1)
	{
		cost_current = getCostCurrent();
		if (n == 1)
		{
			levelsToBuy = 1;
			Cost = cost_current;
			return;
		}
		if (n < 1)
		{
			levelsToBuy = GetAmountAvailable();
			if (n == -25)
			{
				if (levelsToBuy < 25)
				{
					levelsToBuy = 25;
				}
				if (Settings.FloorMultiBuy)
				{
					levelsToBuy -= (levelsToBuy + TotalLevel.ValueInt) % n;
				}
				else
				{
					levelsToBuy -= levelsToBuy % 25;
				}
			}
		}
		else if (Settings.FloorMultiBuy)
		{
			levelsToBuy = n - TotalLevel.ValueInt % n;
		}
		else
		{
			levelsToBuy = n;
		}
		Cost = 0.0;
		int num = 0;
		while (levelsToBuy > 0)
		{
			int num2 = ((levelsToBuy >= 1000) ? 1000 : levelsToBuy);
			BigNumber bigNumber = cost_growth.Value.Pow(num2);
			Cost += cost_current * (1.0 - bigNumber) / (1.0 - cost_growth.Value);
			levelsToBuy -= num2;
			num += num2;
			if (levelsToBuy > 0)
			{
				cost_current = getCostFor(Level.ValueInt + num);
			}
		}
		levelsToBuy = num;
	}

	public int GetAmountAvailable()
	{
		return GetAmountAvailable(GameManager.Instance.Mana.Value);
	}

	public int GetAmountAvailable(BigNumber mana)
	{
		return (int)Math.Floor((1.0 - (1.0 - cost_growth.Value) * mana / getCostCurrent()).Log_a(cost_growth.Value.ToDouble()));
	}
}
