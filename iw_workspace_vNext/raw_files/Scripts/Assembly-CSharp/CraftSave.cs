using System.Collections.Generic;

public class CraftSave
{
	public class ItemS
	{
		public int id;

		public float progress;

		public int tier;

		public int enchant;

		public bool fav;
	}

	public BigNumber CSkillExp = 0.0;

	public BigNumber GSkillExp = 0.0;

	public float ProgressInvest;

	public List<SaveData.IntIntPair> costs;

	public List<ItemS> Items;

	public List<SaveData.IntIntPair> Equiped;

	public WeaponManager.WeaponSave Weapon;

	public MountManager.SaveData Mount;

	public List<WeaponManager.WeaponSave> Charges;

	public Dictionary<int, int> ResearchGilding;

	public Dictionary<int, int> TrophiesGilding;

	public SlotUpgradeController.SlotUpgradeSaveData Proficiency;

	public MythicController.SaveData Mythics;

	public void Save()
	{
		CraftManager craft = GameManager.Instance.Craft;
		CSkillExp = craft.Crafting.totalExp;
		GSkillExp = craft.Gathering.totalExp;
		Items = new List<ItemS>();
		for (int i = 0; i < craft.AvailableItems.Count; i++)
		{
			Item item = craft.AvailableItems[i];
			if (item.Tier != 6)
			{
				ItemS itemS = new ItemS();
				itemS.id = item.ID;
				itemS.tier = item.Tier;
				itemS.progress = item.ProgressInvested;
				itemS.enchant = ((item.Enchant != null) ? item.Enchant.Level : 0);
				itemS.fav = item.Favorite;
				Items.Add(itemS);
			}
		}
		Mythics = craft.window.forge.Save();
		Equiped = new List<SaveData.IntIntPair>();
		for (int j = 0; j < craft.window.doll.Slots.Count; j++)
		{
			ItemSlot itemSlot = craft.window.doll.Slots[j];
			if (itemSlot.Item != null)
			{
				Equiped.Add(new SaveData.IntIntPair((int)itemSlot.Key, itemSlot.Item.ID));
			}
		}
		costs = GameManager.Instance.Craft.Investment.GetCost();
		ProgressInvest = GameManager.Instance.Craft.ProgressToNew;
		Weapon = craft.weapons.Save();
		Mount = craft.mounts.Save();
		Charges = craft.weapons.SaveCharges();
		ResearchGilding = craft.ResearchGilding.Save();
		TrophiesGilding = craft.TrophiesGilding.Save();
		Proficiency = craft.window.slotUpgrade.Save();
	}

	public void Load()
	{
		CraftManager craft = GameManager.Instance.Craft;
		craft.AvailableItems = new List<Item>();
		craft.Crafting.totalExp = CSkillExp;
		craft.Gathering.totalExp = GSkillExp;
		int num = 6;
		for (int i = 0; i < num; i++)
		{
			Statistic.UnlockedByTiers[i].SetValue(0);
		}
		for (int j = 0; j < craft.AllItems.Count; j++)
		{
			craft.AllItems[j].ResetTier();
		}
		for (int k = 0; k < Items.Count; k++)
		{
			ItemS format = Items[k];
			Item item = craft.AllItems.Find((Item x) => x.ID == format.id);
			item.Tier = ((item.BaseTier > format.tier) ? item.BaseTier : format.tier);
			item.ProgressInvested = format.progress;
			item.Enchant?.Load(format.enchant);
			item.Favorite = format.fav;
			craft.AvailableItems.Add(item);
			for (int num2 = 0; num2 <= item.Tier; num2++)
			{
				Statistic.UnlockedByTiers[num2].Change(1);
			}
		}
		Statistic.UnlockedItems.SetValue(craft.AvailableItems.Count);
		craft.window.forge.Load(Mythics);
		craft.weapons.Load(Weapon, Charges);
		craft.mounts.Load(Mount);
		craft.Investment.LoadCost(costs);
		craft.ProgressToNew = ProgressInvest;
		craft.window.craftingMenu.Refresh();
		craft.window.UpdateList();
		craft.Crafting.AddExp(0.0);
		craft.Gathering.AddExp(0.0);
		craft.ResearchGilding.Load(ResearchGilding);
		craft.TrophiesGilding.Load(TrophiesGilding);
		craft.window.slotUpgrade.Load(Proficiency);
		int i2;
		for (i2 = 0; i2 < Equiped.Count; i2++)
		{
			craft.AvailableItems.Find((Item x) => x.ID == Equiped[i2].Value)?.EquipItem(onLoad: true);
		}
	}

	public void LoadEmpty()
	{
		CraftManager craft = GameManager.Instance.Craft;
		craft.DropList = new List<Item>();
		craft.AvailableItems = new List<Item>();
		craft.Crafting.Load(CSkillExp);
		craft.Gathering.Load(GSkillExp);
		int num = 7;
		for (int i = 0; i < num; i++)
		{
			Statistic.UnlockedByTiers[i].SetValue(0);
		}
		for (int j = 0; j < craft.AllItems.Count; j++)
		{
			craft.AllItems[j].ResetTier();
		}
		Statistic.UnlockedItems.SetValue(0);
		GameManager.Instance.Craft.Investment.Recalculate();
		GameManager.Instance.Craft.ProgressToNew = 0f;
		craft.window.craftingMenu.Refresh();
		craft.window.UpdateList();
	}
}
