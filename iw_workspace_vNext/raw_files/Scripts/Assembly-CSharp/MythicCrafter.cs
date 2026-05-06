using System.Collections.Generic;
using UnityEngine;

public class MythicCrafter
{
	public VariableInt Amount;

	public int Seed;

	private List<MythicData> mythicDatas;

	private Dictionary<SlotKey, List<ItemEnchantment>> enchantments;

	public MythicCrafter()
	{
		mythicDatas = GlobalData.Mythics;
		Amount = new VariableInt(0);
		enchantments = new Dictionary<SlotKey, List<ItemEnchantment>>();
		enchantments.Add(SlotKey.Head, new List<ItemEnchantment>
		{
			new ItemEnchantment("PetAP", 1.15f, SlotKey.Head),
			new ItemEnchantment("HeroAP", 1.2f, SlotKey.Head),
			new ItemEnchantment("Evo", 1.15f, SlotKey.Head),
			new ItemEnchantment("Inca", 1.03f, SlotKey.Head),
			new ItemEnchantment("VoidMana", 1.15f, SlotKey.Head),
			new ItemEnchantment("ABP", 1.25f, SlotKey.Head),
			new ItemEnchantment("Autoclick", 1.25f, SlotKey.Head),
			new ItemEnchantment("CritProfit", 1.2f, SlotKey.Head),
			new ItemEnchantment("Shard", 1.2f, SlotKey.Head)
		});
		enchantments.Add(SlotKey.Shoulder, new List<ItemEnchantment>
		{
			new ItemEnchantment("PetAP", 1.15f, SlotKey.Shoulder),
			new ItemEnchantment("HeroAP", 1.15f, SlotKey.Shoulder),
			new ItemEnchantment("Evo", 1.3f, SlotKey.Shoulder),
			new ItemEnchantment("Inca", 1.04f, SlotKey.Shoulder),
			new ItemEnchantment("Idle", 1.15f, SlotKey.Shoulder),
			new ItemEnchantment("VoidMana", 1.1f, SlotKey.Shoulder),
			new ItemEnchantment("ABP", 1.25f, SlotKey.Shoulder),
			new ItemEnchantment("CritRating", 1.2f, SlotKey.Shoulder)
		});
		enchantments.Add(SlotKey.Chest, new List<ItemEnchantment>
		{
			new ItemEnchantment("PetAP", 1.15f, SlotKey.Chest),
			new ItemEnchantment("Evo", 1.2f, SlotKey.Chest),
			new ItemEnchantment("Idle", 1.15f, SlotKey.Chest),
			new ItemEnchantment("VoidMana", 1.15f, SlotKey.Chest),
			new ItemEnchantment("ABP", 1.25f, SlotKey.Chest),
			new ItemEnchantment("CritRating", 1.2f, SlotKey.Chest),
			new ItemEnchantment("Autoclick", 1.25f, SlotKey.Chest),
			new ItemEnchantment("Offline", 1.3f, SlotKey.Chest)
		});
		enchantments.Add(SlotKey.Hands, new List<ItemEnchantment>
		{
			new ItemEnchantment("PetAP", 1.15f, SlotKey.Hands),
			new ItemEnchantment("Evo", 1.3f, SlotKey.Hands),
			new ItemEnchantment("Inca", 1.04f, SlotKey.Hands),
			new ItemEnchantment("VoidMana", 1.15f, SlotKey.Hands),
			new ItemEnchantment("ABP", 1.2f, SlotKey.Hands),
			new ItemEnchantment("Summon", 1.04f, SlotKey.Hands),
			new ItemEnchantment("CritProfit", 1.25f, SlotKey.Hands),
			new ItemEnchantment("Offline", 1.25f, SlotKey.Hands)
		});
		enchantments.Add(SlotKey.Boots, new List<ItemEnchantment>
		{
			new ItemEnchantment("PetAP", 1.15f, SlotKey.Boots),
			new ItemEnchantment("HeroAP", 1.25f, SlotKey.Boots),
			new ItemEnchantment("Evo", 1.2f, SlotKey.Boots),
			new ItemEnchantment("Inca", 1.04f, SlotKey.Boots),
			new ItemEnchantment("Idle", 1.15f, SlotKey.Boots),
			new ItemEnchantment("VoidMana", 1.1f, SlotKey.Boots),
			new ItemEnchantment("ABP", 1.25f, SlotKey.Boots),
			new ItemEnchantment("CritProfit", 1.2f, SlotKey.Boots),
			new ItemEnchantment("Shard", 1.2f, SlotKey.Boots)
		});
		enchantments.Add(SlotKey.Wrist, new List<ItemEnchantment>
		{
			new ItemEnchantment("PetAP", 1.2f, SlotKey.Wrist),
			new ItemEnchantment("HeroAP", 1.2f, SlotKey.Wrist),
			new ItemEnchantment("Evo", 1.2f, SlotKey.Wrist),
			new ItemEnchantment("Inca", 1.04f, SlotKey.Wrist),
			new ItemEnchantment("VoidMana", 1.15f, SlotKey.Wrist),
			new ItemEnchantment("ABP", 1.2f, SlotKey.Wrist),
			new ItemEnchantment("CritProfit", 1.2f, SlotKey.Wrist)
		});
		enchantments.Add(SlotKey.Waist, new List<ItemEnchantment>
		{
			new ItemEnchantment("HeroAP", 1.2f, SlotKey.Waist),
			new ItemEnchantment("Evo", 1.15f, SlotKey.Waist),
			new ItemEnchantment("Inca", 1.05f, SlotKey.Waist),
			new ItemEnchantment("Summon", 1.05f, SlotKey.Waist),
			new ItemEnchantment("Shard", 1.2f, SlotKey.Waist),
			new ItemEnchantment("Autoclick", 1.2f, SlotKey.Waist),
			new ItemEnchantment("Offline", 1.25f, SlotKey.Waist)
		});
		enchantments.Add(SlotKey.Pants, new List<ItemEnchantment>
		{
			new ItemEnchantment("PetAP", 1.15f, SlotKey.Pants),
			new ItemEnchantment("HeroAP", 1.15f, SlotKey.Pants),
			new ItemEnchantment("Evo", 1.2f, SlotKey.Pants),
			new ItemEnchantment("Inca", 1.04f, SlotKey.Pants),
			new ItemEnchantment("Idle", 1.125f, SlotKey.Pants),
			new ItemEnchantment("VoidMana", 1.15f, SlotKey.Pants),
			new ItemEnchantment("ABP", 1.2f, SlotKey.Pants),
			new ItemEnchantment("Shard", 1.15f, SlotKey.Pants),
			new ItemEnchantment("Charge", 1.1f, SlotKey.Pants),
			new ItemEnchantment("CritRating", 1.2f, SlotKey.Pants)
		});
		enchantments.Add(SlotKey.Back, new List<ItemEnchantment>
		{
			new ItemEnchantment("PetAP", 1.15f, SlotKey.Back),
			new ItemEnchantment("Evo", 1.2f, SlotKey.Back),
			new ItemEnchantment("Inca", 1.05f, SlotKey.Back),
			new ItemEnchantment("Summon", 1.04f, SlotKey.Back),
			new ItemEnchantment("VoidMana", 1.15f, SlotKey.Back),
			new ItemEnchantment("ABP", 1.2f, SlotKey.Back),
			new ItemEnchantment("Autoclick", 1.2f, SlotKey.Back),
			new ItemEnchantment("Offline", 1.3f, SlotKey.Back)
		});
		enchantments.Add(SlotKey.Neck, new List<ItemEnchantment>
		{
			new ItemEnchantment("PetAP", 1.2f, SlotKey.Neck),
			new ItemEnchantment("HeroAP", 1.2f, SlotKey.Neck),
			new ItemEnchantment("Evo", 1.2f, SlotKey.Neck),
			new ItemEnchantment("Inca", 1.04f, SlotKey.Neck),
			new ItemEnchantment("VoidMana", 1.15f, SlotKey.Neck),
			new ItemEnchantment("Idle", 1.15f, SlotKey.Neck),
			new ItemEnchantment("Click", 1.25f, SlotKey.Neck),
			new ItemEnchantment("Autoclick", 1.2f, SlotKey.Neck),
			new ItemEnchantment("ABP", 1.25f, SlotKey.Neck),
			new ItemEnchantment("Offline", 1.25f, SlotKey.Neck)
		});
		GameContext.ContextAddResource("Items.Mythics", Amount);
	}

