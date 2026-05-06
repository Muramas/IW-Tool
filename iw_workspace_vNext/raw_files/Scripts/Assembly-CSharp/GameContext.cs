using System;
using System.Collections.Generic;
using CardGame;

public static class GameContext
{
	public const string VARIABLE_COLOR = "<color=#e2b018>";

	public const string COLOR_CLOSE = "</color>";

	private static Dictionary<string, Variable> resource;

	private static Dictionary<string, Effect> effect;

	private static Dictionary<string, Condition> condition;

	public static Action OnChange;

	public static Variable GetResource(string id)
	{
		Variable value = null;
		resource.TryGetValue(id, out value);
		return value;
	}

	public static Effect GetEffect(string id)
	{
		Effect value = null;
		effect.TryGetValue(id, out value);
		return value;
	}

	public static Condition GetCondition(string id)
	{
		Condition value = null;
		condition.TryGetValue(id, out value);
		return value;
	}

	public static void InitContext()
	{
		resource = new Dictionary<string, Variable>();
		effect = new Dictionary<string, Effect>();
		condition = new Dictionary<string, Condition>();
	}

	public static void GenerateEffects()
	{
		effect = new Dictionary<string, Effect>();
		effect.Add(EffectNames.Linear.ToString(), EffectFactory.Create(EffectFactory.Linear, EffectFactory.LinearDelete, EffectFactory.LinearPreview));
		effect.Add(EffectNames.Pow.ToString(), EffectFactory.Create(EffectFactory.Power, EffectFactory.PowerDelete, EffectFactory.PowerPreview));
		effect.Add(EffectNames.PowA.ToString(), EffectFactory.Create(EffectFactory.PowerA, EffectFactory.PowerADelete, EffectFactory.PowerAPreview));
		effect.Add(EffectNames.Log10.ToString(), EffectFactory.Create(EffectFactory.Log10, EffectFactory.Log10Delete, EffectFactory.Log10Preview));
		effect.Add(EffectNames.Ln.ToString(), EffectFactory.Create(EffectFactory.Ln, EffectFactory.LnDelete, EffectFactory.LnPreview));
		effect.Add(EffectNames.LogA.ToString(), EffectFactory.Create(EffectFactory.LogA, EffectFactory.LogADelete, EffectFactory.LogAPreview));
		effect.Add(EffectNames.PowIntW.ToString(), EffectFactory.Create(EffectFactory.PowerIntW, EffectFactory.PowerIntWDelete, EffectFactory.PowerIntWPreview));
		effect.Add(EffectNames.PowW.ToString(), EffectFactory.Create(EffectFactory.PowerW, EffectFactory.PowerWDelete, EffectFactory.PowerWPreview));
		effect.Add(EffectNames.AdditivePow.ToString(), EffectFactory.Create(EffectFactory.AdditivePower, EffectFactory.AdditivePowerDelete, EffectFactory.AdditivePowerPreview));
	}

