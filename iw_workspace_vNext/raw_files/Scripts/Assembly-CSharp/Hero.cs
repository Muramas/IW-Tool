using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Hero : Spec
{
	public HeroesNames NameKey;

	public VariableInt Level;

	public float LevelProgress;

	public BigNumber Exp2LevelUp;

	public BigNumber CurrentExp;

	public string Feature;

	public List<Spells> SpellList;

	public List<string> ClosedBuildings;

	public VariableBignumber experience = new VariableBignumber(0.0);

	public List<HeroesNames> RequedClasses;

	public Action OnApplyEffect;

	protected QuotesSystem quotes;

	public List<int> UpgradeFilter { get; protected set; }

	public override void Init()
	{
		base.Init();
		Level = new VariableInt(1);
		RequedClasses = new List<HeroesNames>();
		Feature = NameKey.ToString() + " Feature";
		Description = NameKey.ToString() + " Lore";
		close_buildings();
	}

	public virtual void PostInit()
	{
	}

	public List<string> GetAllQuotes()
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<QuoteTrigger, HeroQuotes> item in quotes.dict)
		{
			list.AddRange(item.Value.quotes);
		}
		return list;
	}

	public virtual void InitUpgrades()
	{
		UpgradeFilter = new List<int>();
		UpgradeFilter.Add((int)NameKey);
		if (Tier <= 1)
		{
			return;
		}
		foreach (HeroesNames requedClass in RequedClasses)
		{
			UpgradeFilter.Add((int)requedClass);
		}
	}

	public void SubEffect()
	{
		VariableInt level = GameManager.Instance.CurrentHero.Level;
		level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(update_effect));
		VariableComplex abilityPower = GameManager.Instance.CurrentHero.AbilityPower;
		abilityPower.OnChange = (Action)Delegate.Combine(abilityPower.OnChange, new Action(update_effect));
	}

	public void UnsubEffect()
	{
		VariableInt level = GameManager.Instance.CurrentHero.Level;
		level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(update_effect));
		VariableComplex abilityPower = GameManager.Instance.CurrentHero.AbilityPower;
		abilityPower.OnChange = (Action)Delegate.Remove(abilityPower.OnChange, new Action(update_effect));
	}

	public virtual void update_effect()
	{
	}

	public virtual void update()
	{
	}

	public void close_buildings()
	{
		if (ClosedBuildings == null || ClosedBuildings.Count <= 0)
		{
			return;
		}
		foreach (string closedBuilding in ClosedBuildings)
		{
			foreach (BuildingVisual building in GameManager.Instance.Buildings)
			{
				if (building.building.NameStr == closedBuilding)
				{
					Statistic.TotalBuildings.Change(-building.building.Level.ValueInt);
					building.building.Level.SetValue(0);
					building.gameObject.SetActive(value: false);
					building.ChangeAvalible(v: false);
				}
			}
		}
	}

	public void open_buildings()
	{
		if (ClosedBuildings != null && ClosedBuildings.Count > 0)
		{
			foreach (string closedBuilding in ClosedBuildings)
			{
				foreach (BuildingVisual building in GameManager.Instance.Buildings)
				{
					if (building.building.Name == closedBuilding)
					{
						building.gameObject.SetActive(value: true);
						building.ChangeAvalible(v: true);
					}
				}
			}
		}
		Debug.Log("post open ");
	}

	public virtual string Tips_text()
	{
		return BaseTip();
	}

	public string BaseTip()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<b>");
		stringBuilder.Append(base.Name);
		stringBuilder.Append("</b>");
		stringBuilder.Append(" ");
		stringBuilder.Append("Level".Translate());
		stringBuilder.Append(": ");
		stringBuilder.AppendLine(Level.ValueInt.ToString());
		stringBuilder.Append("XP".Translate());
		stringBuilder.Append(": ");
		stringBuilder.Append(CurrentExp.ToReadableString("F0"));
		stringBuilder.Append(" / ");
		stringBuilder.AppendLine(Exp2LevelUp.ToReadableString("F0"));
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}

	public virtual string Descrition_req_text()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Requirements".Translate()).AppendLine(": ");
		if (Tier == 2 && !GameManager.Instance.Paragon.ClassT2IsAvailable)
		{
			stringBuilder.Append("Paragon Level".Translate());
			stringBuilder.Append(" ").AppendLine(GameManager.Instance.Paragon.ClassT2Req.ToString());
		}
		if (RequedClasses.Count > 0)
		{
			for (int i = 0; i < RequedClasses.Count; i++)
			{
				stringBuilder.Append(" - ");
				stringBuilder.AppendLine(GameManager.Instance.CurrentHero.HeroPanel.Find(RequedClasses[i]).Hero.Name);
			}
		}
		stringBuilder.Append("Level".Translate());
		stringBuilder.Append(" ");
		stringBuilder.AppendLine(base.LevelReq.ToString());
		if (!Unlocked && Conditions2Unlock != null && Conditions2Unlock.Count > 0)
		{
			stringBuilder.AppendLine("Requires to unlock".Translate());
			foreach (ConditionUnlock item in Conditions2Unlock)
			{
				stringBuilder.AppendLine(item.Preview());
			}
		}
		return stringBuilder.ToString();
	}

	public string AdditionalDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		BigNumber value = GameManager.Instance.CurrentHero.AbilityPower.Value;
		if (value != 1.0)
		{
			stringBuilder.Append("AP".Translate());
			stringBuilder.Append(": ");
			stringBuilder.Append(GetBonusMult(value + 1.0));
		}
		return stringBuilder.ToString();
	}

	public void UpdateExp()
	{
		int num = GameManager.Instance.CurrentHero.StartingLevel.ValueInt - 1;
		float growBaseLevel = GameManager.Instance.CurrentHero.GrowBaseLevel;
		float num2 = 1500f;
		int valueInt = Level.ValueInt;
		double starting_exp = (double)num2 * (1.0 - Math.Pow(growBaseLevel, num)) / (double)(1f - growBaseLevel);
		double boost = GameManager.Instance.CurrentHero.ExpBoost.Value.ToDouble();
		RecalculateExp(starting_exp, boost);
		int num3 = (int)(1.0 - experience.Value * (1f - growBaseLevel) / num2).Log_a(growBaseLevel);
		Exp2LevelUp = num2 * Mathf.Pow(growBaseLevel, num3);
		CurrentExp = experience.Value - num2 * (1f - Mathf.Pow(growBaseLevel, num3)) / (1f - growBaseLevel);
		num3 += GameManager.Instance.CurrentHero.AddLevel.ValueInt;
		if (Level.ValueInt != num3 + 1)
		{
			Level.SetValue(num3 + 1);
		}
		LevelProgress = (float)(CurrentExp / Exp2LevelUp).ToDouble();
		if (Level.ValueInt > valueInt && Level.ValueInt > num + 1)
		{
			GameManager.Instance.CurrentHero.Hero.quotes.onLevelUp();
			GameManager.Instance.CurrentHero.OnLevelUp();
		}
		if (GameManager.Instance.ChallengeManager.StatsIsOn())
		{
			if (NameKey == HeroesNames.Apprentice && Level.ValueInt > Statistic.ApprenticeMaxLevelRealm.ValueInt)
			{
				Statistic.ChangeSet(Statistic.ApprenticeMaxLevelRealm, Level.ValueInt);
			}
			if (Level.ValueInt > Statistic.HeroMaxLevelAllTime.ValueInt)
			{
				Statistic.ChangeSet(Statistic.HeroMaxLevelAllTime, Level.ValueInt);
			}
		}
	}

	protected virtual BigNumber GetExpBuildings()
	{
		return (Statistic.TotalBuildings.ValueInt * GameManager.Instance.CurrentHero.ExpManaSources.Value + 1.0).Pow(0.8550000190734863);
	}

	protected virtual BigNumber GetExpActive()
	{
		double num = GameManager.Instance.CurrentHero.ExpBoost.Value.ToDouble();
		return (((Statistic.Clicks.Value + Statistic.AutoClicks.Value) * num + 1.0).Log10() + 1.0) * ((Statistic.CastSpell.Value * num + 1.0).Ln() / 4.0 + 1.0) * ((Statistic.ManaSession.Value + 1.0).Log10() + 1.0) * (Math.Log((double)Statistic.ClickableCollect.ValueInt * num + 1.0) / 2.0 + 1.0);
	}

	public BigNumber GetExpMultPart()
	{
		return GameManager.Instance.CurrentHero.ExpMult.Value * ((GetExpBuildings() + 100.0) * GetExpActive() - 100.0);
	}

	protected virtual void RecalculateExp(double starting_exp, double boost)
	{
		if (Statistic.TotalBuildings.ValueInt < 0)
		{
			Statistic.TotalBuildings.SetValue(0);
		}
		experience.SetValue(starting_exp + GameManager.Instance.CurrentHero.ExpStack.Value * GetExpMultPart());
	}

	protected virtual BigNumber GetBonusMult()
	{
		HeroSlot currentHero = GameManager.Instance.CurrentHero;
		return currentHero.AbilityPower.Value * currentHero.Level.Value * currentHero.Level.Value;
	}

	public virtual void PostLoad()
	{
	}
}
