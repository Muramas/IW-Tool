using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemGilding
{
	public Dictionary<int, VariableGilding> Bonuses;

	public VariableBignumber Amount;

	private float researchBonusPerLevel = 0.04f;

	public ItemGilding()
	{
		Bonuses = new Dictionary<int, VariableGilding>();
		Amount = new VariableBignumber(1.0);
		VariableBignumber amount = Amount;
		amount.OnChange = (Action)Delegate.Combine(amount.OnChange, new Action(OnChangeResearchAmount));
	}

	private void OnChangeResearchAmount()
	{
		foreach (KeyValuePair<int, VariableGilding> bonuse in Bonuses)
		{
			if (bonuse.Value.add > 0.0)
			{
				bonuse.Value.SetMult(Amount.Value);
			}
		}
	}

	public void Add(Item item, int times)
	{
		int iD = item.ID;
		if (!Bonuses.ContainsKey(iD))
		{
			Bonuses.Add(iD, new VariableGilding(1));
			item.SetGilding(Bonuses[iD]);
		}
		Bonuses[iD].Change(researchBonusPerLevel * (float)times, 1.0);
		Bonuses[iD].SetMult(Amount.Value);
	}

	public void ResetAll()
	{
		foreach (KeyValuePair<int, VariableGilding> bonuse in Bonuses)
		{
			bonuse.Value.Reset(1.0);
		}
	}

	public int GetLevel(int id)
	{
		if (Bonuses.ContainsKey(id))
		{
			return Mathf.FloorToInt((Bonuses[id].add.ToFloat() + 0.01f) / researchBonusPerLevel);
		}
		return 0;
	}

	public Dictionary<int, int> Save()
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		foreach (KeyValuePair<int, VariableGilding> bonuse in Bonuses)
		{
			int level = GetLevel(bonuse.Key);
			if (level > 0)
			{
				dictionary.Add(bonuse.Key, level);
			}
		}
		return dictionary;
	}

	public void Load(Dictionary<int, int> data)
	{
		ResetAll();
		if (data == null)
		{
			return;
		}
		CraftManager craft = GameManager.Instance.Craft;
		foreach (KeyValuePair<int, int> v in data)
		{
			Item item = craft.AvailableItems.Find((Item x) => x.ID == v.Key);
			if (item == null)
			{
				Debug.LogError("Item null on load");
			}
			else
			{
				Add(item, v.Value);
			}
		}
	}
}
