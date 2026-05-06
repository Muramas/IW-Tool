using System;
using System.Collections.Generic;
using System.Text;

public class ItemEnchantment
{
	public const int BASE_COST = 100;

	public const float COST_GROWTH = 1.5f;

	public string key;

	public Variable Target;

	public BigNumber Bonus;

	public string Description;

	public bool applied;

	private BigNumber prev_bonus;

	private BigNumber cost;

	public Action OnEnchant;

	public SlotKey slot;

	private Dictionary<SlotKey, VariableInt> _slotBonusMap;

	public int Level { get; private set; }

	public ItemEnchantment(string target, float bonus, string descr, SlotKey slot)
	{
		Initialize(bonus, slot);
		Target = GameContext.GetResource(target);
		Description = descr;
	}

	public ItemEnchantment(string key, float bonus, SlotKey slot = SlotKey.None)
	{
		this.key = key;
		Initialize(bonus, slot);
		ItemsCreator.Enchantment enchantment = GlobalData.EnchantmentBonuses.Find((ItemsCreator.Enchantment x) => x.Key == key);
		Target = GameContext.GetResource(enchantment.T);
		Description = enchantment.Description;
	}

	private void Initialize(float bonus, SlotKey slot)
	{
		Level = 0;
		applied = false;
		prev_bonus = 0.0;
		Bonus = bonus;
		this.slot = slot;
		cost = GetCost();
		InitSlotBonusMap();
	}

	private void InitSlotBonusMap()
	{
		_slotBonusMap = new Dictionary<SlotKey, VariableInt>
		{
			{
				SlotKey.Chest,
				GameManager.Instance.Craft.BonusEnchantLevelChest
			},
			{
				SlotKey.Ring,
				GameManager.Instance.Craft.BonusEnchantLevelRing
			},
			{
				SlotKey.Hands,
				GameManager.Instance.Craft.BonusEnchantLevelHand
			},
			{
				SlotKey.Shoulder,
				GameManager.Instance.Craft.BonusEnchantLevelShoulder
			}
		};
	}

	public void Apply()
	{
		if (!applied && Level > 0)
		{
			prev_bonus = Bonus.Pow(GetFullLevel());
			Target.Change(0.0, prev_bonus);
			applied = true;
			VariableInt maxEnchantLevel = GameManager.Instance.Craft.MaxEnchantLevel;
			maxEnchantLevel.OnChange = (Action)Delegate.Combine(maxEnchantLevel.OnChange, new Action(Update));
			if (_slotBonusMap.ContainsKey(slot))
			{
				VariableInt variableInt = _slotBonusMap[slot];
				variableInt.OnChange = (Action)Delegate.Combine(variableInt.OnChange, new Action(Update));
			}
		}
	}

	public int GetFullLevel()
	{
		if (GetLevel() == 0)
		{
			return 0;
		}
		return GetLevel() + GetBonusLevel();
	}

	private int GetBonusLevel()
	{
		int num = GameManager.Instance.Craft.BonusEnchantLevel.ValueInt;
		if (_slotBonusMap.ContainsKey(slot))
		{
			num += _slotBonusMap[slot].ValueInt;
		}
		return num;
	}

	private int GetLevel()
	{
		if (Level <= GameManager.Instance.Craft.MaxEnchantLevel.ValueInt)
		{
			return Level;
		}
		return GameManager.Instance.Craft.MaxEnchantLevel.ValueInt;
	}

	public void Delete()
	{
		if (applied && Level > 0)
		{
			Target.Change(0.0, 1.0 / prev_bonus);
			applied = false;
			VariableInt maxEnchantLevel = GameManager.Instance.Craft.MaxEnchantLevel;
			maxEnchantLevel.OnChange = (Action)Delegate.Remove(maxEnchantLevel.OnChange, new Action(Update));
			if (_slotBonusMap.ContainsKey(slot))
			{
				VariableInt variableInt = _slotBonusMap[slot];
				variableInt.OnChange = (Action)Delegate.Remove(variableInt.OnChange, new Action(Update));
			}
		}
	}

	public void Upgrade(int lvl)
	{
		cost = GetCost(lvl);
		if (!(GameManager.Instance.Craft.EnchantingDust.Value < cost))
		{
			GameManager.Instance.Craft.EnchantingDust.Change(-cost);
			Level += lvl;
			cost = GetCost();
			Update();
			OnEnchant?.Invoke();
		}
	}

	public void Update()
	{
		if (applied)
		{
			Delete();
			Apply();
		}
	}

	public string Preview(bool levels)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Enchanting".Translate());
		stringBuilder.Append(": ");
		stringBuilder.Append(GetDescription());
		stringBuilder.Append(getPreviewEnchant(1));
		if (levels)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(string.Format("ItemEnchantBonus".Translate(), getPreviewEnchant(GetFullLevel()), (GetBonusLevel() > 0) ? (Level + " + " + GetBonusLevel()) : Level.ToString()));
		}
		return stringBuilder.ToString();
	}

	public string PreviewBase()
	{
		return GetDescription() + getPreviewEnchant(1);
	}

	public string PreviewNext()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(GetDescription());
		stringBuilder.Append(getPreviewEnchant(GetFullLevel()));
		stringBuilder.Append(" -> ");
		stringBuilder.Append(getPreviewEnchant(GetFullLevel() + 1));
		if (GetBonusLevel() > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("Level".Translate());
			stringBuilder.Append(" +");
			stringBuilder.Append(GetBonusLevel());
		}
		return stringBuilder.ToString();
	}

	public string GetDescription()
	{
		return TranslationManager.Instance.Process(Description) + " ";
	}

	private string getPreviewEnchant(int lvl)
	{
		return ((Bonus.Pow(lvl) - 1.0) * 100.0).ToReadableString() + "%";
	}

	public BigNumber GetCost(int lvl = 1)
	{
		if (lvl == 1)
		{
			return 100.0 * new BigNumber(1.5).Pow(Level);
		}
		BigNumber bigNumber = getFullCost(Level + lvl) - getFullCost(Level);
		if (BigNumber.Sign(bigNumber) < 0)
		{
			bigNumber = 0.0;
		}
		return bigNumber;
	}

	public BigNumber GetFullCost()
	{
		return getFullCost(Level);
	}

	private BigNumber getFullCost(int lvl)
	{
		return 100.0 * (1.0 - new BigNumber(1.5).Pow(lvl)) / -0.5;
	}

	public void Disenchant()
	{
		Delete();
		GameManager.Instance.Craft.EnchantingDust.Change(GetFullCost());
		Load(0);
	}

	public void Load(int level)
	{
		Level = level;
		cost = GetCost();
	}
}
