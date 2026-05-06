using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class Spell : EffectFormat
{
	public class AdditionalCost
	{
		public Variable resource;

		public int cost;

		public string name;

		public bool isBuilding;

		public int Cost
		{
			get
			{
				float num = (float)cost / GameManager.Instance.Scrolls.SpellSubcostReduction.ValueFloat;
				if (num < 1f)
				{
					num = 1f;
				}
				return Mathf.FloorToInt(num);
			}
			set
			{
				cost = value;
			}
		}
	}

	public string Name;

	public Spells NameKey;

	public SpellTypeBehavior TypeBehavior;

	public SpellTypeGroup Type;

	public VariableComplex Duration;

	public float Period;

	public VariableComplex Use;

	public VariableBignumber UseThisRun;

	public int Priority;

	public bool ResetUses;

	public bool Accumulated;

	public bool IsNotCharging;

	public bool ShardsBuilding = true;

	public bool IsShadow;

	public bool LimitedCharges = true;

	public bool CursedChallenge = true;

	public AdditionalCost SubCost;

	public IEffect PassiveEffect;

	public Action OnChoose;

	public Action OnClear;

	public bool IsWorkOffline;

	public CounterAverage useCounter;

	public CounterAverage useThisRunCounter;

	public VariableFloat GildingCost;

	public VariableInt AdditionalCastRate;

	public float CostModifier = 1f;

	public ulong chargeCount;

	public float chargeProgress;

	public int level_req = 1;

	public bool IsEnhancement;

	public bool choice;

	public List<IEffect> effects;

	public AdditionalSpellEffect addSpellEffect;

	private double full_build;

	protected bool available;

	public bool active;

	public float CostMod
	{
		get
		{
			float num = CostModifier;
			if (GildingCost != null)
			{
				num *= GildingCost.ValueFloat;
			}
			return num;
		}
	}

	public int LevelReq
	{
		get
		{
			int num = level_req - GameManager.Instance.LevelReduction.ValueInt;
			if (num < 1)
			{
				num = 1;
			}
			return num;
		}
		set
		{
			level_req = value;
		}
	}

	public int MaxChargeCount => GameManager.Instance.Scrolls.MaxCharge.ValueInt;

	public bool AvailableToCast
	{
		get
		{
			if (chargeCount != 0)
			{
				if (SubCost == null)
				{
					return true;
				}
				return SubCost.resource.Value > SubCost.Cost;
			}
			return false;
		}
	}

	public bool AvailableToUse => LevelReq <= GameManager.Instance.CurrentHero.Hero.Level.ValueInt;

	public bool IsMax
	{
		get
		{
			if (LimitedCharges)
			{
				return chargeCount >= (ulong)MaxChargeCount;
			}
			return chargeCount >= 1000000000;
		}
	}

	public bool IsAccumulated => Accumulated;

	public bool IsPersistent => !ResetUses;

	public bool IsAugment => TypeBehavior == SpellTypeBehavior.Permanent;

	protected double ProgressBuild => (double)chargeProgress * FullBuild;

	public double FullBuild
	{
		get
		{
			if (ShardsBuilding)
			{
				double num = GameManager.Instance.Scrolls.SpellShardsCostReduction.Value.ToDouble();
				if (num < 0.009999999776482582)
				{
					num = 0.009999999776482582;
				}
				return full_build * num * (double)CostMod;
			}
			double num2 = GameManager.Instance.Scrolls.SpellChargingCostReduction.Value.ToDouble();
			if (num2 < 0.009999999776482582)
			{
				num2 = 0.009999999776482582;
			}
			return full_build * num2 * (double)CostMod;
		}
		set
		{
			full_build = value;
		}
	}

	public float GetAllProgress => (float)chargeCount + chargeProgress;

	public double GetProgressBuild => ProgressBuild;

	public Spell(SpellFormat f)
	{
		Name = f.Name;
		NameKey = (Spells)Enum.Parse(typeof(Spells), f.Key);
		TypeBehavior = (SpellTypeBehavior)Enum.Parse(typeof(SpellTypeBehavior), f.TypeBehavior);
		Type = (SpellTypeGroup)Enum.Parse(typeof(SpellTypeGroup), f.SpellType);
		Duration = new VariableComplex(float.Parse(f.Duration, CultureInfo.InvariantCulture));
		FullBuild = float.Parse(f.Build, CultureInfo.InvariantCulture);
		Description = f.Description;
		LevelReq = int.Parse(f.Requirements);
		Use = new VariableComplex(0.0);
		UseThisRun = new VariableBignumber(0.0);
		if (f.ResetUses == "" || f.ResetUses == "yes")
		{
			ResetUses = true;
		}
		if (f.SB != "")
		{
			ShardsBuilding = false;
			if (f.SB == "none")
			{
				IsNotCharging = true;
			}
		}
		if (f.Shadow != "")
		{
			IsShadow = true;
		}
		if (f.Acc == "true")
		{
			Accumulated = true;
		}
		if (f.Priority != "" && f.Priority != "0")
		{
			Priority = int.Parse(f.Priority);
		}
		if (f.AddCost != "")
		{
			SubCost = new AdditionalCost();
			SubCost.resource = GameContext.GetResource(f.AddCost);
			SubCost.cost = int.Parse(f.AddCostValue);
			SubCost.name = f.AddCostName;
			SubCost.isBuilding = f.AddCost.Contains("Building.");
		}
		LimitedCharges = string.IsNullOrEmpty(f.Limited);
		CursedChallenge = string.IsNullOrEmpty(f.Curse);
		if (TypeBehavior == SpellTypeBehavior.Permanent)
		{
			GameManager.Instance.SpellBook.PermanentSpellList.Add(this);
		}
		useCounter = new CounterAverage();
		useThisRunCounter = new CounterAverage();
	}

	public void Restart(bool full = false)
	{
		if (active)
		{
			Delete();
		}
		chargeCount = 0uL;
		chargeProgress = 0f;
		ResetUsesAll(full);
	}

	public void ChangeRealm()
	{
		if (active)
		{
			Delete();
		}
		chargeCount = 0uL;
		chargeProgress = 0f;
		ResetUsesAll(full: true);
	}

	public void ResetCounters()
	{
		useCounter.Reset();
		useThisRunCounter.Reset();
	}

	public void IncreaseUses(double casts)
	{
		Use.ChangeValue(casts, 1.0);
		useCounter.Add(casts);
		if (Type == SpellTypeGroup.Evocation)
		{
			GameManager.Instance.Scrolls.EvoCastCount.Change(casts);
		}
	}

	public void IncreaseUseThisRun(double casts)
	{
		UseThisRun.Change(casts);
		useThisRunCounter.Add(casts);
	}

	public void ResetShards()
	{
		if (active && !IsAugment)
		{
			Delete();
		}
		chargeProgress = 0f;
		chargeCount = 0uL;
	}

	public void Spend(int casts = 1)
	{
		chargeCount -= (ulong)casts;
		if (SubCost != null)
		{
			int num = SubCost.Cost * casts;
			if (SubCost.isBuilding)
			{
				Statistic.TotalBuildings.Change(-num);
			}
			SubCost.resource.Change(-num, 1.0);
		}
	}

	public void SetEfficiency()
	{
		Variable efficiency = null;
		if (Type == SpellTypeGroup.Incantation)
		{
			efficiency = GameManager.Instance.Scrolls.IncantationEfficiency;
		}
		else if (Type == SpellTypeGroup.Evocation)
		{
			efficiency = GameManager.Instance.Scrolls.EvocationEfficiency;
		}
		else if (Type == SpellTypeGroup.Summoning)
		{
			efficiency = GameManager.Instance.Scrolls.SummoningEfficiency;
		}
		foreach (IEffect effect in effects)
		{
			effect.SetEfficiency(efficiency);
		}
	}

	public void SetGilding(Variable eff)
	{
		foreach (IEffect effect in effects)
		{
			effect.SetGilding(eff);
		}
	}

	public void SetAdditioanlEffect(AdditionalSpellEffect effect)
	{
		if (addSpellEffect != null && addSpellEffect.effect.IsActive)
		{
			addSpellEffect.effect.Delete();
		}
		addSpellEffect = effect;
	}

	public void SetCostGilding(VariableFloat eff)
	{
		GildingCost = eff;
	}

	public virtual void Apply()
	{
		foreach (IEffect effect in effects)
		{
			effect.Apply();
		}
		addSpellEffect?.effect.Apply();
		active = true;
	}

	public virtual void Delete()
	{
		if (!active)
		{
			return;
		}
		foreach (IEffect effect in effects)
		{
			effect.Delete();
		}
		addSpellEffect?.effect.Delete();
		active = false;
	}

	public virtual void Update()
	{
		if (!active)
		{
			return;
		}
		foreach (IEffect effect in effects)
		{
			effect.Update();
		}
		addSpellEffect?.effect.Update();
	}

	public virtual void ForceUpdate()
	{
		if (active)
		{
			Delete();
			Apply();
		}
	}

	public void ApplyProgressBuild(double dBuild)
	{
		if (LimitedCharges && chargeCount >= (ulong)MaxChargeCount)
		{
			chargeProgress = 0f;
			return;
		}
		chargeProgress += (float)(dBuild / FullBuild);
		CheckProgress();
	}

	public void AddProgressBuild(double dBuild, bool shards)
	{
		if (!IsMax && !IsNotCharging)
		{
			if (!shards)
			{
				dBuild *= (double)GameManager.Instance.Scrolls.SpellChargingSpeed.Value.ToFloat();
			}
			ApplyProgressBuild(dBuild);
			if (shards)
			{
				GameManager.Instance.Scrolls.shardsInSec += (BigNumber)dBuild;
			}
		}
	}

	public string GetShardsDesc()
	{
		if (ProgressBuild > 10000.0)
		{
			return new BigNumber(ProgressBuild).ToReadableString();
		}
		return ProgressBuild.ToString("F0");
	}

	public string GetShardCostDesc()
	{
		if (FullBuild > 10000.0)
		{
			return new BigNumber(FullBuild).ToReadableString();
		}
		return FullBuild.ToString("F0");
	}

	public void CheckProgress()
	{
		chargeCount += (ulong)Mathf.FloorToInt(chargeProgress);
		if (!LimitedCharges && chargeCount > 1000000000)
		{
			chargeCount = 1000000000uL;
			chargeProgress = 0f;
			return;
		}
		chargeProgress %= 1f;
		if (LimitedCharges && chargeCount >= (ulong)MaxChargeCount)
		{
			chargeCount = (ulong)MaxChargeCount;
			chargeProgress = 0f;
		}
	}

	public void Load(float progress)
	{
		chargeCount = (ulong)Mathf.FloorToInt(progress);
		chargeProgress = progress - (float)chargeCount;
	}

	public double TakeShards(float advice)
	{
		double num = (double)(chargeProgress + (float)chargeCount) * FullBuild;
		float num2 = (float)chargeCount + chargeProgress - (float)((double)advice / FullBuild);
		if ((double)advice > num)
		{
			chargeProgress = 0f;
			chargeCount = 0uL;
			return num;
		}
		chargeCount = (ulong)Mathf.FloorToInt(num2);
		chargeProgress = num2 - (float)chargeCount;
		return advice;
	}

	public override string GetDescription()
	{
		string text = base.GetDescription();
		if (addSpellEffect != null)
		{
			text = text + "\n" + addSpellEffect.GetDescription();
		}
		if (IsShadow)
		{
			text = text + "\n" + "ShadowSpellDescription".Translate();
		}
		if (IsWorkOffline)
		{
			text = text + "\n" + "SpellOffline".Translate();
		}
		return text;
	}

	protected override string GetPreview(int id, string key)
	{
		return effects[id - 1].Preview(key);
	}

	protected override string ReplaceMatch(Match m)
	{
		string text = "";
		if (m.Length > 2)
		{
			text = m.ToString()[2].ToString();
		}
		if (text == "p")
		{
			if (Settings.ColoredTips)
			{
				return "<color=#e2b018>" + ((PassiveEffect == null) ? 0.ToString() : PassiveEffect.Preview()) + "</color>";
			}
			if (PassiveEffect != null)
			{
				return PassiveEffect.Preview();
			}
			return 0.ToString();
		}
		return base.ReplaceMatch(m);
	}

	public BigNumber get_ingame_duration()
	{
		BigNumber bigNumber = get_duration();
		if (bigNumber < 1.0 && SpellTypeGroup.None != Type)
		{
			bigNumber = 1.0;
		}
		return bigNumber;
	}

	public double get_duration()
	{
		BigNumber bigNumber = Duration.Value;
		if (bigNumber == 0.0)
		{
			return 0.0;
		}
		if (Type == SpellTypeGroup.Incantation)
		{
			bigNumber = GameManager.Instance.Scrolls.IncantationDuration.ApplyModOnVar(bigNumber) / GameManager.Instance.Scrolls.IncantationDurationReduction.Value;
		}
		else if (Type == SpellTypeGroup.Summoning)
		{
			bigNumber /= GameManager.Instance.Scrolls.SummoningDurationReduction.Value;
		}
		else if (Type == SpellTypeGroup.Evocation)
		{
			bigNumber /= GameManager.Instance.Scrolls.EvocationDurationReduction.Value;
		}
		if (bigNumber > 86400.0)
		{
			bigNumber = 86400.0;
		}
		return bigNumber.ToDouble();
	}

	public int GetAdditionalCastRate()
	{
		if (AdditionalCastRate == null)
		{
			return 0;
		}
		return AdditionalCastRate.ValueInt;
	}

	public void ApplyPassive()
	{
		if (PassiveEffect != null)
		{
			PassiveEffect.Apply();
		}
	}

	public void RemovePassive()
	{
		if (PassiveEffect != null)
		{
			PassiveEffect.Delete();
		}
	}

	public void UpdatePassive()
	{
		if (PassiveEffect != null)
		{
			PassiveEffect.Update();
		}
	}

	public string get_color_by_type()
	{
		return Type switch
		{
			SpellTypeGroup.Evocation => "<sprite=3> ", 
			SpellTypeGroup.Incantation => "<sprite=4> ", 
			SpellTypeGroup.Summoning => "<sprite=5> ", 
			_ => string.Empty, 
		};
	}

	public void ResetUsesAll(bool full = false)
	{
		if (Use.ValueFloat != 0f || UseThisRun.Value.ToFloat() != 0f)
		{
			if (full || ResetUses)
			{
				Use.SetValue(0.0);
			}
			UseThisRun.SetValue(0.0);
			ResetCounters();
		}
	}

	public string GetNameString(bool showEnh = true)
	{
		string text = Name.Translate();
		if (IsEnhancement)
		{
			text += "<sprite=12>";
			if (showEnh)
			{
				text = text + " (" + "Enhanced".Translate() + ")";
			}
		}
		if (GameManager.Instance.Paragon.GildingSpellsIsAvailable)
		{
			SpellUpgrade spellUpgrade = GameManager.Instance.Gilding.Spells.Get(NameKey);
			if (spellUpgrade != null && spellUpgrade.Level > 0)
			{
				text = text + " +" + spellUpgrade.GetLevel();
			}
		}
		return text;
	}

	public string GetTooltipDescription(float activeTime = 0f)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Type".Translate());
		stringBuilder.Append(": ");
		if (Type == SpellTypeGroup.None)
		{
			stringBuilder.Append(" - ");
		}
		else
		{
			stringBuilder.Append(get_color_by_type());
			stringBuilder.Append(Type.ToString().Translate());
		}
		if (IsPersistent)
		{
			stringBuilder.Append(" (");
			stringBuilder.Append("Persistent".Translate());
			stringBuilder.Append(")");
		}
		if (IsAccumulated)
		{
			stringBuilder.Append(" (");
			stringBuilder.Append("Accumulated".Translate());
			stringBuilder.Append(")");
		}
		if (IsAugment)
		{
			stringBuilder.Append(" (");
			stringBuilder.Append("Augment".Translate());
			stringBuilder.Append(")");
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		if (ShardsBuilding)
		{
			stringBuilder.Append("Shards".Translate());
			stringBuilder.Append(": ");
			stringBuilder.Append(GetShardsDesc());
			stringBuilder.Append(" / ");
			stringBuilder.AppendLine(GetShardCostDesc());
		}
		else if (IsShadow)
		{
			stringBuilder.Append("Shadows".Translate());
			stringBuilder.Append(": ");
			stringBuilder.Append(GetShardsDesc());
			stringBuilder.Append(" / ");
			stringBuilder.AppendLine(GetShardCostDesc());
		}
		else
		{
			stringBuilder.Append("Charging".Translate());
			stringBuilder.Append(": ");
			stringBuilder.Append((100.0 * new BigNumber(GetProgressBuild) / FullBuild).ToReadableString());
			stringBuilder.AppendLine("%");
		}
		if (SubCost != null)
		{
			stringBuilder.Append(SubCost.name.Translate());
			stringBuilder.Append(": ");
			stringBuilder.Append(SubCost.resource.Value.ToReadableString("F0"));
			stringBuilder.Append(" / ");
			stringBuilder.AppendLine(SubCost.Cost.ToString());
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(GetDescription());
		double duration = get_duration();
		stringBuilder.AppendLine();
		stringBuilder.Append("Duration".Translate());
		stringBuilder.Append(": ");
		if (duration > 0.0)
		{
			if (active && activeTime > 0f)
			{
				double num = duration - (double)activeTime;
				if (num < 0.0)
				{
					num = 0.0;
				}
				if (num >= 1.0)
				{
					stringBuilder.Append(Statistic.time_to_string(num, full: true));
				}
				else
				{
					stringBuilder.Append(num.ToString("F2", CultureInfo.InvariantCulture));
					stringBuilder.Append(" ");
					stringBuilder.Append("sec".Translate());
				}
				stringBuilder.Append(" / ");
			}
			if (duration >= 1.0)
			{
				stringBuilder.Append(Statistic.time_to_string(duration, full: true));
			}
			else
			{
				stringBuilder.Append(duration.ToString("F2", CultureInfo.InvariantCulture));
				stringBuilder.Append(" ");
				stringBuilder.Append("sec".Translate());
			}
		}
		else
		{
			stringBuilder.Append("Instant".Translate());
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		if (IsPersistent)
		{
			stringBuilder.Append("Cast in this Exile".Translate());
			stringBuilder.Append(": ");
		}
		else
		{
			stringBuilder.Append("Real casts".Translate());
			stringBuilder.Append(": ");
		}
		stringBuilder.Append(UseThisRun.Value.ToReadableString("F0"));
		stringBuilder.Append(" (");
		stringBuilder.Append(useThisRunCounter.GetDescription());
		stringBuilder.Append(")");
		stringBuilder.AppendLine();
		stringBuilder.Append("Total casts".Translate());
		stringBuilder.Append(": ");
		stringBuilder.Append(Use.Value.ToReadableString("F0"));
		stringBuilder.Append(" (");
		stringBuilder.Append(useCounter.GetDescription());
		stringBuilder.Append(")");
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}
}
