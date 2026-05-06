using System;
using System.Collections.Generic;
using UnityEngine;

public class RealmManager : MonoBehaviour
{
	public class SaveData
	{
		public ulong Realms;

		public BigNumber Memories = 0.0;

		public BigNumber SwitchMemories = 0.0;

		public BigNumber TotalMemories = 0.0;

		public Dictionary<int, int> Upgrades;

		public List<MemoryPreset> MemorySets;

		public ImprintCarryOver CarryOver;

		public SaveData()
		{
			Realms = 0uL;
			Memories = 0.0;
			TotalMemories = 0.0;
			Upgrades = new Dictionary<int, int>();
			MemorySets = new List<MemoryPreset>();
			CarryOver = null;
		}

		public SaveData(ulong realms, BigNumber mems, BigNumber switchMems, BigNumber totalMems)
		{
			Realms = realms;
			Memories = mems;
			SwitchMemories = switchMems;
			TotalMemories = totalMems;
			Upgrades = new Dictionary<int, int>();
			CarryOver = null;
		}
	}

	public enum MemoryGroups
	{
		Mana = 0,
		Power = 1,
		Tools = 2,
		Foundation = 3,
		Imprint = 10
	}

	public class UpdateFormat
	{
		public string ID;

		public string Name;

		public string Sprite;

		public string Description;

		public string Param;

		public string A;

		public string M;

		public string P;

		public string MaxLvl;

		public string Cost;

		public string CostD;

		public string Req;

		public string Reset;

		public string Group;
	}

	public VariableBignumber Memories;

	public VariableBignumber MemoriesSwitch;

	public VariableBignumber TotalMemories;

	public VariableBignumber MemoriesIncome;

	public VariableLong Realms;

	public RealmWindow window;

	public RealmUpgradePassive enchant;

	public RealmUpgradePassive catalyst;

	private List<RealmUpgrade> upgrades;

	[SerializeField]
	private RealmComic comicsPrefab;

	private RealmComic comics;

	private string realm_message = "RealmChangeMessage";

	private VariableFloat maxCatasMult;

	private VariableFloat maxEdustMult;

	private VariableFloat timeMultSpeed;

	public MemorySets MemorySets;

	public Action OnRealm;

	public ImprintCarryOver CarryOver;

	private bool resetAll;

	public void Init()
	{
		Realms = new VariableLong(0uL);
		Memories = new VariableBignumber(0.0);
		MemoriesSwitch = new VariableBignumber(0.0);
		TotalMemories = new VariableBignumber(0.0);
		MemoriesIncome = new VariableBignumber(1.0);
		maxCatasMult = new VariableFloat(2f);
		maxEdustMult = new VariableFloat(2f);
		timeMultSpeed = new VariableFloat(1f);
		GameContext.ContextAddResource("Realm.Times", Realms);
		GameContext.ContextAddResource("Realm.Memories", Memories);
		GameContext.ContextAddResource("Realm.MemoriesSwitch", MemoriesSwitch);
		GameContext.ContextAddResource("Realm.TotalMemories", TotalMemories);
		GameContext.ContextAddResource("Realm.Income", MemoriesIncome);
		GameContext.ContextAddResource("Realm.MaxCatasMult", maxCatasMult);
		GameContext.ContextAddResource("Realm.MaxEdustMult", maxEdustMult);
		GameContext.ContextAddResource("Realm.TimeMultSpeed", timeMultSpeed);
		enchant = new RealmUpgradePassive(new SimpleEffect(GameManager.Instance.Resources.EDustIncome, 0.035, 0.5, EffectNames.PowA), MemoryGroups.Tools, TotalMemories, "EnchantingMemoryScale");
		catalyst = new RealmUpgradePassive(new SimpleEffect(GameManager.Instance.BuildingManager.AllIncome, 0.035, 0.5, EffectNames.PowA), MemoryGroups.Tools, TotalMemories, "CatalystMemoryScale");
		MemorySets = new MemorySets();
		CreateAll();
		window.Init(upgrades);
	}

	private void CreateAll()
	{
		upgrades = new List<RealmUpgrade>();
		foreach (UpdateFormat realmUpgrade in GlobalData.RealmUpgrades)
		{
			if (realmUpgrade.Param != null && realmUpgrade.Param != string.Empty)
			{
				upgrades.Add(new RealmUpgrade(realmUpgrade));
			}
		}
	}

