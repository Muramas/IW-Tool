using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftManager : MonoBehaviour
{
	public static int DustCap = 2000000000;

	public VariableBignumber Red;

	public VariableBignumber Green;

	public VariableBignumber Blue;

	public VariableBignumber Yellow;

	public VariableBignumber EnchantingDust;

	public VariableInt MaxEnchantLevel;

	public VariableFloat WeaponCharging;

	public WeaponScroll weaponScroll;

	public Dictionary<CraftResource, VariableBignumber> map;

	public Skill Crafting;

	public Skill Gathering;

	public float ProgressToNew;

	public float BaseProgressToUpgradeItem = 125f;

	public int ProgressToUpgradeItemCosts = 1000;

	private float maxProgressToNew = 1f;

	private float baseChanceToSuccessInvest = 0.25f;

	public CraftWindow window;

	public SpriteAtlas icons;

	public WeaponManager weapons;

	public MountManager mounts;

	public PhylacteryManager phylacteries;

	public List<Item> AllItems;

	public List<Item> DropList;

	public List<Item> AvailableItems;

	public ItemGilding ResearchGilding;

	public ItemGilding TrophiesGilding;

	public VariableFloat ResearchSavePart;

	public VariableFloat TrophySavePart;

	public Dictionary<ItemSetKeys, ItemSetBonus> SetBonuses;

	public CraftInvestment Investment;

	public TradeWindow trade;

	public VariableInt BonusEnchantLevel;

	public VariableInt BonusEnchantLevelChest;

	public VariableInt BonusEnchantLevelRing;

	public VariableInt BonusEnchantLevelHand;

	public VariableInt BonusEnchantLevelShoulder;

	public VariableBignumber ExperimentCraftEfficiency;

	public Action OnChange;

	public Action<int> OnEnchant;

	public Action OnEnchantSingle;

	public Action<Item> OnGetItem;

	public Action OnCraftItem;

	public Action OnEquipItem;

	[SerializeField]
	private AudioClip sfxSuccess;

	[SerializeField]
	private AudioClip sfxFail;

	[SerializeField]
	private float volume;

	public SlotUpgradeController Proficiency => window.slotUpgrade;

	public void Init()
	{
		map = new Dictionary<CraftResource, VariableBignumber>();
		Red = new VariableBignumber(2000.0);
		map.Add(CraftResource.Red, Red);
		Green = new VariableBignumber(2000.0);
		map.Add(CraftResource.Green, Green);
		Blue = new VariableBignumber(2000.0);
		map.Add(CraftResource.Blue, Blue);
		Yellow = new VariableBignumber(2000.0);
		map.Add(CraftResource.Yellow, Yellow);
		EnchantingDust = new VariableBignumber(2000.0);
		map.Add(CraftResource.Enchanting, EnchantingDust);
		MaxEnchantLevel = new VariableInt(20);
		WeaponCharging = new VariableFloat(1f);
		Investment = new CraftInvestment();
		window.Init(Investment);
		trade.Init();
		Gathering = GameManager.Instance.SkillManager.Get(SkillManager.SkillNames.Gathering);
		Crafting = GameManager.Instance.SkillManager.Get(SkillManager.SkillNames.Crafting);
		BonusEnchantLevel = new VariableInt(0);
		BonusEnchantLevelChest = new VariableInt(0);
		BonusEnchantLevelRing = new VariableInt(0);
		BonusEnchantLevelHand = new VariableInt(0);
		BonusEnchantLevelShoulder = new VariableInt(0);
		ExperimentCraftEfficiency = new VariableBignumber(1.0);
		ResearchSavePart = new VariableFloat(1f);
		TrophySavePart = new VariableFloat(1f);
		GameContext.ContextAddResource("Craft.BonusEnchant", BonusEnchantLevel);
		GameContext.ContextAddResource("Craft.BonusEnchantChest", BonusEnchantLevelChest);
		GameContext.ContextAddResource("Craft.BonusEnchantRing", BonusEnchantLevelRing);
		GameContext.ContextAddResource("Craft.BonusEnchantHand", BonusEnchantLevelHand);
		GameContext.ContextAddResource("Craft.BonusEnchantShoulder", BonusEnchantLevelShoulder);
		GameContext.ContextAddResource("Craft.MaxEnchant", MaxEnchantLevel);
		GameContext.ContextAddResource("Experiment.Efficiency", ExperimentCraftEfficiency);
		GameContext.ContextAddResource("Craft.ResearchSavePart", ResearchSavePart);
		GameContext.ContextAddResource("Craft.TrophySavePart", TrophySavePart);
		GameContext.ContextAddResource(ResourceType.Items.ToString() + "." + ItemKeys.WeaponCharging, WeaponCharging);
		ResearchGilding = new ItemGilding();
		GameContext.ContextAddResource("Craft.ResearchAmount", ResearchGilding.Amount);
		TrophiesGilding = new ItemGilding();
		GameContext.ContextAddResource("Craft.TrophyAmount", TrophiesGilding.Amount);
		window.presets.Init();
		GameContext.AddItems();
		foreach (ItemSlot slot in window.doll.Slots)
		{
			slot.OnEquip = (Action<Item>)Delegate.Combine(slot.OnEquip, new Action<Item>(OnEquipCheck));
		}
	}

	private void OnEquipCheck(Item item)
	{
		if (item != null)
		{
			OnEquipItem?.Invoke();
		}
	}

	public void InitItems()
	{
		AllItems = new List<Item>();
		AvailableItems = new List<Item>();
		new ItemsCreator().Init();
		weapons = new WeaponManager();
		weapons.Init(weaponScroll);
		mounts = new MountManager();
		mounts.Init();
		phylacteries = new PhylacteryManager();
		phylacteries.Init();
		ResetDropList();
		window.UpdateList();
	}

	private void Update()
	{
		window.presets.CheckInputPresets();
	}

	public void ClearSlots()
	{
		for (int i = 0; i < window.doll.Slots.Count; i++)
		{
			window.doll.Slots[i].Clear();
		}
	}

	public void PreLoad()
	{
		ClearSlots();
	}

	public void ResetDropList()
	{
		DropList = new List<Item>();
		foreach (Item allItem in AllItems)
		{
			if (allItem.Slot != SlotKey.Research && allItem.Slot != SlotKey.Misc && allItem.Slot != SlotKey.Mount && window.doll.SlotIsAvailable(allItem.Slot) && !AvailableItems.Contains(allItem))
			{
				DropList.Add(allItem);
			}
		}
		mounts.CheckCrafted();
	}

	public void RealmReset()
	{
		ClearSlots();
		_ = GameManager.Instance.Realmcraft.IsActive;
		foreach (Item availableItem in AvailableItems)
		{
			availableItem.Enchant?.Disenchant();
			availableItem.SetEfficiency(null);
		}
		ResearchGilding.ResetAll();
		TrophiesGilding.ResetAll();
		window.craftingMenu.experimetCounterRealm.SetValue(0);
	}

	public void AddProgress2New(SlotKey key)
	{
		float progressIvest = GetProgressIvest((int)(ProgressToNew * 100f));
		if (Settings.SoundOn)
		{
			if (progressIvest >= 0.5f)
			{
				PlaySuccessSFX();
			}
			else
			{
				PlayFailSFX();
			}
		}
		progressIvest *= maxProgressToNew;
		ProgressToNew += progressIvest;
		if (ProgressToNew >= maxProgressToNew)
		{
			AddNewItem(key);
			ProgressToNew -= maxProgressToNew;
		}
		Crafting.AddExp(progressIvest);
	}

	public float GetProgressPreview()
	{
		return ProgressToNew + GetProgressIvest((int)(ProgressToNew * 100f)) * maxProgressToNew;
	}

	public void PlaySuccessSFX()
	{
		SoundManager.Instance.PlaySound(sfxSuccess, volume);
	}

	public void PlayFailSFX()
	{
		SoundManager.Instance.PlaySound(sfxFail, volume);
	}

	public Variable GetEfficiency(SlotKey key, int number)
	{
		if (key == SlotKey.Phylactery)
		{
			return phylacteries.Efficiency;
		}
		if (!GameManager.Instance.Paragon.ProficiencyIsAvailable)
		{
			return null;
		}
		SlotUpgrade slotUpgrade = window.slotUpgrade.Upgrades.Find((SlotUpgrade x) => x.Slot == key && x.Number == number);
		if (slotUpgrade == null)
		{
			return null;
		}
		return slotUpgrade.efficiency;
	}

	public float GetProgressIvest(int seed)
	{
		float num = 0f;
		_ = UnityEngine.Random.state;
		seed = (GameManager.Instance.UserID + seed) % 1000;
		float num2 = RandomSeed.Get((int)(((float)seed + Crafting.exp.ToFloat() + (float)AvailableItems.Count + (float)(seed / 100)) % 100f), 0f, 1f);
		num = ((!(num2 < baseChanceToSuccessInvest)) ? RandomSeed.Get(seed + (int)(num2 * 1000f), 0.2f, 0.4f) : 0.5f);
		num *= Crafting.GetBonus().ToFloat() * ExperimentCraftEfficiency.Value.ToFloat();
		if (num > 1f)
		{
			num = 1f;
		}
		return num;
	}

	public float GetProgressInvestItem()
	{
		return 0.8f * Crafting.GetBonus().ToFloat() * ExperimentCraftEfficiency.Value.ToFloat();
	}

	public float GetMaxInvest()
	{
		float num = 0.5f * Crafting.GetBonus().ToFloat() * ExperimentCraftEfficiency.Value.ToFloat();
		if (num > 1f)
		{
			num = 1f;
		}
		return num;
	}

	private void AddNewItem(SlotKey key)
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(GameManager.Instance.UserID + AvailableItems.Count);
		int rarity = UnityEngine.Random.Range(0, 5);
		List<Item> list = new List<Item>();
		list = DropList;
		if (key != SlotKey.None)
		{
			list = list.FindAll((Item x) => x.Slot == key);
		}
		while (rarity < 4 && list.Count == 0)
		{
			list = DropList.FindAll((Item x) => x.Rarity <= rarity);
			if (list.Count == 0)
			{
				rarity++;
			}
		}
		if (list.Count == 0)
		{
			list = DropList;
		}
		if (list.Count != 0)
		{
			int index = UnityEngine.Random.Range(0, list.Count);
			Item item = list[index];
			UnlockItem(item);
			OnCraftItem?.Invoke();
			window.AddData(item);
			UnityEngine.Random.state = state;
		}
	}

	public void UnlockItem(Item item)
	{
		if (DropList.Contains(item))
		{
			DropList.Remove(item);
		}
		if (!AvailableItems.Contains(item))
		{
			AvailableItems.Insert(0, item);
			Statistic.UnlockedItems.Change(1);
			for (int i = 0; i <= item.Tier; i++)
			{
				Statistic.UnlockedByTiers[i].Change(1);
			}
			OnGetItem?.Invoke(item);
		}
	}

	public void DestroyItem(Item item)
	{
		if (AvailableItems.Contains(item))
		{
			AvailableItems.Remove(item);
			Statistic.UnlockedItems.Change(-1);
			for (int i = 0; i <= item.Tier; i++)
			{
				Statistic.UnlockedByTiers[i].Change(-1);
			}
		}
	}

	public void AddResearchGilding(int id, int times)
	{
		Item item = AvailableItems.Find((Item x) => x.ID == id);
		if (item == null)
		{
			Debug.LogError("Item null on research upgrade");
		}
		ResearchGilding.Add(item, times);
	}

	public void AddTrophiesGilding(Item item, int times)
	{
		if (item == null)
		{
			Debug.LogError("Item null on trophy upgrade");
		}
		TrophiesGilding.Add(item, times);
	}

	public int SaveRetainResearch()
	{
		if (ResearchSavePart.ValueFloat <= 1f)
		{
			return 0;
		}
		int num = 0;
		foreach (Item item in AvailableItems.FindAll((Item x) => x.Slot == SlotKey.Research))
		{
			num += GetResearchGildingLevel(item.ID);
		}
		return Mathf.FloorToInt((float)num * (ResearchSavePart.ValueFloat - 1f));
	}

	public void LoadRetainResearch(int levels)
	{
		if (levels == 0)
		{
			return;
		}
		List<Item> list = AvailableItems.FindAll((Item x) => x.Slot == SlotKey.Research && x.Tier >= 4);
		int num = levels / list.Count;
		foreach (Item item in list)
		{
			AddResearchGilding(item.ID, num);
		}
		levels -= num * list.Count;
		if (levels <= 0)
		{
			return;
		}
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			if (levels <= 0)
			{
				break;
			}
			AddResearchGilding(list[num2].ID, 1);
			levels--;
		}
	}

	public int SaveRetainTrophy()
	{
		if (TrophySavePart.ValueFloat <= 1f)
		{
			return 0;
		}
		int num = 0;
		foreach (Item item in AvailableItems.FindAll((Item x) => x.Slot == SlotKey.Misc))
		{
			num += GetTrophiesGildingLevel(item.ID);
		}
		return Mathf.FloorToInt((float)num * (TrophySavePart.ValueFloat - 1f));
	}

	public void LoadRetainTrophies(int levels)
	{
		if (levels == 0)
		{
			return;
		}
		List<Item> list = AvailableItems.FindAll((Item x) => x.Slot == SlotKey.Misc && x.Tier >= 4);
		int num = levels / list.Count;
		foreach (Item item in list)
		{
			AddTrophiesGilding(item, num);
		}
		levels -= num * list.Count;
		if (levels <= 0)
		{
			return;
		}
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			if (levels <= 0)
			{
				break;
			}
			AddTrophiesGilding(list[num2], 1);
			levels--;
		}
	}

	public int GetResearchGildingLevel(int id)
	{
		return ResearchGilding.GetLevel(id);
	}

	public int GetTrophiesGildingLevel(int id)
	{
		return TrophiesGilding.GetLevel(id);
	}

	public List<SaveData.IntBignumberPair> SaveResources()
	{
		List<SaveData.IntBignumberPair> list = new List<SaveData.IntBignumberPair>();
		foreach (KeyValuePair<CraftResource, VariableBignumber> item in map)
		{
			list.Add(new SaveData.IntBignumberPair((int)item.Key, item.Value.Value));
		}
		return list;
	}

	public void LoadResources(List<SaveData.IntBignumberPair> list)
	{
		if (list == null)
		{
			for (int i = 1; i < 5; i++)
			{
				map[(CraftResource)i].SetValue(2000.0);
			}
			return;
		}
		for (int j = 0; j < list.Count; j++)
		{
			BigNumber value = list[j].Value;
			if (list[j].ID != 5)
			{
				value = value.Clamp(0.0, "1e9");
			}
			map[(CraftResource)list[j].ID].SetValue(value);
		}
	}

	public void PostLoad()
	{
		ApplyEnchBonus();
		window.presets.LoadPresetVisual();
		if (GameManager.Instance.Paragon.ProficiencyIsAvailable)
		{
			window.doll.UpdateEff();
		}
		Statistic.UnlockedItems.SetValue(AvailableItems.Count);
		List<ItemSlot> slots = window.doll.Slots;
		HashSet<ItemSetBonus> hashSet = new HashSet<ItemSetBonus>();
		foreach (ItemSlot item in slots)
		{
			if (item.Item != null && item.Item.Set != null)
			{
				hashSet.Add(item.Item.Set);
			}
		}
		foreach (ItemSetBonus item2 in hashSet)
		{
			item2.Activate();
		}
	}

	public void ApplyEnchBonus()
	{
		VariableInt bonusEnchantLevel = BonusEnchantLevel;
		bonusEnchantLevel.OnChange = (Action)Delegate.Combine(bonusEnchantLevel.OnChange, new Action(ChangeBonusEnchantLevel));
		ChangeBonusEnchantLevel();
	}

	public void RemoveEnchBonus()
	{
		VariableInt bonusEnchantLevel = BonusEnchantLevel;
		bonusEnchantLevel.OnChange = (Action)Delegate.Remove(bonusEnchantLevel.OnChange, new Action(ChangeBonusEnchantLevel));
		foreach (ItemSlot slot in window.doll.Slots)
		{
			if (slot.Item != null && slot.Item.Enchant != null && slot.Item.Enchant.Level > 0 && slot.Item.active)
			{
				slot.Item.Enchant.Delete();
			}
		}
	}

	public void RemoveEffects()
	{
		VariableInt bonusEnchantLevel = BonusEnchantLevel;
		bonusEnchantLevel.OnChange = (Action)Delegate.Remove(bonusEnchantLevel.OnChange, new Action(ChangeBonusEnchantLevel));
		List<ItemSlot> slots = window.doll.Slots;
		HashSet<ItemSetBonus> hashSet = new HashSet<ItemSetBonus>();
		foreach (ItemSlot item in slots)
		{
			if (item.Item != null && item.Item.active && item.Item.Set != null)
			{
				hashSet.Add(item.Item.Set);
			}
		}
		foreach (ItemSetBonus item2 in hashSet)
		{
			item2.Deactivate();
		}
		foreach (ItemSlot item3 in slots)
		{
			if (item3.Item != null)
			{
				if (item3.Key == SlotKey.Weapon)
				{
					weaponScroll.StopAction();
					weaponScroll.weapon.RemoveEffects();
				}
				else
				{
					item3.Item.UnsubCheckReqs();
					item3.Item.RemoveEffects();
				}
			}
		}
	}

	public void ApplyEffects()
	{
		VariableInt bonusEnchantLevel = BonusEnchantLevel;
		bonusEnchantLevel.OnChange = (Action)Delegate.Combine(bonusEnchantLevel.OnChange, new Action(ChangeBonusEnchantLevel));
		List<ItemSlot> slots = window.doll.Slots;
		HashSet<ItemSetBonus> hashSet = new HashSet<ItemSetBonus>();
		foreach (ItemSlot item in slots)
		{
			if (item.Item != null)
			{
				item.Item.SubCheckReqs();
				if (item.Item.CheckReqs())
				{
					item.Item.ApplyEffects();
				}
				if (item.Item.Set != null)
				{
					hashSet.Add(item.Item.Set);
				}
			}
		}
		foreach (ItemSetBonus item2 in hashSet)
		{
			item2.Activate();
		}
	}

	public void OnChangeItems()
	{
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public void OnEnchantItem(int lvl)
	{
		OnEnchant?.Invoke(lvl);
		OnEnchantSingle?.Invoke();
	}

	public void ApplyDisenchantment()
	{
		GameManager.Instance.Nullifier.Change(-1);
	}

	public void ApplyDisenchantmentAll()
	{
		if (GameManager.Instance.Nullifier.ValueInt < 10)
		{
			return;
		}
		foreach (Item availableItem in AvailableItems)
		{
			if (availableItem.Enchant != null && availableItem.Enchant.Level > 0)
			{
				availableItem.Enchant.Disenchant();
			}
		}
		GameManager.Instance.Nullifier.Change(-10);
		window.UpdateList();
	}

	public void GiveEnchantingDust(int b)
	{
		EnchantingDust.Change(b);
		Statistic.EnchantingDustExile.Change(b);
		Statistic.GetEdustRealm().Change(b);
		Statistic.GetEdustTotal().Change(b);
	}

	public void GiveEnchantingDust(BigNumber b)
	{
		EnchantingDust.Change(b);
		Statistic.EnchantingDustExile.Change(b);
		Statistic.GetEdustRealm().Change(b);
		Statistic.GetEdustTotal().Change(b);
	}

	public void GiveEachDust(int k)
	{
		Red.Change(k);
		Blue.Change(k);
		Green.Change(k);
		Yellow.Change(k);
		Statistic.ResourcesTotal[0].Change(k);
		Statistic.ResourcesTotal[1].Change(k);
		Statistic.ResourcesTotal[2].Change(k);
		Statistic.ResourcesTotal[3].Change(k);
		Statistic.ResourcesRealm[0].Change(k);
		Statistic.ResourcesRealm[1].Change(k);
		Statistic.ResourcesRealm[2].Change(k);
		Statistic.ResourcesRealm[3].Change(k);
		Statistic.ResourcesCollected.Change(k * 4);
		Statistic.ResourcesCollectedRealm.Change(k * 4);
		Statistic.ResourcesCollectedTotal.Change(k * 4);
	}

	public CraftResource GetMinimumDust()
	{
		CraftResource craftResource = CraftResource.Blue;
		BigNumber value = map[craftResource].Value;
		foreach (KeyValuePair<CraftResource, VariableBignumber> item in map)
		{
			if (item.Key != CraftResource.Enchanting && item.Value.Value < value)
			{
				craftResource = item.Key;
				value = item.Value.Value;
			}
		}
		return craftResource;
	}

	private void ChangeBonusEnchantLevel()
	{
		foreach (ItemSlot slot in window.doll.Slots)
		{
			if (slot.Item != null && slot.Item.Enchant != null && slot.Item.Enchant.Level > 0 && slot.Item.active)
			{
				slot.Item.Enchant.Delete();
				slot.Item.Enchant.Apply();
			}
		}
	}

	public bool IsEnoughResources(Dictionary<CraftResource, BigNumber> cost)
	{
		foreach (KeyValuePair<CraftResource, BigNumber> item in cost)
		{
			if (item.Value > map[item.Key].Value)
			{
				return false;
			}
		}
		return true;
	}
}