	public Dictionary<CraftResource, BigNumber> GetCost(int seed)
	{
		List<CraftResource> list = new List<CraftResource>
		{
			CraftResource.Red,
			CraftResource.Blue,
			CraftResource.Green,
			CraftResource.Yellow
		};
		Dictionary<CraftResource, BigNumber> dictionary = new Dictionary<CraftResource, BigNumber>
		{
			{
				CraftResource.Red,
				0.0
			},
			{
				CraftResource.Blue,
				0.0
			},
			{
				CraftResource.Green,
				0.0
			},
			{
				CraftResource.Yellow,
				0.0
			}
		};
		CraftResource craftResource = list[RandomSeed.Get(seed, 0, list.Count)];
		list.Remove(craftResource);
		CraftResource key = list[RandomSeed.Get(seed + 1, 0, list.Count)];
		dictionary[craftResource] = RandomSeed.Get(seed + 2, 9000, 12000);
		dictionary[key] = 15000.0 - dictionary[craftResource];
		return dictionary;
	}

	public List<ItemEnchantment> GetEnhantments(SlotKey slot)
	{
		return enchantments[slot];
	}

	public MythicItem Create(MythicItem selected, List<MythicItem> crafted)
	{
		MythicData mythicData = null;
		if (selected != null)
		{
			mythicData = mythicDatas.Find((MythicData x) => x.Id == selected.MythicId);
		}
		if (mythicData == null)
		{
			List<MythicData> list = mythicDatas.FindAll((MythicData x) => !crafted.Exists((MythicItem y) => y.MythicId == x.Id));
			if (list.Count == 0)
			{
				list = mythicDatas;
			}
			mythicData = list[RandomSeed.Get(Amount.ValueInt, 0, list.Count)];
		}
		MythicItem mythicItem = new MythicItem();
		mythicItem.Create(10000 + Amount.ValueInt, Seed, mythicData);
		List<Affix> list2 = new List<Affix>();
		List<AffixData> list3 = new List<AffixData>();
		foreach (AffixData affix in GlobalData.Affixes)
		{
			list3.Add(affix);
		}
		int num = 0;
		for (int num2 = 0; num2 < 3; num2++)
		{
			AffixData affixData = list3[RandomSeed.Get(mythicItem.ID + num, 0, list3.Count)];
			num++;
			list3.Remove(affixData);
			list2.Add(new Affix(GetRandRange(mythicItem.ID + num), affixData));
			num++;
		}
		mythicItem.Seed = num;
		mythicItem.AddTier(new Affix(mythicData), list2);
		mythicItem.SetMasterwork(0);
		Amount.Change(1);
		Seed += 3;
		return mythicItem;
	}

