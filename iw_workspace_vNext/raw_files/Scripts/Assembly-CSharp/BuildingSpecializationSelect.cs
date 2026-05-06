using System;
using System.Globalization;
using ModelShark;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingSpecializationSelect : MonoBehaviour, ITooltipBodyHandler
{
	public BuildingGilding.SourceType Key;

	[SerializeField]
	private TextMeshProUGUI level;

	[SerializeField]
	private TextMeshProUGUI descr;

	[SerializeField]
	private TextMeshProUGUI costLabel;

	[SerializeField]
	private Button button;

	[SerializeField]
	private Image icon;

	[SerializeField]
	private Image back;

	[SerializeField]
	private ModelShark.TooltipTrigger tooltip;

	private int count;

	private Action<BuildingSpecializationSelect> onSelect;

	public BuildingSpecBase spec { get; private set; }

	public void Init(BuildingSpecBase spec, Action<BuildingSpecializationSelect> onSelect)
	{
		this.spec = spec;
		this.onSelect = onSelect;
		tooltip.bodyHandler = this;
		VariableBignumber brickCurrent = GameManager.Instance.Gilding.Buildings.brickCurrent;
		brickCurrent.OnChange = (Action)Delegate.Combine(brickCurrent.OnChange, new Action(UpdateInfo));
	}

	private void OnEnable()
	{
		UpdateInfo();
	}

	private void Update()
	{
		if ((!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) || (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)))
		{
			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
			{
				SetCount(5);
			}
			else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				SetCount(10);
			}
			else
			{
				SetCount(1);
			}
		}
	}

	private void SetCount(int count)
	{
		if (this.count != count)
		{
			this.count = count;
			UpdateInfo();
		}
	}

	public void Select()
	{
		if (spec.building == null)
		{
			onSelect(this);
		}
	}

	public void HightlightOn()
	{
		back.enabled = true;
	}

	public void HightlightOff()
	{
		back.enabled = false;
	}

	public Sprite GetIcon()
	{
		return icon.sprite;
	}

	public void Upgrade()
	{
		if (spec != null && !spec.IsMaxLevel())
		{
			BigNumber cost = spec.GetCost(spec.Level.ValueInt, count);
			VariableBignumber brickCurrent = GameManager.Instance.Gilding.Buildings.brickCurrent;
			if (!(brickCurrent.Value.Floor() < cost))
			{
				brickCurrent.Change(-cost);
				spec.Upgrade(count);
				UpdateInfo();
			}
		}
	}

	public void UpdateInfo()
	{
		if (spec == null)
		{
			TextMeshProUGUI textMeshProUGUI = level;
			TextMeshProUGUI textMeshProUGUI2 = descr;
			string text = (costLabel.text = string.Empty);
			string text2 = (textMeshProUGUI2.text = text);
			textMeshProUGUI.text = text2;
			button.interactable = false;
		}
		else
		{
			level.text = spec.Level.ValueInt.ToString();
			descr.text = spec.GetDescription();
			BigNumber cost = spec.GetCost(spec.Level.ValueInt, count);
			costLabel.text = cost.ToReadableString("F0");
			button.interactable = GameManager.Instance.Gilding.Buildings.brickCurrent.Value.Floor() >= cost;
		}
	}

	public string GetBody()
	{
		string empty = string.Empty;
		empty = ((!(spec is BuildingSpecialization)) ? string.Format("BuildingSpecProdGrowthTooltip".Translate(), (spec as BuildingSpecProduction).MaxLvl) : spec.GetBuildingGrowthRate().ToString(CultureInfo.InvariantCulture));
		return string.Format("BuidlingSpecTooltip".Translate(), spec.GetPPS().ToReadableString(), spec.BuildingCost.ToReadableString(), empty, (spec.building == null) ? string.Empty : ("\n" + "BuidlingSpecAssign".Translate() + " " + spec.building.Name + "\n"));
	}
}
