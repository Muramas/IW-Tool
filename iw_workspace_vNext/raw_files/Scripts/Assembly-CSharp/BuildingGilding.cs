using System;
using System.Collections.Generic;

public class BuildingGilding
{
	public enum SourceType
	{
		Production = 0,
		Cap = 1,
		Autoclick = 2,
		VpE = 3,
		Exp = 4,
		Evo = 5,
		Inca = 6,
		Sum = 7,
		Pap = 8,
		DemiurgeShaman = 9,
		DemiurgeTemporalist = 10,
		DemiurgeHeretic = 11
	}

	public class SaveData
	{
		public BigNumber current;

		public BigNumber total;

		public List<SaveDataSpec> specs;

		public void Add(SourceType type, BuildingSpecBase spec)
		{
			if (specs == null)
			{
				specs = new List<SaveDataSpec>();
			}
			specs.Add(new SaveDataSpec((int)type, spec.Level.ValueInt, (spec.building != null) ? spec.building.Tier : 0));
		}
	}

	public class SaveDataSpec
	{
		public int ID;

		public int Lvl;

		public int Tier;

		public SaveDataSpec(int id, int lvl, int tier)
		{
			ID = id;
			Lvl = lvl;
			Tier = tier;
		}
	}

	public VariableInt Level;

	public Dictionary<SourceType, BuildingSpecBase> Map;

	public VariableBignumber brickCurrent;

	public VariableBignumber brickTotal;

	public VariableComplex efficiency;

	public Action OnSetSpec;

	public BuildingGilding(List<BuildingSpecializationFormat> formats)
	{
		Level = new VariableInt(0);
		Map = new Dictionary<SourceType, BuildingSpecBase>();
		efficiency = new VariableComplex(1.0);
		foreach (BuildingSpecializationFormat format in formats)
		{
			SourceType key = (SourceType)Enum.Parse(typeof(SourceType), format.Key);
			if (string.IsNullOrEmpty(format.Target))
			{
				Map.Add(key, new BuildingSpecProduction(format, efficiency));
			}
			else
			{
				Map.Add(key, new BuildingSpecialization(format, efficiency));
			}
		}
		brickCurrent = new VariableBignumber(0.0);
		brickTotal = new VariableBignumber(0.0);
		GameContext.ContextAddResource("BuildingGilding.IgnotsTotal", brickTotal);
		GameContext.ContextAddResource("BuildingGilding.Eff", efficiency);
	}

	public void Deactivate()
	{
		foreach (KeyValuePair<SourceType, BuildingSpecBase> item in Map)
		{
			if (item.Value.building != null)
			{
				item.Value.building.ResetSpec();
			}
		}
	}

	public void Activate(bool reset)
	{
		foreach (KeyValuePair<SourceType, BuildingSpecBase> item in Map)
		{
			item.Value.Activate(reset);
		}
	}

	public void Reset()
	{
		foreach (KeyValuePair<SourceType, BuildingSpecBase> item in Map)
		{
			item.Value.ResetBuilding();
		}
	}

	public void ResetSoft()
	{
		Reset();
		foreach (KeyValuePair<SourceType, BuildingSpecBase> item in Map)
		{
			item.Value.Level.SetValue(1);
		}
		brickCurrent.SetValue(brickTotal.Value);
		brickCurrent.OnChange?.Invoke();
		brickTotal.OnChange?.Invoke();
	}

	public void ResetFull()
	{
		brickTotal.SetValue(0.0);
		brickCurrent.SetValue(0.0);
		foreach (KeyValuePair<SourceType, BuildingSpecBase> item in Map)
		{
			item.Value.Level.SetValue(1);
			item.Value.ResetBuilding();
		}
	}

	public void AddBricks(BigNumber value)
	{
		brickTotal.Change(value);
		brickCurrent.Change(value);
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData();
		saveData.total = brickTotal.Value;
		saveData.current = brickCurrent.Value;
		foreach (KeyValuePair<SourceType, BuildingSpecBase> item in Map)
		{
			if (item.Value.Level.ValueInt > 1 || item.Value.building != null)
			{
				saveData.Add(item.Key, item.Value);
			}
		}
		return saveData;
	}

	public void Load(SaveData data)
	{
		if (data == null)
		{
			return;
		}
		BuildingManager buildingManager = GameManager.Instance.BuildingManager;
		brickTotal.SetValue(data.total);
		brickCurrent.SetValue(data.current);
		if (data.specs == null)
		{
			return;
		}
		foreach (SaveDataSpec spec in data.specs)
		{
			BuildingSpecBase buildingSpecBase = Map[(SourceType)spec.ID];
			buildingSpecBase.Level.SetValue(spec.Lvl);
			if (GameManager.Instance.Paragon.GildingBuildingsIsAvailable)
			{
				if (spec.Tier != 0 && spec.Tier < 9)
				{
					buildingSpecBase.SelectBuilding(buildingManager.GetBuilding(spec.Tier), reset: false);
				}
			}
			else if (spec.Tier != 0 && spec.Tier < 9)
			{
				buildingSpecBase.SetBuilding(buildingManager.GetBuilding(spec.Tier));
			}
		}
	}
}
