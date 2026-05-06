using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingVisual : MonoBehaviour
{
	public int Tier;

	public Building building;

	public TextMeshProUGUI NameLabel;

	public Image Icon;

	public Image sprite;

	public Button button;

	public TextMeshProUGUI level_label;

	public TextMeshProUGUI cost_label;

	public TextMeshProUGUI profit_label;

	public TextMeshProUGUI profit_part_label;

	public TextMeshProUGUI tip_label;

	public double PartOfProfit;

	private int count_to_buy;

	private int prev_pack = -1;

	private BigNumber cost = 0.0;

	private bool tipIsActive;

	private float timer;

	private bool subscribe;

	public void Init()
	{
		VariableComplex base_cost = building.base_cost;
		base_cost.OnChange = (Action)Delegate.Combine(base_cost.OnChange, new Action(Recalculate));
		VariableComplex pps_per_building = building.pps_per_building;
		pps_per_building.OnChange = (Action)Delegate.Combine(pps_per_building.OnChange, new Action(Recalculate));
		VariableInt temporalyLevel = building.TemporalyLevel;
		temporalyLevel.OnChange = (Action)Delegate.Combine(temporalyLevel.OnChange, new Action(Recalculate));
		TranslationManager instance = TranslationManager.Instance;
		instance.OnChangeLanguage = (Action)Delegate.Combine(instance.OnChangeLanguage, new Action(UpdateName));
	}

	private void OnEnable()
	{
		UpdateName();
	}

	private void UpdateName()
	{
		NameLabel.text = building.Name;
	}

	public void Restart()
	{
		update_text();
		NameLabel.text = building.Name;
		building.Restart();
		ChangeAvalible(v: true);
	}

	private void LateUpdate()
	{
		if (Time.timeScale == 0f)
		{
			return;
		}
		timer += Time.unscaledDeltaTime;
		if ((double)timer > 0.25)
		{
			update_building();
			if (tipIsActive)
			{
				update_tips();
			}
			timer = 0f;
		}
		RecalculatePPS();
	}

	private void enable()
	{
		button.gameObject.SetActive(value: true);
		if (subscribe)
		{
			subscribe = false;
			VariableBignumber mana = GameManager.Instance.Mana;
			mana.OnChange = (Action)Delegate.Remove(mana.OnChange, new Action(CheckEnable));
		}
	}

	public void ChangeAvalible(bool v)
	{
		building.Available = v;
		if (v)
		{
			if (!subscribe)
			{
				subscribe = true;
				VariableBignumber mana = GameManager.Instance.Mana;
				mana.OnChange = (Action)Delegate.Combine(mana.OnChange, new Action(CheckEnable));
				building.Acitvate();
				button.interactable = false;
				base.gameObject.SetActive(value: false);
			}
		}
		else
		{
			if (subscribe)
			{
				subscribe = false;
				building.Deactivate();
				VariableBignumber mana2 = GameManager.Instance.Mana;
				mana2.OnChange = (Action)Delegate.Remove(mana2.OnChange, new Action(CheckEnable));
			}
			base.gameObject.SetActive(value: false);
		}
	}

	public void CheckEnable()
	{
		if (!building.Available)
		{
			return;
		}
		if (building.Tier > 1)
		{
			if (Statistic.ManaSession.Value >= building.CostBase || building.Level.ValueInt + building.TemporalyLevel.ValueInt > 0)
			{
				enable();
			}
			else
			{
				button.gameObject.SetActive(value: false);
			}
		}
		else
		{
			enable();
		}
	}

	public bool IsAvailable()
	{
		return building.Available;
	}

	public void Buy()
	{
		Recalculate();
		if (cost <= GameManager.Instance.Mana.Value)
		{
			building.Buy(GameManager.Instance.ManaChange);
			Recalculate();
		}
	}

	public void RecalculatePPS()
	{
		building.CalculatePps();
	}

	public void Recalculate()
	{
		RecalculatePPS();
		building.CalculateCost(GameManager.Instance.BuyPack);
		count_to_buy = building.levelsToBuy;
		cost = building.Cost;
	}

	public void update_text()
	{
		updatePartOfProfit();
		if (button.interactable)
		{
			if (count_to_buy > 0)
			{
				cost_label.text = count_to_buy + "X " + cost.ToReadableString();
			}
			else
			{
				cost_label.text = 1 + "X " + building.cost_current.ToReadableString();
			}
		}
		else
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (count_to_buy > 0)
			{
				stringBuilder.Append(count_to_buy);
				stringBuilder.Append("X ");
				if (Settings.RedCost)
				{
					stringBuilder.Append("<color=#ff3333ff>");
				}
				stringBuilder.Append(cost.ToReadableString());
				if (Settings.RedCost)
				{
					stringBuilder.Append("</color>");
				}
			}
			else
			{
				stringBuilder.Append(1);
				stringBuilder.Append("X ");
				if (Settings.RedCost)
				{
					stringBuilder.Append("<color=#ff3333ff>");
				}
				stringBuilder.Append(building.cost_current.ToReadableString());
				if (Settings.RedCost)
				{
					stringBuilder.Append("</color>");
				}
			}
			cost_label.text = stringBuilder.ToString();
		}
		level_label.text = building.TotalLevel.ValueInt.ToString();
		profit_label.text = building.Pps.Value.ToReadableString() + "/sec";
	}

	private void updatePartOfProfit()
	{
		double num;
		if ((float)BigNumber.Sign(GameManager.Instance.PPS.Value) > 0f)
		{
			building.CalculateCost();
			num = (building.Pps.Value / GameManager.Instance.PPS.Value).ToDouble();
		}
		else
		{
			num = 0.0;
		}
		if (num <= 1.0)
		{
			PartOfProfit = num;
		}
		if (PartOfProfit != 0.0)
		{
			profit_part_label.text = (PartOfProfit * 100.0).ToString("0.00", CultureInfo.InvariantCulture) + "%";
		}
		else
		{
			profit_part_label.text = string.Empty;
		}
	}

	private void update_building()
	{
		building.CalculateCost(GameManager.Instance.BuyPack);
		count_to_buy = building.levelsToBuy;
		cost = building.Cost;
		button.interactable = cost <= GameManager.Instance.Mana.Value && count_to_buy > 0;
		update_text();
	}

	private void CheckCost()
	{
		bool flag = false;
		if (prev_pack != GameManager.Instance.BuyPack)
		{
			building.CalculateCost(GameManager.Instance.BuyPack);
			count_to_buy = building.levelsToBuy;
			cost = building.Cost;
			prev_pack = GameManager.Instance.BuyPack;
			flag = true;
		}
		button.interactable = cost <= GameManager.Instance.Mana.Value && count_to_buy > 0;
		if (flag)
		{
			update_text();
		}
	}

	public void ShowTipLabel()
	{
		tipIsActive = true;
		update_tips();
		tip_label.transform.parent.gameObject.SetActive(value: true);
	}

	private void update_tips()
	{
		int num = 1;
		string text;
		if (count_to_buy > 0)
		{
			num = count_to_buy;
			text = cost.ToReadableString();
		}
		else
		{
			text = building.cost_current.ToReadableString();
		}
		string text2 = string.Empty;
		string empty = string.Empty;
		if (!building.IsEnableGoals())
		{
			text2 = building.spec.GetFullDescription() + "\n";
			empty = building.spec.GetNextGoal().ToString();
		}
		else
		{
			empty = building.NextGoal + ", x" + building.NextGoalBonus.ToString("F2", CultureInfo.InvariantCulture);
		}
		tip_label.text = string.Format("ManaSourceTooltip".Translate(), building.Name, (building.Level.ValueInt + building.TemporalyLevel.ValueInt).ToString(), num.ToString(), text, building.GetBasePPS.ToReadableString(), building.Pps.Value.ToReadableString(), (PartOfProfit * 100.0).ToString("F2", CultureInfo.InvariantCulture) + "%", text2, empty, building.Description.Translate(), "<sprite=7>x" + building.ACatalyst + " +" + (building.GetGreenCataPower() * 100.0).ToReadableString() + "%", ((1.0 + building.ACatalyst * building.GetGreenCataPower() - 1.0) * 100.0).ToReadableString() + "%", "<sprite=6>x" + building.MCatalyst + " x" + (GameManager.Instance.BuildingManager.CatalystMultPower.Value * 100.0).ToReadableString() + "%", (((1.0 + GameManager.Instance.BuildingManager.CatalystMultPower.Value).Pow(building.MCatalyst) - 1.0) * 100.0).ToReadableString() + "%", "<sprite=10>x" + building.RCatalyst + " +" + GameManager.Instance.BuildingManager.CatalystTempPower.Value.ToReadableString("F1"), (int)(GameManager.Instance.BuildingManager.CatalystTempPower.Value.ToFloat() * (float)building.RCatalyst));
	}

	public void HideTipLabel()
	{
		tip_label.transform.parent.gameObject.SetActive(value: false);
		tip_label.text = "";
		tipIsActive = false;
	}

	public bool isAvailableToBuy()
	{
		return cost <= GameManager.Instance.Mana.Value;
	}
}
