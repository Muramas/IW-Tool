using System;
using System.Collections.Generic;
using System.Globalization;

[Serializable]
public class BuildingCreator
{
	public SpriteAtlas Atlas;

	private List<BuildingFormat> BuildingsFormatList;

	public void Create()
	{
		BuildingsFormatList = GlobalData.Buildings;
		List<BuildingVisual> Buildings = GameManager.Instance.Buildings;
		int i;
		for (i = 0; i < Buildings.Count; i++)
		{
			InitBuilding(Buildings[i], BuildingsFormatList.Find((BuildingFormat x) => x.Tier == Buildings[i].Tier.ToString()));
		}
		InitBuilding(GameManager.Instance.BuildingManager.special, BuildingsFormatList.Find((BuildingFormat x) => x.Tier == "9"));
	}

	public void SetBuilding(int tier, string name)
	{
		BuildingFormat buildingFormat = BuildingsFormatList.Find((BuildingFormat x) => x.Name == name);
		BuildingVisual buildingVisual = GameManager.Instance.BuildingManager.Buildings.Find((BuildingVisual x) => x.Tier == tier);
		buildingVisual.building.NameStr = buildingFormat.Name;
		buildingVisual.NameLabel.text = buildingVisual.building.Name;
		buildingVisual.building.Description = buildingFormat.Description;
		buildingVisual.building.Upgrade = ((!string.IsNullOrEmpty(buildingFormat.Upgrade)) ? int.Parse(buildingFormat.Upgrade) : 0);
		buildingVisual.Icon.sprite = Atlas.Get(buildingFormat.Sprite);
		buildingVisual.building.base_cost_growth = float.Parse(buildingFormat.Percentage, CultureInfo.InvariantCulture);
		buildingVisual.building.base_cost_string = buildingFormat.BaseCost;
		buildingVisual.building.base_pps = buildingFormat.BaseProfit;
		buildingVisual.building.ReApply();
	}

	private void InitBuilding(BuildingVisual visual, BuildingFormat format)
	{
		visual.building.Tier = int.Parse(format.Tier);
		visual.building.NameStr = format.Name;
		visual.building.base_cost_string = format.BaseCost;
		visual.building.base_cost_growth = float.Parse(format.Percentage, CultureInfo.InvariantCulture);
		visual.building.base_pps = format.BaseProfit;
		visual.building.Description = format.Description;
		visual.building.Upgrade = ((!string.IsNullOrEmpty(format.Upgrade)) ? int.Parse(format.Upgrade) : 0);
		visual.Icon.sprite = Atlas.Get(format.Sprite);
		visual.building.Init();
	}
}
