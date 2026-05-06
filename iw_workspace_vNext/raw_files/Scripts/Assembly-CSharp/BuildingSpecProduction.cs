using System;
using UnityEngine;

public class BuildingSpecProduction : BuildingSpecBase
{
	public int MaxLvl = 20;

	public BuildingSpecProduction(BuildingSpecializationFormat format, Variable efficiency)
		: base(format, efficiency)
	{
		efficiency.OnChange = (Action)Delegate.Combine(efficiency.OnChange, new Action(UpdatePPS));
	}

	public override BigNumber GetPPS()
	{
		return base.GetPPS() * efficiency.Value;
	}

	public void UpdatePPS()
	{
		if (base.building != null)
		{
			base.building.pps_per_building.SetValue(GetPPS());
			base.building.CalculatePps();
		}
	}

	public override void SelectBuilding(Building building, bool reset = true)
	{
		base.SelectBuilding(building, reset);
		UpdateCostGrowthRate();
		VariableInt level = Level;
		level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(UpdateCostGrowthRate));
	}

	public override void ResetBuilding()
	{
		if (base.building != null)
		{
			base.ResetBuilding();
			VariableInt level = Level;
			level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(UpdateCostGrowthRate));
		}
	}

	public void UpdateCostGrowthRate()
	{
		if (base.building != null)
		{
			base.building.cost_growth.SetValue(GetBuildingGrowthRate());
		}
	}

	public override float GetBuildingGrowthRate()
	{
		if (base.building == null)
		{
			return BuildingGrowthRate;
		}
		float num = base.building.base_cost_growth - BuildingGrowthRate;
		num *= Mathf.Pow(BonusPerLevel, Level.ValueInt - 1);
		return BuildingGrowthRate + num;
	}

	public float Recalculate(BigNumber bricks)
	{
		return BigNumber.AmountOfElementsGeometryProgression(bricks, CostGrowthRate, StartCost).ToFloat() + 1f;
	}

	public override bool IsMaxLevel()
	{
		return Level.ValueInt >= MaxLvl;
	}

	public override int GetNextGoal()
	{
		if (base.building == null)
		{
			return 0;
		}
		return base.building.NextGoal;
	}

	public override string GetFullDescription()
	{
		return GetDescription();
	}
}