	public List<RealmUpgrade> GetBought()
	{
		return upgrades.FindAll((RealmUpgrade x) => !x.IsSwitch && x.Level.ValueInt > 0);
	}

	public RealmUpgrade GetUpgrade(int id)
	{
		return upgrades.Find((RealmUpgrade x) => x.Id == id);
	}

	public bool IsAvailable()
	{
		if (!GameManager.Instance.Paragon.RealmsIsAvailable)
		{
			return TotalMemories.Value > 1.0;
		}
		return true;
	}

	public void ResetOnExile()
	{
		foreach (RealmUpgrade upgrade in upgrades)
		{
			if (upgrade.Reset == 0)
			{
				upgrade.Remove();
			}
		}
		enchant.Remove();
		catalyst.Remove();
	}

	public void RemoveAll()
	{
		foreach (RealmUpgrade upgrade in upgrades)
		{
			upgrade.Remove();
		}
		enchant.Remove();
		catalyst.Remove();
	}

	public void ApplyAll()
	{
		foreach (RealmUpgrade upgrade in upgrades)
		{
			upgrade.Apply();
		}
		enchant.Apply();
		catalyst.Apply();
	}

	public BigNumber GetMemoriesMysts(BigNumber mysts)
	{
		if (mysts.Exponent < 360)
		{
			return 0.0;
		}
		BigNumber bigNumber = new BigNumber(mysts.Log10() - 360.0).Pow(1.6699999570846558);
		if (bigNumber < 1.0)
		{
			bigNumber = 0.0;
		}
		return bigNumber * MemoriesIncome.Value;
	}

	public BigNumber GetMemoriesCatas(BigNumber catas)
	{
		return 5.179999828338623 * (catas * 0.5).Pow(0.4000000059604645) * MemoriesIncome.Value;
	}

	public BigNumber GetMemoriesEdust(BigNumber dust)
	{
		return 1.809999942779541 * (dust * 30.0).Pow(0.43299999833106995) * MemoriesIncome.Value;
	}

	public float GetCatasMultipler()
	{
		BigNumber bigNumber = 1.0 + GetMemoriesMysts(GameManager.Instance.Reborn.GetAllMysts());
		BigNumber memoriesCatas = GetMemoriesCatas(GameManager.Instance.BuildingManager.CatalystAmount.Value);
		return (1.0 + memoriesCatas / bigNumber).Clamp(0.0, maxCatasMult.ValueFloat).ToFloat();
	}

	public float GetEdustMultipler()
	{
		BigNumber bigNumber = 1.0 + GetMemoriesMysts(GameManager.Instance.Reborn.GetAllMysts());
		BigNumber memoriesEdust = GetMemoriesEdust(Statistic.GetEdustRealm().Value);
		return (1.0 + memoriesEdust / bigNumber).Clamp(0.0, maxEdustMult.ValueFloat).ToFloat();
	}

	public float GetTimeMultiplier()
	{
		float num = (float)Statistic.TimeRealm.ValueInt * timeMultSpeed.ValueFloat / 86400f;
		if (num > 120f)
		{
			return 10f;
		}
		if (num <= 1f)
		{
			return num * num;
		}
		if (num <= 10f)
		{
			return 1f + (num - 1f) / 4.5f;
		}
		float num2 = 2f + Mathf.Pow(num / 10f, 0.84f);
		if (num2 >= 10f)
		{
			return 10f;
		}
		return num2;
	}

	public BigNumber CalcualteMemories()
	{
		return GetMemoriesMysts(GameManager.Instance.Reborn.GetAllMysts()) * GetCatasMultipler() * GetEdustMultipler() * GetTimeMultiplier();
	}

	public void GetMemories()
	{
		BigNumber addendum = CalcualteMemories();
		Memories.Change(addendum);
		MemoriesSwitch.Change(addendum);
		TotalMemories.Change(addendum);
		Realms.Change(1);
	}

