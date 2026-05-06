using System;
using System.Globalization;
using UnityEngine;

public class BuildingSpecBase
{
	public BuildingGilding.SourceType Key;

	public BigNumber BuildingProfit;

	public BigNumber BuildingCost;

	public float BuildingGrowthRate;

	public string Description;

	public VariableInt Level;

	public int StartCost;

	public float CostGrowthRate;

	public float BonusPerLevel;

	protected Variable efficiency;

	public Building building { get; private set; }

	public BuildingSpecBase(BuildingSpecializationFormat format, Variable efficiency)
	{
		Key = (BuildingGilding.SourceType)Enum.Parse(typeof(BuildingGilding.SourceType), format.Key);
		BuildingProfit = format.BuildingProfit;
		BuildingCost = format.BuildingCost;
		BuildingGrowthRate = float.Parse(format.BuildingGrowth, CultureInfo.InvariantCulture);
		Description = format.Description;
		Level = new VariableInt(1);
		StartCost = int.Parse(format.Cost);
		CostGrowthRate = float.Parse(format.Growth, CultureInfo.InvariantCulture);
		BonusPerLevel = float.Parse(format.Reduction, CultureInfo.InvariantCulture);
		this.efficiency = efficiency;
	}

	public virtual BigNumber GetPPS()
	{
		return BuildingProfit;
	}

	public void Activate(bool reset = false)
	{
		if (building != null)
		{
			SelectBuilding(building, reset);
		}
	}

	public void SetBuilding(Building b)
	{
		if (building != null && building.spec != null)
		{
			if (building == b && building.spec == this)
			{
				return;
			}
			building.spec.ResetBuilding();
		}
		building = b;
	}

	public virtual void SelectBuilding(Building b, bool reset = true)
	{
		if (reset)
		{
			ResetBuilding();
		}
		SetBuilding(b);
		building.SetSpecialization(this, reset);
		GameManager.Instance.Gilding.Buildings.OnSetSpec?.Invoke();
	}

	public virtual void ResetBuilding()
	{
		if (building != null)
		{
			building.ResetSpec();
			building = null;
		}
	}

	public virtual void TurnOff()
	{
	}

	public void Upgrade(int lvls = 1)
	{
		Level.Change(lvls);
	}

	public BigNumber GetCost(int startFrom = 1, int levels = 1)
	{
		if (levels == 1)
		{
			return (new BigNumber(StartCost) * Mathf.Pow(CostGrowthRate, startFrom - 1)).Floor();
		}
		BigNumber result = 0.0;
		for (int i = 0; i < levels; i++)
		{
			result += GetCost(startFrom + i);
		}
		return result;
	}

	public virtual string GetDescription()
	{
		return TranslationManager.Instance.Process(Description);
	}

	public virtual string GetFullDescription()
	{
		return GetDescription();
	}

	public virtual float GetBuildingGrowthRate()
	{
		return BuildingGrowthRate;
	}

	public virtual bool IsMaxLevel()
	{
		return false;
	}

	public virtual int GetNextGoal()
	{
		return 0;
	}

	protected string getFormated(BigNumber value, bool isPercent = false)
	{
		string str = ((!isPercent) ? value.ToReadableString() : ("+" + ((value - 1.0) * 100.0).ToReadableString() + "%"));
		return formate(str);
	}

	protected string formate(string str)
	{
		if (Settings.ColoredTips)
		{
			return "<color=#e2b018>" + str + "</color>";
		}
		return str;
	}
}
