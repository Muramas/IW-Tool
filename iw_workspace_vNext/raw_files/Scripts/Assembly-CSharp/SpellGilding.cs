using System.Collections.Generic;

public class SpellGilding
{
	public class SaveData
	{
		public BigNumber current;

		public BigNumber total;

		public BigNumber masteryExp;

		public int shardsLvl;

		public int maxLevel;

		public List<SaveDataSpell> data;

		public void Add(Spells spell, SpellUpgrade sp)
		{
			if (data == null)
			{
				data = new List<SaveDataSpell>();
			}
			data.Add(new SaveDataSpell((int)spell, sp.Level, sp.Current));
		}
	}

	public class SaveDataSpell
	{
		public int ID;

		public int Lvl;

		public int Current;

		public SaveDataSpell(int id, int lvl, int current)
		{
			ID = id;
			Lvl = lvl;
			Current = current;
		}
	}

	public Dictionary<Spells, SpellUpgrade> Map;

	public VariableBignumber ink;

	public VariableBignumber inkTotal;

	public VariableComplex inkIncome;

	public ShardsPoolUpgrade shardsPool;

	public SpellMastery mastery;

	public VariableInt EvoLevel;

	private BigNumber baseCost = 10.0;

	private BigNumber growthRate = 1.2400000095367432;

	private VariableInt maxSpellLevel;

	public VariableInt costReduction;

	public SpellGilding()
	{
		Map = new Dictionary<Spells, SpellUpgrade>();
		ink = new VariableBignumber(0.0);
		inkTotal = new VariableBignumber(0.0);
		inkIncome = new VariableComplex(1.0);
		maxSpellLevel = new VariableInt(0);
		costReduction = new VariableInt(0);
		EvoLevel = new VariableInt(0);
		GameContext.ContextAddResource("SpellGilding.InkTotal", inkTotal);
		GameContext.ContextAddResource("SpellGilding.InkIncome", inkIncome);
		GameContext.ContextAddResource("SpellGilding.MaxSpellLevel", maxSpellLevel);
		GameContext.ContextAddResource("SpellGilding.CostReduction", costReduction);
		GameContext.ContextAddResource("SpellGilding.EvoLevel", EvoLevel);
		mastery = new SpellMastery();
		shardsPool = new ShardsPoolUpgrade();
	}

	public void CheckMax(int lvl)
	{
		if (lvl > maxSpellLevel.ValueInt)
		{
			maxSpellLevel.SetValue(lvl);
		}
	}

	public void Reset()
	{
		foreach (KeyValuePair<Spells, SpellUpgrade> item in Map)
		{
			item.Value.Reset();
		}
		mastery.Skill.Reset();
		shardsPool.Remove();
	}

	public void RealmChange()
	{
		Remove();
		mastery.Bonuses.Preload();
	}

	public void ResetSoft()
	{
		Reset();
		ink.SetValue(inkTotal.Value);
		ink.OnChange?.Invoke();
	}

	public void ResetFull()
	{
		Reset();
		inkTotal.SetValue(0.0);
		ink.SetValue(0.0);
		maxSpellLevel.SetValue(0);
	}

	public void Remove()
	{
		foreach (KeyValuePair<Spells, SpellUpgrade> item in Map)
		{
			item.Value.Remove();
		}
		shardsPool.Delete();
	}

	public void Apply()
	{
		foreach (KeyValuePair<Spells, SpellUpgrade> item in Map)
		{
			item.Value.Apply();
		}
		shardsPool.UpdateEffect();
	}

	public void AddInk(BigNumber amount)
	{
		amount *= inkIncome.Value;
		inkTotal.Change(amount);
		ink.Change(amount);
	}

	public void AddInkConst(BigNumber amount)
	{
		inkTotal.Change(amount);
		ink.Change(amount);
	}

	public BigNumber GetCost(int startFrom = 0, int levels = 1)
	{
		if (levels == 1)
		{
			return (baseCost * growthRate.Pow(startFrom)).Floor();
		}
		BigNumber result = 0.0;
		for (int i = 0; i < levels; i++)
		{
			result += GetCost(startFrom + i);
		}
		return result;
	}

	public SpellUpgrade Get(Spells key)
	{
		if (Map.ContainsKey(key))
		{
			return Map[key];
		}
		return null;
	}

	public SpellUpgrade Add(Spells key)
	{
		SpellUpgrade spellUpgrade = new SpellUpgrade(key);
		Map.Add(key, spellUpgrade);
		return spellUpgrade;
	}

	public void PreLoad()
	{
		Remove();
		mastery.Bonuses.Preload();
		shardsPool.Remove();
		mastery.Skill.Reset();
		ResetFull();
	}

	public void PreExile()
	{
		shardsPool.Delete();
		mastery.Bonuses.Preload();
	}

	public void PostExile()
	{
		mastery.Bonuses.CheckActivate();
		if (GameManager.Instance.Paragon.GildingSpellsIsAvailable)
		{
			Apply();
		}
	}

	public void PostLoad()
	{
		mastery.Bonuses.CheckActivate();
		if (GameManager.Instance.Paragon.GildingSpellsIsAvailable)
		{
			Apply();
		}
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData();
		saveData.total = inkTotal.Value;
		saveData.current = ink.Value;
		saveData.masteryExp = mastery.Skill.totalExp;
		saveData.shardsLvl = shardsPool.Level;
		saveData.maxLevel = maxSpellLevel.ValueInt;
		foreach (KeyValuePair<Spells, SpellUpgrade> item in Map)
		{
			if (item.Value.Level > 0)
			{
				saveData.Add(item.Key, item.Value);
			}
		}
		return saveData;
	}

	public void Load(SaveData data)
	{
		if (data == null)
		{
			ResetFull();
			return;
		}
		_ = GameManager.Instance.SpellBook;
		inkTotal.SetValue(data.total);
		ink.SetValue(data.current);
		mastery.Skill.Load(data.masteryExp);
		shardsPool.Load(data.shardsLvl);
		maxSpellLevel.SetValue(data.maxLevel);
		int num = 0;
		if (data.data == null)
		{
			return;
		}
		foreach (SaveDataSpell datum in data.data)
		{
			SpellUpgrade spellUpgrade = Get((Spells)datum.ID);
			if (spellUpgrade == null)
			{
				spellUpgrade = Add((Spells)datum.ID);
			}
			spellUpgrade.Load(datum.Lvl, datum.Current);
			if (datum.Lvl > num)
			{
				num = datum.Lvl;
			}
		}
		if (maxSpellLevel.ValueInt < num)
		{
			maxSpellLevel.SetValue(num);
		}
	}
}
