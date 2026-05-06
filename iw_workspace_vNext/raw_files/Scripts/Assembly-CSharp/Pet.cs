using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Pet : Spec
{
	public PetNames NameKey;

	public PetFamily Family;

	public VariableInt Level;

	public VariableBignumber Experience;

	public BigNumber Exp2LevelUp;

	public VariableBignumber TotalExp;

	public string Feature;

	public string ExpDescription;

	public PetNames Evolution;

	protected PetSlot slot;

	protected List<Pet> reqPet;

	protected int reqPetLevel;

	public float grow_base = SpecConsts.pet_grow_base;

	private bool init;

	public VariableComplex ExpBonus => GameManager.Instance.CurrentPet.ExpBonus;

	public VariableLong PlayedTime
	{
		get
		{
			if (GameManager.Instance.CurrentPet.SecondPetIsAcitve() && GameManager.Instance.CurrentPet.PetPanel.secondSlot.Pet == this)
			{
				return GameManager.Instance.CurrentPet.PetPanel.secondSlot.PlayedTime;
			}
			return GameManager.Instance.CurrentPet.PlayedTime;
		}
	}

	public VariableBignumber SkipedPlayedTime
	{
		get
		{
			if (GameManager.Instance.CurrentPet.SecondPetIsAcitve() && GameManager.Instance.CurrentPet.PetPanel.secondSlot.Pet == this)
			{
				return GameManager.Instance.CurrentPet.PetPanel.secondSlot.SkipedPlayedTime;
			}
			return GameManager.Instance.CurrentPet.SkipedPlayedTime;
		}
	}

	public override void Init()
	{
		base.Init();
		Experience = new VariableBignumber(0.0);
		Exp2LevelUp = new BigNumber(100.0);
		TotalExp = new VariableBignumber(0.0);
		reqPet = new List<Pet>();
		Feature = NameString + " Feature";
		Description = NameString + " Lore";
		ExpDescription = NameString + " XP";
		init = true;
	}

	public void SubscribeAP()
	{
		VariableInt level = Level;
		level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(UpdateEffect));
		VariableComplex abilityPower = GameManager.Instance.CurrentPet.AbilityPower;
		abilityPower.OnChange = (Action)Delegate.Combine(abilityPower.OnChange, new Action(UpdateEffect));
		if (Tier == 1)
		{
			VariableComplex abilityPowerT = GameManager.Instance.CurrentPet.AbilityPowerT1;
			abilityPowerT.OnChange = (Action)Delegate.Combine(abilityPowerT.OnChange, new Action(UpdateEffect));
		}
		else if (Tier == 2)
		{
			VariableComplex abilityPowerT2 = GameManager.Instance.CurrentPet.AbilityPowerT2;
			abilityPowerT2.OnChange = (Action)Delegate.Combine(abilityPowerT2.OnChange, new Action(UpdateEffect));
		}
		UpdateEffect();
	}

	public void UnsubscribeAP()
	{
		VariableInt level = Level;
		level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(UpdateEffect));
		VariableComplex abilityPower = GameManager.Instance.CurrentPet.AbilityPower;
		abilityPower.OnChange = (Action)Delegate.Remove(abilityPower.OnChange, new Action(UpdateEffect));
		if (Tier == 1)
		{
			VariableComplex abilityPowerT = GameManager.Instance.CurrentPet.AbilityPowerT1;
			abilityPowerT.OnChange = (Action)Delegate.Remove(abilityPowerT.OnChange, new Action(UpdateEffect));
		}
		else if (Tier == 2)
		{
			VariableComplex abilityPowerT2 = GameManager.Instance.CurrentPet.AbilityPowerT2;
			abilityPowerT2.OnChange = (Action)Delegate.Remove(abilityPowerT2.OnChange, new Action(UpdateEffect));
		}
	}

	public virtual void UpdateEffect()
	{
	}

	public BigNumber GetAbilityPower()
	{
		BigNumber result = GameManager.Instance.CurrentPet.AbilityPower.Value;
		if (Tier == 1)
		{
			result *= GameManager.Instance.CurrentPet.AbilityPowerT1.Value;
		}
		else if (Tier == 2)
		{
			result *= GameManager.Instance.CurrentPet.AbilityPowerT2.Value;
		}
		if (GameManager.Instance.Ascension.IsActive && GameManager.Instance.CurrentPet.PetPanel.SecondIsActive && GameManager.Instance.CurrentPet.PetPanel.GetKey(isFirst: false) == NameKey)
		{
			result = result.Pow(0.4000000059604645);
		}
		return result;
	}

	public override void Restart()
	{
		if (init)
		{
			if (Level != null)
			{
				Level.SetValue(1);
			}
			Experience.SetValue(0.0);
			Exp2LevelUp = 100.0;
			TotalExp.SetValue(0.0);
		}
	}

	public virtual void AddExp(BigNumber exp)
	{
		BigNumber exp2 = ExpBonus.ApplyModOnVar(exp);
		AddExpConst(exp2);
	}

	public virtual void OfflineWork(int sec)
	{
	}

	public virtual void OfflineWork(int time, bool isReal)
	{
		OfflineWork(time);
	}

	public virtual void ResetExp()
	{
		RecalculateLevel(0.0);
	}

	public virtual void AddExpConst(BigNumber exp, bool offline = false)
	{
		TotalExp.Change(exp);
		if (!offline)
		{
			GameManager.Instance.CurrentPet.ExpCounter.Add(exp);
		}
		GameManager.Instance.CurrentPet.OnGetExp?.Invoke(exp);
		if (BigNumber.Sign(TotalExp.Value) != 1)
		{
			TotalExp.SetValue(0.0);
		}
		RecalculateLevel(TotalExp.Value);
		if (Level.ValueInt > Statistic.PetMaxLevel.ValueInt)
		{
			Statistic.PetMaxLevel.SetValue(Level.ValueInt);
		}
		if (Level.ValueInt > Statistic.PetMaxLevelAllTime.ValueInt)
		{
			Statistic.ChangeSet(Statistic.PetMaxLevelAllTime, Level.ValueInt);
		}
	}

	public void RecalculateLevel()
	{
		RecalculateLevel(TotalExp.Value);
	}

	public void SetLevel(float level)
	{
		int num = Mathf.FloorToInt(level);
		BigNumber value = 100.0 * (1.0 - new BigNumber(grow_base).Pow(num - 1)) / (1f - grow_base);
		TotalExp.SetValue(value);
		if (Level.ValueInt != num)
		{
			Level.SetValue(num);
		}
		Exp2LevelUp = 100.0 * new BigNumber(grow_base).Pow(num - 1);
		Experience.SetValue(Exp2LevelUp * (level % 1f));
		if (Statistic.PetMaxLevel.ValueInt < Level.ValueInt)
		{
			Statistic.PetMaxLevel.SetValue(Level.ValueInt);
		}
	}

	public virtual void RecalculateLevel(BigNumber total)
	{
		BigNumber bigNumber = 100.0 * (1.0 - Math.Pow(grow_base, GameManager.Instance.CurrentPet.StartingLevel.ValueInt)) / (double)(1f - grow_base);
		TotalExp.SetValue(total);
		int num = (int)(1.0 - (bigNumber + TotalExp.Value) * (1f - grow_base) / 100.0).Log_a(grow_base);
		if (num < 0)
		{
			Debug.Log("pet level <0");
			num = 0;
		}
		if (Level.ValueInt != num + 1)
		{
			Level.SetValue(num + 1);
		}
		Exp2LevelUp = 100.0 * new BigNumber(grow_base).Pow(num);
		Experience.SetValue(bigNumber + TotalExp.Value - 100.0 * (1.0 - new BigNumber(grow_base).Pow(num)) / (1f - grow_base));
	}

	public virtual string Tips_text()
	{
		return BaseText();
	}

	public virtual void OnHideTip()
	{
	}

	public string BaseText()
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
		stringBuilder.Append(Experience.Value.ToReadableString("F0"));
		stringBuilder.Append(" / ");
		stringBuilder.Append(Exp2LevelUp.ToReadableString("F0"));
		stringBuilder.Append(" (");
		stringBuilder.Append(GameManager.Instance.CurrentPet.ExpCounter.GetDescription());
		stringBuilder.AppendLine(")");
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}

	public virtual void update()
	{
	}

	public string GetRequirements()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Requirements".Translate()).AppendLine(": ");
		if (Tier == 2 && !GameManager.Instance.Paragon.PetT2IsAvailable)
		{
			stringBuilder.AppendLine("Paragon Level".Translate() + " " + GameManager.Instance.Paragon.PetT2Req);
		}
		else if (Tier == 3 && !GameManager.Instance.Paragon.PetT3IsAvailable)
		{
			stringBuilder.AppendLine("Paragon Level".Translate() + " " + GameManager.Instance.Paragon.PetT3Req);
		}
		stringBuilder.Append("Character Level".Translate()).Append(" ");
		stringBuilder.AppendLine(base.LevelReq.ToString());
		if (!Unlocked && reqPet != null && reqPet.Count > 0)
		{
			stringBuilder.Append("Level".Translate()).Append(" ");
			int num = reqPetLevel - GameManager.Instance.LevelReduction.ValueInt;
			if (num < 1)
			{
				num = 1;
			}
			stringBuilder.Append(num.ToString());
			stringBuilder.Append(" ");
			stringBuilder.Append(reqPet[0].Name);
			for (int i = 1; i < reqPet.Count; i++)
			{
				stringBuilder.Append(" ").Append("or").Append(" ");
				stringBuilder.Append(reqPet[i].Name);
			}
			stringBuilder.AppendLine();
		}
		if (Conditions2Reqs != null)
		{
			foreach (ConditionUnlock conditions2Req in Conditions2Reqs)
			{
				stringBuilder.AppendLine(conditions2Req.Preview());
			}
		}
		if (!Unlocked)
		{
			stringBuilder.AppendLine("Requires to unlock".Translate());
			foreach (ConditionUnlock item in Conditions2Unlock)
			{
				stringBuilder.AppendLine(item.Preview());
			}
		}
		return stringBuilder.ToString();
	}

	public string GetDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Feature.Translate());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(ExpDescription.Translate());
		stringBuilder.AppendLine();
		stringBuilder.Append(Description.Translate());
		return stringBuilder.ToString();
	}

	public virtual string Description_text()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<b>");
		stringBuilder.Append(base.Name);
		stringBuilder.AppendLine("</b>");
		stringBuilder.AppendLine();
		stringBuilder.Append("Requirements".Translate()).AppendLine(": ");
		stringBuilder.Append("Character Level".Translate()).Append(" ");
		stringBuilder.AppendLine(base.LevelReq.ToString());
		if (!Unlocked && reqPet != null && reqPet.Count > 0)
		{
			stringBuilder.Append("Level".Translate()).Append(" ");
			int num = reqPetLevel - GameManager.Instance.LevelReduction.ValueInt;
			if (num < 1)
			{
				num = 1;
			}
			stringBuilder.Append(num.ToString());
			stringBuilder.Append(" ");
			stringBuilder.Append(reqPet[0].Name);
			for (int i = 1; i < reqPet.Count; i++)
			{
				stringBuilder.Append(" ").Append("or").Append(" ");
				stringBuilder.Append(reqPet[i].Name);
			}
			stringBuilder.AppendLine();
		}
		if (Conditions2Reqs != null)
		{
			foreach (ConditionUnlock conditions2Req in Conditions2Reqs)
			{
				stringBuilder.AppendLine(conditions2Req.Preview());
			}
		}
		if (!Unlocked)
		{
			stringBuilder.AppendLine("Requires to unlock".Translate());
			if (Tier == 2)
			{
				stringBuilder.AppendLine("Paragon level".Translate() + " 5");
			}
			foreach (ConditionUnlock item in Conditions2Unlock)
			{
				stringBuilder.AppendLine(item.Preview());
			}
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(Feature.Translate());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(ExpDescription.Translate());
		stringBuilder.AppendLine();
		stringBuilder.Append(Description.Translate());
		return stringBuilder.ToString();
	}

	public virtual string Progress_Text()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Time left ");
		stringBuilder.Append("");
		stringBuilder.AppendLine();
		stringBuilder.Append("Yield ");
		return stringBuilder.ToString();
	}

	public string AdditionalDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		BigNumber abilityPower = GetAbilityPower();
		if (abilityPower != 1.0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("AP".Translate());
			stringBuilder.Append(": ");
			stringBuilder.Append(GetBonusMult(abilityPower + 1.0));
		}
		return stringBuilder.ToString();
	}

	public bool CheckReqs()
	{
		bool flag = GameManager.Instance.CurrentHero.Level.ValueInt >= base.LevelReq;
		if (flag && (!Unlocked || NameKey == PetNames.PsychicCacophony))
		{
			Pet currentPet = GameManager.Instance.CurrentPet.PetPanel.GetCurrentPet();
			flag = (currentPet == null && reqPet.Count == 0) || CheckReqPet(currentPet);
		}
		if (flag && Conditions2Reqs != null)
		{
			foreach (ConditionUnlock conditions2Req in Conditions2Reqs)
			{
				flag = conditions2Req.Check();
				if (!flag)
				{
					break;
				}
			}
		}
		return flag;
	}

	public bool CheckReqPet(Pet pet)
	{
		if (Unlocked)
		{
			return true;
		}
		if (reqPet.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < reqPet.Count; i++)
		{
			if (pet == reqPet[i])
			{
				return pet.Level.ValueInt >= reqPetLevel - GameManager.Instance.LevelReduction.ValueInt;
			}
		}
		return false;
	}
}
