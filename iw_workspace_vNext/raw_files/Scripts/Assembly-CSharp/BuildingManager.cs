using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
	[Serializable]
	public class RetainSaveData
	{
		public ulong g;

		public ulong b;

		public ulong r;
	}

	public struct CatalystSaveFormat
	{
		public int t;

		public ulong a;

		public ulong m;

		public ulong r;

		public CatalystSaveFormat(int tier, ulong aCat, ulong mCat, ulong rCat)
		{
			t = tier;
			a = aCat;
			m = mCat;
			r = rCat;
		}
	}

	public class CatalystSave
	{
		public List<CatalystSaveFormat> Catalysts;

		public BigNumber Total;

		public BigNumber tA;

		public BigNumber fA;

		public BigNumber tM;

		public BigNumber fM;

		public BigNumber tR;

		public BigNumber fR;

		public bool IsAll;
	}

	[SerializeField]
	public BuildingCreator BuildCreator;

	public List<BuildingVisual> Buildings;

	public VariableComplex CatalystAddPower;

	public VariableComplex CatalystAddPowerScale;

	public VariableComplex CatalystMultPower;

	public VariableComplex CatalystTempPower;

	public VariableBignumber CostReduction;

	public VariableComplex PPSFromBuildings;

	public VariableBignumber CatalystAmount;

	public VariableBignumber CatalystAmountTotal;

	public VariableBignumber TotalGreen;

	public VariableBignumber FreeGreenCatalysts;

	public VariableBignumber TotalBlue;

	public VariableBignumber FreeBlueCatalysts;

	public VariableBignumber TotalRed;

	public VariableBignumber FreeRedCatalysts;

	public VariableComplex GreenIncome;

	public VariableComplex BlueIncome;

	public VariableComplex RedIncome;

	public VariableComplex AllIncome;

	public VariableComplex AllIncomeOverCap;

	public VariableFloat GreenCapBonus;

	public VariableFloat GreenSave;

	public VariableFloat BlueSave;

	public VariableFloat RedSave;

	public VariableComplex AllBuildings;

	public GameObject CatalystTradeButton;

	public CatalystTrade CatalystTrade;

	public Action<BigNumber> OnInvestCatalysts;

	public Action OnGainCatalysts;

	public Action OnChangeBuildings;

	[SerializeField]
	private BuildingListAnimation list;

	public BuildingVisual special;

	private const float spaceActive = -15f;

	private const float spaceInactive = -6.5f;

	private IEnumerator currentBuildingUp;

	private float timerBuildingUp;

	private void Start()
	{
		if (CatalystAmount.Value > 0.0)
		{
			EnableTrade();
			return;
		}
		VariableBignumber catalystAmount = CatalystAmount;
		catalystAmount.OnChange = (Action)Delegate.Combine(catalystAmount.OnChange, new Action(EnableTrade));
	}

	private void EnableTrade()
	{
		CatalystTradeButton.SetActive(value: true);
		VariableBignumber catalystAmount = CatalystAmount;
		catalystAmount.OnChange = (Action)Delegate.Remove(catalystAmount.OnChange, new Action(EnableTrade));
	}

	public void TurnOnRed()
	{
		foreach (BuildingVisual building in Buildings)
		{
			building.building.TurnOnRed();
		}
	}

	public void TurnOffRed()
	{
		foreach (BuildingVisual building in Buildings)
		{
			building.building.TurnOffRed();
		}
	}

	public void Init()
	{
		PPSFromBuildings = new VariableComplex(0.0);
		CatalystAmount = new VariableBignumber(0.0);
		CatalystAmountTotal = new VariableBignumber(0.0);
		FreeGreenCatalysts = new VariableBignumber(0.0);
		FreeBlueCatalysts = new VariableBignumber(0.0);
		FreeRedCatalysts = new VariableBignumber(0.0);
		TotalGreen = new VariableBignumber(0.0);
		TotalBlue = new VariableBignumber(0.0);
		TotalRed = new VariableBignumber(0.0);
		CatalystAddPower = new VariableComplex(0.125);
		CatalystAddPowerScale = new VariableComplex(1.0);
		CatalystMultPower = new VariableComplex(0.04);
		CatalystTempPower = new VariableComplex(1.0);
		GreenIncome = new VariableComplex(1.0);
		BlueIncome = new VariableComplex(1.0);
		RedIncome = new VariableComplex(1.0);
		AllIncome = new VariableComplex(1.0);
		AllIncomeOverCap = new VariableComplex(1.0);
		GreenCapBonus = new VariableFloat(1f);
		GreenSave = new VariableFloat(1f);
		BlueSave = new VariableFloat(1f);
		RedSave = new VariableFloat(1f);
		CostReduction = new VariableBignumber(1.0);
		AllBuildings = new VariableComplex(0.0);
		BuildCreator.Create();
		foreach (BuildingVisual building in Buildings)
		{
			building.Init();
			VariableComplex catalystTempPower = CatalystTempPower;
			catalystTempPower.OnChange = (Action)Delegate.Combine(catalystTempPower.OnChange, new Action(building.building.RecalculateTemp));
		}
		VariableComplex catalystTempPower2 = CatalystTempPower;
		catalystTempPower2.OnChange = (Action)Delegate.Combine(catalystTempPower2.OnChange, new Action(special.building.RecalculateTemp));
		GameContext.ContextAddResource("Base.Catalysts", CatalystAmount);
		GameContext.ContextAddResource("Base.CatalystsTotal", CatalystAmountTotal);
		GameContext.ContextAddResource("Base.AllBuildings", AllBuildings);
		GameContext.ContextAddResource("Base.BuildingCostReduction", CostReduction);
		GameContext.ContextAddResource("Catalysts.GreenBonus", CatalystAddPower);
		GameContext.ContextAddResource("Catalysts.GreenScale", CatalystAddPowerScale);
		GameContext.ContextAddResource("Catalysts.BlueBonus", CatalystMultPower);
		GameContext.ContextAddResource("Catalysts.RedBonus", CatalystTempPower);
		GameContext.ContextAddResource("Catalysts.GreenIncome", GreenIncome);
		GameContext.ContextAddResource("Catalysts.BlueIncome", BlueIncome);
		GameContext.ContextAddResource("Catalysts.RedIncome", RedIncome);
		GameContext.ContextAddResource("Catalysts.AllIncome", AllIncome);
		GameContext.ContextAddResource("Catalysts.AllIncomeOverCap", AllIncomeOverCap);
		GameContext.ContextAddResource("Catalysts.GreenCapBonus", GreenCapBonus);
		GameContext.ContextAddResource("Catalysts.GreenSave", GreenSave);
		GameContext.ContextAddResource("Catalysts.BlueSave", BlueSave);
		GameContext.ContextAddResource("Catalysts.RedSave", RedSave);
		CatalystTrade.Init();
	}

	public void ResetBuildings()
	{
		for (int i = 0; i < Buildings.Count; i++)
		{
			Buildings[i].Restart();
		}
	}

	public void ResetBuildingsRealm()
	{
		TotalGreen.SetValue(0.0);
		TotalBlue.SetValue(0.0);
		TotalRed.SetValue(0.0);
		for (int i = 0; i < Buildings.Count; i++)
		{
			Buildings[i].Restart();
			Building building = Buildings[i].building;
			building.ACatalyst = 0uL;
			building.MCatalyst = 0uL;
			building.SetRedCatalysts(0uL);
		}
		FreeGreenCatalysts.SetValue(0.0);
		FreeBlueCatalysts.SetValue(0.0);
		FreeRedCatalysts.SetValue(0.0);
		CatalystAmount.SetValue(0.0);
	}

	public Dictionary<int, RetainSaveData> SaveRetain()
	{
		if (GreenSave.ValueFloat == 0f && BlueSave.ValueFloat == 0f && RedSave.ValueFloat == 0f)
		{
			return null;
		}
		Dictionary<int, RetainSaveData> dictionary = new Dictionary<int, RetainSaveData>();
		for (int i = 0; i < Buildings.Count; i++)
		{
			RetainSaveData retainSaveData = new RetainSaveData();
			Building building = Buildings[i].building;
			if (building.Tier <= 8)
			{
				retainSaveData.g = getSavedCatas(building.ACatalyst, GreenSave.ValueFloat);
				retainSaveData.b = getSavedCatas(building.MCatalyst, BlueSave.ValueFloat);
				retainSaveData.r = getSavedCatas(building.RCatalyst, RedSave.ValueFloat);
				if (retainSaveData.g != 0L || retainSaveData.b != 0L || retainSaveData.r != 0L)
				{
					dictionary.Add(building.Tier, retainSaveData);
				}
			}
		}
		return dictionary;
	}

	public void LoadRetain(Dictionary<int, RetainSaveData> save)
	{
		if (save == null)
		{
			return;
		}
		foreach (KeyValuePair<int, RetainSaveData> item in save)
		{
			Building building = GetBuilding(item.Key);
			building.ACatalyst = item.Value.g;
			building.MCatalyst = item.Value.b;
			building.SetRedCatalysts(item.Value.r);
			TotalGreen.Change(CatalystTrade.GetGreenInvest().GetSumm(building.ACatalyst) + GetSumm(building.ACatalyst));
			TotalBlue.Change(CatalystTrade.GetBlueInvest().GetSumm(building.MCatalyst) + GetSumm(building.MCatalyst));
			TotalRed.Change(CatalystTrade.GetRedInvest().GetSumm(building.RCatalyst) + GetSumm(building.RCatalyst));
		}
		CatalystAmount.SetValue(TotalGreen.Value + TotalBlue.Value + TotalRed.Value);
	}

	private ulong getSavedCatas(ulong current, float savePart)
	{
		if (savePart > 1f)
		{
			return (ulong)Mathf.FloorToInt((float)current * (savePart - 1f));
		}
		return 0uL;
	}

	public void HardReset()
	{
		ResetBuildingsRealm();
		CatalystAmountTotal.SetValue(0.0);
	}

	public BigNumber GetBuildingProfit()
	{
		BigNumber result = 0.0;
		foreach (BuildingVisual building2 in Buildings)
		{
			if (building2 == null || building2.building == null)
			{
				Debug.LogError("OUT of range!");
				return result;
			}
			Building building = building2.building;
			if (building.Level.ValueInt + building.TemporalyLevel.ValueInt > 0)
			{
				building.CalculatePps();
				result += building.Pps.Value;
			}
		}
		return result;
	}

	public Building GetBuilding(int tier)
	{
		return Buildings.Find((BuildingVisual x) => x.Tier == tier)?.building;
	}

	public void GiveGreen(BigNumber value)
	{
		FreeGreenCatalysts.Change(value);
		TotalGreen.Change(value);
		CatalystAmount.Change(value);
		CatalystAmountTotal.Change(value);
		OnGainCatalysts?.Invoke();
	}

	public void GiveRed(BigNumber value)
	{
		FreeRedCatalysts.Change(value);
		TotalRed.Change(value);
		CatalystAmount.Change(value);
		CatalystAmountTotal.Change(value);
		OnGainCatalysts?.Invoke();
	}

	public void GiveBlue(BigNumber value)
	{
		FreeBlueCatalysts.Change(value);
		TotalBlue.Change(value);
		CatalystAmount.Change(value);
		CatalystAmountTotal.Change(value);
		OnGainCatalysts?.Invoke();
	}

	public ulong GetGreenIncome(int tier)
	{
		return (((Buildings.Find((BuildingVisual x) => x.Tier == tier).building.ACatalyst + 1) * GreenIncome.Value * AllIncome.Value).ToUlong() * AllIncomeOverCap.Value).ToUlong();
	}

	public ulong GetBlueIncome(int tier)
	{
		return (((Buildings.Find((BuildingVisual x) => x.Tier == tier).building.MCatalyst + 1) * BlueIncome.Value * AllIncome.Value).ToUlong() * AllIncomeOverCap.Value).ToUlong();
	}

	public ulong GetRedIncome(int tier)
	{
		return (((Buildings.Find((BuildingVisual x) => x.Tier == tier).building.RCatalyst + 1) * RedIncome.Value * AllIncome.Value).ToUlong() * AllIncomeOverCap.Value).ToUlong();
	}

	public void AddACatalysts(int tier, ulong amount)
	{
		GetBuilding(tier).ACatalyst += amount;
		if (OnInvestCatalysts != null)
		{
			OnInvestCatalysts(amount);
		}
	}

	public void AddMCatalysts(int tier, ulong amount)
	{
		GetBuilding(tier).MCatalyst += amount;
		if (OnInvestCatalysts != null)
		{
			OnInvestCatalysts(amount);
		}
	}

	public void AddRCatalysts(int tier, ulong amount)
	{
		Building building = GetBuilding(tier);
		building.SetRedCatalysts(building.RCatalyst + amount);
		if (OnInvestCatalysts != null)
		{
			OnInvestCatalysts(amount);
		}
	}

	public void CalculateCatalysts()
	{
		CatalystAmount.SetValue(TotalGreen.Value + TotalRed.Value + TotalBlue.Value);
	}

	public ulong GetSumm(ulong n)
	{
		return n * (n + 1) / 2;
	}

	public void SetACatalysts(int tier, ulong amount)
	{
		GetBuilding(tier).ACatalyst = amount;
	}

	public void SetMCatalysts(int tier, ulong amount)
	{
		GetBuilding(tier).MCatalyst = amount;
	}

	public void SetRCatalysts(int tier, ulong amount)
	{
		GetBuilding(tier).LoadRedCatalysts(amount);
	}

	public CatalystSave SaveCatalysts()
	{
		CatalystSave catalystSave = new CatalystSave();
		catalystSave.Catalysts = new List<CatalystSaveFormat>();
		for (int i = 0; i < Buildings.Count; i++)
		{
			Building building = Buildings[i].building;
			if (building.Tier <= 8 && (building.ACatalyst != 0L || building.MCatalyst != 0L || building.RCatalyst != 0L))
			{
				catalystSave.Catalysts.Add(new CatalystSaveFormat(building.Tier, building.ACatalyst, building.MCatalyst, building.RCatalyst));
			}
		}
		catalystSave.fA = FreeGreenCatalysts.Value;
		catalystSave.tA = TotalGreen.Value;
		catalystSave.fM = FreeBlueCatalysts.Value;
		catalystSave.tM = TotalBlue.Value;
		catalystSave.fR = FreeRedCatalysts.Value;
		catalystSave.tR = TotalRed.Value;
		catalystSave.Total = CatalystAmountTotal.Value;
		catalystSave.IsAll = CatalystTrade.IsAll.isOn;
		return catalystSave;
	}

	public void LoadCatalysts(CatalystSave save)
	{
		for (int i = 1; i < Buildings.Count + 1; i++)
		{
			SetACatalysts(i, 0uL);
			SetMCatalysts(i, 0uL);
			SetRCatalysts(i, 0uL);
		}
		FreeGreenCatalysts.SetValue(0.0);
		FreeBlueCatalysts.SetValue(0.0);
		FreeRedCatalysts.SetValue(0.0);
		TotalGreen.SetValue(0.0);
		TotalBlue.SetValue(0.0);
		TotalRed.SetValue(0.0);
		CatalystAmount.SetValue(0.0);
		CatalystAmountTotal.SetValue(0.0);
		if (save == null)
		{
			return;
		}
		for (int j = 0; j < save.Catalysts.Count; j++)
		{
			CatalystSaveFormat catalystSaveFormat = save.Catalysts[j];
			if (catalystSaveFormat.t <= 8)
			{
				SetACatalysts(catalystSaveFormat.t, catalystSaveFormat.a);
				SetMCatalysts(catalystSaveFormat.t, catalystSaveFormat.m);
				SetRCatalysts(catalystSaveFormat.t, catalystSaveFormat.r);
			}
		}
		FreeGreenCatalysts.SetValue(save.fA);
		FreeBlueCatalysts.SetValue(save.fM);
		FreeRedCatalysts.SetValue(save.fR);
		TotalGreen.SetValue(save.tA);
		TotalBlue.SetValue(save.tM);
		TotalRed.SetValue(save.tR);
		CalculateCatalysts();
		CatalystAmountTotal.SetValue(save.Total);
		CatalystTrade.IsAll.isOn = save.IsAll;
		CatalystTrade.OnChangeMassInvest();
	}

	public void ResetUpBuilding()
	{
		timerBuildingUp = 0f;
		if (currentBuildingUp != null)
		{
			StopCoroutine(currentBuildingUp);
			currentBuildingUp = null;
		}
	}

	public void UpBuilding(float time, Func<BigNumber, int> recalc, List<BuildingVisual> list, BigNumber total_mana_mult, Action Fail)
	{
		if (list == null)
		{
			Debug.LogError("Targets for Building auto-buy is NULL");
		}
		else if (currentBuildingUp == null)
		{
			timerBuildingUp = time;
			list = list.FindAll((BuildingVisual x) => x.building.Level.ValueInt > 0);
			if (total_mana_mult != 0.0)
			{
				BigNumber max_building_cost = Statistic.ManaRealm.Value * total_mana_mult;
				list = FindAllForBuildingUp(list, null, max_building_cost);
			}
			if (list.Count == 0)
			{
				Fail?.Invoke();
				return;
			}
			currentBuildingUp = up_buildings_co(recalc, list, total_mana_mult, Fail);
			StartCoroutine(currentBuildingUp);
		}
	}

	public void EnableNine(BuildingGilding.SourceType key)
	{
		Buildings.Add(special);
		list.UpdateSpacing(-15f);
		special.ChangeAvalible(v: true);
		GameManager.Instance.Gilding.Buildings.Map[key].SelectBuilding(special.building, reset: false);
		OnChangeBuildings?.Invoke();
	}

	public void DisableNine(BuildingGilding.SourceType key)
	{
		Buildings.Remove(special);
		special.ChangeAvalible(v: false);
		if (special.building != null)
		{
			if (special.building.spec != null)
			{
				special.building.spec.ResetBuilding();
			}
			special.building.Restart();
		}
		Statistic.TotalBuildings.Change(-special.building.Level.ValueInt);
		list.UpdateSpacing(-6.5f);
		OnChangeBuildings?.Invoke();
	}

	public float GetSpacing()
	{
		if (Buildings.Count > 8)
		{
			return -15f;
		}
		return -6.5f;
	}

	public List<int> GetAllUpgradeId()
	{
		List<int> list = new List<int>();
		foreach (BuildingVisual building in Buildings)
		{
			if (building.building.Upgrade != 0)
			{
				list.Add(building.building.Upgrade);
			}
		}
		return list;
	}

	private IEnumerator up_buildings_co(Func<BigNumber, int> recalc, List<BuildingVisual> list, BigNumber total_mana_mult, Action Fail)
	{
		bool success = true;
		BigNumber max_building_cost = 0.0;
		Dictionary<int, int> additionalLevels = new Dictionary<int, int>();
		float period = recalc?.Invoke(Statistic.TotalBuildings.Value) ?? 1;
		while (timerBuildingUp >= period && success)
		{
			if (total_mana_mult != 0.0)
			{
				max_building_cost = Statistic.ManaRealm.Value * total_mana_mult;
			}
			for (int i = 0; i < 1000 && success; i++)
			{
				if (!(timerBuildingUp >= period))
				{
					break;
				}
				if (total_mana_mult != 0.0)
				{
					list = FindAllForBuildingUp(list, additionalLevels, max_building_cost);
				}
				if (list.Count > 0)
				{
					Building building = list[UnityEngine.Random.Range(0, list.Count)].building;
					if (additionalLevels.ContainsKey(building.Tier))
					{
						additionalLevels[building.Tier]++;
					}
					else
					{
						additionalLevels.Add(building.Tier, 1);
					}
					success = true;
				}
				else
				{
					Fail?.Invoke();
					success = false;
				}
				timerBuildingUp -= period;
				if (recalc != null)
				{
					period = recalc(Statistic.TotalBuildings.Value + i);
				}
			}
			if (additionalLevels.Count > 0)
			{
				int num = 0;
				foreach (KeyValuePair<int, int> item in additionalLevels)
				{
					GetBuilding(item.Key).Level.Change(item.Value);
					num += item.Value;
				}
				Statistic.TotalBuildings.Change(num);
				additionalLevels = new Dictionary<int, int>();
			}
			yield return null;
		}
		timerBuildingUp = 0f;
		currentBuildingUp = null;
	}

	private BigNumber GetCost(Building building, Dictionary<int, int> additionalLevels = null)
	{
		int additionalLevels2 = 0;
		if (additionalLevels != null && additionalLevels.ContainsKey(building.Tier))
		{
			additionalLevels2 = additionalLevels[building.Tier];
		}
		return building.GetCost(additionalLevels2);
	}

	private List<BuildingVisual> FindAllForBuildingUp(List<BuildingVisual> buildings, Dictionary<int, int> additionalLevels, BigNumber max_building_cost)
	{
		List<BuildingVisual> list = new List<BuildingVisual>();
		for (int i = 0; i < buildings.Count; i++)
		{
			BuildingVisual buildingVisual = buildings[i];
			if (GetCost(buildingVisual.building, additionalLevels) <= max_building_cost)
			{
				list.Add(buildingVisual);
			}
		}
		return list;
	}
}