	public void Convert()
	{
		resetAll = false;
		if (TotalMemories.Value > 1.0)
		{
			GameManager.Instance.ConfirmWindow.Open(realm_message.Translate(), ChangeWithComics, "RealmChangeEnableMemories".Translate(), delegate
			{
				resetAll = true;
			});
			if (GameManager.Instance.Paragon.GildingBuildingsIsAvailable)
			{
				GameManager.Instance.ConfirmWindow.AddOption2("RealmChangeEnableSources".Translate(), GameManager.Instance.Gilding.Buildings.ResetSoft);
			}
			if (GameManager.Instance.Paragon.GildingSpellsIsAvailable)
			{
				GameManager.Instance.ConfirmWindow.AddOption3("RealmChangeEnableSpells".Translate(), GameManager.Instance.Gilding.Spells.ResetSoft);
			}
			if (GameManager.Instance.Realmcraft.IsInvested())
			{
				GameManager.Instance.ConfirmWindow.AddOption4("RealmChangeEnableDelusion".Translate(), GameManager.Instance.Realmcraft.ResetSoft);
			}
			if (GameManager.Instance.Paragon.GildingPantheonIsAvailable)
			{
				GameManager.Instance.ConfirmWindow.AddOption5("ExileResetPantheon".Translate(), GameManager.Instance.Gilding.Pantheon.ResetSoft);
			}
		}
		else
		{
			GameManager.Instance.ConfirmWindow.Open(realm_message.Translate(), ChangeWithComics);
		}
	}

	public void ChangeWithComics()
	{
		if (Settings.ShowComics)
		{
			ShowComics();
			Invoke("ChangeRealm", 1f);
			if (Realms.ValueInt == 0L)
			{
				Settings.ShowComics = false;
			}
		}
		else
		{
			ChangeRealm();
		}
	}

	private void ChangeRealm()
	{
		GetMemories();
		GameManager.Instance.RealmChange(ResetAll);
		OnRealm?.Invoke();
	}

	public void ResetAll(bool resetAll)
	{
		this.resetAll = resetAll;
		ResetAll();
	}

	private void ResetAll()
	{
		if (resetAll)
		{
			window.ResetAll(toZero: false);
			window.Open();
		}
	}

	public void PreReset()
	{
		if (!GameManager.Instance.Realmcraft.IsActive)
		{
			CarryOver = new ImprintCarryOver();
			CarryOver.Save();
		}
	}

	public void PostReset()
	{
		if (!GameManager.Instance.Realmcraft.IsActive && CarryOver != null)
		{
			CarryOver.Load();
			CarryOver = null;
		}
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData(Realms.ValueInt, Memories.Value, MemoriesSwitch.Value, TotalMemories.Value);
		foreach (RealmUpgrade upgrade in upgrades)
		{
			if (upgrade.Level.ValueInt > 0)
			{
				saveData.Upgrades.Add(upgrade.Id, upgrade.Level.ValueInt);
			}
		}
		saveData.MemorySets = MemorySets.Save();
		saveData.CarryOver = CarryOver;
		return saveData;
	}

	public void ShowComics()
	{
		if (comics == null)
		{
			comics = UnityEngine.Object.Instantiate(comicsPrefab);
		}
		comics.Show(OnEndComics);
	}

	private void OnEndComics()
	{
		UnityEngine.Object.Destroy(comics.gameObject);
	}

	public void Load(SaveData data)
	{
		Clear();
		if (data == null)
		{
			return;
		}
		Realms.SetValue(data.Realms);
		Memories.SetValue(data.Memories);
		MemoriesSwitch.SetValue(data.SwitchMemories);
		TotalMemories.SetValue(data.TotalMemories);
		foreach (KeyValuePair<int, int> v in data.Upgrades)
		{
			upgrades.Find((RealmUpgrade x) => x.Id == v.Key)?.Level.SetValue(v.Value);
		}
		MemorySets.Load(data.MemorySets);
		CarryOver = data.CarryOver;
		window.Refresh();
		window.ApplyAll();
	}

	public void Clear()
	{
		Realms.SetValue(0uL);
		Memories.SetValue(0.0);
		MemoriesSwitch.SetValue(0.0);
		TotalMemories.SetValue(0.0);
		window.ResetAllUpgrades();
		foreach (RealmUpgrade upgrade in upgrades)
		{
			upgrade.Remove();
			upgrade.Level.SetValue(0);
		}
		window.Refresh();
	}

	public void ResetMemoriesLegacy()
	{
		Memories.SetValue(TotalMemories.Value);
		window.Abort();
		foreach (RealmUpgrade upgrade in upgrades)
		{
			if (!upgrade.IsSwitch)
			{
				upgrade.Remove();
				upgrade.Level.SetValue(0);
			}
		}
		window.Refresh();
	}
}
