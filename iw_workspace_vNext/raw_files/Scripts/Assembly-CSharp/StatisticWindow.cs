using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class StatisticWindow : Window
{
	public GameObject base_window;

	public TextMeshProUGUI base_label_1;

	public TextMeshProUGUI base_label_2;

	public GameObject hero_pet;

	public PortraitFrame Portrait;

	public PetPortraitFrame PetPortrait;

	public TextMeshProUGUI hero_label_1;

	public TextMeshProUGUI hero_label_2;

	public TextMeshProUGUI pet_label_1;

	public TextMeshProUGUI pet_label_2;

	public GameObject total;

	public TextMeshProUGUI total_label;

	public TextMeshProUGUI total_run_label;

	public TextMeshProUGUI total_run_bottom_label;

	public TextMeshProUGUI total_realm_label;

	public TextMeshProUGUI total_all_label;

	public TextMeshProUGUI total_add_label;

	public TextMeshProUGUI total_add_values;

	public GameObject classes;

	public TextMeshProUGUI HC;

	public TextMeshProUGUI CT;

	public TextMeshProUGUI LS;

	public List<ClassPlayedTimeStats> cpts;

	public Switcher Base;

	public Switcher Hero;

	public Switcher Total;

	public Switcher Classes;

	public DisableCanvas canvas;

	private Action current_update;

	private float updateAccumulator;

	public override void Open()
	{
		canvas.On();
		OpenBase();
		base.gameObject.SetActive(value: true);
		updateAccumulator = 0f;
	}

	public override void Close()
	{
		canvas.Off();
		base.gameObject.SetActive(value: false);
		current_update = null;
	}

	protected override void Update()
	{
		base.Update();
		if (current_update != null)
		{
			updateAccumulator += Time.unscaledDeltaTime;
			if (updateAccumulator >= 1f)
			{
				current_update();
				updateAccumulator = 0f;
			}
		}
	}

	public void OpenBase()
	{
		update_base_page();
		current_update = update_base_page;
		base_window.SetActive(value: true);
		hero_pet.SetActive(value: false);
		total.SetActive(value: false);
		classes.SetActive(value: false);
		Base.On();
		Hero.Off();
		Total.Off();
		Classes.Off();
	}

	public void OpenHeroPet()
	{
		Portrait.UpdateFrame(GameManager.Instance.CurrentHero.Portrait.Portrait);
		if (GameManager.Instance.CurrentPet.Pet != null)
		{
			PetPortrait.UpdateFrame(GameManager.Instance.CurrentPet.Portrait.Portrait);
		}
		else
		{
			PetPortrait.gameObject.SetActive(value: false);
		}
		hero_update();
		pet_update();
		current_update = delegate
		{
			hero_update();
			pet_update();
		};
		base_window.SetActive(value: false);
		hero_pet.SetActive(value: true);
		total.SetActive(value: false);
		classes.SetActive(value: false);
		Base.Off();
		Hero.On();
		Total.Off();
		Classes.Off();
	}

	public void OpenTotal()
	{
		total_update();
		current_update = total_update;
		base_window.SetActive(value: false);
		hero_pet.SetActive(value: false);
		total.SetActive(value: true);
		classes.SetActive(value: false);
		Base.Off();
		Hero.Off();
		Total.On();
		Classes.Off();
	}

	public void OpenClasses()
	{
		classes_update();
		current_update = classes_update;
		base_window.SetActive(value: false);
		hero_pet.SetActive(value: false);
		total.SetActive(value: false);
		classes.SetActive(value: true);
		Base.Off();
		Hero.Off();
		Total.Off();
		Classes.On();
	}

	private void update_base_page()
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		stringBuilder.AppendLine("Mana".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.Mana.Value.ToReadableString());
		stringBuilder.AppendLine("ManaPerSec".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.PPS.Value.ToReadableString());
		stringBuilder.AppendLine("Bought mana sources".Translate());
		stringBuilder2.AppendLine(Statistic.TotalBuildings.ValueInt.ToString());
		stringBuilder.AppendLine("Bought upgrades".Translate());
		stringBuilder2.AppendLine(Statistic.BoughtUpgrades.ValueInt.ToString());
		stringBuilder.AppendLine("Mysteries".Translate());
		stringBuilder2.AppendLine(Reborn.Souls.Value.ToReadableString());
		stringBuilder.AppendLine("MystM".Translate());
		stringBuilder2.Append(((Reborn.SoulPower.add + Reborn.SoulPower.GetInternalValue) * 100.0).ToReadableString());
		stringBuilder2.Append("% (base) / ");
		stringBuilder2.Append((Reborn.Instance.GetSoulPower() * 100.0).ToReadableString("F2", negativeExp: true));
		stringBuilder2.AppendLine("% (total)");
		stringBuilder.AppendLine("Idle".Translate());
		stringBuilder2.Append(((GameManager.Instance.Idle.GetIdleBonus() - 1.0) * 100.0).ToReadableString());
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine("Offline".Translate());
		stringBuilder2.Append(((GameManager.Instance.OfflineProduction.Value - 1.0) * 100.0).ToReadableString());
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine();
		stringBuilder2.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder2.AppendLine();
		stringBuilder.AppendLine("Void Mana".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.VoidMana.Value.ToReadableString());
		stringBuilder.AppendLine("VPE".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity.Value.ToReadableString());
		stringBuilder.AppendLine("EntitySpawnrate".Translate());
		stringBuilder2.Append((GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed.Value * 100.0).ToReadableString());
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine("EntityLifetime".Translate());
		stringBuilder2.AppendLine(Statistic.time_to_string(GameManager.Instance.VoidManaManager.VoidCore.BonusLifeTime.Value));
		stringBuilder.AppendLine("VPm".Translate());
		stringBuilder2.Append((GameManager.Instance.VoidManaManager.Power.Value * 100.0).ToReadableString());
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine("VManaDegenRate".Translate());
		stringBuilder2.Append((GameManager.Instance.VoidManaManager.Decrease.ApplyModOnVar(1.0) * 100.0).ToReadableString());
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine("Max".Translate());
		stringBuilder2.AppendLine(Statistic.MaxVoidManaSession.Value.ToReadableString());
		stringBuilder.AppendLine();
		stringBuilder2.AppendLine();
		stringBuilder.AppendLine("ClickProfit".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.Orb.click_profit.Value.ToReadableString());
		stringBuilder.AppendLine("CritChance".Translate());
		stringBuilder2.Append(GameManager.Instance.Orb.GetCritChange.ToString("F2"));
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine("CritProfit".Translate());
		stringBuilder2.Append((GameManager.Instance.Orb.crit_profit.Value * 100.0).ToReadableString());
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine("AutoclickProfit".Translate());
		stringBuilder2.Append((GameManager.Instance.Orb.autoclick_profit.Value * 100.0).ToReadableString());
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine("PassiveShardGain".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.Scrolls.ShardsPassive.Value.ToReadableString());
		stringBuilder.AppendLine("ClickSpellShards".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.Scrolls.ShardsPerClick.Value.ToReadableString());
		stringBuilder.AppendLine("ShardPoolEff".Translate());
		stringBuilder2.Append((GameManager.Instance.Scrolls.ShardsPool.Efficiency.Value * 100.0).ToReadableString());
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine("ShardPoolCap".Translate());
		stringBuilder2.Append(GameManager.Instance.Scrolls.ShardsPool.Capacity.Value.ToReadableString("F0"));
		base_label_1.text = stringBuilder.ToString();
		base_label_2.text = stringBuilder2.ToString();
	}

	private void hero_update()
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		stringBuilder.Append("<b>").Append("Class".Translate()).AppendLine("</b>");
		stringBuilder2.Append("<b>");
		stringBuilder2.Append(GameManager.Instance.CurrentHero.Hero.Name);
		stringBuilder2.AppendLine("</b>");
		stringBuilder.AppendLine("Level".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.CurrentHero.Level.ValueInt.ToString());
		stringBuilder.AppendLine("XP".Translate());
		stringBuilder2.Append(GameManager.Instance.CurrentHero.Hero.CurrentExp.ToReadableString("F0"));
		stringBuilder2.Append(" / ");
		stringBuilder2.AppendLine(GameManager.Instance.CurrentHero.Hero.Exp2LevelUp.ToReadableString("F0"));
		stringBuilder.AppendLine("TotalXP".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.CurrentHero.Hero.experience.Value.ToReadableString("F0"));
		stringBuilder.AppendLine("AP".Translate());
		stringBuilder2.Append((GameManager.Instance.CurrentHero.AbilityPower.Value * 100.0).ToReadableString());
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine("Starting level".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.CurrentHero.StartingLevel.ValueInt.ToString());
		stringBuilder.AppendLine("XPActions".Translate());
		stringBuilder2.Append((GameManager.Instance.CurrentHero.ExpBoost.Value * 100.0).ToReadableString());
		stringBuilder2.AppendLine("%");
		if (GameManager.Instance.CurrentHero.ExpManaSources.Value > 1.0)
		{
			stringBuilder.AppendLine("XPSources".Translate());
			stringBuilder2.Append((GameManager.Instance.CurrentHero.ExpManaSources.Value * 100.0).ToReadableString());
			stringBuilder2.AppendLine("%");
		}
		stringBuilder.AppendLine("LevelReduction".Translate());
		stringBuilder2.Append("- ");
		stringBuilder2.AppendLine(GameManager.Instance.LevelReduction.ValueInt.ToString());
		stringBuilder.AppendLine("Real time".Translate());
		stringBuilder2.AppendLine(Statistic.time_to_string(GameManager.Instance.CurrentHero.PlayedTime.ValueInt));
		stringBuilder.AppendLine("Game time".Translate());
		stringBuilder2.AppendLine(Statistic.time_to_string(GameManager.Instance.CurrentHero.PlayedTime.ValueInt + GameManager.Instance.CurrentHero.SkipedPlayedTime.Value));
		hero_label_1.text = stringBuilder.ToString();
		hero_label_2.text = stringBuilder2.ToString();
	}

	private void pet_update()
	{
		if (GameManager.Instance.CurrentPet.Pet != null)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			stringBuilder.Append("<b>").Append("Pet".Translate()).AppendLine("</b>");
			stringBuilder2.Append("<b>");
			stringBuilder2.Append(GameManager.Instance.CurrentPet.Pet.Name);
			stringBuilder2.AppendLine("</b>");
			stringBuilder.AppendLine("Level".Translate());
			stringBuilder2.AppendLine(GameManager.Instance.CurrentPet.Level.ValueInt.ToString());
			stringBuilder.AppendLine("XP".Translate());
			stringBuilder2.Append(GameManager.Instance.CurrentPet.Pet.Experience.Value.ToReadableString("F0"));
			stringBuilder2.Append(" / ");
			stringBuilder2.AppendLine(GameManager.Instance.CurrentPet.Pet.Exp2LevelUp.ToReadableString("F0"));
			stringBuilder.AppendLine("TotalXP".Translate());
			stringBuilder2.AppendLine(GameManager.Instance.CurrentPet.Pet.TotalExp.Value.ToReadableString("F0"));
			stringBuilder.AppendLine("AP".Translate());
			if (GameManager.Instance.CurrentPet.Pet == null)
			{
				stringBuilder2.Append((GameManager.Instance.CurrentPet.AbilityPower.Value * 100.0).ToReadableString());
			}
			else
			{
				stringBuilder2.Append((GameManager.Instance.CurrentPet.Pet.GetAbilityPower() * 100.0).ToReadableString());
			}
			stringBuilder2.AppendLine("%");
			stringBuilder.AppendLine("AdditionalXP".Translate());
			stringBuilder2.AppendLine(GameManager.Instance.CurrentPet.ExpBonus.add.ToReadableString());
			stringBuilder.AppendLine("ExperienceBonus".Translate());
			stringBuilder2.Append(((GameManager.Instance.CurrentPet.ExpBonus.mult - 1.0) * 100.0).ToReadableString());
			stringBuilder2.AppendLine("%");
			stringBuilder.AppendLine("Real time".Translate());
			stringBuilder2.AppendLine(Statistic.time_to_string(GameManager.Instance.CurrentPet.PlayedTime.ValueInt));
			stringBuilder.AppendLine("Game time".Translate());
			stringBuilder2.AppendLine(Statistic.time_to_string(GameManager.Instance.CurrentPet.PlayedTime.ValueInt + GameManager.Instance.CurrentPet.SkipedPlayedTime.Value));
			PetPortrait.gameObject.SetActive(value: true);
			pet_label_1.text = stringBuilder.ToString();
			pet_label_2.text = stringBuilder2.ToString();
		}
		else
		{
			PetPortrait.gameObject.SetActive(value: false);
			pet_label_1.text = string.Empty;
			pet_label_2.text = string.Empty;
		}
	}

	private void total_update()
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		StringBuilder stringBuilder3 = new StringBuilder();
		StringBuilder stringBuilder4 = new StringBuilder();
		stringBuilder.AppendLine("Total Realms".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.Realm.Realms.ValueInt.ToString());
		stringBuilder.AppendLine("Collectables".Translate());
		stringBuilder2.AppendLine(Statistic.Collectables.ValueInt.ToString());
		stringBuilder.AppendLine("Challenges completed".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.ChallengeManager.CompletedChallenges.ValueInt.ToString());
		stringBuilder.AppendLine("Achievements unlocked".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.AchievManager.AchievUnlocked.ValueInt.ToString());
		stringBuilder.AppendLine("achievement points".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.AchievManager.AchievsPoints.ValueInt.ToString());
		stringBuilder.AppendLine("Character max level".Translate());
		stringBuilder2.AppendLine(GameManager.Instance.CurrentHero.Level.ValueInt.ToString("F0") + " / " + Statistic.HeroMaxLevelAllTime.ValueInt.ToString("F0"));
		stringBuilder.AppendLine("Pet max level".Translate());
		stringBuilder2.AppendLine(Statistic.PetMaxLevel.ValueInt.ToString("F0") + " / " + Statistic.PetMaxLevelAllTime.ValueInt.ToString("F0"));
		stringBuilder.AppendLine("Runes' generation rate".Translate());
		stringBuilder2.Append((GameManager.Instance.Trials.TimeReduction.Value * 100.0).ToReadableString("F0"));
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine("Catalyst income".Translate());
		stringBuilder2.Append((GameManager.Instance.BuildingManager.AllIncome.Value * 100.0).ToReadableString("F0"));
		stringBuilder2.AppendLine("%");
		stringBuilder.AppendLine();
		stringBuilder2.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder2.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		total_run_label.text = stringBuilder2.ToString();
		stringBuilder2 = new StringBuilder();
		stringBuilder2.Append("<b>").Append("Run".Translate()).AppendLine("</b>");
		stringBuilder3.Append("<b>").Append("Realm".Translate()).AppendLine("</b>");
		stringBuilder4.Append("<b>").Append("Total".Translate()).AppendLine("</b>");
		stringBuilder.AppendLine("Mana".Translate());
		stringBuilder2.AppendLine(Statistic.ManaSession.Value.ToReadableString("F0"));
		stringBuilder3.AppendLine(Statistic.ManaRealm.Value.ToReadableString("F0"));
		stringBuilder4.AppendLine(Statistic.ManaAllTime.Value.ToReadableString("F0"));
		stringBuilder.AppendLine("Void Mana".Translate());
		stringBuilder2.AppendLine(Statistic.VoidManaSession.Value.ToReadableString("F0"));
		stringBuilder3.AppendLine(Statistic.VoidManaRealm.Value.ToReadableString("F0"));
		stringBuilder4.AppendLine(Statistic.VoidManaAllTime.Value.ToReadableString("F0"));
		stringBuilder.AppendLine("Void Entities".Translate());
		stringBuilder2.AppendLine(Statistic.ClickableCollect.Value.ToReadableString("F0"));
		stringBuilder3.AppendLine(Statistic.ClickableCollectRealm.Value.ToReadableString("F0"));
		stringBuilder4.AppendLine(Statistic.ClickableCollectTotal.Value.ToReadableString("F0"));
		stringBuilder.AppendLine("Clicks".Translate());
		stringBuilder2.AppendLine(Statistic.Clicks.Value.ToReadableString("F0"));
		stringBuilder3.AppendLine(Statistic.ClicksRealm.Value.ToReadableString("F0"));
		stringBuilder4.AppendLine(Statistic.ClicksTotal.Value.ToReadableString("F0"));
		stringBuilder.AppendLine("Autoclicks".Translate());
		stringBuilder2.AppendLine(Statistic.AutoClicks.Value.ToReadableString("F0"));
		stringBuilder3.AppendLine(Statistic.AutoClicksRealm.Value.ToReadableString("F0"));
		stringBuilder4.AppendLine(Statistic.AutoClicksTotal.Value.ToReadableString("F0"));
		stringBuilder.AppendLine("Cast spells".Translate());
		stringBuilder2.AppendLine(Statistic.CastSpell.Value.ToReadableString("F0"));
		stringBuilder3.AppendLine(Statistic.CastSpellRealm.Value.ToReadableString("F0"));
		stringBuilder4.AppendLine(Statistic.CastSpellTotal.Value.ToReadableString("F0"));
		stringBuilder.AppendLine("Spell Shards".Translate());
		stringBuilder2.AppendLine(Statistic.ShardsSession.Value.ToReadableString("F0"));
		stringBuilder3.AppendLine(Statistic.ShardsRealm.Value.ToReadableString("F0"));
		stringBuilder4.AppendLine(Statistic.ShardsTotal.Value.ToReadableString("F0"));
		stringBuilder.AppendLine("Real time".Translate());
		stringBuilder2.AppendLine(Statistic.time_to_string(Statistic.TimeSession.ValueInt));
		stringBuilder3.AppendLine(Statistic.time_to_string(Statistic.TimeRealm.ValueInt));
		stringBuilder4.AppendLine(Statistic.time_to_string(Statistic.TimeTotal.ValueInt));
		stringBuilder.AppendLine("Game time".Translate());
		stringBuilder2.AppendLine(Statistic.time_to_string(Statistic.TimeSession.ValueInt + Statistic.SkipedTimeSession.Value));
		stringBuilder3.AppendLine(Statistic.time_to_string(Statistic.TimeRealm.ValueInt + Statistic.SkipedTimeRealm.Value));
		stringBuilder4.AppendLine(Statistic.time_to_string(Statistic.TimeTotal.ValueInt + Statistic.SkipedTimeTotal.Value));
		stringBuilder.AppendLine("Idle time".Translate());
		stringBuilder2.AppendLine(Statistic.time_to_string(Statistic.TimeIdleSession.ValueInt));
		stringBuilder3.AppendLine(Statistic.time_to_string(Statistic.TimeIdleRealm.ValueInt));
		stringBuilder4.AppendLine(Statistic.time_to_string(Statistic.TimeIdleTotal.ValueInt));
		stringBuilder.AppendLine("Offline time".Translate());
		stringBuilder2.AppendLine(Statistic.time_to_string(Statistic.TimeOfflineSession.ValueInt));
		stringBuilder3.AppendLine(Statistic.time_to_string(Statistic.TimeOfflineRealm.ValueInt));
		stringBuilder4.AppendLine(Statistic.time_to_string(Statistic.TimeOfflineTotal.ValueInt));
		stringBuilder.AppendLine("Enchanting dust".Translate());
		stringBuilder2.AppendLine(Statistic.EnchantingDustExile.Value.ToReadableString("F0"));
		stringBuilder3.AppendLine(Statistic.GetEdustRealm().Value.ToReadableString("F0"));
		stringBuilder4.AppendLine(Statistic.GetEdustTotal().Value.ToReadableString("F0"));
		stringBuilder.AppendLine("Exiles".Translate());
		stringBuilder2.AppendLine("-");
		stringBuilder3.AppendLine(Statistic.AscendsInRealm.ValueInt.ToString());
		stringBuilder4.AppendLine(Statistic.Ascends.ValueInt.ToString());
		total_label.text = stringBuilder.ToString();
		total_run_bottom_label.text = stringBuilder2.ToString();
		total_realm_label.text = stringBuilder3.ToString();
		total_all_label.text = stringBuilder4.ToString();
		stringBuilder = new StringBuilder();
		stringBuilder4 = new StringBuilder();
		stringBuilder.AppendLine("BatsSpawnRate".Translate());
		stringBuilder4.Append((GameManager.Instance.MBSpawner.SpawnRate.Value * 100.0).ToReadableString("F0"));
		stringBuilder4.AppendLine("%");
		stringBuilder.AppendLine("Trial reward".Translate());
		stringBuilder4.Append((GameManager.Instance.Trials.RewardSize.Value * 100.0).ToReadableString("F0"));
		stringBuilder4.AppendLine("%");
		stringBuilder.AppendLine("JarMaxM".Translate());
		stringBuilder4.AppendLine(GameManager.Instance.Resources.MaxProgress.Value.ToReadableString("F0"));
		if (GameManager.Instance.Paragon.ItemsIsAvailable)
		{
			BigNumber craftingDustIncome = GameManager.Instance.Resources.GetCraftingDustIncome();
			stringBuilder.AppendLine("Crafting dust income".Translate());
			stringBuilder4.Append((craftingDustIncome * 100.0).ToReadableString());
			stringBuilder4.AppendLine("%");
			craftingDustIncome = 86400.0 * GameManager.Instance.Resources.GetCraftingDustIncome(isAverage: true);
			stringBuilder.AppendLine("Average income".Translate());
			stringBuilder4.Append(craftingDustIncome.ToReadableString("F0"));
			stringBuilder4.Append(" ").AppendLine("per day".Translate());
		}
		if (GameManager.Instance.Paragon.EnchantingIsAvailable)
		{
			BigNumber craftingDustIncome = GameManager.Instance.Resources.GetEnchantingBonus();
			stringBuilder.AppendLine("EDustGain".Translate());
			stringBuilder4.Append((craftingDustIncome * 100.0).ToReadableString());
			stringBuilder4.AppendLine("%");
			craftingDustIncome = 86400.0 * GameManager.Instance.Resources.GetEnchantingDustIncomeAverage();
			stringBuilder.AppendLine("Average income".Translate());
			stringBuilder4.Append(craftingDustIncome.ToReadableString("F0"));
			stringBuilder4.Append(" ").AppendLine("per day".Translate());
		}
		if (GameManager.Instance.Paragon.GildingIsAvailable)
		{
			stringBuilder.AppendLine("Memetic Splinter income".Translate());
			stringBuilder4.Append((GameManager.Instance.Gilding.GetSplinterIncome() * 100.0).ToReadableString("F0"));
			stringBuilder4.AppendLine("%");
		}
		if (GameManager.Instance.Paragon.AscensionIsAvailable)
		{
			stringBuilder.AppendLine("AscensionXP".Translate());
			stringBuilder4.Append(((GameManager.Instance.Ascension.ExpBonus.Value - 1.0) * 100.0).ToReadableString());
			stringBuilder4.AppendLine("%");
		}
		total_add_label.text = stringBuilder.ToString();
		total_add_values.text = stringBuilder4.ToString();
	}

	private void classes_update()
	{
		HC.text = Statistic.HCTotal.Value.ToReadableString("F0");
		CT.text = Statistic.CTTotal.Value.ToReadableString();
		LS.text = Statistic.LSTotal.Value.ToReadableString();
		ulong num = 0uL;
		for (int i = 0; i < 18; i++)
		{
			num += Statistic.ClassTime[i].ValueInt;
		}
		ulong num2 = 0uL;
		for (int j = 19; j < Statistic.ClassTime.Count; j++)
		{
			num2 += Statistic.ClassTime[j].ValueInt;
		}
		cpts[0].UpdateVaues(HeroesNames.Apprentice, num);
		cpts[1].UpdateVaues(HeroesNames.Druid, num);
		cpts[2].UpdateVaues(HeroesNames.Demonologist, num);
		cpts[3].UpdateVaues(HeroesNames.Necromancer, num);
		cpts[4].UpdateVaues(HeroesNames.Arcanist, num);
		cpts[5].UpdateVaues(HeroesNames.Prodigy, num);
		cpts[6].UpdateVaues(HeroesNames.Voidmancer, num);
		cpts[7].UpdateVaues(HeroesNames.Exorcist, num);
		cpts[8].UpdateVaues(HeroesNames.Chronomancer, num);
		cpts[9].UpdateVaues(HeroesNames.Umbramancer, num);
		cpts[10].UpdateVaues(HeroesNames.Alchemist, num);
		cpts[11].UpdateVaues(HeroesNames.Ironsoul, num);
		cpts[12].UpdateVaues(HeroesNames.Abolisher, num);
		cpts[13].UpdateVaues(HeroesNames.Shaman, num);
		cpts[14].UpdateVaues(HeroesNames.Heretic, num);
		cpts[15].UpdateVaues(HeroesNames.Oni, num);
		cpts[16].UpdateVaues(HeroesNames.Archon, num);
		cpts[17].UpdateVaues(HeroesNames.Temporalist, num);
		cpts[18].UpdateVaues(HeroesNames.Desolator, num);
		cpts[19].UpdateVaues(HeroesNames.Tempest, num2);
		cpts[20].UpdateVaues(HeroesNames.Demiurge, num2);
		cpts[21].UpdateVaues(HeroesNames.Defiance, num2);
		cpts[22].UpdateVaues(HeroesNames.Dread, num2);
	}
}
