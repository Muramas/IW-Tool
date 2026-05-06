using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Item
{
	public const int LEGENDARY_TIER = 4;

	public const int MYTHIC_TIER = 6;

	public int ID = -1;

	public int Tier;

	public int BaseTier;

	public string NameKey;

	public SlotKey Slot;

	public float ProgressInvested;

	public Sprite Icon;

	public string LoreKey;

	public int Rarity;

	public bool Unscaled;

	public bool Favorite;

	public bool equiped;

	public bool active;

	public bool parameter;

	public ItemSetBonus Set;

	public Dictionary<int, IItemTier> ItemTiers;

	public ItemEnchantment Enchant;

	private Variable Gilding;

	public Variable Efficiency;

	private bool isSubscribedCheckReqs;

	public string Name => NameKey.Translate();

	public string LoreDescription => LoreKey.Translate();

	public int GetReq(Attributes attr)
	{
		return ItemTiers[Tier].GetReqs(attr);
	}

	public void ResetTier()
	{
		Tier = BaseTier;
		Enchant?.Load(0);
		ProgressInvested = 0f;
	}

	public void Invest(float progress)
	{
		ProgressInvested += progress;
		if (ProgressInvested >= (float)GameManager.Instance.Craft.ProgressToUpgradeItemCosts)
		{
			ProgressInvested -= GameManager.Instance.Craft.ProgressToUpgradeItemCosts;
			IncreaseTier();
		}
	}

	public bool CheckReqs()
	{
		return ItemTiers[Tier].CheckReqs();
	}

	public void SetGilding(Variable variable)
	{
		if (Gilding != null)
		{
			Gilding.OnChange = null;
		}
		Gilding = variable;
		foreach (KeyValuePair<int, IItemTier> itemTier in ItemTiers)
		{
			itemTier.Value.SetGilding(Gilding);
		}
		Variable gilding = Gilding;
		gilding.OnChange = (Action)Delegate.Combine(gilding.OnChange, new Action(UpdateEffect));
		UpdateEffect();
	}

	public void SetEfficiency(Variable variable)
	{
		if (Efficiency != null)
		{
			Efficiency.OnChange = null;
		}
		foreach (KeyValuePair<int, IItemTier> itemTier in ItemTiers)
		{
			itemTier.Value.SetEfficiency(variable);
		}
		Efficiency = variable;
		if (Efficiency != null)
		{
			Efficiency.OnChange = UpdateEffect;
		}
		UpdateEffect();
	}

	public virtual void UpdateEffect()
	{
		if (active)
		{
			ItemTiers[Tier].Update();
		}
	}

	public virtual void ApplyEffects()
	{
		if (!active)
		{
			active = true;
			ItemTiers[Tier].Apply();
			Enchant?.Apply();
			if (Set != null)
			{
				Set.UpdateSet();
			}
		}
	}

	public virtual void RemoveEffects()
	{
		if (active)
		{
			active = false;
			Enchant?.Delete();
			ItemTiers[Tier].Delete();
			if (Set != null)
			{
				Set.UpdateSet();
			}
		}
	}

	public virtual string GetDescr()
	{
		return ItemTiers[Tier].GetDescription();
	}

	public Dictionary<CraftResource, int> GetCost()
	{
		if (Tier < BaseTier)
		{
			Tier = BaseTier;
		}
		return ItemTiers[Tier].GetCost();
	}

	private void IncreaseTier()
	{
		if (equiped)
		{
			UnsubCheckReqs();
			RemoveEffects();
		}
		Tier++;
		Statistic.UnlockedByTiers[Tier].Change(1);
		if (equiped)
		{
			ActiveCheck();
			SubCheckReqs();
			GameManager.Instance.Craft.window.doll.Slots.Find((ItemSlot x) => x.Item != null && x.Item.ID == ID).UpdateVisual();
		}
		GameManager.Instance.Craft.OnGetItem?.Invoke(this);
	}

	public virtual void Equip()
	{
		equiped = true;
		ActiveCheck();
		SubCheckReqs();
		if (Set != null)
		{
			Set.Activate();
		}
	}

	public virtual void EquipItem(bool onLoad = false)
	{
		ItemsDoll doll = GameManager.Instance.Craft.window.doll;
		if (!onLoad && !doll.SlotIsAvailable(Slot))
		{
			return;
		}
		ItemSlot slot = doll.GetSlot(Slot);
		if (slot.Item == this)
		{
			return;
		}
		if (equiped)
		{
			doll.Slots.Find((ItemSlot x) => x.Item == this).Clear();
		}
		slot.SelectItem(this);
		if (!onLoad && Set != null)
		{
			Set.Activate();
		}
	}

	public void Unequip()
	{
		UnsubCheckReqs();
		equiped = false;
		RemoveEffects();
	}

	public void SubCheckReqs()
	{
		if (isSubscribedCheckReqs)
		{
			return;
		}
		List<ItemReqs> reqs = ItemTiers[Tier].GetReqs();
		if (reqs != null)
		{
			for (int i = 0; i < reqs.Count; i++)
			{
				Variable variable = reqs[i].Parameter;
				variable.OnChange = (Action)Delegate.Combine(variable.OnChange, new Action(ActiveCheck));
			}
			isSubscribedCheckReqs = true;
		}
	}

	public void UnsubCheckReqs()
	{
		if (!isSubscribedCheckReqs || !ItemTiers.ContainsKey(Tier))
		{
			return;
		}
		List<ItemReqs> reqs = ItemTiers[Tier].GetReqs();
		if (reqs != null)
		{
			for (int i = 0; i < reqs.Count; i++)
			{
				Variable variable = reqs[i].Parameter;
				variable.OnChange = (Action)Delegate.Remove(variable.OnChange, new Action(ActiveCheck));
			}
			isSubscribedCheckReqs = false;
		}
	}

	private void ActiveCheck()
	{
		bool flag = CheckReqs();
		if (active && !flag)
		{
			RemoveEffects();
		}
		if (!active && flag)
		{
			ApplyEffects();
		}
	}

	public virtual string Preview()
	{
		if (!equiped)
		{
			if (Slot != SlotKey.Ring)
			{
				SetEfficiency(GameManager.Instance.Craft.GetEfficiency(Slot, 0));
			}
			else
			{
				ItemSlot itemSlot = GameManager.Instance.Craft.window.doll.Slots.Find((ItemSlot x) => x.Key == SlotKey.Ring && x.Number == 1);
				int number = 0;
				if (itemSlot != null && itemSlot.Item == null)
				{
					number = 1;
				}
				SetEfficiency(GameManager.Instance.Craft.GetEfficiency(Slot, number));
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<b>");
		stringBuilder.Append(Name);
		stringBuilder.Append("</b>");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.Append("Slot".Translate());
		stringBuilder.Append(": ");
		stringBuilder.AppendLine(Slot.ToString().Translate());
		stringBuilder.Append("Quality".Translate());
		stringBuilder.Append(": ");
		stringBuilder.AppendLine(GameManager.Instance.Craft.window.TierNames[Tier].Translate());
		stringBuilder.Append(ItemTiers[Tier].GetReqDescription());
		stringBuilder.AppendLine();
		_ = ItemTiers[Tier];
		stringBuilder.AppendLine(GetDescr());
		if (Slot == SlotKey.Research)
		{
			int researchGildingLevel = GameManager.Instance.Craft.GetResearchGildingLevel(ID);
			if (researchGildingLevel > 0)
			{
				stringBuilder.Append("Breakthroughs".Translate()).Append(": ").Append(researchGildingLevel)
					.AppendLine();
			}
		}
		else if (Slot == SlotKey.Misc)
		{
			int trophiesGildingLevel = GameManager.Instance.Craft.GetTrophiesGildingLevel(ID);
			if (trophiesGildingLevel > 0)
			{
				stringBuilder.Append("Exhibits".Translate()).Append(": ").Append(trophiesGildingLevel)
					.AppendLine();
			}
		}
		stringBuilder.AppendLine();
		if (Enchant != null)
		{
			stringBuilder.AppendLine(Enchant.Preview(Tier >= 4 && Enchant.Level > 0));
		}
		if (Set != null)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(Set.Preview());
		}
		if (!string.IsNullOrEmpty(LoreKey))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(LoreDescription);
		}
		if (Tier == 6)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("MythicOnlyOne".Translate());
		}
		return stringBuilder.ToString();
	}
}
