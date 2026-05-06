using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BuildingGildingWindow : MonoBehaviour
{
	public List<BuildingSelect> buildings;

	public List<BuildingSpecializationSelect> specs;

	[SerializeField]
	private SpecProductionVisual prodVisual;

	[SerializeField]
	private TextMeshProUGUI brickLabel;

	[SerializeField]
	private BuyBricks buyBricks;

	[SerializeField]
	private GameObject holder;

	private float timer;

	public BuildingSpecializationSelect CurrentSelectedSpec { get; private set; }

	private void OnEnable()
	{
		UpdateBrickLabel();
		CheckButtonsState();
	}

	private void Update()
	{
		timer += Time.unscaledDeltaTime;
		if (timer >= 0.5f)
		{
			CheckButtonsState();
			timer = 0f;
		}
	}

	private void CheckButtonsState()
	{
		bool active = false;
		foreach (BuildingSelect building in buildings)
		{
			if (building.SpecSelect != null && building.buildingVisual.building.spec == null)
			{
				active = true;
				break;
			}
		}
		holder.SetActive(active);
	}

	public bool CheckAvailable()
	{
		return GameManager.Instance.Paragon.GildingBuildingsIsAvailable;
	}

	public void Init()
	{
		Dictionary<BuildingGilding.SourceType, BuildingSpecBase> map = GameManager.Instance.Gilding.Buildings.Map;
		foreach (BuildingSpecializationSelect spec in specs)
		{
			spec.Init(map[spec.Key], SelectSpec);
		}
		foreach (BuildingSelect building in buildings)
		{
			building.SetWindow(this);
		}
		prodVisual.Init(specs.Find((BuildingSpecializationSelect x) => x.Key == BuildingGilding.SourceType.Production).spec as BuildingSpecProduction);
		VariableBignumber brickCurrent = GameManager.Instance.Gilding.Buildings.brickCurrent;
		brickCurrent.OnChange = (Action)Delegate.Combine(brickCurrent.OnChange, new Action(UpdateBrickLabel));
	}

	private void DebugSources()
	{
		BigNumber bigNumber = 1.1549999713897705;
		BigNumber bigNumber2 = "1e3100";
		BigNumber bigNumber3 = "1e10";
		BigNumber bigNumber4 = (1.0 - (1.0 - bigNumber) * bigNumber2 / bigNumber3).Log_a(bigNumber.ToDouble());
		bigNumber = 1.16;
		BigNumber bigNumber5 = (1.0 - (1.0 - bigNumber) * bigNumber2 / bigNumber3).Log_a(bigNumber.ToDouble());
		Debug.Log("nexuses:");
		Debug.Log((bigNumber4 - bigNumber5).ToReadableString() + " " + bigNumber5.ToReadableString());
	}

	public void Open()
	{
		UpdateBrickLabel();
		buyBricks.UpdateAmountToBuy();
		base.gameObject.SetActive(value: true);
	}

	public void Close()
	{
		ResetChange();
		SelectSpec(null);
		base.gameObject.SetActive(value: false);
	}

	public void ResetSelectSpec()
	{
		if (CurrentSelectedSpec != null)
		{
			CurrentSelectedSpec.HightlightOff();
		}
		CurrentSelectedSpec = null;
	}

	public void OnSelectBuilding(BuildingSelect buildingSelect)
	{
		foreach (BuildingSelect building in buildings)
		{
			if (building != buildingSelect && building.SpecSelect == buildingSelect.SpecSelect)
			{
				building.ClearSelect();
			}
		}
		SelectSpec(null);
	}

	public void ApplyChange()
	{
		GameManager.Instance.ConfirmWindow.Open("Are you sure?".Translate(), Apply);
	}

	private void Apply()
	{
		foreach (BuildingSelect building in buildings)
		{
			building.ApplySelect();
		}
		holder.SetActive(value: false);
	}

	public void ResetChange()
	{
		foreach (BuildingSelect building in buildings)
		{
			building.ClearSelect();
		}
		holder.SetActive(value: false);
	}

	public void ResetAll()
	{
		if (!specs.Any((BuildingSpecializationSelect x) => x.spec.building != null) && specs.FindAll((BuildingSpecializationSelect x) => x.spec.Level.ValueInt > 1).Count <= 1)
		{
			return;
		}
		if (GameManager.Instance.Nullifier.ValueInt >= 100)
		{
			GameManager.Instance.ConfirmWindow.Open("This action costs 100 <sprite=4>\nProceed?", delegate
			{
				GameManager.Instance.Nullifier.Change(-100);
				GameManager.Instance.Gilding.Buildings.ResetSoft();
			});
		}
		else
		{
			GameManager.Instance.ConfirmWindow.Open("You don't have enough <sprite=4> left.", delegate
			{
				GameManager.Instance.Shop.Open();
			});
		}
	}

	public void PostLoad()
	{
		foreach (BuildingSpecializationSelect v in specs)
		{
			if (v.spec.building != null)
			{
				buildings.Find((BuildingSelect x) => x.buildingVisual.Tier == v.spec.building.Tier).SetSpec(v);
			}
		}
		prodVisual.Recalculate();
	}

	private void SelectSpec(BuildingSpecializationSelect select)
	{
		CurrentSelectedSpec = select;
		foreach (BuildingSpecializationSelect spec in specs)
		{
			if (spec != select)
			{
				spec.HightlightOff();
			}
			else
			{
				spec.HightlightOn();
			}
		}
	}

	private void UpdateBrickLabel()
	{
		BuildingGilding buildingGilding = GameManager.Instance.Gilding.Buildings;
		brickLabel.text = NumberUtils.BigNumberToReadableStringTruncated(buildingGilding.brickCurrent.Value, 2, "F0") + "/" + NumberUtils.BigNumberToReadableStringTruncated(buildingGilding.brickTotal.Value, 2, "F0");
	}
}