	public static void GenerateContext()
	{
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.Mana, GameManager.Instance.Mana);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.AllBuildingsProfit, GameManager.Instance.Profit);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.PPS, GameManager.Instance.PPS);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.Souls, Reborn.Souls);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.SoulPower, Reborn.SoulPower);
		resource.Add(ResourceType.Base.ToString() + ".SoulPowerFactor", Reborn.SoulPowerFactor);
		resource.Add(ResourceType.Base.ToString() + ".SoulPowerTotal", Reborn.SoulPowerTotal);
		resource.Add(ResourceType.Base.ToString() + ".StartingSouls", Reborn.StartingSouls);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.Ascends, Statistic.Ascends);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.IdleTime, GameManager.Instance.Idle.TimeToIdle);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.IdleBonus, GameManager.Instance.Idle.IdleBonus);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.IdleOfflineBonus, GameManager.Instance.Idle.IdleAndOfflineBonus);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.IdleSaveClicks, GameManager.Instance.Idle.SaveClicks);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.IdleSaveClicksCD, GameManager.Instance.Idle.SaveClickCD);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.ManaTotal, Statistic.ManaSession);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.ManaAllTime, Statistic.ManaAllTime);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.OfflineProduction, GameManager.Instance.OfflineProduction);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.TotalBuildings, Statistic.TotalBuildings);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.TotalUpgrades, Statistic.BoughtUpgrades);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.PlayedTimeTotal, Statistic.TimeTotal);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.PlayedTimeRealm, Statistic.TimeRealm);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.PlayedTime, Statistic.TimeSession);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.IdleTimeSession, Statistic.TimeIdleSession);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.IdleTimeTotal, Statistic.TimeIdleTotal);
		resource.Add(ResourceType.Base.ToString() + "." + ResourceBase.OfflineTimeTotal, Statistic.TimeOfflineTotal);
		resource.Add(ResourceType.Base.ToString() + ".Bats", Statistic.Collectables);
		resource.Add(ResourceType.Base.ToString() + ".BatsRealm", Statistic.CollectablesRealm);
		resource.Add(ResourceType.Base.ToString() + ".BatsExile", Statistic.CollectablessExile);
		resource.Add(ResourceType.Base.ToString() + ".BatsOnly", Statistic.BatsExile);
		resource.Add(ResourceType.Base.ToString() + ".LevelReq", GameManager.Instance.LevelReduction);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.Value, GameManager.Instance.VoidMana);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.Power, GameManager.Instance.VoidManaManager.Power);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.Decrease, GameManager.Instance.VoidManaManager.Decrease);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.LifeTime, GameManager.Instance.VoidManaManager.VoidCore.BonusLifeTime);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.SpawnPeriod, GameManager.Instance.VoidManaManager.VoidCore.TimeInterval);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.SpawnSpeed, GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.Bonus, GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.Total, Statistic.VoidManaSession);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.TotalAllTime, Statistic.VoidManaAllTime);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.Collect, Statistic.ClickableCollect);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.CollectRealm, Statistic.ClickableCollectRealm);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.CollectTotal, Statistic.ClickableCollectTotal);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.BuffChance, GameManager.Instance.VoidManaManager.VoidCore.BuffOnCollect);
		resource.Add(ResourceType.VoidMana.ToString() + "." + ResourceVoidMana.DecreaseSpawnOnCollect, GameManager.Instance.VoidManaManager.VoidCore.DecreaseSpawnOnCollect);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.Profit, GameManager.Instance.Orb.click_profit);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.AutoClickProfit, GameManager.Instance.Orb.autoclick_profit);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.CritChance, GameManager.Instance.Orb.crit_chance);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.CritProfit, GameManager.Instance.Orb.crit_profit);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.PercentPPS, GameManager.Instance.Orb.percent_pps);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.Total, Statistic.Clicks);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.TotalAllTime, Statistic.ClicksTotal);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.AutoTotal, Statistic.AutoClicks);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.AutoTotalRealm, Statistic.AutoClicksRealm);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.AutoTotalAllTime, Statistic.AutoClicksTotal);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.CritRating, GameManager.Instance.Orb.critRating.crit_rating);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.Auto, GameManager.Instance.Orb.autoclicks);
		resource.Add(ResourceType.Click.ToString() + "." + ResourceClick.AutoClicksFromSpell, GameManager.Instance.Orb.autoclicksFromSpell);
		resource.Add(ResourceType.Pet.ToString() + "." + ResourcePet.MaxLevel, Statistic.PetMaxLevel);
		resource.Add(ResourceType.Pet.ToString() + "." + ResourcePet.MaxLevelAllTime, Statistic.PetMaxLevelAllTime);
		condition.Add(ConditionNames.More.ToString(), ConditionFactory.Create(ConditionFactory.More));
		condition.Add(ConditionNames.Less.ToString(), ConditionFactory.Create(ConditionFactory.Less));
		condition.Add(ConditionNames.Max.ToString(), ConditionFactory.Create(ConditionFactory.Max));
		condition.Add(ConditionNames.Min.ToString(), ConditionFactory.Create(ConditionFactory.Min));
		condition.Add(ConditionNames.PetLevel.ToString(), ConditionFactory.Create(ConditionFactory.PetLevel));
		condition.Add(ConditionNames.HeroLevel.ToString(), ConditionFactory.Create(ConditionFactory.HeroLevel));
		condition.Add(ConditionNames.SpellUse.ToString(), ConditionFactory.Create(ConditionFactory.SpellUse));
		condition.Add(ConditionNames.Achieve.ToString(), ConditionFactory.Create(ConditionFactory.Achieve));
		condition.Add(ConditionNames.GodLevel.ToString(), ConditionFactory.Create(ConditionFactory.GodLevel));
		condition.Add(ConditionNames.AscensionLevel.ToString(), ConditionFactory.Create(ConditionFactory.AscentionLevel));
		GenerateEffects();
		foreach (object value in Enum.GetValues(typeof(HeroesNames)))
		{
			_ = value;
			Statistic.ClassTime.Add(new VariableLong(0uL));
		}
		ContextAddTime(Statistic.ClassTime);
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public static void ContextAddBuilding(Building building)
	{
		resource.Add(ResourceType.Building.ToString() + "." + building.Tier + "." + ResourceBuild.Profit, building.pps_per_building);
		resource.Add(ResourceType.Building.ToString() + "." + building.Tier + "." + ResourceBuild.Cost, building.base_cost);
		resource.Add(ResourceType.Building.ToString() + "." + building.Tier + "." + ResourceBuild.Level, building.Level);
		resource.Add(ResourceType.Building.ToString() + "." + building.Tier + "." + ResourceBuild.TotalLevel, building.TotalLevel);
		resource.Add(ResourceType.Building.ToString() + "." + building.Tier + "." + ResourceBuild.CostGrowth, building.cost_growth);
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public static void ContextAddTime(List<VariableLong> time)
	{
		string text = "Time.";
		resource.Add(text + HeroesNames.Apprentice, Statistic.ClassTime[0]);
		resource.Add(text + HeroesNames.Druid, Statistic.ClassTime[2]);
		resource.Add(text + HeroesNames.Demonologist, Statistic.ClassTime[1]);
		resource.Add(text + HeroesNames.Necromancer, Statistic.ClassTime[5]);
		resource.Add(text + HeroesNames.Arcanist, Statistic.ClassTime[4]);
		resource.Add(text + HeroesNames.Prodigy, Statistic.ClassTime[3]);
		resource.Add(text + HeroesNames.Voidmancer, Statistic.ClassTime[7]);
		resource.Add(text + HeroesNames.Exorcist, Statistic.ClassTime[6]);
		resource.Add(text + HeroesNames.Chronomancer, Statistic.ClassTime[8]);
		resource.Add(text + HeroesNames.Umbramancer, Statistic.ClassTime[9]);
		resource.Add(text + HeroesNames.Alchemist, Statistic.ClassTime[10]);
		resource.Add(text + HeroesNames.Ironsoul, Statistic.ClassTime[11]);
		resource.Add(text + HeroesNames.Abolisher, Statistic.ClassTime[12]);
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public static void AddPromoResources()
	{
		resource.Add(ResourcesGame.Relic.ToString(), GameManager.Instance.Relics.Relic);
		resource.Add(ResourcesGame.AlterationSand.ToString(), GameManager.Instance.AlterationSand);
		resource.Add(ResourcesGame.Nullifier.ToString(), GameManager.Instance.Nullifier);
		resource.Add(ResourcesGame.RedDust.ToString(), GameManager.Instance.Craft.Red);
		resource.Add(ResourcesGame.GreenDust.ToString(), GameManager.Instance.Craft.Green);
		resource.Add(ResourcesGame.BlueDust.ToString(), GameManager.Instance.Craft.Blue);
		resource.Add(ResourcesGame.YellowDust.ToString(), GameManager.Instance.Craft.Yellow);
		resource.Add(ResourcesGame.EnchantingDust.ToString(), GameManager.Instance.Craft.EnchantingDust);
		resource.Add(ResourcesGame.TrialRune.ToString(), GameManager.Instance.Trials.Keys);
		resource.Add(ResourcesGame.GCat.ToString(), GameManager.Instance.BuildingManager.FreeGreenCatalysts);
		resource.Add(ResourcesGame.BCat.ToString(), GameManager.Instance.BuildingManager.FreeBlueCatalysts);
		resource.Add(ResourcesGame.RCat.ToString(), GameManager.Instance.BuildingManager.FreeRedCatalysts);
		resource.Add(ResourcesGame.Gilding.ToString(), GameManager.Instance.Gilding.Resource);
		resource.Add(ResourcesGame.KeyShattered.ToString(), ExpeditionManager.Instance.Keys.keyMap[Keys.Shattered.ToString()]);
		resource.Add(ResourcesGame.KeyChaos.ToString(), ExpeditionManager.Instance.Keys.keyMap[Keys.Chaos.ToString()]);
		resource.Add(ResourcesGame.KeyCathedral.ToString(), ExpeditionManager.Instance.Keys.keyMap[Keys.Cathedral.ToString()]);
		resource.Add(ResourcesGame.KeyAltar.ToString(), ExpeditionManager.Instance.Keys.keyMap[Keys.Altar.ToString()]);
		resource.Add(ResourcesGame.KeyFragment.ToString(), ExpeditionManager.Instance.Keys.keyMap[Keys.Fragment.ToString()]);
	}

	public static void AddSpellContext()
	{
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.SpellCast, Statistic.CastSpell);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.SpellCastRealm, Statistic.CastSpellRealm);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.SpellCastTotal, Statistic.CastSpellTotal);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.Shards, Statistic.ShardsSession);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.ShardsRealm, Statistic.ShardsRealm);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.ShardsTotal, Statistic.ShardsTotal);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.ScrollCount, GameManager.Instance.Scrolls.ScrollCount);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.MaxCharge, GameManager.Instance.Scrolls.MaxCharge);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.BaseProgress, GameManager.Instance.Scrolls.ShardsPassive);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.ClickProgress, GameManager.Instance.Scrolls.ShardsPerClick);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.PoolProgress, GameManager.Instance.Scrolls.ShardsPool.Efficiency);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.PoolCapacity, GameManager.Instance.Scrolls.ShardsPool.Capacity);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.AutoCastCount, GameManager.Instance.Scrolls.AutoCastCount);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.EvocationEfficiency, GameManager.Instance.Scrolls.EvocationEfficiency);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.EvocationDuration, GameManager.Instance.Scrolls.EvocationDurationReduction);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.IncantationEfficiency, GameManager.Instance.Scrolls.IncantationEfficiency);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.IncantationDuration, GameManager.Instance.Scrolls.IncantationDuration);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.IncantationReduction, GameManager.Instance.Scrolls.IncantationDurationReduction);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.SummoningEfficiency, GameManager.Instance.Scrolls.SummoningEfficiency);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.SummoningDuration, GameManager.Instance.Scrolls.SummoningDurationReduction);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.PersistentGain, GameManager.Instance.Scrolls.PersistentGain);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.PersistentMult, GameManager.Instance.Scrolls.PersistentMult);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.PersistentActiveMult, GameManager.Instance.Scrolls.PersistentActiveMult);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.PersistentSave, GameManager.Instance.Scrolls.PersistentSave);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.AccumutaledCasts, GameManager.Instance.Scrolls.AccumulatedCasts);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.AccumMult, GameManager.Instance.Scrolls.AccumMult);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.AccumCastCount, GameManager.Instance.Scrolls.AccumCastCount);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.EvoCastCount, GameManager.Instance.Scrolls.EvoCastCount);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.AugmentMult, GameManager.Instance.Scrolls.AugmentMult);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.CastRate, GameManager.Instance.Scrolls.CastRate);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.CostReduction, GameManager.Instance.Scrolls.SpellShardsCostReduction);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.NonShardCostReduction, GameManager.Instance.Scrolls.SpellChargingCostReduction);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.SubcostReduction, GameManager.Instance.Scrolls.SpellSubcostReduction);
		resource.Add(ResourceType.Spell.ToString() + "." + ResourceSpell.NonShardChargeSpeed, GameManager.Instance.Scrolls.SpellChargingSpeed);
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public static void AddItems()
	{
		resource.Add(ResourceType.Items.ToString() + "." + ItemKeys.UnlockedTotal, Statistic.UnlockedItems);
		resource.Add(ResourceType.Items.ToString() + "." + ItemKeys.Gathered, Statistic.ResourcesCollected);
		resource.Add(ResourceType.Items.ToString() + "." + ItemKeys.EDust, Statistic.EnchantingDustExile);
		resource.Add(ResourceType.Items.ToString() + ".EDustRealm", Statistic.ResourcesRealm[4]);
		resource.Add(ResourceType.Items.ToString() + ".EDustTotal", Statistic.ResourcesTotal[4]);
		resource.Add(ResourceType.Items.ToString() + "." + ItemKeys.GatherTotal, Statistic.ResourcesCollectedTotal);
		resource.Add(ResourceType.Items.ToString() + "." + ItemKeys.GatherRealm, Statistic.ResourcesCollectedRealm);
		resource.Add(ResourceType.Items.ToString() + ".Common", Statistic.UnlockedByTiers[0]);
		resource.Add(ResourceType.Items.ToString() + ".Uncommon", Statistic.UnlockedByTiers[1]);
		resource.Add(ResourceType.Items.ToString() + ".Rare", Statistic.UnlockedByTiers[2]);
		resource.Add(ResourceType.Items.ToString() + ".Epic", Statistic.UnlockedByTiers[3]);
		resource.Add(ResourceType.Items.ToString() + ".Legendary", Statistic.UnlockedByTiers[4]);
		resource.Add(ResourceType.Items.ToString() + ".Mythic", Statistic.UnlockedByTiers[6]);
		if (OnChange != null)
		{
			OnChange();
		}
	}

	public static void ContextAddResource(string key, Variable variable)
	{
		resource.Add(key, variable);
		if (OnChange != null)
		{
			OnChange();
		}
	}
}