	public MythicItem CreateOnLoad(MythicItem.SaveData data)
	{
		MythicItem mythicItem = new MythicItem();
		MythicData data2 = mythicDatas.Find((MythicData x) => x.Id == data.Id);
		mythicItem.Create(data.ItemId, data.CostSeed, data2);
		mythicItem.Rerolls = data.Rerolls;
		mythicItem.Seed = data.Seed;
		List<Affix> list = new List<Affix>();
		foreach (Affix.SaveData v in data.Affixes)
		{
			AffixData affixData = GlobalData.Affixes.Find((AffixData x) => x.Id == v.id);
			if (affixData == null)
			{
				Debug.LogError("null model " + v.id);
			}
			else
			{
				list.Add(new Affix(v.v, affixData));
			}
		}
		mythicItem.AddTier(new Affix(data2), list);
		if (data.Tempering != null)
		{
			List<Affix> list2 = new List<Affix>();
			foreach (Affix.SaveData v2 in data.Tempering)
			{
				AffixData affixData = GlobalData.Tempering.Find((TemperingData x) => x.Id == v2.id);
				if (affixData == null)
				{
					Debug.LogError("null model " + v2.id);
				}
				else
				{
					list2.Add(new Affix(v2.v, affixData));
				}
			}
			mythicItem.GetTier().Tempering = list2;
		}
		mythicItem.Tempering = data.Temper;
		mythicItem.SetMasterwork(data.MasterworkLvl);
		if (!string.IsNullOrEmpty(data.Ench))
		{
			mythicItem.SetEnchantment(enchantments[mythicItem.Slot].Find((ItemEnchantment x) => x.key == data.Ench));
			mythicItem.Enchant.Load(data.EnchLvl);
		}
		mythicItem.Favorite = data.Fav;
		mythicItem.GetTier().InitMasterwork();
		return mythicItem;
	}

	public Affix RerollAffix(MythicItem item, List<Affix> except)
	{
		List<AffixData> affixes = GlobalData.Affixes;
		new List<int>();
		item.GetAffixes();
		List<AffixData> list = new List<AffixData>();
		foreach (AffixData v in affixes)
		{
			if (!except.Exists((Affix x) => x.GetTarget() == v.Target))
			{
				list.Add(v);
			}
		}
		AffixData model = list[RandomSeed.Get(item.ID + item.Seed, 0, list.Count)];
		float randRange = GetRandRange(item.ID + item.Seed + 1);
		item.Seed += 2;
		return new Affix(randRange, model);
	}

	private float GetRandRange(int seed)
	{
		return Mathf.Clamp01((float)RandomSeed.Get(seed, -2, 103) / 100f);
	}
}
