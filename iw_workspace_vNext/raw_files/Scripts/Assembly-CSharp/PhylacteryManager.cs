using System;
using System.Collections.Generic;
using PhylacteryList;

public class PhylacteryManager
{
	public class PhylacteryFormat
	{
		public string ID;

		public string Name;

		public string Sprite;

		public string Description;

		public string Enchant;

		public string EValue;

		public string Lore;
	}

	private List<Phylactery> phylacteries;

	public VariableInt CharLevel;

	public VariableBignumber Efficiency;

	public PhylacteryManager()
	{
		CharLevel = new VariableInt(0);
		Efficiency = new VariableBignumber(1.0);
		GameContext.ContextAddResource("PhylacteryEfficiency", Efficiency);
	}

	public void Init()
	{
		phylacteries = new List<Phylactery>();
		Add(new Inca());
		Add(new PhylacteryList.Char());
		Add(new PhylacteryList.Pet());
		Add(new PhylacteryList.Idle());
		Add(new Myst());
		Add(new Vpe());
		Add(new Autoclick());
		Add(new SummDur());
		VariableInt level = GameManager.Instance.CurrentHero.Level;
		level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(RecalculateLevel));
		VariableInt levelReduction = GameManager.Instance.LevelReduction;
		levelReduction.OnChange = (Action)Delegate.Combine(levelReduction.OnChange, new Action(RecalculateLevel));
		RecalculateLevel();
		foreach (Phylactery phylactery in phylacteries)
		{
			phylactery.SetEfficiency(Efficiency);
		}
	}

	public int GetStartingCharLevel()
	{
		return 200 - GameManager.Instance.LevelReduction.ValueInt;
	}

	private void RecalculateLevel()
	{
		int num = GameManager.Instance.CurrentHero.Level.ValueInt - GetStartingCharLevel();
		if (num < 0)
		{
			num = 0;
		}
		if (CharLevel.ValueInt != num)
		{
			CharLevel.SetValue(num);
		}
	}

	private void Add(Phylactery mount)
	{
		mount.Init();
		GameManager.Instance.Craft.AllItems.Add(mount);
		phylacteries.Add(mount);
	}
}
