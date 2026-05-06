using System;
using System.Collections.Generic;
using System.Globalization;

public class ItemsCreator
{
	public class ItemDetail
	{
		public class TierDetail
		{
			public string Tier;

			public string Desc;

			public List<int> Cost;

			public List<EffectObj> Effs;
		}

		public class ItemReq
		{
			public string attribute;

			public string value;

			public ItemReq(string attr, string v)
			{
				attribute = attr;
				value = v;
			}
		}

		public class EffectObj
		{
			public string target;

			public string effect;

			public string parameter;

			public string a;

			public string m;

			public string diminish;
		}

		public string ID;

		public string Name;

		public string Icon;

		public string Lore;

		public string Rarity;

		public string Slot;

		public List<ItemReq> Reqs;

		public string Diminish;

		public List<TierDetail> Tiers;

		public string Set;

		public string Enchant;

		public string EBase;
	}

	public class SetDetails
	{
		public string Key;

		public string Amount;

		public string Description;

		public string T;

		public string A;

		public string M;

		public string V;

		public string F;
	}

	public class Enchantment
	{
		public string Key;

		public string Description;

		public string T;
	}

	public void Init()
	{
		InitSetBonuses();
		CreateAllSetBonuses(GlobalData.SetBonuses);
		CreateAll(GlobalData.Items, GlobalData.EnchantmentBonuses);
	}

	private void CreateAll(List<ItemDetail> data, List<Enchantment> enchants)
	{
		CraftManager craft = GameManager.Instance.Craft;
		List<Item> list = (craft.AllItems = new List<Item>());
		string description = "";
		foreach (ItemDetail d in data)
		{
			Item item = new Item();
			item.ID = int.Parse(d.ID);
			item.Rarity = int.Parse(d.Rarity);
			item.NameKey = d.Name;
			item.Slot = (SlotKey)Enum.Parse(typeof(SlotKey), d.Slot);
			item.Icon = craft.icons.Get(d.Icon);
			item.LoreKey = d.Lore;
			item.BaseTier = int.Parse(d.Tiers[0].Tier);
			item.ItemTiers = new Dictionary<int, IItemTier>();
			item.Unscaled = d.Diminish == "0";
			for (int i = 0; i < d.Tiers.Count; i++)
			{
				ItemTier itemTier = new ItemTier();
				itemTier.Description = d.Tiers[i].Desc;
				if (string.IsNullOrEmpty(itemTier.Description))
				{
					itemTier.Description = description;
				}
				else
				{
					description = itemTier.Description;
				}
				if (d.Tiers[i].Cost == null)
				{
					d.Tiers[i].Cost = new List<int> { 0, 0, 0, 0 };
				}
				itemTier.UpgradeCost = new Dictionary<CraftResource, int>
				{
					{
						CraftResource.Red,
						d.Tiers[i].Cost[0]
					},
					{
						CraftResource.Green,
						d.Tiers[i].Cost[1]
					},
					{
						CraftResource.Blue,
						d.Tiers[i].Cost[2]
					},
					{
						CraftResource.Yellow,
						d.Tiers[i].Cost[3]
					}
				};
				itemTier.Reqs = new List<ItemReqs>();
				for (int j = 0; j < d.Reqs.Count; j++)
				{
					itemTier.Reqs.Add(new ItemReqs((Attributes)int.Parse(d.Reqs[j].attribute), d.Reqs[j].value));
				}
				itemTier.Effects = new List<IEffect>();
				for (int k = 0; k < d.Tiers[i].Effs.Count; k++)
				{
					ItemDetail.EffectObj effectObj = d.Tiers[i].Effs[k];
					SimpleEffect simpleEffect = new SimpleEffect();
					simpleEffect.effect = GameContext.GetEffect(((EffectNames)int.Parse(effectObj.effect)/*cast due to .constrained prefix*/).ToString());
					simpleEffect.target = GameContext.GetResource(effectObj.target);
					if (!string.IsNullOrEmpty(effectObj.parameter))
					{
						simpleEffect.parameter = GameContext.GetResource(effectObj.parameter);
						item.parameter = true;
					}
					simpleEffect.add = effectObj.a;
					if (string.IsNullOrEmpty(effectObj.m))
					{
						effectObj.m = "1";
					}
					simpleEffect.mult = effectObj.m;
					simpleEffect.efficiency = null;
					if (!string.IsNullOrEmpty(effectObj.diminish))
					{
						simpleEffect.pow_diminishing = float.Parse(effectObj.diminish, CultureInfo.InvariantCulture);
					}
					else if (!string.IsNullOrEmpty(d.Diminish))
					{
						simpleEffect.pow_diminishing = float.Parse(d.Diminish, CultureInfo.InvariantCulture);
					}
					itemTier.Effects.Add(simpleEffect);
				}
				item.ItemTiers.Add(int.Parse(d.Tiers[i].Tier), itemTier);
			}
			if (!string.IsNullOrEmpty(d.Set))
			{
				item.Set = craft.SetBonuses[(ItemSetKeys)int.Parse(d.Set)];
				item.Set.Items.Add(item);
			}
			Enchantment enchantment = enchants.Find((Enchantment x) => x.Key == d.Enchant);
			item.Enchant = new ItemEnchantment(enchantment.T, 1f + float.Parse(d.EBase, CultureInfo.InvariantCulture), enchantment.Description, item.Slot);
			list.Add(item);
		}
	}

	public void InitSetBonuses()
	{
		CraftManager craft = GameManager.Instance.Craft;
		craft.SetBonuses = new Dictionary<ItemSetKeys, ItemSetBonus>();
		foreach (ItemSetKeys value in Enum.GetValues(typeof(ItemSetKeys)))
		{
			craft.SetBonuses.Add(value, new ItemSetBonus(value));
		}
	}

	public void CreateAllSetBonuses(List<SetDetails> data)
	{
		CraftManager craft = GameManager.Instance.Craft;
		for (int i = 0; i < data.Count; i++)
		{
			SetDetails setDetails = data[i];
			ItemSetKeys key = (ItemSetKeys)Enum.Parse(typeof(ItemSetKeys), setDetails.Key);
			if (craft.SetBonuses.ContainsKey(key))
			{
				ItemSetBonus itemSetBonus = craft.SetBonuses[key];
				SimpleEffect simpleEffect = new SimpleEffect(GameContext.GetResource(setDetails.T));
				simpleEffect.add = setDetails.A;
				simpleEffect.mult = setDetails.M;
				if (!string.IsNullOrEmpty(setDetails.V))
				{
					simpleEffect.parameter = GameContext.GetResource(setDetails.V);
				}
				if (!string.IsNullOrEmpty(setDetails.F))
				{
					simpleEffect.effect = GameContext.GetEffect(setDetails.F);
				}
				ItemSetBonus.Bonus item = new ItemSetBonus.Bonus(int.Parse(setDetails.Amount), setDetails.Description, simpleEffect);
				itemSetBonus.Bonuses.Add(item);
			}
		}
	}
}
