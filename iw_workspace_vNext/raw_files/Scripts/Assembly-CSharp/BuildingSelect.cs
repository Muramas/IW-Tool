using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingSelect : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	[SerializeField]
	private int tier;

	[SerializeField]
	private Image icon;

	[SerializeField]
	private Image iconSpec;

	[SerializeField]
	private TextMeshProUGUI profit;

	[SerializeField]
	private TextMeshProUGUI cost;

	[SerializeField]
	private TextMeshProUGUI growth;

	private BuildingGildingWindow window;

	public BuildingVisual buildingVisual { get; private set; }

	public BuildingSpecializationSelect specSelect { get; private set; }

	public BuildingSpecializationSelect SpecSelect { get; private set; }

	private void OnEnable()
	{
		UpdateInfo();
	}

	public void SetWindow(BuildingGildingWindow window)
	{
		this.window = window;
		buildingVisual = GameManager.Instance.BuildingManager.Buildings.Find((BuildingVisual x) => x.Tier == tier);
	}

	private void Update()
	{
		UpdateInfo();
	}

	public void Select()
	{
		if (!(window.CurrentSelectedSpec == null) && buildingVisual.building.spec == null)
		{
			SpecSelect = window.CurrentSelectedSpec;
			window.OnSelectBuilding(this);
			UpdateInfo();
		}
	}

	public void SetSpec(BuildingSpecializationSelect spec)
	{
		specSelect = spec;
		UpdateInfo();
	}

	public void ApplySelect()
	{
		if (!(SpecSelect == null))
		{
			specSelect = SpecSelect;
			specSelect.spec.SelectBuilding(buildingVisual.building);
			SpecSelect = null;
			window.ResetSelectSpec();
			UpdateInfo();
		}
	}

	public void ClearSelect()
	{
		if (buildingVisual.building.spec == null)
		{
			SpecSelect = null;
			UpdateInfo();
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			ClearSelect();
		}
	}

	private void UpdateInfo()
	{
		icon.sprite = buildingVisual.Icon.sprite;
		if (SpecSelect != null && buildingVisual.building.spec == null)
		{
			profit.text = SpecSelect.spec.GetPPS().ToReadableString();
			cost.text = SpecSelect.spec.BuildingCost.ToReadableString();
			growth.text = new BigNumber(SpecSelect.spec.GetBuildingGrowthRate()).ToReadableString("F3");
			iconSpec.enabled = true;
			iconSpec.sprite = SpecSelect.GetIcon();
			return;
		}
		profit.text = buildingVisual.building.pps_per_building.GetInternalValue.ToReadableString();
		cost.text = buildingVisual.building.base_cost.GetInternalValue.ToReadableString();
		growth.text = buildingVisual.building.cost_growth.Value.ToReadableString("F3");
		if (buildingVisual.building.spec != null)
		{
			iconSpec.enabled = true;
			iconSpec.sprite = specSelect.GetIcon();
		}
		else
		{
			iconSpec.enabled = false;
		}
	}
}
