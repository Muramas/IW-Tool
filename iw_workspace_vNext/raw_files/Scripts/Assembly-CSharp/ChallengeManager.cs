using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class ChallengeManager : MonoBehaviour
{
	public VariableInt CompletedChallenges;

	public ChallengeChoose ChallengeChoose;

	public List<Challenge> Challenges;

	public List<ChallengeChoose> ChallengeButtons;

	public TextMeshProUGUI DescriptionLabel;

	public Challenge SelectedChallenge;

	public GameObject SelectPlace;

	public Challenge ActiveChallenge;

	private string challenge_message = "ChallengeMessage";

	private string challenge_close_message = "ChallengeHomecoming";

	private string challenge_description = "ChallengeDescription";

	public Button Start;

	public ExilePage exile_page;

	public ChallengeMessage Message;

	public Toggle ShowCompleted;

	public TextMeshProUGUI CompletedLabel;

	public GameObject Blind;

	public SettingMenu SettingsMenu;

	public VariableBignumber Target;

	public DisableCanvasSingle canvas;

	public Transform BlindParent;

	public Action onExit;

	private Action on_action;

	private Action<Spell> on_cast;

	private Action<Spell> on_cast_2;

	private Action<float> on_tick;

	private Action<float> on_crit;

	private Action on_hc;

	public Action onFail;

	public Action onComplete;

	public Action afterComplete;

	public Action afterEscape;

	public Action beforeStart;

	public Journal journal;

	public ChallengeSounds sounds;

	private bool applied;

	private bool active;

	public VariableBignumber Progress;

	private Gallery Gallery;

	private LegionChallenge Legion;

	private WizardSquadChallenge Squad;

	public Trial trialOfSkill;

	public void Init()
	{
		Target = new VariableBignumber(0.0);
		CompletedChallenges = new VariableInt(0);
		Progress = new VariableBignumber(0.0);
		CreateAll();
		CreateButtons();
		VariableLong timeSession = Statistic.TimeSession;
		timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(update_rules));
		GameContext.ContextAddResource(ResourceType.Achiev.ToString() + "." + ResourceAchiev.Challenge, CompletedChallenges);
		Gallery = GameManager.Instance.Gallery;
	}

	public void Open()
	{
		if (ActiveChallenge == null)
		{
			DescriptionLabel.text = challenge_description.Translate();
			Start.gameObject.SetActive(value: false);
		}
		else
		{
			DescriptionLabel.text = ActiveChallenge.GetDescription();
		}
		ShowHideCompleted();
		foreach (ChallengeChoose challengeButton in ChallengeButtons)
		{
			challengeButton.Check();
		}
		CompletedLabel.text = "Completed".Translate() + " " + CompletedChallenges.ValueInt + "/" + Challenges.Count;
		base.gameObject.SetActive(value: true);
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		journal.Close();
	}

	private void CreateAll()
	{
		Challenges = new List<Challenge>();
		Challenge challenge = new Challenge("Like the first time", 1);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e12", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true);
		challenge.Rules.Other = delegate
		{
			List<string> closedBuildings = new List<string> { "Dimensional Rift", "The Nexus" };
			GameManager.Instance.CurrentHero.Hero.ClosedBuildings = closedBuildings;
			GameManager.Instance.CurrentHero.Hero.close_buildings();
			GameContext.GetResource("Building.1.Cost").Change(0.0, 0.25);
			GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == 1).Recalculate();
		};
		challenge.Rules.OtherDescription = "Like the first time Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.CurrentHero.Hero.open_buildings();
			GameManager.Instance.CurrentHero.Hero.ClosedBuildings = new List<string>();
		};
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e12", "Earn Mana"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 3.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{3}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Like the first time] II", 101, Challenges.Last());
		challenge.Rules.Other = delegate
		{
			List<string> closedBuildings = new List<string> { "The Nexus" };
			GameManager.Instance.CurrentHero.Hero.ClosedBuildings = closedBuildings;
			GameManager.Instance.CurrentHero.Hero.close_buildings();
			GameContext.GetResource("Building.1.Cost").Change(0.0, 0.25);
			GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == 1).Recalculate();
			Gallery.Set(HeroesNames.Apprentice, "0#1");
		};
		challenge.Rules.OtherDescription = "Like the first time II Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.CurrentHero.Hero.open_buildings();
			GameManager.Instance.CurrentHero.Hero.ClosedBuildings = new List<string>();
			Gallery.RefreshFrame();
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e16", "Earn Mana"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 3.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{3}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Like the first time] III", 201, Challenges.Last());
		challenge.Rules.Other = delegate
		{
			GameContext.GetResource("Building.1.Cost").Change(0.0, 0.25);
			GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == 1).Recalculate();
			Gallery.Set(HeroesNames.Apprentice, "0#2");
		};
		challenge.Rules.OtherDescription = "Like the first time III Description";
		challenge.Rules.Remove = delegate
		{
			Gallery.RefreshFrame();
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e20", "Earn Mana"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 3.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{3}";
		Challenges.Add(challenge);
		challenge = new Challenge("Hand-made forest", 2);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e13", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Druid, 0, "Unlock Druid:"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Pixie, 0, "Unlock Pixie:"));
		challenge.Rules = new ChallengeRulles();
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Druid);
			SetPet(PetNames.Pixie);
			List<string> closedBuildings = new List<string> { "Circle Of Power", "Dimensional Rift", "The Nexus" };
			GameManager.Instance.CurrentHero.Hero.ClosedBuildings = closedBuildings;
			GameManager.Instance.CurrentHero.Hero.close_buildings();
			BuildingVisual buildingVisual = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 4);
			if (buildingVisual.building.Level.ValueInt == 0)
			{
				buildingVisual.building.Level.SetValue(10);
				Statistic.TotalBuildings.Change(10);
			}
			buildingVisual.CheckEnable();
			buildingVisual.Recalculate();
			GameManager.Instance.CurrentHero.StartingLevel.Change(9);
			GameManager.Instance.Orb.click_profit.Change(0.0, 10.0);
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.CurrentHero.StartingLevel.Change(-9);
			GameManager.Instance.CurrentHero.Hero.open_buildings();
			GameManager.Instance.CurrentHero.Hero.ClosedBuildings = new List<string>();
		};
		challenge.Rules.OtherDescription = "Hand-made forest Description";
		challenge.Objectives.Add(new ConditionUnlockMore("Building.4.TotalLevel", "130", "Highest amount of Trees Of Life:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Hand-made forest] II", 202, Challenges.Last());
		challenge.Rules = new ChallengeRulles();
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Druid);
			SetPet(PetNames.Pixie);
			BuildingVisual buildingVisual = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 4);
			if (buildingVisual.building.Level.ValueInt == 0)
			{
				buildingVisual.building.Level.SetValue(130);
				Statistic.TotalBuildings.Change(130);
			}
			buildingVisual.CheckEnable();
			buildingVisual.Recalculate();
			GameManager.Instance.CurrentHero.StartingLevel.Change(9);
			GameManager.Instance.Orb.click_profit.Change(0.0, 20.0);
			Gallery.Set(HeroesNames.Druid, "2#2");
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.CurrentHero.StartingLevel.Change(-9);
			Gallery.RefreshFrame();
		};
		challenge.Rules.OtherDescription = "Hand-made forest II Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Building.4.TotalLevel", "180", "Highest amount of Trees Of Life:", calculate_des: true, 130.0));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Slavedriver", 3);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e15", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Demonologist, 0, "Unlock Demonologist:"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Daemon, 0, "Unlock Daemon:"));
		challenge.Rules = new ChallengeRulles();
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Demonologist);
			SetPet(PetNames.Daemon);
			BuildingVisual buildingVisual = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 6);
			if (buildingVisual.building.Level.ValueInt == 0)
			{
				buildingVisual.building.Level.SetValue(100);
				Statistic.TotalBuildings.Change(100);
			}
			GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.FireBall).level_req -= 17;
			GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.UncleanKnowledge).level_req -= 72;
			GameManager.Instance.PPSFromBuildings.Change(0.0, 0.0);
			GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, 66.0);
			GameManager.Instance.Orb.autoclicksFromSpell.Change(0.0, 10.0);
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.FireBall).level_req += 17;
			GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.UncleanKnowledge).level_req += 72;
			GameManager.Instance.Orb.autoclicksFromSpell.Change(0.0, 0.10000000149011612);
		};
		challenge.Rules.OtherDescription = "Slavedriver Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "66", "Reach Pet level:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.Orb.autoclicksFromSpell, 0.10000000149011612, 1.0);
		challenge.RewardDescription = "[IncreaseAClickAmountSpells]{10%}";
		Challenges.Add(challenge);
		challenge = new Challenge("Idle Beginning", 40);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e16", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true);
		challenge.Rules.Other = delegate
		{
			List<string> closedBuildings = new List<string> { "Dimensional Rift", "The Nexus" };
			GameManager.Instance.CurrentHero.Hero.ClosedBuildings = closedBuildings;
			GameManager.Instance.CurrentHero.Hero.close_buildings();
			GameManager.Instance.Orb.Collider.enabled = false;
			GameContext.GetResource("Building.1.Cost").Change(0.0, 0.25);
			BuildingVisual buildingVisual = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 1);
			if (buildingVisual.building.Level.ValueInt == 0)
			{
				buildingVisual.building.Level.SetValue(1);
				Statistic.TotalBuildings.Change(1);
			}
			buildingVisual.Recalculate();
		};
		challenge.Rules.OtherDescription = "Idle Beginning Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.CurrentHero.Hero.open_buildings();
			GameManager.Instance.CurrentHero.Hero.ClosedBuildings = new List<string>();
			GameManager.Instance.Orb.Collider.enabled = true;
		};
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e12", "Earn Mana"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Vision of the Future", 29);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e17", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Bonus = "1e120";
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Prodigy);
		};
		challenge.Rules.Remove = delegate
		{
			SetHero(HeroesNames.Apprentice);
		};
		challenge.Rules.OtherDescription = "Vision of the Future Description";
		challenge.Objectives.Add(new ConditionUnlockSpellUse(Spells.LeyOverdrive, "1"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Active Necromancer", 4);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e18", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Necromancer, 0, "Unlock Necromancer:"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.ZombieWarrior, 0, "Unlock Zombie:"));
		challenge.Rules = new ChallengeRulles();
		challenge.Rules.TimeLimit = 3300uL;
		challenge.Rules.Other = delegate
		{
			BuildingVisual buildingVisual = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 2);
			if (buildingVisual.building.Level.ValueInt == 0)
			{
				buildingVisual.building.Level.SetValue(25);
				Statistic.TotalBuildings.Change(25);
			}
			SetHero(HeroesNames.Necromancer);
			SetPet(PetNames.ZombieWarrior);
		};
		challenge.Rules.OtherDescription = "Active Necromancer Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.AutoClicks, "1.1e4", "Total autoclicks:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Spell-Sources", 5);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e21", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Arcanist, 0, "Unlock Arcanist:"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Spellhound, 0, "Unlock Spellhound:"));
		challenge.Rules = new ChallengeRulles(myst: true);
		challenge.Rules.Other = delegate
		{
			Target.SetValue("1e4");
			int per_cast = 2;
			SetHero(HeroesNames.Arcanist);
			SetPet(PetNames.Spellhound);
			BuildingVisual buildingVisual = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 3);
			if (buildingVisual.building.Level.ValueInt == 0)
			{
				buildingVisual.building.Level.SetValue(100);
				Statistic.TotalBuildings.SetValue(100);
				buildingVisual.CheckEnable();
				buildingVisual.Recalculate();
			}
			else
			{
				foreach (Spell availableSpell in GameManager.Instance.SpellBook.AvailableSpells)
				{
					Target.Change(-per_cast * availableSpell.UseThisRun.Value);
				}
			}
			on_cast = delegate
			{
				if (Target.Value > per_cast)
				{
					Target.Change(-per_cast);
				}
				else
				{
					Target.SetValue(0.0);
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
		};
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
		};
		challenge.Rules.OtherDescription = "Spell-Sources Description";
		challenge.Objectives.Add(new ConditionUnlockMoreThan(Statistic.TotalBuildings, Target, "1", "Spell-Sources Objective"));
		challenge.Reward = new ChallengeReward();
		SimpleEffect simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentHero.ExpBoost;
		simpleEffect.mult = 1.5;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[ActionsXP]{50%}";
		Challenges.Add(challenge);
		challenge = new Challenge("Truest of Sorceries", 6);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e27", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Prodigy, 0, "Unlock Prodigy:"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockVariable(Statistic.HeroMaxLevelAllTime, "80", "Reach Character level:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: true);
		challenge.Rules.TimeLimit = 3600uL;
		challenge.Rules.Other = delegate
		{
			Target.SetValue(0.0);
			SetHero(HeroesNames.Prodigy);
		};
		challenge.Rules.Remove = null;
		challenge.Rules.OtherDescription = "Truest of Sorceries Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.SpellBook.GetSpell(Spells.TrueSorcery).UseThisRun, "1", "[Cast] [TrueSorcery]"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.CurrentHero.StartingLevel, 1.0, 1.0);
		challenge.RewardDescription = "[IncreaseStartingLevel]{1}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Truest of Sorceries] II", 106, Challenges.Last());
		challenge.Rules.TimeLimit = 1200uL;
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.CurrentHero.StartingLevel, 1.0, 1.0);
		Challenges.Add(challenge);
		challenge = new Challenge("[Truest of Sorceries] III", 206, Challenges.Last());
		challenge.Rules.TimeLimit = 300uL;
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.CurrentHero.StartingLevel, 1.0, 1.0);
		Challenges.Add(challenge);
		challenge = new Challenge("Void Milking", 13);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e30", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockVariable(Statistic.ClickableCollectTotal, "5e3", "Total Void Entities collected:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: true);
		challenge.Rules.Other = delegate
		{
			GameManager.Instance.LevelReduction.Change(60);
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.LevelReduction.Change(-60);
		};
		challenge.Rules.OtherDescription = "Void Milking Description";
		challenge.Conditions2Fail = new List<ConditionUnlock>();
		challenge.Conditions2Fail.Add(new ConditionUnlockVariable(Statistic.ClickableCollect, "10", "Void Milking Fail"));
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.VoidManaSession, "5e6", "Exile Void Mana collected:"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.LevelReduction;
		simpleEffect.add = 1.0;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[IncreaseLevelReduction]{1}";
		Challenges.Add(challenge);
		challenge = new Challenge("Bad Bargain", 8);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e30", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Arcanist, 0, "Unlock Arcanist:"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Devourer, 0, "Unlock Devourer:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: true);
		challenge.Rules.Other = delegate
		{
			Arcanist obj = GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Arcanist).Hero as Arcanist;
			obj.SpellList.Remove(Spells.ConjureLesserElemental);
			obj.SpellList.Remove(Spells.ConjureGreaterElemental);
			obj.SpellList.Remove(Spells.ConjurePrimalElemental);
			SetHero(HeroesNames.Arcanist);
			SetPet(PetNames.Devourer);
		};
		challenge.Rules.Remove = delegate
		{
			Arcanist obj = GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Arcanist).Hero as Arcanist;
			obj.SpellList.Add(Spells.ConjureLesserElemental);
			obj.SpellList.Add(Spells.ConjureGreaterElemental);
			obj.SpellList.Add(Spells.ConjurePrimalElemental);
		};
		challenge.Rules.OtherDescription = "Bad Bargain Description";
		challenge.Objectives.Add(new ConditionUnlockSpellUse(Spells.KarnaphensSpellshroud, "10"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Voidmaniac", 7);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e33", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Voidmancer, 0, "Unlock Voidmancer:"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockVariable(Statistic.VoidManaAllTime, "1e6", "Total Void Mana collected:"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: true);
		challenge.Bonus = "1e35";
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Voidmancer);
		};
		challenge.Rules.OtherDescription = "Voidmaniac Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.VoidMana, "1e6", "Have Void Mana:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Full-Power Alchemist", 9);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e36", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockVariable(Statistic.HeroMaxLevelAllTime, "90", "Reach Character level:"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Homunculus, 0, "Unlock Homunculus:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: true, change_pet: false, vip_available: true, null, statistic: false, achieves: false);
		Homunculus h = GameManager.Instance.CurrentPet.PetPanel.Pets.Find((PetChoose x) => x.PetName == PetNames.Homunculus).Pet as Homunculus;
		int mult = 0;
		challenge.Rules.Other = delegate
		{
			SetPet(PetNames.Homunculus);
			BuildingVisual buildingVisual = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 5);
			if (buildingVisual.building.Level.ValueInt == 0)
			{
				buildingVisual.building.Level.SetValue(1);
				Statistic.TotalBuildings.SetValue(1);
			}
			mult = h.total_mana_mult;
			h.targets = new List<BuildingVisual>();
			h.targets.Add(buildingVisual);
			h.total_mana_mult = 0;
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(-0.75, 1.0);
		};
		challenge.Rules.Remove = delegate
		{
			h.total_mana_mult = mult;
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(0.75, 1.0);
		};
		challenge.Rules.OtherDescription = "Full-Power Alchemist Description";
		challenge.Objectives.Add(new ConditionUnlockMore("Building.5.TotalLevel", "2500", "Highest amount of Alchemy Desk:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.CurrentHero.ExpBoost, 0.0, 1.5);
		challenge.RewardDescription = "[ActionsXP]{50%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Full-Power Alchemist] II", 109, Challenges.Last());
		mult = 0;
		challenge.Rules.Other = delegate
		{
			SetPet(PetNames.Homunculus);
			BuildingVisual buildingVisual = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 5);
			if (buildingVisual.building.Level.ValueInt == 0)
			{
				buildingVisual.building.Level.SetValue(2500);
				Statistic.TotalBuildings.SetValue(2500);
			}
			mult = h.total_mana_mult;
			h.total_mana_mult = 0;
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(-0.75, 1.0);
		};
		challenge.Rules.Remove = delegate
		{
			h.total_mana_mult = mult;
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(0.75, 1.0);
		};
		challenge.Rules.OtherDescription = "Full-Power Alchemist II Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Building.5.TotalLevel", "3500", "Highest amount of Alchemy Desk:", calculate_des: true, 2500.0));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.CurrentHero.ExpBoost, 0.0, 1.5);
		Challenges.Add(challenge);
		challenge = new Challenge("[Full-Power Alchemist] III", 209, Challenges.Last());
		challenge.Rules.Other = delegate
		{
			SetPet(PetNames.Homunculus);
			BuildingVisual buildingVisual = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 5);
			if (buildingVisual.building.Level.ValueInt == 0)
			{
				buildingVisual.building.Level.SetValue(3500);
				Statistic.TotalBuildings.SetValue(3500);
			}
			mult = h.total_mana_mult;
			h.total_mana_mult = 0;
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(-0.5, 1.0);
		};
		challenge.Rules.Remove = delegate
		{
			h.total_mana_mult = mult;
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(0.5, 1.0);
		};
		challenge.Rules.OtherDescription = "Full-Power Alchemist III Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Building.5.TotalLevel", "4000", "Highest amount of Alchemy Desk:", calculate_des: true, 3500.0));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.CurrentHero.ExpBoost, 0.0, 1.5);
		Challenges.Add(challenge);
		challenge = new Challenge("Lean Mean Shard Machine", 10);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e39", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Arcanist, 0, "Unlock Arcanist:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.TimeLimit = 3600uL;
		challenge.EffectRules = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.ShardsPerClick;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 0.10000000149011612;
		simpleEffect.parameter = GameManager.Instance.SpellBook.GetSpell(Spells.SpellStaffOfChamaon).UseThisRun;
		challenge.EffectRules.Add(simpleEffect);
		challenge.EffectDescription = new List<string>();
		challenge.EffectDescription.Add("[Lean Mean Shard Machine Spellstaff]{10%}");
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Arcanist);
		};
		challenge.Rules.OtherDescription = "Lean Mean Shard Machine Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.ShardsSession, "1.25e7", "Exile Spell Shards collected:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Lean Mean Shard Machine] II", 210, Challenges.Last());
		challenge.Rules.TimeLimit = 3600uL;
		challenge.EffectRules = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.ShardsPerClick;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 0.05000000074505806;
		simpleEffect.parameter = GameManager.Instance.SpellBook.GetSpell(Spells.SpellStaffOfChamaon).UseThisRun;
		challenge.EffectRules.Add(simpleEffect);
		challenge.EffectDescription = new List<string>();
		challenge.EffectDescription.Add("[Lean Mean Shard Machine Spellstaff]{5%}");
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Arcanist);
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.ShardsSession, "1.6e7", "Exile Spell Shards collected:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Lean Mean Shard Machine] III", 310, Challenges.Last());
		challenge.Rules.TimeLimit = 3600uL;
		challenge.EffectRules = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.ShardsPerClick;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 0.05000000074505806;
		simpleEffect.parameter = GameManager.Instance.SpellBook.GetSpell(Spells.SpellStaffOfChamaon).UseThisRun;
		challenge.EffectRules.Add(simpleEffect);
		challenge.EffectDescription = new List<string>();
		challenge.EffectDescription.Add("[Lean Mean Shard Machine Spellstaff]{5%}");
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Arcanist);
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.ShardsSession, "1.65e8", "Exile Spell Shards collected:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Infernal Crops", 11);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e42", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Demonologist, 0, "Unlock Demonologist:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			Target.SetValue(0.0);
			SetHero(HeroesNames.Demonologist);
			GameManager.Instance.CurrentHero.StartingLevel.Change(14);
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.ReapWhatYouSow)
				{
					Target.SetValue((s.effects[0] as EffectReapPetExp).reap);
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.CurrentHero.StartingLevel.Change(-14);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
		};
		challenge.Rules.OtherDescription = "Infernal Crops Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(Target, "2e11", "Infernal Crops Objective"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.CurrentHero.StartingLevel, 1.0, 1.0));
		challenge.RewardDescription = "[IncreaseStartingLevel]{1}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Infernal Crops] II", 211, Challenges.Last());
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Rules.OtherDescription = "Infernal Crops II Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(Target, "1e12", "Infernal Crops Objective"));
		float k = 0f;
		EffectReapPetExp effect = GameManager.Instance.SpellBook.GetSpell(Spells.ReapWhatYouSow).effects[0] as EffectReapPetExp;
		challenge.Rules.Other = delegate
		{
			Target.SetValue(0.0);
			SetHero(HeroesNames.Demonologist);
			GameManager.Instance.CurrentHero.StartingLevel.Change(14);
			k = effect.mod;
			effect.mod = 1f;
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.ReapWhatYouSow)
				{
					Target.SetValue(effect.reap);
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			Gallery.Set(HeroesNames.Demonologist, "1#3");
		};
		challenge.Rules.Remove = delegate
		{
			effect.mod = k;
			GameManager.Instance.CurrentHero.StartingLevel.Change(-14);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			Gallery.RefreshFrame();
		};
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.CurrentHero.StartingLevel, 1.0, 1.0));
		Challenges.Add(challenge);
		challenge = new Challenge("Places Of Power", 12);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e45", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		BigNumber bonus = 1.0;
		challenge.Rules.Other = delegate
		{
			GameManager.Instance.Scrolls.ShardsPassive.Change(197.0, 1.0);
			GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).level_req -= 60;
			bonus = new BigNumber(2.0).Pow(Convert.ToInt32(GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).UseThisRun.Value.ToDouble()));
			GameManager.Instance.Profit.Change(0.0, bonus);
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 5.0);
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 0.05000000074505806);
			Spell prev = GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower);
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.RitualOfPower || s.NameKey == Spells.ERoP)
				{
					if (prev.NameKey != s.NameKey && (prev.Type == SpellTypeGroup.Incantation || prev.Type == SpellTypeGroup.Summoning))
					{
						GameManager.Instance.Profit.Change(0.0, 2.0);
						bonus *= 2.0;
					}
					else
					{
						Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == s.NameKey);
						if (scroll != null)
						{
							scroll.StopAction();
						}
						GameManager.Instance.Profit.Change(0.0, 0.5);
						bonus *= 0.5;
					}
				}
				prev = s;
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			GameManager.Instance.StartingMana.Change(10000.0);
		};
		challenge.Rules.OtherDescription = "Places Of Power Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Scrolls.ShardsPassive.Change(-197.0, 1.0);
			GameManager.Instance.StartingMana.Change(-10000.0);
			GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).level_req += 60;
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			GameManager.Instance.Profit.Change(0.0, 1.0 / bonus);
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 0.20000000298023224);
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 20.0);
		};
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e100", "Earn Mana"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.AccumulatedCasts;
		simpleEffect.add = 200.0;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[AccumulatedAmount]{200}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Places Of Power] II", 212, Challenges.Last());
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			GameManager.Instance.Scrolls.ShardsPassive.Change(197.0, 1.0);
			GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).level_req -= 60;
			bonus = new BigNumber(10.0).Pow(Convert.ToInt32(GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).UseThisRun.Value.ToDouble()));
			GameManager.Instance.Profit.Change(0.0, bonus);
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 10.0);
			Spell prev = GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower);
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.RitualOfPower || s.NameKey == Spells.ERoP)
				{
					if (prev.NameKey != s.NameKey && prev.Type == SpellTypeGroup.Summoning)
					{
						GameManager.Instance.Profit.Change(0.0, 10.0);
						bonus *= 10.0;
					}
					else
					{
						Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == s.NameKey);
						if (scroll != null)
						{
							scroll.StopAction();
						}
						GameManager.Instance.Profit.Change(0.0, 0.1);
						bonus *= 0.10000000149011612;
					}
				}
				prev = s;
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			GameManager.Instance.Scrolls.ShardsPerClick.Change(0.0, 3.0);
		};
		challenge.Rules.OtherDescription = "Places Of Power II Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Scrolls.ShardsPassive.Change(-197.0, 1.0);
			GameManager.Instance.Scrolls.ShardsPerClick.Change(0.0, 0.3333333432674408);
			GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).level_req += 60;
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			GameManager.Instance.Profit.Change(0.0, 1.0 / bonus);
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 0.10000000149011612);
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e110", "Earn Mana"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.AccumulatedCasts;
		simpleEffect.add = 400.0;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[AccumulatedAmount]{400}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Places Of Power] III", 312, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Chronomancer, 0, "Unlock Chronomancer:"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Chronomancer).Hero as Chronomancer).SpellList.Remove(Spells.Wormhole);
			SetHero(HeroesNames.Chronomancer);
			GameManager.Instance.Scrolls.ShardsPassive.Change(97.0, 1.0);
			GameManager.Instance.LevelReduction.Change(40);
			GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).level_req -= 60;
			bonus = new BigNumber(2.0).Pow(Convert.ToInt32(GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).UseThisRun.Value.ToDouble()));
			GameManager.Instance.Profit.Change(0.0, bonus);
			Spell prev = GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower);
			on_cast = delegate(Spell s)
			{
				if (s != null)
				{
					if (s.NameKey == Spells.RitualOfPower || s.NameKey == Spells.ERoP)
					{
						if (prev.SubCost != null)
						{
							GameManager.Instance.Profit.Change(0.0, 2.0);
							bonus *= 2.0;
						}
						else
						{
							Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == s.NameKey);
							if (scroll != null)
							{
								scroll.StopAction();
							}
							GameManager.Instance.Profit.Change(0.0, 0.5);
							bonus *= 0.5;
						}
					}
					prev = s;
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			GameManager.Instance.Scrolls.ShardsPerClick.Change(0.0, 2.0);
		};
		challenge.Rules.OtherDescription = "Places Of Power III Description";
		challenge.Rules.Remove = delegate
		{
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Chronomancer).Hero as Chronomancer).SpellList.Add(Spells.Wormhole);
			GameManager.Instance.Scrolls.ShardsPassive.Change(-97.0, 1.0);
			GameManager.Instance.LevelReduction.Change(-40);
			GameManager.Instance.Scrolls.ShardsPerClick.Change(0.0, 0.5);
			GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).level_req += 60;
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			GameManager.Instance.Profit.Change(0.0, 1.0 / bonus);
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e120", "Earn Mana"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.AccumulatedCasts;
		simpleEffect.add = 500.0;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[AccumulatedAmount]{500}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Places Of Power] IV", 412, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Umbramancer, 0, "Unlock Umbramancer:"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Umbramancer).Hero as Umbramancer).SpellList.Add(Spells.RitualOfPower);
			SetHero(HeroesNames.Umbramancer);
			GameManager.Instance.Scrolls.ShardsPassive.Change(97.0, 1.0);
			GameManager.Instance.LevelReduction.Change(60);
			GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).level_req -= 60;
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 0.20000000298023224);
			bonus = new BigNumber(2.0).Pow(Convert.ToInt32(GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).UseThisRun.Value.ToDouble()));
			GameManager.Instance.Profit.Change(0.0, bonus);
			Spell prev = GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower);
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.RitualOfPower || s.NameKey == Spells.ERoP)
				{
					if (!prev.ShardsBuilding)
					{
						GameManager.Instance.Profit.Change(0.0, 2.0);
						bonus *= 2.0;
					}
					else
					{
						Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == s.NameKey);
						if (scroll != null)
						{
							scroll.StopAction();
						}
						GameManager.Instance.Profit.Change(0.0, 0.5);
						bonus *= 0.5;
					}
				}
				prev = s;
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			GameManager.Instance.Scrolls.ShardsPerClick.Change(0.0, 2.0);
		};
		challenge.Rules.OtherDescription = "Places Of Power IV Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Scrolls.ShardsPassive.Change(-97.0, 1.0);
			GameManager.Instance.LevelReduction.Change(-60);
			GameManager.Instance.Scrolls.ShardsPerClick.Change(0.0, 0.5);
			GameManager.Instance.SpellBook.GetActualSpell(Spells.RitualOfPower).level_req += 60;
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			GameManager.Instance.Profit.Change(0.0, 1.0 / bonus);
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 5.0);
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Umbramancer).Hero as Umbramancer).SpellList.Remove(Spells.RitualOfPower);
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e130", "Earn Mana"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.AccumulatedCasts;
		simpleEffect.add = 500.0;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[AccumulatedAmount]{500}";
		Challenges.Add(challenge);
		challenge = new Challenge("These Will Suffice", 14);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e48", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Voidmancer, 0, "Unlock Voidmancer:"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true);
		challenge.Rules.TimeLimit = 3600uL;
		challenge.Bonus = "1e45";
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Voidmancer);
		};
		challenge.Rules.OtherDescription = "These Will Suffice Description";
		challenge.Conditions2Fail = new List<ConditionUnlock>();
		challenge.Conditions2Fail.Add(new ConditionUnlockVariable(Statistic.BoughtUpgrades, "25", "These Will Suffice Fail"));
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.ClickableCollect, "45", "Exile Void Entities collected:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[These Will Suffice] II", 114, Challenges.Last());
		challenge.Rules.TimeLimit = 3600uL;
		challenge.Bonus = "1e48";
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Voidmancer);
		};
		challenge.Conditions2Fail = new List<ConditionUnlock>();
		challenge.Conditions2Fail.Add(new ConditionUnlockVariable(Statistic.BoughtUpgrades, "15", "These Will Suffice II Fail"));
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.CastSpell, "1e3", "Exile Spells cast:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[These Will Suffice] III", 214, Challenges.Last());
		challenge.Rules.TimeLimit = 3600uL;
		challenge.Bonus = "1e48";
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Voidmancer);
		};
		challenge.Conditions2Fail = new List<ConditionUnlock>();
		challenge.Conditions2Fail.Add(new ConditionUnlockVariable(Statistic.BoughtUpgrades, "10", "These Will Suffice III Fail"));
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.TotalBuildings, "7.6e3", "Reach Total Mana Sources:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Upgrademeister", 15);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e51", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		float time_bonus = 0f;
		bool bonus_active = false;
		bonus = 1.0;
		challenge.Rules.Other = delegate
		{
			bonus = 1.0;
			time_bonus = 0f;
			bonus_active = false;
			GameManager.Instance.Scrolls.ShardsPassive.Change(97.0, 1.0);
			GameManager.Instance.LevelReduction.Change(50);
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(-0.5, 1.0);
			GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed.Change(0.0, 5.0);
			bonus = new BigNumber(1.4500000476837158).Pow(Statistic.BoughtUpgrades.ValueInt);
			on_action = delegate
			{
				bonus *= (BigNumber)1.4500000476837158;
				if (bonus_active)
				{
					GameManager.Instance.Profit.Change(0.0, 1.4500000476837158);
				}
				else
				{
					GameManager.Instance.Profit.Change(0.0, bonus);
					bonus_active = true;
				}
				time_bonus = 0f;
			};
			on_tick = delegate(float t)
			{
				if (bonus_active)
				{
					time_bonus += t;
					if (time_bonus >= 10f)
					{
						GameManager.Instance.Profit.Change(0.0, 1.0 / bonus);
						bonus_active = false;
					}
				}
			};
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Combine(instance.GameTick, on_tick);
			VariableInt boughtUpgrades = Statistic.BoughtUpgrades;
			boughtUpgrades.OnChange = (Action)Delegate.Combine(boughtUpgrades.OnChange, on_action);
		};
		challenge.Rules.OtherDescription = "Upgrademeister Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Scrolls.ShardsPassive.Change(-97.0, 1.0);
			GameManager.Instance.LevelReduction.Change(-50);
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(0.5, 1.0);
			GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed.Change(0.0, 0.20000000298023224);
			VariableInt boughtUpgrades = Statistic.BoughtUpgrades;
			boughtUpgrades.OnChange = (Action)Delegate.Remove(boughtUpgrades.OnChange, on_action);
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Remove(instance.GameTick, on_tick);
			if (bonus_active)
			{
				GameManager.Instance.Profit.Change(0.0, 1.0 / bonus);
			}
		};
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e96", "Earn Mana"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.LevelReduction, 1.0, 1.0));
		challenge.RewardDescription = "[IncreaseLevelReduction]{1}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Vision of the Future] II", 129, Challenges.Find((Challenge x) => x.ID == 29));
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e52", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: false, vip_available: false, null, statistic: false);
		challenge.Bonus = "1e120";
		challenge.Rules.Other = delegate
		{
			Target.SetValue(GameManager.Instance.SpellBook.GetSpell(Spells.StabilizeTheFlow).Use.Value);
			SetHero(HeroesNames.Chronomancer);
			SetPet(PetNames.Golem);
			on_cast = delegate(Spell s)
			{
				if (Time.timeScale >= 9f && s.NameKey == Spells.StabilizeTheFlow)
				{
					Target.Change(1.0);
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnPreCast = (Action<Spell>)Delegate.Combine(scrolls.OnPreCast, on_cast);
		};
		challenge.Rules.Remove = delegate
		{
			SetHero(HeroesNames.Apprentice);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnPreCast = (Action<Spell>)Delegate.Remove(scrolls.OnPreCast, on_cast);
		};
		challenge.Rules.OtherDescription = "Vision of the Future II Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(Target, "10", "Vision of the Future II Objective"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0));
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		VariableBignumber TargetRON = new VariableBignumber(82.0);
		challenge = new Challenge("Rule of Nature", 16);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e54", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockSpellUse(Spells.RulesOfNature, "500"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true);
		challenge.Rules.TimeLimit = 1200uL;
		challenge.EffectRules = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Orb.crit_chance;
		simpleEffect.add = -0.01;
		simpleEffect.mult = 0.0;
		simpleEffect.parameter = Statistic.TimeSession;
		challenge.EffectRules.Add(simpleEffect);
		challenge.EffectDescription = new List<string>();
		challenge.EffectDescription.Add("[Rule of Nature Crit Chance]{0.01%}");
		VariableInt count_crits = new VariableInt(0);
		float dt = 0f;
		List<int> crits = new List<int>();
		int in_last_sec = 0;
		challenge.Rules.Other = delegate
		{
			dt = 0f;
			SetHero(HeroesNames.Druid);
			GameManager.Instance.Orb.crit_chance.Change(65.0, 1.0);
			on_tick = delegate(float t)
			{
				dt += t;
				if (dt > 1f)
				{
					if (crits.Count > 5)
					{
						crits.RemoveAt(0);
					}
					crits.Add(in_last_sec);
					in_last_sec = 0;
					int num = 0;
					foreach (int item2 in crits)
					{
						num += item2;
					}
					count_crits.SetValue(num);
					dt -= 1f;
				}
			};
			on_crit = delegate(float amount)
			{
				in_last_sec += (int)amount;
			};
			Orb orb = GameManager.Instance.Orb;
			orb.OnCrit = (Action<float>)Delegate.Combine(orb.OnCrit, on_crit);
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Combine(instance.GameTick, on_tick);
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Orb.crit_chance.Change(-65.0, 1.0);
			Orb orb = GameManager.Instance.Orb;
			orb.OnCrit = (Action<float>)Delegate.Remove(orb.OnCrit, on_crit);
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Remove(instance.GameTick, on_tick);
			count_crits.SetValue(0);
			crits = new List<int>();
		};
		challenge.Rules.OtherDescription = "Rule of Nature Description";
		challenge.Objectives.Add(new ConditionUnlockMoreThan(count_crits, TargetRON, "1", "Rule of Nature Objective"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.SpellChargingCostReduction;
		simpleEffect.add = -0.05;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[ChargeSpellsChargeTime]{5%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Rule of Nature] II", 116, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockSpellUse(Spells.RulesOfNature, "1000"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.TimeLimit = 2400uL;
		challenge.EffectRules = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Orb.crit_chance;
		simpleEffect.add = -0.04;
		simpleEffect.mult = 0.0;
		simpleEffect.parameter = Statistic.TimeSession;
		challenge.EffectRules.Add(simpleEffect);
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.target = GameManager.Instance.Profit;
		simpleEffect.add = 1.0;
		simpleEffect.mult = 12.0;
		simpleEffect.parameter = Statistic.AutoClicks;
		challenge.EffectRules.Add(simpleEffect);
		challenge.EffectDescription = new List<string> { "[Rule of Nature Crit Chance]{0.04}", "[Rule of Nature Autoclick]" };
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Druid);
			GameManager.Instance.Orb.crit_chance.Change(65.0, 1.0);
			Target.SetValue(0.0);
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.RulesOfNature || s.NameKey == Spells.ERoN)
				{
					Target.SetValue(1.0);
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
		};
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			GameManager.Instance.Orb.crit_chance.Change(-65.0, 1.0);
			Target.SetValue(0.0);
		};
		challenge.Rules.OtherDescription = "Rule of Nature Description";
		challenge.Objectives = new List<ConditionUnlock>
		{
			new ConditionUnlockVariable(Target, "1", "Rule of Nature II Objective")
		};
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.SpellChargingCostReduction;
		simpleEffect.add = -0.05;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[ChargeSpellsChargeTime]{5%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Rule of Nature] III", 216, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockSpellUse(Spells.RulesOfNature, "2000"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.TimeLimit = 1200uL;
		challenge.EffectRules = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Orb.crit_chance;
		simpleEffect.add = -0.08;
		simpleEffect.mult = 0.0;
		simpleEffect.parameter = Statistic.TimeSession;
		challenge.EffectRules.Add(simpleEffect);
		simpleEffect = new SimpleEffect();
		simpleEffect.effect = GameContext.GetEffect("Pow");
		simpleEffect.target = GameManager.Instance.Profit;
		simpleEffect.add = 1.0;
		simpleEffect.mult = 12.0;
		simpleEffect.parameter = Statistic.AutoClicks;
		challenge.EffectRules.Add(simpleEffect);
		challenge.EffectDescription = new List<string> { "[Rule of Nature Crit Chance]{0.05}", "[Rule of Nature Autoclick]" };
		challenge.Rules.Other = delegate
		{
			TargetRON.SetValue(82.0);
			count_crits.SetValue(0);
			SetHero(HeroesNames.Druid);
			dt = 0f;
			Scroll ron = null;
			GameManager.Instance.Orb.crit_chance.Change(65.0, 1.0);
			on_tick = delegate(float t)
			{
				ron = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && (x.spell.NameKey == Spells.RulesOfNature || x.spell.NameKey == Spells.ERoN));
				if (!(ron == null) && ron.active)
				{
					dt += t;
					if (dt > 1f)
					{
						if (crits.Count > 5)
						{
							crits.RemoveAt(0);
						}
						crits.Add(in_last_sec);
						in_last_sec = 0;
						int num = 0;
						foreach (int item3 in crits)
						{
							num += item3;
						}
						count_crits.SetValue(num);
						dt -= 1f;
					}
				}
			};
			on_crit = delegate(float amount)
			{
				ron = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && (x.spell.NameKey == Spells.RulesOfNature || x.spell.NameKey == Spells.ERoN));
				if (!(ron == null) && ron.active)
				{
					in_last_sec += (int)amount;
				}
			};
			Orb orb = GameManager.Instance.Orb;
			orb.OnCrit = (Action<float>)Delegate.Combine(orb.OnCrit, on_crit);
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Combine(instance.GameTick, on_tick);
			Gallery.Set(HeroesNames.Druid, "2#2");
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Orb.crit_chance.Change(-65.0, 1.0);
			Orb orb = GameManager.Instance.Orb;
			orb.OnCrit = (Action<float>)Delegate.Remove(orb.OnCrit, on_crit);
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Remove(instance.GameTick, on_tick);
			Gallery.RefreshFrame();
			count_crits.SetValue(0);
			crits = new List<int>();
		};
		challenge.Rules.OtherDescription = "Rule of Nature Description";
		challenge.Conditions2Fail = new List<ConditionUnlock>
		{
			new ConditionUnlockVariable(Statistic.Clicks, "20", "Rule of Nature III Fail")
		};
		challenge.Objectives = new List<ConditionUnlock>
		{
			new ConditionUnlockMoreThan(count_crits, TargetRON, "1", "Rule of Nature III Objective")
		};
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.SpellChargingCostReduction;
		simpleEffect.add = -0.05;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[ChargeSpellsChargeTime]{5%}";
		Challenges.Add(challenge);
		challenge = new Challenge("Unstable Curse", 17);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e57", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			on_cast = delegate(Spell s)
			{
				if (UnityEngine.Random.Range(0f, 1f) < 0.15f)
				{
					List<Spell> list2 = GameManager.Instance.SpellBook.SpellList.Where((Spell x) => !GameManager.Instance.SpellBook.AvailableSpells.Contains(x) && x.SubCost == null && x.CursedChallenge && !GameManager.Instance.SpellBook.Enhancements.IsEnhancement(x.NameKey)).ToList();
					if (list2.Count > 0)
					{
						Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell == s);
						GameManager.Instance.Scrolls.ChoosePanel.Close();
						GameManager.Instance.Scrolls.ChoosePanel.scroll = scroll;
						Spell sp = list2[UnityEngine.Random.Range(0, list2.Count)];
						GameManager.Instance.SpellBook.AddSpell(sp);
						GameManager.Instance.SpellBook.SpellChooses.Find((SpellChoose x) => x.Spell != null && x.Spell.NameKey == sp.NameKey).Choose();
					}
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCastEnd = (Action<Spell>)Delegate.Combine(scrolls.OnCastEnd, on_cast);
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(-0.7, 1.0);
		};
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCastEnd = (Action<Spell>)Delegate.Remove(scrolls.OnCastEnd, on_cast);
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(0.7, 1.0);
		};
		challenge.Rules.OtherDescription = "Unstable Curse Description";
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e120", "Earn Mana"));
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.VoidManaSession, "1e7", "Exile Void Mana collected:"));
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.AutoClicks, "1e5", "Exile autoclicks:"));
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "85", "Reach Pet level:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.CurrentHero.ExpBoost, 0.0, 1.75));
		challenge.RewardDescription = "[ActionsXP]{75%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Unstable Curse] II", 117, Challenges.Last());
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			on_cast = delegate(Spell s)
			{
				if (UnityEngine.Random.Range(0f, 1f) < 0.2f)
				{
					List<Spell> list2 = GameManager.Instance.SpellBook.SpellList.Where((Spell x) => !GameManager.Instance.SpellBook.AvailableSpells.Contains(x) && x.SubCost == null && x.CursedChallenge && !GameManager.Instance.SpellBook.Enhancements.IsEnhancement(x.NameKey)).ToList();
					if (list2.Count > 0)
					{
						Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell == s);
						GameManager.Instance.Scrolls.ChoosePanel.Close();
						GameManager.Instance.Scrolls.ChoosePanel.scroll = scroll;
						Spell sp = list2[UnityEngine.Random.Range(0, list2.Count)];
						GameManager.Instance.SpellBook.AddSpell(sp);
						GameManager.Instance.SpellBook.SpellChooses.Find((SpellChoose x) => x.Spell != null && x.Spell.NameKey == sp.NameKey).Choose();
					}
				}
				if (UnityEngine.Random.Range(0f, 1f) < 0.1f)
				{
					BigNumber exp = ((GameManager.Instance.CurrentPet.Pet == null) ? ((BigNumber)0.0) : GameManager.Instance.CurrentPet.Pet.TotalExp.Value);
					List<PetChoose> list3 = GameManager.Instance.CurrentPet.PetPanel.Pets.Where((PetChoose x) => x.Pet != GameManager.Instance.CurrentPet.Pet && x.Pet.NameKey <= PetNames.Ebonsand).ToList();
					list3[UnityEngine.Random.Range(0, list3.Count)].SetPet();
					GameManager.Instance.CurrentPet.Pet.AddExpConst(exp);
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCastEnd = (Action<Spell>)Delegate.Combine(scrolls.OnCastEnd, on_cast);
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(-0.7, 1.0);
		};
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCastEnd = (Action<Spell>)Delegate.Remove(scrolls.OnCastEnd, on_cast);
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(0.7, 1.0);
		};
		challenge.Rules.OtherDescription = "Unstable Curse II Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.VoidManaSession, "2.5e7", "Exile Void Mana collected:"));
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.AutoClicks, "1e5", "Exile autoclicks:"));
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "95", "Reach Pet level:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.CurrentHero.ExpBoost, 0.0, 1.75));
		challenge.RewardDescription = "[ActionsXP]{75%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Unstable Curse] III", 217, Challenges.Last());
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			on_cast = delegate(Spell s)
			{
				if (UnityEngine.Random.Range(0f, 1f) <= 0.1f)
				{
					List<Spell> list2 = GameManager.Instance.SpellBook.SpellList.Where((Spell x) => !GameManager.Instance.SpellBook.AvailableSpells.Contains(x) && x.SubCost == null && x.CursedChallenge && !GameManager.Instance.SpellBook.Enhancements.IsEnhancement(x.NameKey)).ToList();
					if (list2.Count > 0)
					{
						Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell == s);
						GameManager.Instance.Scrolls.ChoosePanel.Close();
						GameManager.Instance.Scrolls.ChoosePanel.scroll = scroll;
						Spell sp = list2[UnityEngine.Random.Range(0, list2.Count)];
						GameManager.Instance.SpellBook.AddSpell(sp);
						GameManager.Instance.SpellBook.SpellChooses.Find((SpellChoose x) => x.SpellName == sp.NameKey).Choose();
					}
				}
				if (UnityEngine.Random.Range(0f, 1f) <= 0.05f)
				{
					BigNumber exp = ((GameManager.Instance.CurrentPet.Pet == null) ? ((BigNumber)0.0) : GameManager.Instance.CurrentPet.Pet.TotalExp.Value);
					List<PetChoose> list3 = GameManager.Instance.CurrentPet.PetPanel.Pets.Where((PetChoose x) => x.Pet != GameManager.Instance.CurrentPet.Pet && x.Pet.NameKey <= PetNames.Ebonsand).ToList();
					list3[UnityEngine.Random.Range(0, list3.Count)].SetPet();
					GameManager.Instance.CurrentPet.Pet.AddExpConst(exp);
				}
				if (UnityEngine.Random.Range(0f, 1f) <= 0.1f)
				{
					switch (UnityEngine.Random.Range(0, 7))
					{
					case 0:
						new EffectAddShardInstant("2e4").Apply();
						break;
					case 1:
						GameManager.Instance.BonusSpawner.SpawnBonus();
						GameManager.Instance.BonusSpawner.SpawnBonus();
						GameManager.Instance.BonusSpawner.SpawnBonus();
						GameManager.Instance.BonusSpawner.SpawnBonus();
						break;
					case 2:
						if (GameManager.Instance.CurrentPet.Pet != null)
						{
							GameManager.Instance.CurrentPet.Pet.AddExp(10000.0);
						}
						break;
					case 3:
						GameManager.Instance.VoidMana.SetValue(0.0);
						break;
					case 4:
						GameManager.Instance.Scrolls.StopAll();
						break;
					case 5:
						GameManager.Instance.Mana.SetValue(0.0);
						break;
					case 6:
						GameManager.Instance.Buildings[UnityEngine.Random.Range(0, 8)].building.Level.Change(5);
						Statistic.TotalBuildings.Change(5);
						break;
					}
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCastEnd = (Action<Spell>)Delegate.Combine(scrolls.OnCastEnd, on_cast);
		};
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCastEnd = (Action<Spell>)Delegate.Remove(scrolls.OnCastEnd, on_cast);
		};
		challenge.Rules.OtherDescription = "Unstable Curse III Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.VoidManaSession, "3e7", "Exile Void Mana collected:"));
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.AutoClicks, "2e5", "Exile autoclicks:"));
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "100", "Reach Pet level:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.CurrentHero.ExpBoost, 0.0, 1.75));
		challenge.RewardDescription = "[ActionsXP]{75%}";
		Challenges.Add(challenge);
		challenge = new Challenge("Blind Wizard", 18);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e60", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockVariable(Statistic.HeroMaxLevelAllTime, "105", "Reach Character level:"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.TimeLimit = 600uL;
		Blind blind = null;
		challenge.Bonus = "1e52";
		challenge.Rules.Other = delegate
		{
			if (blind == null)
			{
				blind = UnityEngine.Object.Instantiate(Blind, BlindParent).GetComponent<Blind>();
			}
			blind.transform.SetAsFirstSibling();
			blind.gameObject.SetActive(value: true);
			blind.StartBlind(120f);
			blind.StartShowText(limit: true);
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(-0.6, 1.0);
			GameManager.Instance.Scrolls.ShardsPassive.Change(0.0, 5.0);
			GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed.Change(0.0, 5.0);
			SetHero(HeroesNames.Prodigy);
			onFail = (Action)Delegate.Combine(onFail, new Action(blind.StopBlind));
			onComplete = (Action)Delegate.Combine(onComplete, new Action(blind.StopBlind));
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(0.6, 1.0);
			GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed.Change(0.0, 0.20000000298023224);
			GameManager.Instance.Scrolls.ShardsPassive.Change(0.0, 0.20000000298023224);
			onFail = (Action)Delegate.Remove(onFail, new Action(blind.StopBlind));
			onComplete = (Action)Delegate.Remove(onComplete, new Action(blind.StopBlind));
			blind.StopAll();
		};
		challenge.Rules.OtherDescription = "Blind Wizard Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "89", "Reach Character level:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 10.0, 1.0));
		challenge.RewardDescription = "[AttributeRewards]{10}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Blind Wizard] II", 118, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockVariable(Statistic.HeroMaxLevelAllTime, "110", "Reach Character level:"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.TimeLimit = 600uL;
		challenge.Bonus = "1e60";
		challenge.Rules.Other = delegate
		{
			if (blind == null)
			{
				blind = UnityEngine.Object.Instantiate(Blind, BlindParent).GetComponent<Blind>();
			}
			blind.transform.SetAsFirstSibling();
			blind.gameObject.SetActive(value: true);
			blind.StartBlind(120f);
			blind.StartShowText(limit: true);
			GameManager.Instance.Scrolls.ShardsPassive.Change(0.0, 5.0);
			GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed.Change(0.0, 5.0);
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Prodigy).Hero as Prodigy).SpellList.Remove(Spells.VoidAutomaton);
			SetHero(HeroesNames.Prodigy);
			onFail = (Action)Delegate.Combine(onFail, new Action(blind.StopBlind));
			onComplete = (Action)Delegate.Combine(onComplete, new Action(blind.StopBlind));
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed.Change(0.0, 0.20000000298023224);
			GameManager.Instance.Scrolls.ShardsPassive.Change(0.0, 0.20000000298023224);
			onFail = (Action)Delegate.Remove(onFail, new Action(blind.StopBlind));
			onComplete = (Action)Delegate.Remove(onComplete, new Action(blind.StopBlind));
			blind.StopAll();
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Prodigy).Hero as Prodigy).SpellList.Add(Spells.VoidAutomaton);
		};
		challenge.Rules.OtherDescription = "Blind Wizard II Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "91", "Reach Character level:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 10.0, 1.0));
		challenge.RewardDescription = "[AttributeRewards]{10}";
		Challenges.Add(challenge);
		challenge = new Challenge("Holy Wrath", 19);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e63", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Exorcist, 0, "Unlock Exorcist:"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.HolySpirit, 0, "Unlock Interrogator:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: true);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Exorcist);
			SetPet(PetNames.HolySpirit);
			GameManager.Instance.LevelReduction.Change(12);
			Gallery.Set(HeroesNames.Exorcist, "6#3");
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.LevelReduction.Change(-12);
		};
		challenge.Conditions2Fail = new List<ConditionUnlock>();
		challenge.Conditions2Fail.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.ClassBonusStacks, "21", "Holy Wrath Fail"));
		challenge.Rules.OtherDescription = "Holy Wrath Description";
		challenge.Objectives.Add(new ConditionUnlockSpellUse(Spells.ShatteringStrike, "1"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0));
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Blessed by the Void", 27);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e65", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Druid, 6));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Necromancer, 6));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Arcanist, 6));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Prodigy, 6));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Voidmancer, 5));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: true, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Voidmancer);
			List<Spells> spellList = GameManager.Instance.CurrentHero.Hero.SpellList;
			spellList.Add(Spells.RulesOfNature);
			spellList.Add(Spells.Nightfall);
			spellList.Add(Spells.JAMissileStorm);
			spellList.Add(Spells.TrueSorcery);
			GameManager.Instance.SpellBook.ChangeSpellSet();
		};
		challenge.Rules.Remove = delegate
		{
			List<Spells> spellList = GameManager.Instance.CurrentHero.Hero.SpellList;
			spellList.Remove(Spells.RulesOfNature);
			spellList.Remove(Spells.Nightfall);
			spellList.Remove(Spells.JAMissileStorm);
			spellList.Remove(Spells.TrueSorcery);
		};
		challenge.Rules.OtherDescription = "Blessed by the Void Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "105", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.AccumulatedCasts;
		simpleEffect.add = 500.0;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[AccumulatedAmount]{500}";
		Challenges.Add(challenge);
		challenge = new Challenge("Wizard Squad", 20);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e66", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: true, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			Progress.SetValue(0.0);
			Squad = new WizardSquadChallenge();
			Squad.Init(challenge, Progress.Value.ToInt());
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(Squad.CheckCondition));
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(-0.4, 1.0);
		};
		challenge.Rules.Remove = delegate
		{
			Progress.SetValue(0.0);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, new Action(Squad.CheckCondition));
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(0.4, 1.0);
		};
		challenge.Rules.OtherDescription = "Wizard Squad Description";
		challenge.Objectives = new List<ConditionUnlock>
		{
			new ConditionUnlockSpellUse(Spells.ForceOfNature, "10"),
			new ConditionUnlockSpellUse(Spells.GobletOfFire, "10"),
			new ConditionUnlockSpellUse(Spells.DreadedScriptOfHarvest, "10"),
			new ConditionUnlockSpellUse(Spells.KarnaphensSpellshroud, "10"),
			new ConditionUnlockSpellUse(Spells.PrimalPower, "10"),
			new ConditionUnlockSpellUse(Spells.EbonTruncheon, "10"),
			new ConditionUnlockSpellUse(Spells.HallowedWritings, "4")
		};
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.LevelReduction, 1.0, 1.0));
		challenge.RewardDescription = "[IncreaseLevelReduction]{1}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Wizard Squad] II", 120, Challenges.Last());
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: true, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			on_action = delegate
			{
				if ((GameManager.Instance.CurrentPet.Pet == null) ? (GameManager.Instance.CurrentHero.PlayedTime.ValueInt >= 300) : (GameManager.Instance.CurrentPet.PlayedTime.ValueInt >= 300))
				{
					BigNumber exp = ((GameManager.Instance.CurrentPet.Pet == null) ? ((BigNumber)0.0) : GameManager.Instance.CurrentPet.Pet.TotalExp.Value);
					List<PetChoose> list2 = GameManager.Instance.CurrentPet.PetPanel.Pets.Where((PetChoose x) => x.Pet != GameManager.Instance.CurrentPet.Pet && x.Pet.NameKey <= PetNames.Ebonsand && x.Pet.NameKey != PetNames.Archivist && x.Pet.NameKey != PetNames.HolySpirit).ToList();
					list2[UnityEngine.Random.Range(0, list2.Count)].SetPet();
					GameManager.Instance.CurrentPet.Pet.AddExpConst(exp);
				}
			};
			Squad = new WizardSquadChallenge();
			Progress.SetValue(0.0);
			Squad.Init(challenge, Progress.Value.ToInt());
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(Squad.CheckCondition));
			VariableLong timeSession2 = Statistic.TimeSession;
			timeSession2.OnChange = (Action)Delegate.Combine(timeSession2.OnChange, on_action);
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(-0.3, 1.0);
		};
		challenge.Rules.Remove = delegate
		{
			Progress.SetValue(0.0);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, on_action);
			VariableLong timeSession2 = Statistic.TimeSession;
			timeSession2.OnChange = (Action)Delegate.Remove(timeSession2.OnChange, new Action(Squad.CheckCondition));
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(0.3, 1.0);
		};
		challenge.Rules.OtherDescription = "Wizard Squad II Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "110", "Reach Character level:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.LevelReduction, 1.0, 1.0));
		challenge.RewardDescription = "[IncreaseLevelReduction]{1}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Wizard Squad] III", 220, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Chronomancer, 0, "Unlock Chronomancer:"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Umbramancer, 0, "Unlock Umbramancer:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: true, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			on_action = delegate
			{
				if ((GameManager.Instance.CurrentPet.Pet == null) ? (GameManager.Instance.CurrentHero.PlayedTime.ValueInt >= 300) : (GameManager.Instance.CurrentPet.PlayedTime.ValueInt >= 300))
				{
					BigNumber exp = ((GameManager.Instance.CurrentPet.Pet == null) ? ((BigNumber)0.0) : GameManager.Instance.CurrentPet.Pet.TotalExp.Value);
					List<PetChoose> list2 = GameManager.Instance.CurrentPet.PetPanel.Pets.Where((PetChoose x) => x.Pet != GameManager.Instance.CurrentPet.Pet && x.Pet.NameKey <= PetNames.Ebonsand && x.Pet.NameKey != PetNames.Archivist && x.Pet.NameKey != PetNames.HolySpirit).ToList();
					list2[UnityEngine.Random.Range(0, list2.Count)].SetPet();
					GameManager.Instance.CurrentPet.Pet.AddExpConst(exp);
				}
			};
			Progress.SetValue(0.0);
			Squad = new WizardSquadChallenge();
			Squad.Init2(challenge, Progress.Value.ToInt());
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(Squad.CheckCondition));
			VariableLong timeSession2 = Statistic.TimeSession;
			timeSession2.OnChange = (Action)Delegate.Combine(timeSession2.OnChange, on_action);
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(-0.2, 1.0);
		};
		challenge.Rules.Remove = delegate
		{
			Progress.SetValue(0.0);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, on_action);
			VariableLong timeSession2 = Statistic.TimeSession;
			timeSession2.OnChange = (Action)Delegate.Remove(timeSession2.OnChange, new Action(Squad.CheckCondition));
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(0.2, 1.0);
		};
		challenge.Rules.OtherDescription = "Wizard Squad III Description";
		challenge.Objectives = new List<ConditionUnlock>
		{
			new ConditionUnlockSpellUse(Spells.ForceOfNature, "10"),
			new ConditionUnlockSpellUse(Spells.HellStorm, "10"),
			new ConditionUnlockSpellUse(Spells.VoraciousPlague, "10"),
			new ConditionUnlockSpellUse(Spells.KarnaphensSpellshroud, "10"),
			new ConditionUnlockSpellUse(Spells.LeyOverdrive, "10"),
			new ConditionUnlockSpellUse(Spells.RealityWarping, "10"),
			new ConditionUnlockSpellUse(Spells.SpiritOfValor, "5"),
			new ConditionUnlockSpellUse(Spells.Superposition, "10"),
			new ConditionUnlockSpellUse(Spells.UmbralRage, "10"),
			new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "120", "Reach Character level:")
		};
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.LevelReduction, 1.0, 1.0));
		challenge.RewardDescription = "[IncreaseLevelReduction]{1}";
		Challenges.Add(challenge);
		challenge = new Challenge("Insatiable Hunger", 21);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e69", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Devourer, 0, "Unlock Devourer:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: true);
		challenge.Rules.Other = delegate
		{
			SetPet(PetNames.Devourer);
			on_action = delegate
			{
				int valueInt = GameManager.Instance.CurrentPet.Level.ValueInt;
				foreach (BuildingVisual building in GameManager.Instance.Buildings)
				{
					int num = Mathf.Min(building.building.Level.ValueInt, valueInt);
					if (num > 0)
					{
						building.building.Level.Change(-num);
						Statistic.TotalBuildings.Change(-num);
						GameManager.Instance.CurrentPet.Pet.AddExp(num * 5);
					}
				}
			};
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, on_action);
		};
		challenge.Rules.Remove = delegate
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, on_action);
		};
		challenge.Rules.OtherDescription = "Insatiable Hunger Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "90", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Insatiable Hunger] II", 121, Challenges.Last());
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: true);
		challenge.Rules.Other = delegate
		{
			SetPet(PetNames.Devourer);
			BuildingVisual buildingVisual = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.building.Tier == 1);
			if (buildingVisual.building.Level.ValueInt == 0)
			{
				buildingVisual.building.Level.SetValue(150);
				Statistic.TotalBuildings.Change(150);
			}
			buildingVisual.CheckEnable();
			buildingVisual.Recalculate();
			on_action = delegate
			{
				int valueInt = GameManager.Instance.CurrentPet.Level.ValueInt;
				foreach (BuildingVisual building2 in GameManager.Instance.Buildings)
				{
					int num = Mathf.Min(building2.building.Level.ValueInt, valueInt);
					if (num > 0)
					{
						building2.building.Level.Change(-num);
						Statistic.TotalBuildings.Change(-num);
						GameManager.Instance.CurrentPet.Pet.AddExp(num * 5);
					}
				}
				if (GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == 1).building.Level.ValueInt < 100)
				{
					FailChallenge();
				}
			};
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, on_action);
		};
		challenge.Rules.Remove = delegate
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, on_action);
		};
		challenge.Rules.OtherDescription = "Insatiable Hunger II Description";
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0));
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Excommunicated", 22);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e72", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Exorcist, 0, "Unlock Exorcist:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: true, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			Exorcist obj = GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Exorcist).Hero as Exorcist;
			obj.SpellList.Add(Spells.VoidPrison);
			obj.SpellList.Add(Spells.VoidSyphon);
			obj.SpellList.Add(Spells.EbonTruncheon);
			obj.SpellList.Add(Spells.VoidRadiance);
			obj.SpellList.Add(Spells.VoidAutomaton);
			obj.SpellList.Add(Spells.SyntheticEntity);
			SetHero(HeroesNames.Exorcist);
			GameManager.Instance.VoidManaManager.VoidCore.SpawnRate = 1f;
			GameManager.Instance.BonusSpawner.Restart();
			GameManager.Instance.VoidManaManager.Income.SetValue(1.0);
			Gallery.Set(HeroesNames.Exorcist, "6#2");
		};
		challenge.Rules.Remove = delegate
		{
			Exorcist obj = GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Exorcist).Hero as Exorcist;
			obj.SpellList.Remove(Spells.VoidPrison);
			obj.SpellList.Remove(Spells.VoidSyphon);
			obj.SpellList.Remove(Spells.EbonTruncheon);
			obj.SpellList.Remove(Spells.VoidRadiance);
			obj.SpellList.Remove(Spells.VoidAutomaton);
			obj.SpellList.Remove(Spells.SyntheticEntity);
			Gallery.RefreshFrame();
		};
		challenge.Rules.OtherDescription = "Excommunicated Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.BoughtUpgrades, "400", "Total upgrades acquired:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 6.0, 1.0));
		challenge.RewardDescription = "[AttributeRewards]{6}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Excommunicated] II", 122, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Voidterror, 0, "Unlock Voidterror:"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: false, vip_available: true, null, statistic: false);
		Action<bool> onHCExom = delegate
		{
			GameManager.Instance.CurrentPet.Pet.AddExp(1000.0);
		};
		challenge.Bonus = "1e80";
		challenge.Rules.Other = delegate
		{
			Exorcist obj = GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Exorcist).Hero as Exorcist;
			SetHero(HeroesNames.Exorcist);
			SetPet(PetNames.Voidterror);
			GameManager.Instance.VoidManaManager.VoidCore.SpawnRate = 1f;
			GameManager.Instance.BonusSpawner.Restart();
			GameManager.Instance.VoidManaManager.Income.SetValue(1.0);
			MegaClick mc = obj.mc;
			mc.onMega = (Action<bool>)Delegate.Combine(mc.onMega, onHCExom);
			Gallery.Set(HeroesNames.Exorcist, "6#2");
		};
		challenge.Rules.Remove = delegate
		{
			MegaClick mc = (GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Exorcist).Hero as Exorcist).mc;
			mc.onMega = (Action<bool>)Delegate.Remove(mc.onMega, onHCExom);
			Gallery.RefreshFrame();
		};
		challenge.Rules.OtherDescription = "Excommunicated II Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.BoughtUpgrades, "500", "Total upgrades acquired:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameContext.GetResource("Exorcist.MegaProfit"), 0.0, 2.0));
		challenge.RewardDescription = "[IncreaseHCProfit]{100%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Excommunicated] III", 222, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Voidterror, 0, "Unlock Voidterror:"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: false, vip_available: true, null, statistic: false);
		challenge.Bonus = "1e90";
		challenge.Rules.Other = delegate
		{
			Exorcist obj = GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Exorcist).Hero as Exorcist;
			obj.SpellList.Add(Spells.VoidPrison);
			obj.SpellList.Add(Spells.VoidSyphon);
			obj.SpellList.Add(Spells.EbonTruncheon);
			obj.SpellList.Add(Spells.VoidRadiance);
			obj.SpellList.Add(Spells.VoidAutomaton);
			obj.SpellList.Add(Spells.SyntheticEntity);
			obj.SpellList.Add(Spells.VoidElemental);
			SetHero(HeroesNames.Exorcist);
			SetPet(PetNames.Voidterror);
			GameManager.Instance.VoidManaManager.VoidCore.SpawnRate = 1f;
			GameManager.Instance.BonusSpawner.Restart();
			GameManager.Instance.VoidManaManager.Income.SetValue(1.0);
			MegaClick mc = obj.mc;
			mc.onMega = (Action<bool>)Delegate.Combine(mc.onMega, onHCExom);
			Gallery.Set(HeroesNames.Exorcist, "6#2");
		};
		challenge.Rules.Remove = delegate
		{
			Exorcist obj = GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Exorcist).Hero as Exorcist;
			MegaClick mc = obj.mc;
			mc.onMega = (Action<bool>)Delegate.Remove(mc.onMega, onHCExom);
			obj.SpellList.Remove(Spells.VoidPrison);
			obj.SpellList.Remove(Spells.VoidSyphon);
			obj.SpellList.Remove(Spells.EbonTruncheon);
			obj.SpellList.Remove(Spells.VoidRadiance);
			obj.SpellList.Remove(Spells.VoidAutomaton);
			obj.SpellList.Remove(Spells.SyntheticEntity);
			obj.SpellList.Remove(Spells.VoidElemental);
			Gallery.RefreshFrame();
		};
		challenge.Rules.OtherDescription = "- You start as an Exorcist with Voidterror Pet selected.\n- Each Hallowed clicks earns Pet experience.\n- You have access to Void Mana and some of Voidmancer's Spells.";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.BoughtUpgrades, "560", "Total upgrades acquired:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.action = delegate
		{
			GameManager.Instance.Gallery.Unlock(6, 2);
		};
		challenge.RewardDescription = "Excommunicated III Reward";
		Challenges.Add(challenge);
		challenge = new Challenge("[Vision of the Future] III", 229, Challenges.Find((Challenge x) => x.ID == 129));
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e74", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: false, vip_available: false, null, statistic: false);
		challenge.Bonus = "1e140";
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Chronomancer);
			SetPet(PetNames.Golem);
			Gallery.Set(HeroesNames.Chronomancer, "8#3");
		};
		challenge.Rules.Remove = delegate
		{
			Gallery.RefreshFrame();
		};
		challenge.Rules.OtherDescription = "Vision of the Future III Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockSpellUse(Spells.TimeHelix, "10"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("No Rest for the Wicked", 23);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e75", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.RisenGiant, 0, "Unlock Risen Giant:"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: true, change_pet: false, vip_available: true, null, statistic: true, achieves: true, equipment: true);
		challenge.Bonus = "1e71";
		challenge.Rules.Other = delegate
		{
			SetPet(PetNames.RisenGiant);
			on_action = delegate
			{
				if (GameManager.Instance.Idle.IdleIsActive)
				{
					FailChallenge();
				}
			};
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, on_action);
		};
		challenge.Rules.Remove = delegate
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, on_action);
		};
		challenge.Rules.OtherDescription = "No Rest for the Wicked Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(Statistic.TotalBuildings, "15500", "Reach Total Mana Sources:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Transmutation Effect", 24);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e78", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: true, change_pet: true, vip_available: true, null, statistic: false, achieves: true, equipment: true);
		float time2change = 0f;
		challenge.Rules.Other = delegate
		{
			on_action = delegate
			{
				if (time2change >= 60f)
				{
					List<BuildingVisual> buildings = GameManager.Instance.Buildings;
					int index;
					for (int i = 0; i < 8; i++)
					{
						int valueInt = buildings[i].building.Level.ValueInt;
						index = UnityEngine.Random.Range(0, 8);
						buildings[i].building.Level.SetValue(buildings[index].building.Level.ValueInt);
						buildings[index].building.Level.SetValue(valueInt);
					}
					index = UnityEngine.Random.Range(0, 8);
					buildings[index].building.Level.Change(5);
					Statistic.TotalBuildings.Change(5);
					foreach (BuildingVisual item4 in buildings)
					{
						item4.update_text();
					}
					time2change -= 60f;
				}
				time2change += 1f;
			};
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, on_action);
		};
		challenge.Rules.Remove = delegate
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, on_action);
		};
		challenge.Rules.OtherDescription = "Transmutation Effect Description";
		challenge.Objectives = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Building.1.TotalLevel", "3000", "Highest amount of Mana Gem:"),
			new ConditionUnlockMore("Building.2.TotalLevel", "3000", "Highest amount of Grimoire:"),
			new ConditionUnlockMore("Building.3.TotalLevel", "3000", "Highest amount of Spell Fountain:"),
			new ConditionUnlockMore("Building.4.TotalLevel", "3000", "Highest amount of Enchanted Tree:"),
			new ConditionUnlockMore("Building.5.TotalLevel", "3000", "Highest amount of Alchemy Desk:"),
			new ConditionUnlockMore("Building.6.TotalLevel", "3000", "Highest amount of Circle Of Power:"),
			new ConditionUnlockMore("Building.7.TotalLevel", "3000", "Highest amount of Dimensional Rift:"),
			new ConditionUnlockMore("Building.8.TotalLevel", "3000", "Highest amount of Nexus:")
		};
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Transmutation Effect] II", 124, Challenges.Last());
		challenge.Rules.Other = delegate
		{
			on_action = delegate
			{
				if (time2change >= 30f)
				{
					List<BuildingVisual> buildings = GameManager.Instance.Buildings;
					int index;
					for (int i = 0; i < 8; i++)
					{
						int valueInt = buildings[i].building.Level.ValueInt;
						index = UnityEngine.Random.Range(0, 8);
						buildings[i].building.Level.SetValue(buildings[index].building.Level.ValueInt);
						buildings[index].building.Level.SetValue(valueInt);
					}
					index = UnityEngine.Random.Range(0, 8);
					buildings[index].building.Level.Change(6);
					Statistic.TotalBuildings.Change(6);
					foreach (BuildingVisual item5 in buildings)
					{
						item5.update_text();
					}
					time2change -= 30f;
					GameManager.Instance.VoidMana.SetValue(0.0);
				}
				time2change += 1f;
			};
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, on_action);
		};
		challenge.Rules.Remove = delegate
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, on_action);
		};
		challenge.Rules.OtherDescription = "Transmutation Effect II Description";
		challenge.Objectives = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Building.1.TotalLevel", "3200", "Highest amount of Mana Gem:"),
			new ConditionUnlockMore("Building.2.TotalLevel", "3200", "Highest amount of Grimoire:"),
			new ConditionUnlockMore("Building.3.TotalLevel", "3200", "Highest amount of Spell Fountain:"),
			new ConditionUnlockMore("Building.4.TotalLevel", "3200", "Highest amount of Enchanted Tree:"),
			new ConditionUnlockMore("Building.5.TotalLevel", "3200", "Highest amount of Alchemy Desk:"),
			new ConditionUnlockMore("Building.6.TotalLevel", "3200", "Highest amount of Circle Of Power:"),
			new ConditionUnlockMore("Building.7.TotalLevel", "3200", "Highest amount of Dimensional Rift:"),
			new ConditionUnlockMore("Building.8.TotalLevel", "3200", "Highest amount of Nexus:")
		};
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Transmutation Effect] III", 224, Challenges.Last());
		challenge.Rules.Other = delegate
		{
			on_action = delegate
			{
				if (time2change >= 10f)
				{
					List<BuildingVisual> buildings = GameManager.Instance.Buildings;
					int index;
					for (int i = 0; i < 8; i++)
					{
						int valueInt = buildings[i].building.Level.ValueInt;
						index = UnityEngine.Random.Range(0, 8);
						buildings[i].building.Level.SetValue(buildings[index].building.Level.ValueInt);
						buildings[index].building.Level.SetValue(valueInt);
					}
					index = UnityEngine.Random.Range(0, 8);
					buildings[index].building.Level.Change(12);
					Statistic.TotalBuildings.Change(12);
					foreach (BuildingVisual item6 in buildings)
					{
						item6.update_text();
					}
					time2change -= 10f;
					GameManager.Instance.VoidMana.SetValue(0.0);
					GameManager.Instance.Scrolls.StopAll();
				}
				List<BuildingVisual> list2 = GameManager.Instance.Buildings.FindAll((BuildingVisual x) => x.building.Level.ValueInt > 0);
				if (list2.Count > 0)
				{
					list2[UnityEngine.Random.Range(0, list2.Count)].building.Level.Change(-1);
					Statistic.TotalBuildings.Change(-1);
				}
				time2change += 1f;
			};
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, on_action);
		};
		challenge.Rules.Remove = delegate
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, on_action);
		};
		challenge.Rules.OtherDescription = "Transmutation Effect III Description";
		challenge.Objectives = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Building.1.TotalLevel", "3300", "Highest amount of Mana Gem:"),
			new ConditionUnlockMore("Building.2.TotalLevel", "3300", "Highest amount of Grimoire:"),
			new ConditionUnlockMore("Building.3.TotalLevel", "3300", "Highest amount of Spell Fountain:"),
			new ConditionUnlockMore("Building.4.TotalLevel", "3300", "Highest amount of Enchanted Tree:"),
			new ConditionUnlockMore("Building.5.TotalLevel", "3300", "Highest amount of Alchemy Desk:"),
			new ConditionUnlockMore("Building.6.TotalLevel", "3300", "Highest amount of Circle Of Power:"),
			new ConditionUnlockMore("Building.7.TotalLevel", "3300", "Highest amount of Dimensional Rift:"),
			new ConditionUnlockMore("Building.8.TotalLevel", "3300", "Highest amount of Nexus:")
		};
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Pet Cemetery", 26);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e81", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: true, null, statistic: false, achieves: true, equipment: true);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Necromancer);
			SetPet(PetNames.Devourer);
			GameManager.Instance.Scrolls.ShardsPerClick.Change(0.0, 5.0);
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Scrolls.ShardsPerClick.Change(0.0, 0.20000000298023224);
		};
		challenge.Rules.OtherDescription = "Pet Cemetery Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "100", "Reach Pet level:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Pet Cemetery] II", 126, Challenges.Last());
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: true, null, statistic: false, achieves: true, equipment: true);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Necromancer);
			SetPet(PetNames.Daemon);
			on_cast = delegate(Spell s)
			{
				if (s.Type == SpellTypeGroup.Summoning || s.NameKey == Spells.PlagueZombie)
				{
					GameManager.Instance.CurrentPet.Pet.AddExp(GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == 6).building.Level.Value.Pow(0.5));
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			Gallery.Set(HeroesNames.Necromancer, "5#2");
		};
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
		};
		challenge.Rules.OtherDescription = "Pet Cemetery II Description";
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Pet Cemetery] III", 226, Challenges.Last());
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Necromancer);
			SetPet(PetNames.Voidfiend);
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.Nightfall || s.NameKey == Spells.ENightfall)
				{
					GameManager.Instance.CurrentPet.Pet.AddExp(10000.0);
				}
			};
			GameManager.Instance.CurrentPet.AbilityPower.Change(0.0, 10.0);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			Gallery.Set(HeroesNames.Necromancer, "5#3");
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.CurrentPet.AbilityPower.Change(0.0, 0.10000000149011612);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
		};
		challenge.Rules.OtherDescription = "Pet Cemetery III Description";
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Exam", 28);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e84", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		List<Spells> spell_back_up = new List<Spells>();
		challenge.Rules.Other = delegate
		{
			int count = GameManager.Instance.SpellBook.SpellList.FindAll((Spell x) => x.UseThisRun.Value >= 1.0).Count;
			bonus = new BigNumber(10.0).Pow(count);
			GameManager.Instance.Profit.Change(0.0, bonus);
			spell_back_up = GameManager.Instance.CurrentHero.Hero.SpellList;
			GameManager.Instance.CurrentHero.Hero.SpellList = new List<Spells>();
			SpellBook spellBook = GameManager.Instance.SpellBook;
			foreach (Spell spell2 in spellBook.SpellList)
			{
				if (spell2.UseThisRun.Value < 1.0 && spell2.SubCost == null && spell2.NameKey != Spells.Wormhole && spell2.NameKey != Spells.TimeHelix && spell2.CursedChallenge && !spellBook.Enhancements.IsEnhancement(spell2.NameKey))
				{
					GameManager.Instance.CurrentHero.Hero.SpellList.Add(spell2.NameKey);
				}
			}
			GameManager.Instance.SpellBook.ChangeSpellSet();
			on_cast = delegate
			{
				GameManager.Instance.Profit.Change(0.0, 10.0);
				bonus *= (BigNumber)10.0;
			};
			on_cast_2 = delegate(Spell s)
			{
				GameManager.Instance.CurrentHero.Hero.SpellList.Remove(s.NameKey);
				GameManager.Instance.SpellBook.ChangeSpellSet();
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnCastEnd = (Action<Spell>)Delegate.Combine(scrolls2.OnCastEnd, on_cast_2);
			GameManager.Instance.StartingMana.Change(10000.0);
			GameManager.Instance.LevelReduction.Change(60);
		};
		challenge.Rules.OtherDescription = "Exam Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.StartingMana.Change(-10000.0);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnCastEnd = (Action<Spell>)Delegate.Remove(scrolls2.OnCastEnd, on_cast_2);
			GameManager.Instance.Profit.Change(0.0, 1.0 / bonus);
			GameManager.Instance.LevelReduction.Change(-60);
			GameManager.Instance.CurrentHero.Hero.SpellList = spell_back_up;
			GameManager.Instance.SpellBook.ChangeSpellSet();
		};
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e140", "Earn Mana"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.LevelReduction, 1.0, 1.0));
		challenge.RewardDescription = "[IncreaseLevelReduction]{1}";
		Challenges.Add(challenge);
		challenge = new Challenge("For the Seventh Star", 25, Challenges.Find((Challenge x) => x.ID == 27));
		challenge.Conditions2Unlock = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Base.Souls", "1e85", "Earn mysteries"),
			new ConditionUnlockAchievement(AchievementKey.Druid, 6),
			new ConditionUnlockAchievement(AchievementKey.Necromancer, 6),
			new ConditionUnlockAchievement(AchievementKey.Arcanist, 6),
			new ConditionUnlockAchievement(AchievementKey.Prodigy, 6),
			new ConditionUnlockAchievement(AchievementKey.Exorcist, 4)
		};
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: true, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Exorcist);
			List<Spells> spellList = GameManager.Instance.CurrentHero.Hero.SpellList;
			spellList.Add(Spells.RulesOfNature);
			spellList.Add(Spells.Nightfall);
			spellList.Add(Spells.JAMissileStorm);
			spellList.Add(Spells.TrueSorcery);
			GameManager.Instance.SpellBook.ChangeSpellSet();
		};
		challenge.Rules.Remove = delegate
		{
			List<Spells> spellList = GameManager.Instance.CurrentHero.Hero.SpellList;
			spellList.Remove(Spells.RulesOfNature);
			spellList.Remove(Spells.Nightfall);
			spellList.Remove(Spells.JAMissileStorm);
			spellList.Remove(Spells.TrueSorcery);
		};
		challenge.Rules.OtherDescription = "For the Seventh Star Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "115", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.AccumulatedCasts;
		simpleEffect.add = 700.0;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[AccumulatedAmount]{700}";
		Challenges.Add(challenge);
		challenge = new Challenge("Time Acceleration", 30);
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e88", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Chronomancer);
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Chronomancer).Hero as Chronomancer).counter.maxDistortion.Change(90f);
		};
		challenge.Rules.Remove = delegate
		{
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Chronomancer).Hero as Chronomancer).counter.maxDistortion.Change(-90f);
		};
		challenge.Rules.OtherDescription = "Time Acceleration Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "140", "Reach Pet level:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Critical Rush", 31);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e91", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: true, change_pet: true, vip_available: false, null, statistic: false);
		challenge.EffectRules = new List<IEffect>();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Orb.critRating.crit_rating;
		simpleEffect.add = -2000.0;
		simpleEffect.mult = 0.0;
		simpleEffect.parameter = Statistic.TimeSession;
		challenge.EffectRules.Add(simpleEffect);
		challenge.EffectDescription = new List<string>();
		challenge.EffectDescription.Add("Critical Rush Crit");
		BigNumber crBonus = new BigNumber(10000000.0);
		challenge.Rules.Other = delegate
		{
			GameManager.Instance.Orb.critRating.crit_rating.Change(crBonus, 1.0);
			GameManager.Instance.PPSFromBuildings.Change(0.0, 0.0);
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Orb.critRating.crit_rating.Change(-crBonus, 1.0);
		};
		challenge.Rules.OtherDescription = "Critical Rush Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "116", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Experiment with Anomaly", 32);
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e94", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: false, null, statistic: false, achieves: true, equipment: true);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Prodigy);
			Time.timeScale = 5f;
		};
		challenge.Rules.Remove = delegate
		{
		};
		challenge.Rules.OtherDescription = "Experiment with Anomaly Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "120", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Experiment with Anomaly] II", 132, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e95", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: false, null, statistic: false, achieves: true, equipment: true);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Prodigy);
			Time.timeScale = 15f;
		};
		challenge.Rules.Remove = delegate
		{
		};
		challenge.Rules.OtherDescription = "Experiment with Anomaly II Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "130", "Reach Pet level:"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentHero.StartingLevel;
		simpleEffect.add = 2.0;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[IncreaseStartingLevel]{2}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Experiment with Anomaly] III", 232, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e96", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: false, null, statistic: false, achieves: true, equipment: true);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Prodigy);
			Time.timeScale = 25f;
			GameManager.Instance.Scrolls.CastRateMult.Change(0f, 5f);
			GameManager.Instance.Scrolls.MaxCharge.Change(20);
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Scrolls.CastRateMult.Change(0.0, 0.2);
			GameManager.Instance.Scrolls.MaxCharge.Change(-20);
		};
		challenge.Rules.OtherDescription = "Experiment with Anomaly III Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Spell.SpellCast", "1000000", "Exile Spells cast:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.Scrolls.AccumulatedCasts, 500.0, 1.0));
		challenge.RewardDescription = "[AccumulatedAmount]{500}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Experiment with Anomaly] IV", 332, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e97", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: false, null, statistic: false, achieves: true, equipment: true);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Prodigy);
			Time.timeScale = 50f;
			GameManager.Instance.Scrolls.SpellChargingSpeed.Change(0.0, 20.0);
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Scrolls.SpellChargingSpeed.Change(0.0, 0.05000000074505806);
		};
		challenge.Rules.OtherDescription = "Experiment with Anomaly IV Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("VoidMana.Collect", "1000000", "Exile Void Entities collected:"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentHero.ExpBoost;
		simpleEffect.mult = 1.75;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[ActionsXP]{75%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Exam] II", 128, Challenges.Find((Challenge x) => x.ID == 28));
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e100", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		spell_back_up = new List<Spells>();
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Chronomancer);
			int count = GameManager.Instance.SpellBook.SpellList.FindAll((Spell x) => x.UseThisRun.Value >= 1.0).Count;
			bonus = new BigNumber(10.0).Pow(count);
			GameManager.Instance.Profit.Change(0.0, bonus);
			spell_back_up = GameManager.Instance.CurrentHero.Hero.SpellList;
			GameManager.Instance.CurrentHero.Hero.SpellList = new List<Spells>();
			SpellBook spellBook = GameManager.Instance.SpellBook;
			foreach (Spell spell3 in spellBook.SpellList)
			{
				if (spell3.UseThisRun.Value < 1.0 && spell3.SubCost == null && spell3.NameKey != Spells.Wormhole && spell3.CursedChallenge && !spellBook.Enhancements.IsEnhancement(spell3.NameKey))
				{
					GameManager.Instance.CurrentHero.Hero.SpellList.Add(spell3.NameKey);
				}
			}
			foreach (Spells item7 in spell_back_up)
			{
				if (!GameManager.Instance.CurrentHero.Hero.SpellList.Contains(item7))
				{
					GameManager.Instance.CurrentHero.Hero.SpellList.Add(item7);
				}
			}
			GameManager.Instance.SpellBook.ChangeSpellSet();
			on_cast = delegate
			{
				GameManager.Instance.Profit.Change(0.0, 10.0);
				bonus *= (BigNumber)10.0;
			};
			on_cast_2 = delegate(Spell s)
			{
				GameManager.Instance.CurrentHero.Hero.SpellList.Remove(s.NameKey);
				GameManager.Instance.SpellBook.ChangeSpellSet();
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnCastEnd = (Action<Spell>)Delegate.Combine(scrolls2.OnCastEnd, on_cast_2);
			GameManager.Instance.StartingMana.Change(10000.0);
			GameManager.Instance.LevelReduction.Change(60);
		};
		challenge.Rules.OtherDescription = "Exam II Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.StartingMana.Change(-10000.0);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnCastEnd = (Action<Spell>)Delegate.Remove(scrolls2.OnCastEnd, on_cast_2);
			GameManager.Instance.Profit.Change(0.0, 1.0 / bonus);
			GameManager.Instance.LevelReduction.Change(-60);
			GameManager.Instance.CurrentHero.Hero.SpellList = spell_back_up;
			GameManager.Instance.SpellBook.ChangeSpellSet();
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e150", "Earn Mana"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.LevelReduction, 1.0, 1.0));
		challenge.RewardDescription = "[IncreaseLevelReduction]{1}";
		Challenges.Add(challenge);
		challenge = new Challenge("Time for the Conflux", 33, Challenges.Find((Challenge x) => x.ID == 25));
		challenge.Conditions2Unlock = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Base.Souls", "1e105", "Earn mysteries"),
			new ConditionUnlockAchievement(AchievementKey.Druid, 7),
			new ConditionUnlockAchievement(AchievementKey.Necromancer, 7),
			new ConditionUnlockAchievement(AchievementKey.Arcanist, 7),
			new ConditionUnlockAchievement(AchievementKey.Prodigy, 7),
			new ConditionUnlockAchievement(AchievementKey.Chronomancer, 4)
		};
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: true, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Chronomancer);
			List<Spells> spellList = GameManager.Instance.CurrentHero.Hero.SpellList;
			spellList.Add(Spells.RulesOfNature);
			spellList.Add(Spells.Nightfall);
			spellList.Add(Spells.JAMissileStorm);
			spellList.Add(Spells.TrueSorcery);
			GameManager.Instance.SpellBook.ChangeSpellSet();
		};
		challenge.Rules.Remove = delegate
		{
			List<Spells> spellList = GameManager.Instance.CurrentHero.Hero.SpellList;
			spellList.Remove(Spells.RulesOfNature);
			spellList.Remove(Spells.Nightfall);
			spellList.Remove(Spells.JAMissileStorm);
			spellList.Remove(Spells.TrueSorcery);
		};
		challenge.Rules.OtherDescription = "Time for the Conflux Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "125", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Scrolls.AccumulatedCasts;
		simpleEffect.add = 700.0;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[AccumulatedAmount]{700}";
		Challenges.Add(challenge);
		challenge = new Challenge("Unstable Time", 34, Challenges.Find((Challenge x) => x.ID == 217));
		challenge.Conditions2Unlock = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Base.Souls", "1e110", "Earn mysteries"),
			new ConditionUnlockAchievement(AchievementKey.Chronomancer, 0, "Unlock Chronomancer:")
		};
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: false, null, statistic: false, achieves: true, equipment: true);
		challenge.Rules.Other = delegate
		{
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Chronomancer).Hero as Chronomancer).SpellList.Remove(Spells.Wormhole);
			GameManager.Instance.Scrolls.RecheckSpells();
			on_cast = delegate(Spell s)
			{
				if (UnityEngine.Random.Range(0f, 1f) <= 0.15f)
				{
					List<Spell> list2 = GameManager.Instance.SpellBook.SpellList.Where((Spell x) => !GameManager.Instance.SpellBook.AvailableSpells.Contains(x) && x.SubCost == null && (x.CursedChallenge & (x.NameKey != Spells.Wormhole))).ToList();
					if (list2.Count > 0)
					{
						Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell == s);
						GameManager.Instance.Scrolls.ChoosePanel.Close();
						GameManager.Instance.Scrolls.ChoosePanel.scroll = scroll;
						Spell sp = list2[UnityEngine.Random.Range(0, list2.Count)];
						GameManager.Instance.SpellBook.AddSpell(sp);
						GameManager.Instance.SpellBook.SpellChooses.Find((SpellChoose x) => x.SpellName == sp.NameKey).Choose();
					}
				}
				if (UnityEngine.Random.Range(0f, 1f) <= 0.05f)
				{
					BigNumber exp = ((GameManager.Instance.CurrentPet.Pet == null) ? ((BigNumber)0.0) : GameManager.Instance.CurrentPet.Pet.TotalExp.Value);
					List<PetChoose> list3 = GameManager.Instance.CurrentPet.PetPanel.Pets.Where((PetChoose x) => x.Pet != GameManager.Instance.CurrentPet.Pet && x.Pet.NameKey <= PetNames.Ebonsand).ToList();
					list3[UnityEngine.Random.Range(0, list3.Count)].SetPet();
					GameManager.Instance.CurrentPet.Pet.AddExpConst(exp);
				}
				if (UnityEngine.Random.Range(0f, 1f) <= 0.05f)
				{
					if (GameManager.Instance.CurrentHero.Hero.NameKey != HeroesNames.Prodigy)
					{
						GameManager.Instance.CurrentHero.SetHero(HeroesNames.Prodigy);
					}
					else
					{
						GameManager.Instance.CurrentHero.SetHero(HeroesNames.Chronomancer);
					}
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnPostCast = (Action<Spell>)Delegate.Combine(scrolls.OnPostCast, on_cast);
		};
		challenge.Rules.Remove = delegate
		{
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Chronomancer).Hero as Chronomancer).SpellList.Add(Spells.Wormhole);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnPostCast = (Action<Spell>)Delegate.Remove(scrolls.OnPostCast, on_cast);
		};
		challenge.Rules.OtherDescription = "Unstable Time Description";
		challenge.Objectives = new List<ConditionUnlock>
		{
			new ConditionUnlockSpellUse(Spells.LeyOverdrive, "10"),
			new ConditionUnlockSpellUse(Spells.TimeHelix, "10"),
			new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "120", "Reach Character level:"),
			new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "100", "Reach Pet level:")
		};
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentHero.ExpBoost;
		simpleEffect.mult = 1.75;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[ActionsXP]{75%}";
		Challenges.Add(challenge);
		challenge = new Challenge("Taming Hunger", 35, Challenges.Find((Challenge x) => x.ID == 121));
		challenge.Conditions2Unlock = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Base.Souls", "1e115", "Earn mysteries"),
			new ConditionUnlockAchievement(AchievementKey.Exorcist, 0, "Unlock Exorcist:"),
			new ConditionUnlockAchievement(AchievementKey.Hungerer, 0, "Unlock Hungerer:")
		};
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: true, change_pet: false, vip_available: true, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Exorcist);
			SetPet(PetNames.Hungerer);
			(GameManager.Instance.CurrentPet.PetPanel.Pets.Find((PetChoose x) => x.Pet.NameKey == PetNames.Hungerer).Pet as Hungerer).consumeMod = 20;
			GameManager.Instance.Scrolls.ShardsPassive.Change(0.0, 0.0010000000474974513);
			GameManager.Instance.Scrolls.ShardsPerClick.Change(0.0, 0.0010000000474974513);
			Target.SetValue(Statistic.TotalBuildings.Value);
			on_action = delegate
			{
				if (Target.Value < Statistic.TotalBuildings.Value)
				{
					GameManager.Instance.Scrolls.AddShardsRandom(10f * (Statistic.TotalBuildings.Value - Target.Value).ToFloat());
				}
				Target.SetValue(Statistic.TotalBuildings.Value);
			};
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, on_action);
		};
		challenge.Rules.Remove = delegate
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, on_action);
			GameManager.Instance.Scrolls.ShardsPassive.Change(0.0, 1000.0);
			GameManager.Instance.Scrolls.ShardsPerClick.Change(0.0, 1000.0);
			PetChoose petChoose = GameManager.Instance.CurrentPet.PetPanel.Pets.Find((PetChoose x) => x.Pet.NameKey == PetNames.Hungerer);
			if (petChoose != null)
			{
				(petChoose.Pet as Hungerer).consumeMod = 1;
			}
		};
		challenge.Rules.OtherDescription = "Taming Hunger Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "120", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentHero.ExpBoost;
		simpleEffect.mult = 2.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[ActionsXP]{100%}";
		Challenges.Add(challenge);
		challenge = new Challenge("Gate to the Past", 329, Challenges.Find((Challenge x) => x.ID == 229));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Bonus = "1e200";
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Chronomancer);
			Gallery.Set(HeroesNames.Chronomancer, "8#5");
			Target.SetValue(0.0);
			WarpEffect w = GameManager.Instance.SpellBook.GetSpell(Spells.Wormhole).effects[0] as WarpEffect;
			w.k = GameManager.Instance.SpellBook.GetSpell(Spells.GenerateParadox).UseThisRun.Value + 1.0;
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 20.0);
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.TimeHelix)
				{
					float num = (GameManager.Instance.CurrentHero.SkipedPlayedTime.Value / 31536000.0).ToFloat();
					if (num >= 1980f && num < 1981f)
					{
						Target.SetValue(1.0);
					}
				}
				else if (s.NameKey == Spells.GenerateParadox)
				{
					w.k += (BigNumber)1.0;
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnPreCast = (Action<Spell>)Delegate.Combine(scrolls.OnPreCast, on_cast);
		};
		challenge.Rules.Remove = delegate
		{
			(GameManager.Instance.SpellBook.GetSpell(Spells.Wormhole).effects[0] as WarpEffect).k = 1.0;
			Gallery.RefreshFrame();
			Target.SetValue(0.0);
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 0.20000000298023224);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnPreCast = (Action<Spell>)Delegate.Remove(scrolls.OnPreCast, on_cast);
		};
		challenge.Rules.OtherDescription = "Gate to the Past Description";
		challenge.Objectives = new List<ConditionUnlock>
		{
			new ConditionUnlockVariable(Target, "1", "Gate to the Past Objective")
		};
		challenge.Reward = new ChallengeReward();
		challenge.Reward.action = delegate
		{
			GameManager.Instance.Gallery.Unlock(8, 5);
		};
		challenge.RewardDescription = "Gate to the Past Reward";
		Challenges.Add(challenge);
		challenge = new Challenge("Shadows in Darkness", 36, Challenges.Find((Challenge x) => x.ID == 118));
		challenge.Conditions2Unlock = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Base.Souls", "1e120", "Earn mysteries"),
			new ConditionUnlockAchievement(AchievementKey.Umbramancer, 0, "Unlock Umbramancer:"),
			new ConditionUnlockVariable(Statistic.HeroMaxLevelAllTime, "125", "Reach Character level:")
		};
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.TimeLimit = 600uL;
		challenge.Bonus = "1e120";
		challenge.Rules.Other = delegate
		{
			if (blind == null)
			{
				blind = UnityEngine.Object.Instantiate(Blind, BlindParent).GetComponent<Blind>();
			}
			blind.transform.SetAsFirstSibling();
			blind.gameObject.SetActive(value: true);
			blind.StartBlind(120f);
			blind.StartShowText(limit: true);
			SetHero(HeroesNames.Umbramancer);
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(-0.6, 1.0);
			GameManager.Instance.Scrolls.ShardsPassive.Change(0.0, 5.0);
			(GameManager.Instance.CurrentHero.Hero as Umbramancer).shadowEnergy.ShadowCore.SpawnSpeed.Change(0.0, 2.5);
			onFail = (Action)Delegate.Combine(onFail, new Action(blind.StopBlind));
			onComplete = (Action)Delegate.Combine(onComplete, new Action(blind.StopBlind));
		};
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Scrolls.SpellShardsCostReduction.Change(0.6, 1.0);
			GameManager.Instance.Scrolls.ShardsPassive.Change(0.0, 0.20000000298023224);
			(GameManager.Instance.CurrentHero.Hero as Umbramancer).shadowEnergy.ShadowCore.SpawnSpeed.Change(0.0, 0.4000000059604645);
			onFail = (Action)Delegate.Remove(onFail, new Action(blind.StopBlind));
			onComplete = (Action)Delegate.Remove(onComplete, new Action(blind.StopBlind));
			blind.StopAll();
		};
		challenge.Rules.OtherDescription = "Shadows in Darkness Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "115", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 10.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{10}";
		Challenges.Add(challenge);
		challenge = new Challenge("Hellish Zoo", 37);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e125", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Simulacrum, 0, "Unlock Simulacrum:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Demonologist).Hero as Demonologist).SpellList.Remove(Spells.GemResonance);
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 5.0);
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 0.20000000298023224);
			SetHero(HeroesNames.Demonologist);
			SetPet(PetNames.Simulacrum);
			if (GameManager.Instance.CurrentHero.ClassBonusStacks.Value < 1.0)
			{
				GameManager.Instance.CurrentHero.ClassBonusStacks.SetValue(1.0);
			}
			GameManager.Instance.CurrentPet.ExpBonus.Change(0.0, GameManager.Instance.CurrentHero.ClassBonusStacks.Value);
			Spell prev = GameManager.Instance.SpellBook.GetSpell(Spells.FireBall);
			on_cast = delegate(Spell s)
			{
				if (s.Type == SpellTypeGroup.Evocation && prev.Type == SpellTypeGroup.Incantation)
				{
					GameManager.Instance.CurrentPet.ExpBonus.Change(0.0, 3.5);
					GameManager.Instance.CurrentHero.ClassBonusStacks.Change(0.0, 3.5);
				}
				prev = s;
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
		};
		challenge.Rules.OtherDescription = "Hellish Zoo Description";
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			GameManager.Instance.CurrentPet.ExpBonus.Change(0.0, 1.0 / GameManager.Instance.CurrentHero.ClassBonusStacks.Value);
			GameManager.Instance.CurrentHero.ClassBonusStacks.SetValue(0.0);
			Demonologist obj = GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Demonologist).Hero as Demonologist;
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 0.20000000298023224);
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 5.0);
			obj.SpellList.Add(Spells.GemResonance);
		};
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "666", "Reach Pet level:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.CurrentPet.ExpBonus, 0.0, 2.0));
		challenge.RewardDescription = "[IncreasePetXP]{100%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Hellish Zoo] II", 137, Challenges.Last());
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Demonologist).Hero as Demonologist).SpellList.Remove(Spells.GemResonance);
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 5.0);
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 0.20000000298023224);
			SetHero(HeroesNames.Demonologist);
			SetPet(PetNames.Daemon);
			if (GameManager.Instance.CurrentHero.ClassBonusStacks.Value < 1.0)
			{
				GameManager.Instance.CurrentHero.ClassBonusStacks.SetValue(1.0);
			}
			GameManager.Instance.CurrentPet.ExpBonus.Change(0.0, GameManager.Instance.CurrentHero.ClassBonusStacks.Value);
			Spell prev = GameManager.Instance.SpellBook.GetSpell(Spells.FireBall);
			on_cast = delegate(Spell s)
			{
				if (s.Type == SpellTypeGroup.Evocation)
				{
					if (prev.Type == SpellTypeGroup.Incantation)
					{
						GameManager.Instance.CurrentPet.ExpBonus.Change(0.0, 2.0);
						GameManager.Instance.CurrentHero.ClassBonusStacks.Change(0.0, 2.0);
					}
					if (prev.Type == SpellTypeGroup.Summoning)
					{
						GameManager.Instance.CurrentPet.ExpBonus.Change(0.0, 3.0);
						GameManager.Instance.CurrentHero.ClassBonusStacks.Change(0.0, 3.0);
					}
				}
				prev = s;
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			Gallery.Set(HeroesNames.Demonologist, "1#2");
		};
		challenge.Rules.OtherDescription = "Hellish Zoo II Description";
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			GameManager.Instance.CurrentPet.ExpBonus.Change(0.0, 1.0 / GameManager.Instance.CurrentHero.ClassBonusStacks.Value);
			GameManager.Instance.CurrentHero.ClassBonusStacks.SetValue(0.0);
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Demonologist).Hero as Demonologist).SpellList.Add(Spells.GemResonance);
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 0.20000000298023224);
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 5.0);
		};
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentPet.AbilityPowerT1;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 2.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[IncreasePAPT1]{100%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Hellish Zoo] III", 237, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.PitLord, 0, "Unlock Pit Lord:"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Demonologist).Hero as Demonologist).SpellList.Remove(Spells.GemResonance);
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 5.0);
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 0.20000000298023224);
			SetHero(HeroesNames.Demonologist);
			SetPet(PetNames.PitLord);
			if (GameManager.Instance.CurrentHero.ClassBonusStacks.Value < 1.0)
			{
				GameManager.Instance.CurrentHero.ClassBonusStacks.SetValue(1.0);
			}
			GameManager.Instance.CurrentPet.ExpBonus.Change(0.0, GameManager.Instance.CurrentHero.ClassBonusStacks.Value);
			Spell prev = GameManager.Instance.SpellBook.GetSpell(Spells.FireBall);
			on_cast = delegate(Spell s)
			{
				if (s.Type == SpellTypeGroup.Evocation && prev.Type == SpellTypeGroup.Incantation)
				{
					GameManager.Instance.CurrentPet.ExpBonus.Change(0.0, 2.0);
					GameManager.Instance.CurrentHero.ClassBonusStacks.Change(0.0, 2.0);
				}
				if (s.Type == SpellTypeGroup.Summoning && prev.Type == SpellTypeGroup.Incantation)
				{
					GameManager.Instance.CurrentPet.ExpBonus.Change(0.0, 4.0);
					GameManager.Instance.CurrentHero.ClassBonusStacks.Change(0.0, 4.0);
				}
				prev = s;
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			Gallery.Set(HeroesNames.Demonologist, "1#3");
		};
		challenge.Rules.OtherDescription = "Hellish Zoo III Description";
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			GameManager.Instance.CurrentPet.ExpBonus.Change(0.0, 1.0 / GameManager.Instance.CurrentHero.ClassBonusStacks.Value);
			GameManager.Instance.CurrentHero.ClassBonusStacks.SetValue(0.0);
			(GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Demonologist).Hero as Demonologist).SpellList.Add(Spells.GemResonance);
			GameManager.Instance.Scrolls.SummoningDurationReduction.Change(0.0, 0.20000000298023224);
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 5.0);
		};
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentPet.AbilityPowerT2;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 2.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[IncreasePAPT2]{100%}";
		Challenges.Add(challenge);
		challenge = new Challenge("Balanced Darkness", 38);
		challenge.Conditions2Unlock = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Base.Souls", "1e130", "Earn mysteries"),
			new ConditionUnlockAchievement(AchievementKey.Umbramancer, 0, "Unlock Umbramancer:")
		};
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Umbramancer);
			ShadowEnergyManager sem = (GameManager.Instance.CurrentHero.Hero as Umbramancer).shadowEnergy;
			sem.ShadowCore.AmountPerEntity.Change(0.0, 10.0);
			sem.ShadowCore.SpawnSpeed.Change(0.0, 2.0);
			on_tick = delegate
			{
				if (GameManager.Instance.CurrentHero.ClassBonusStacks.Value > 1.0 && sem.ShadowEnergy.Value < GameManager.Instance.CurrentHero.ClassBonusStacks.Value)
				{
					Debug.Log("spent " + GameManager.Instance.CurrentHero.ClassBonusStacks.Value.ToReadableString());
					Debug.Log("current " + sem.ShadowEnergy.Value.ToReadableString());
					FailChallenge();
				}
			};
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Combine(instance.GameTick, on_tick);
			Gallery.Set(HeroesNames.Umbramancer, "9#1");
		};
		challenge.Rules.Remove = delegate
		{
			GameContext.GetResource("Shadow.BonusIncome").Change(0.0, 0.10000000149011612);
			GameContext.GetResource("Shadow.SpawnSpeed").Change(0.0, 0.5);
			GameManager instance = GameManager.Instance;
			instance.GameTick = (Action<float>)Delegate.Remove(instance.GameTick, on_tick);
		};
		challenge.Rules.OtherDescription = "Balanced Darkness Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.ClassBonusStacks, "1e7", "Total Liquid Shadow spent:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 10.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{10}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Exam] III", 228, Challenges.Find((Challenge x) => x.ID == 128));
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e135", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		spell_back_up = new List<Spells>();
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Umbramancer);
			int count = GameManager.Instance.SpellBook.SpellList.FindAll((Spell x) => x.UseThisRun.Value >= 1.0).Count;
			bonus = new BigNumber(10.0).Pow(count);
			GameManager.Instance.Profit.Change(0.0, bonus);
			spell_back_up = GameManager.Instance.CurrentHero.Hero.SpellList;
			GameManager.Instance.CurrentHero.Hero.SpellList = new List<Spells>();
			SpellBook spellBook = GameManager.Instance.SpellBook;
			foreach (Spell spell4 in spellBook.SpellList)
			{
				if (spell4.UseThisRun.Value < 1.0 && spell4.SubCost == null && spell4.NameKey != Spells.Wormhole && spell4.CursedChallenge && !spellBook.Enhancements.IsEnhancement(spell4.NameKey))
				{
					GameManager.Instance.CurrentHero.Hero.SpellList.Add(spell4.NameKey);
				}
			}
			foreach (Spells item8 in spell_back_up)
			{
				if (!GameManager.Instance.CurrentHero.Hero.SpellList.Contains(item8))
				{
					GameManager.Instance.CurrentHero.Hero.SpellList.Add(item8);
				}
			}
			GameManager.Instance.SpellBook.ChangeSpellSet();
			on_cast = delegate
			{
				GameManager.Instance.Profit.Change(0.0, 10.0);
				bonus *= (BigNumber)10.0;
			};
			on_cast_2 = delegate(Spell s)
			{
				s.Delete();
				GameManager.Instance.CurrentHero.Hero.SpellList.Remove(s.NameKey);
				GameManager.Instance.SpellBook.ChangeSpellSet();
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnPostCast = (Action<Spell>)Delegate.Combine(scrolls2.OnPostCast, on_cast_2);
			GameManager.Instance.StartingMana.Change(10000.0);
			GameManager.Instance.LevelReduction.Change(60);
		};
		challenge.Rules.OtherDescription = "Exam III Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.StartingMana.Change(-10000.0);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnPostCast = (Action<Spell>)Delegate.Remove(scrolls2.OnPostCast, on_cast_2);
			GameManager.Instance.Profit.Change(0.0, 1.0 / bonus);
			GameManager.Instance.LevelReduction.Change(-60);
			GameManager.Instance.CurrentHero.Hero.SpellList = spell_back_up;
			GameManager.Instance.SpellBook.ChangeSpellSet();
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e165", "Earn Mana"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.LevelReduction, 1.0, 1.0));
		challenge.RewardDescription = "[IncreaseLevelReduction]{1}";
		Challenges.Add(challenge);
		challenge = new Challenge("Shadow of the Pyre", 39, Challenges.Find((Challenge x) => x.ID == 33));
		challenge.Conditions2Unlock = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Base.Souls", "1e140", "Earn mysteries"),
			new ConditionUnlockAchievement(AchievementKey.Druid, 8),
			new ConditionUnlockAchievement(AchievementKey.Necromancer, 8),
			new ConditionUnlockAchievement(AchievementKey.Arcanist, 8),
			new ConditionUnlockAchievement(AchievementKey.Prodigy, 8),
			new ConditionUnlockAchievement(AchievementKey.Umbramancer, 4)
		};
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: true, null, statistic: false, achieves: true, equipment: true);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Umbramancer);
			List<Spells> spellList = GameManager.Instance.CurrentHero.Hero.SpellList;
			spellList.Add(Spells.RulesOfNature);
			spellList.Add(Spells.Nightfall);
			spellList.Add(Spells.JAMissileStorm);
			spellList.Add(Spells.TrueSorcery);
			GameManager.Instance.SpellBook.ChangeSpellSet();
			Gallery.Set(HeroesNames.Umbramancer, "9#2");
		};
		challenge.Rules.Remove = delegate
		{
			List<Spells> spellList = GameManager.Instance.CurrentHero.Hero.SpellList;
			spellList.Remove(Spells.RulesOfNature);
			spellList.Remove(Spells.Nightfall);
			spellList.Remove(Spells.JAMissileStorm);
			spellList.Remove(Spells.TrueSorcery);
		};
		challenge.Rules.OtherDescription = "Shadow of the Pyre Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Level, "135", "Reach Character level:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.Scrolls.AccumulatedCasts, 1000.0, 1.0));
		challenge.RewardDescription = "[AccumulatedAmount]{1000}";
		Challenges.Add(challenge);
		challenge = new Challenge("Unholy Crusader", 41);
		challenge.Conditions2Unlock = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Base.Souls", "1e150", "Earn mysteries")
		};
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: false, vip_available: false, null, statistic: false, achieves: true, equipment: true);
		challenge.Bonus = "1e150";
		Action<bool> onHCUnholy = delegate
		{
			GameManager.Instance.CurrentPet.Pet.AddExpConst(new BigNumber(1.026).Pow(Statistic.BoughtUpgrades.ValueInt));
		};
		Action onIBUnholy = delegate
		{
			GameManager.Instance.CurrentPet.Pet.RecalculateLevel(0.0);
		};
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Exorcist);
			SetPet(PetNames.PitLord);
			MegaClick mc = (GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Exorcist).Hero as Exorcist).mc;
			mc.onMega = (Action<bool>)Delegate.Combine(mc.onMega, onHCUnholy);
			Idle idle = GameManager.Instance.Idle;
			idle.OnBreak = (Action)Delegate.Combine(idle.OnBreak, onIBUnholy);
		};
		challenge.Rules.Remove = delegate
		{
			MegaClick mc = (GameManager.Instance.CurrentHero.HeroPanel.Heroes.Find((HeroesChoose x) => x.HeroName == HeroesNames.Exorcist).Hero as Exorcist).mc;
			mc.onMega = (Action<bool>)Delegate.Remove(mc.onMega, onHCUnholy);
			Idle idle = GameManager.Instance.Idle;
			idle.OnBreak = (Action)Delegate.Remove(idle.OnBreak, onIBUnholy);
		};
		challenge.Rules.OtherDescription = "Unholy Crusader Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "100", "Reach Pet level:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0));
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Unholy Traveler", 141, Challenges.Last());
		challenge.Bonus = "1e150";
		Action<Spell> onCTUnholy = delegate(Spell x)
		{
			if (x.SubCost != null)
			{
				GameManager.Instance.CurrentPet.Pet.AddExpConst(new BigNumber(1.02425).Pow(Statistic.BoughtUpgrades.ValueInt));
			}
			if (x.NameKey == Spells.Wormhole)
			{
				onIBUnholy();
			}
		};
		Action onDTReset = delegate
		{
			if (Time.timeScale == 1f)
			{
				onIBUnholy();
			}
		};
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Chronomancer);
			SetPet(PetNames.PitLord);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, onCTUnholy);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, onDTReset);
		};
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, onCTUnholy);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, onDTReset);
		};
		challenge.Rules.OtherDescription = "Unholy Traveler Description";
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Unholy Nomad", 241, Challenges.Last());
		challenge.Bonus = "1e150";
		Action<Spell> onSSUnholy = delegate(Spell x)
		{
			if (!x.ShardsBuilding)
			{
				GameManager.Instance.CurrentPet.Pet.AddExpConst(new BigNumber(1.024).Pow(Statistic.BoughtUpgrades.ValueInt));
			}
		};
		Action onIdleUnholy = delegate
		{
			if (GameManager.Instance.Idle.IdleIsActive)
			{
				GameManager.Instance.CurrentPet.Pet.RecalculateLevel(0.0);
			}
		};
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Umbramancer);
			SetPet(PetNames.PitLord);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, onSSUnholy);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, onIdleUnholy);
		};
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, onSSUnholy);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, onIdleUnholy);
		};
		challenge.Rules.OtherDescription = "Unholy Nomad Description";
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Unholy Scientist", 341, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Alchemist, 0, "Unlock Alchemist:"));
		challenge.Bonus = "1e150";
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Alchemist);
			SetPet(PetNames.PitLord);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, onCTUnholy);
			VariableInt level = GameManager.Instance.CurrentHero.Level;
			level.OnChange = (Action)Delegate.Combine(level.OnChange, onIBUnholy);
		};
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, onCTUnholy);
			VariableInt level = GameManager.Instance.CurrentHero.Level;
			level.OnChange = (Action)Delegate.Remove(level.OnChange, onIBUnholy);
		};
		challenge.Rules.OtherDescription = "Unholy Scientist Description";
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Pest Control", 42);
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e160", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: true, change_pet: true, vip_available: true, null, statistic: false, achieves: true, equipment: true);
		Action mbonCollect = delegate
		{
			Progress.Change(1.00000000001);
		};
		Action<Spell> pcOnCast = delegate(Spell x)
		{
			if (GameManager.Instance.MBSpawner.timeToNext > 8f)
			{
				BigNumber bigNumber = 1.0 + x.FullBuild / 10000.0;
				if (x.SubCost != null)
				{
					bigNumber += (BigNumber)(x.SubCost.Cost / 2000);
				}
				GameManager.Instance.MBSpawner.timeToNext /= bigNumber.ToFloat();
				if (GameManager.Instance.MBSpawner.timeToNext < 8f)
				{
					GameManager.Instance.MBSpawner.timeToNext = 8f;
				}
			}
		};
		challenge.Rules.Other = delegate
		{
			MovableBonusSpawner mBSpawner = GameManager.Instance.MBSpawner;
			mBSpawner.onCollect = (Action)Delegate.Combine(mBSpawner.onCollect, mbonCollect);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, pcOnCast);
			mBSpawner.isChallenge = true;
		};
		challenge.Rules.Remove = delegate
		{
			MovableBonusSpawner mBSpawner = GameManager.Instance.MBSpawner;
			mBSpawner.onCollect = (Action)Delegate.Remove(mBSpawner.onCollect, mbonCollect);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, pcOnCast);
			mBSpawner.bonus.Give();
			mBSpawner.InitTimer();
			mBSpawner.isChallenge = false;
			Progress.SetValue(0.0);
		};
		challenge.Rules.OtherDescription = "Pest Control Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(Progress, "100", "Catch Bats:"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.MBSpawner.SpawnRate;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 1.05;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[BatsRate]{5%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Pest Control] II", 142, Challenges.Last());
		Action<Spell> pc2OnCast = delegate(Spell x)
		{
			if (GameManager.Instance.MBSpawner.bonus.speed > 3f)
			{
				BigNumber bigNumber = 1.0 + x.FullBuild / 10000.0;
				if (x.SubCost != null)
				{
					bigNumber += (BigNumber)(x.SubCost.Cost / 1000);
				}
				if (bigNumber > 2.0)
				{
					bigNumber = 2.0;
				}
				GameManager.Instance.MBSpawner.bonus.speed /= bigNumber.ToFloat();
				if (GameManager.Instance.MBSpawner.bonus.speed < 3f)
				{
					GameManager.Instance.MBSpawner.bonus.speed = 3f;
				}
			}
		};
		Action pc2OnTick = delegate
		{
			if (GameManager.Instance.MBSpawner.bonus.speed < 60f)
			{
				GameManager.Instance.MBSpawner.bonus.speed *= 1.02f;
				GameManager.Instance.MBSpawner.bonus.speed += 0.5f;
			}
			if (GameManager.Instance.MBSpawner.timeToNext > 2f)
			{
				GameManager.Instance.MBSpawner.timeToNext = 2f;
			}
		};
		challenge.Rules.Other = delegate
		{
			MovableBonusSpawner mBSpawner = GameManager.Instance.MBSpawner;
			mBSpawner.bonus.GlobalSpeed = 20f;
			mBSpawner.onCollect = (Action)Delegate.Combine(mBSpawner.onCollect, mbonCollect);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, pc2OnCast);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, pc2OnTick);
			mBSpawner.isChallenge = true;
		};
		challenge.Rules.Remove = delegate
		{
			MovableBonusSpawner mBSpawner = GameManager.Instance.MBSpawner;
			mBSpawner.onCollect = (Action)Delegate.Remove(mBSpawner.onCollect, mbonCollect);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, pc2OnCast);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, pc2OnTick);
			mBSpawner.bonus.GlobalSpeed = 1f;
			mBSpawner.bonus.Give();
			mBSpawner.InitTimer();
			mBSpawner.isChallenge = false;
			Progress.SetValue(0.0);
		};
		challenge.Rules.OtherDescription = "Pest Control II Description";
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.MBSpawner.SpawnRate;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 1.05;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[BatsRate]{5%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Pest Control] III", 242, Challenges.Last());
		Action<Spell> pc3OnCast = delegate(Spell x)
		{
			if (GameManager.Instance.MBSpawner.timeToNext > 8f)
			{
				BigNumber bigNumber = 1.0 + x.FullBuild / 10000.0;
				if (x.SubCost != null)
				{
					bigNumber += (BigNumber)(x.SubCost.Cost / 2000);
				}
				GameManager.Instance.MBSpawner.timeToNext /= bigNumber.ToFloat();
				if (GameManager.Instance.MBSpawner.timeToNext < 8f)
				{
					GameManager.Instance.MBSpawner.timeToNext = 8f;
				}
			}
			if (GameManager.Instance.MBSpawner.bonus.speed > 3f)
			{
				BigNumber bigNumber2 = 1.0 + x.FullBuild / 5000.0;
				if (x.SubCost != null)
				{
					bigNumber2 += (BigNumber)(x.SubCost.Cost / 1000);
				}
				if (bigNumber2 > 2.0)
				{
					bigNumber2 = 2.0;
				}
				GameManager.Instance.MBSpawner.bonus.speed /= bigNumber2.ToFloat();
				if (GameManager.Instance.MBSpawner.bonus.speed < 3f)
				{
					GameManager.Instance.MBSpawner.bonus.speed = 3f;
				}
			}
		};
		Action pc3OnTick = delegate
		{
			if (GameManager.Instance.MBSpawner.bonus.speed < 60f)
			{
				GameManager.Instance.MBSpawner.bonus.speed *= 1.02f;
				GameManager.Instance.MBSpawner.bonus.speed += 0.5f;
			}
		};
		challenge.Rules.Other = delegate
		{
			MovableBonusSpawner mBSpawner = GameManager.Instance.MBSpawner;
			mBSpawner.bonus.GlobalSpeed = 20f;
			mBSpawner.onCollect = (Action)Delegate.Combine(mBSpawner.onCollect, mbonCollect);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, pc3OnCast);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, pc3OnTick);
			mBSpawner.isChallenge = true;
			SetHero(HeroesNames.Arcanist);
		};
		challenge.Rules.Remove = delegate
		{
			MovableBonusSpawner mBSpawner = GameManager.Instance.MBSpawner;
			mBSpawner.onCollect = (Action)Delegate.Remove(mBSpawner.onCollect, mbonCollect);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, pc3OnCast);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, pc3OnTick);
			mBSpawner.bonus.GlobalSpeed = 1f;
			mBSpawner.bonus.Give();
			mBSpawner.InitTimer();
			mBSpawner.isChallenge = false;
			Progress.SetValue(0.0);
		};
		challenge.Rules.OtherDescription = "Pest Control III Description";
		challenge.Reward = new ChallengeReward();
		challenge.Reward.action = delegate
		{
			GameManager.Instance.Gallery.Unlock(4, 5);
		};
		challenge.RewardDescription = "Pest Control III Reward";
		Challenges.Add(challenge);
		challenge = new Challenge("Secret Recipe", 43);
		challenge.Conditions2Unlock = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Base.Souls", "1e170", "Earn mysteries"),
			new ConditionUnlockAchievement(AchievementKey.Alchemist, 0, "Unlock Alchemist:")
		};
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: true, vip_available: true, null, statistic: true, achieves: true, equipment: true);
		challenge.Rules.Other = delegate
		{
			Target.SetValue(0.0);
			SetHero(HeroesNames.Alchemist);
			UnityEngine.Object.Instantiate(Resources.Load<SecretRecipe>("SecretRecipe"), GameManager.Instance.transform);
		};
		challenge.Rules.Remove = delegate
		{
			UnityEngine.Object.Destroy(GameManager.Instance.transform.GetComponentInChildren<SecretRecipe>().gameObject);
		};
		challenge.Rules.OtherDescription = "Secret Recipe Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(Target, "1", "Secret Recipe Objective"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Golden Sword", 44);
		challenge.Conditions2Unlock = new List<ConditionUnlock>
		{
			new ConditionUnlockMore("Base.Souls", "1e200", "Earn mysteries"),
			new ConditionUnlockAchievement(AchievementKey.Ironsoul, 0, "Unlock Ironsoul:")
		};
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: true, null, statistic: true, achieves: true, equipment: true);
		challenge.Bonus = "1e200";
		Action<Spell> gsOnPostCast = delegate(Spell x)
		{
			if (x.Type == SpellTypeGroup.Evocation)
			{
				if (GameManager.Instance.Mana.Value >= "1e390")
				{
					Target.Change(1.0);
				}
				else
				{
					FailChallenge();
				}
			}
		};
		challenge.Rules.Other = delegate
		{
			Target.SetValue(0.0);
			SetHero(HeroesNames.Ironsoul);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnPostCast = (Action<Spell>)Delegate.Combine(scrolls.OnPostCast, gsOnPostCast);
		};
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnPostCast = (Action<Spell>)Delegate.Remove(scrolls.OnPostCast, gsOnPostCast);
			Target.SetValue(0.0);
		};
		challenge.Rules.OtherDescription = "Golden Sword Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(Target, "1", "Golden Sword Objective"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Stolen Pet", 45);
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e220", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: true, null, statistic: false, achieves: true, equipment: true);
		challenge.Rules.Other = delegate
		{
			Target.SetValue(0.0);
			SetHero(HeroesNames.Demonologist);
			SetPet(PetNames.Doppelganger);
		};
		challenge.Rules.Remove = delegate
		{
		};
		challenge.Rules.OtherDescription = "Stolen Pet Description";
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Hero.Level, "130", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Stolen Pet] II", 145, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Abolisher, 0, "Unlock Abolisher:"));
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Abolisher);
			SetPet(PetNames.AnimaConstruct);
		};
		challenge.Rules.Remove = delegate
		{
		};
		challenge.Rules.OtherDescription = "Stolen Pet II Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Hero.Level, "135", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Stolen Pet] III", 245, Challenges.Last());
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Abolisher);
			SetPet(PetNames.PitLord);
		};
		challenge.Rules.Remove = delegate
		{
		};
		challenge.Rules.OtherDescription = "Stolen Pet III Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Hero.Level, "140", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.effect = new SimpleEffect(GameManager.Instance.AttributeManager.BonusPoints, 5.0, 1.0);
		challenge.RewardDescription = "[AttributeRewards]{5}";
		Challenges.Add(challenge);
		challenge = new Challenge("Handmade Copy", 46, Challenges.Last());
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockVariable(Statistic.GetEdustTotal(), "1e4", "Total Enchanting dust collected:"));
		Action<Spell> hcOnCast = delegate(Spell x)
		{
			float num = (float)(x.FullBuild / 12000.0);
			GameManager.Instance.CurrentPet.Pet.AddExp(GameManager.Instance.CurrentHero.HeroExp.Value.Pow(0.05f + num));
		};
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Prodigy);
			SetPet(PetNames.Doppelganger);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, hcOnCast);
		};
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, hcOnCast);
		};
		challenge.Rules.OtherDescription = "Handmade Copy Description";
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentHero.Hero.Level, "140", "Reach Character level:"));
		challenge.Reward = new ChallengeReward();
		challenge.Reward.action = delegate
		{
			GameManager.Instance.Gallery.Unlock(3, 6);
		};
		challenge.RewardDescription = "Handmade Copy Reward";
		Challenges.Add(challenge);
		challenge = new Challenge("[Exam] IV", 328, Challenges.Find((Challenge x) => x.ID == 228));
		challenge.Conditions2Unlock = new List<ConditionUnlock>();
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e240", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		spell_back_up = new List<Spells>();
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Abolisher);
			int count = GameManager.Instance.SpellBook.SpellList.FindAll((Spell x) => x.UseThisRun.Value >= 1.0).Count;
			bonus = new BigNumber(10.0).Pow(count);
			GameManager.Instance.Profit.Change(0.0, bonus);
			spell_back_up = GameManager.Instance.CurrentHero.Hero.SpellList;
			GameManager.Instance.CurrentHero.Hero.SpellList = new List<Spells>();
			SpellBook spellBook = GameManager.Instance.SpellBook;
			foreach (Spell spell5 in spellBook.SpellList)
			{
				if (spell5.UseThisRun.Value < 1.0 && spell5.SubCost == null && spell5.NameKey != Spells.Wormhole && spell5.CursedChallenge && !spellBook.Enhancements.IsEnhancement(spell5.NameKey))
				{
					GameManager.Instance.CurrentHero.Hero.SpellList.Add(spell5.NameKey);
				}
			}
			foreach (Spells item9 in spell_back_up)
			{
				if (!GameManager.Instance.CurrentHero.Hero.SpellList.Contains(item9))
				{
					GameManager.Instance.CurrentHero.Hero.SpellList.Add(item9);
				}
			}
			GameManager.Instance.SpellBook.ChangeSpellSet();
			on_cast = delegate
			{
				GameManager.Instance.Profit.Change(0.0, 10.0);
				bonus *= (BigNumber)10.0;
			};
			on_cast_2 = delegate(Spell s)
			{
				GameManager.Instance.CurrentHero.Hero.SpellList.Remove(s.NameKey);
				GameManager.Instance.SpellBook.ChangeSpellSet();
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnCastEnd = (Action<Spell>)Delegate.Combine(scrolls2.OnCastEnd, on_cast_2);
			GameManager.Instance.StartingMana.Change(10000.0);
			GameManager.Instance.LevelReduction.Change(50);
		};
		challenge.Rules.OtherDescription = "Exam IV Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.StartingMana.Change(-10000.0);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnCastEnd = (Action<Spell>)Delegate.Remove(scrolls2.OnCastEnd, on_cast_2);
			GameManager.Instance.Profit.Change(0.0, 1.0 / bonus);
			GameManager.Instance.LevelReduction.Change(-50);
			GameManager.Instance.CurrentHero.Hero.SpellList = spell_back_up;
			GameManager.Instance.SpellBook.ChangeSpellSet();
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e200", "Earn Mana"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.LevelReduction;
		simpleEffect.add = 2.0;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[IncreaseLevelReduction]{2}";
		Challenges.Add(challenge);
		challenge = new Challenge("Army of Two", 47);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e250", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			GameManager.Instance.LevelReduction.Change(120);
			SetHero(HeroesNames.Abolisher);
			float base_bonus = 1.1f;
			Spell counter = GameManager.Instance.SpellBook.GetSpell(Spells.CounterSpell);
			bonus = new BigNumber(base_bonus).Pow(counter.UseThisRun.Value.ToInt());
			GameManager.Instance.Scrolls.SummoningEfficiency.Change(0.0, bonus);
			Spell prev = GameManager.Instance.SpellBook.GetSpell(Spells.CounterSpell);
			List<Scroll> scrolls = GameManager.Instance.Scrolls.Scrolls;
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.CounterSpell)
				{
					if (prev.NameKey != s.NameKey && prev.Type == SpellTypeGroup.Incantation)
					{
						GameManager.Instance.Scrolls.SummoningEfficiency.Change(0.0, base_bonus);
						bonus *= base_bonus;
					}
					else
					{
						for (int i = 0; i < scrolls.Count; i++)
						{
							if (scrolls[i].spell != null && scrolls[i].spell != s)
							{
								scrolls[i].StopAction();
								scrolls[i].spell.ResetShards();
							}
						}
						GameManager.Instance.Scrolls.CheckFilledList();
						counter.UseThisRun.Change(-1.0);
						if (Statistic.CastSpell.Value >= 1.0)
						{
							Statistic.CastSpell.Change(-1.0);
						}
					}
				}
				prev = s;
			};
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnCast = (Action<Spell>)Delegate.Combine(scrolls2.OnCast, on_cast);
		};
		challenge.Rules.OtherDescription = "Army of Two Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.LevelReduction.Change(-120);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			GameManager.Instance.Scrolls.SummoningEfficiency.Change(0.0, 1.0 / bonus);
		};
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e150", "Earn Mana"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.CurrentPet.ExpBonus, 0.0, 1.5));
		challenge.RewardDescription = "[IncreasePetXP]{50%}";
		Challenges.Add(challenge);
		challenge = new Challenge("Children of the Night", 48);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e260", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Arcanist);
			Gallery.Set(HeroesNames.Arcanist, "4#0");
			GameManager.Instance.CurrentHero.Hero.SpellList.Add(Spells.Nightfall);
			Spell nf = GameManager.Instance.SpellBook.GetSpell(Spells.Nightfall);
			nf.PassiveEffect = null;
			GameManager.Instance.SpellBook.ChangeSpellSet();
			float base_bonus = 1.1f;
			nf.level_req -= 100;
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.Nightfall)
				{
					bonus = new BigNumber(base_bonus).Pow(Convert.ToInt32(GameManager.Instance.CurrentHero.ClassBonusStacks.Value.ToDouble()));
					GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, bonus);
					Gallery.Set(HeroesNames.Arcanist, "4#6");
				}
				else if (nf.active && s.Type == SpellTypeGroup.Evocation)
				{
					GameManager.Instance.CurrentHero.ClassBonusStacks.Change(1.0);
					bonus *= base_bonus;
					GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, base_bonus);
				}
			};
			on_cast_2 = delegate(Spell s)
			{
				if (s.NameKey == Spells.Nightfall)
				{
					GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, 1.0 / bonus);
					bonus = 1.0;
					Gallery.Set(HeroesNames.Arcanist, "4#0");
				}
			};
			on_action = delegate
			{
				Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == Spells.Nightfall);
				if (scroll != null)
				{
					scroll.AddProgressBuild(720.0, text: false, shards: false);
				}
			};
			VariableInt batsExile = Statistic.BatsExile;
			batsExile.OnChange = (Action)Delegate.Combine(batsExile.OnChange, on_action);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnPostCast = (Action<Spell>)Delegate.Combine(scrolls2.OnPostCast, on_cast_2);
		};
		challenge.Rules.OtherDescription = "Children of the Night Description";
		challenge.Rules.Remove = delegate
		{
			VariableInt batsExile = Statistic.BatsExile;
			batsExile.OnChange = (Action)Delegate.Remove(batsExile.OnChange, on_action);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnPostCast = (Action<Spell>)Delegate.Remove(scrolls2.OnPostCast, on_cast_2);
			GameManager.Instance.Scrolls.StopAll();
			GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, 1.0 / bonus);
			bonus = 1.0;
			GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.Nightfall).level_req += 100;
			GameManager.Instance.CurrentHero.ClassBonusStacks.SetValue(0.0);
			GameManager.Instance.CurrentHero.Hero.SpellList.Remove(Spells.Nightfall);
			GameManager.Instance.SpellBook.ChangeSpellSet();
			GameManager.Instance.SpellBook.GetSpell(Spells.Nightfall).PassiveEffect = GameManager.Instance.SpellBook.persistentPassive[Spells.Nightfall];
		};
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e500", "Earn Mana"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentHero.ExpBoost;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 1.5;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[ActionsXP]{50%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Children of the Night] II", 148, Challenges.Last());
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Prodigy);
			Gallery.Set(HeroesNames.Prodigy, "3#0");
			GameManager.Instance.CurrentHero.Hero.SpellList.Add(Spells.Nightfall);
			Spell nf = GameManager.Instance.SpellBook.GetSpell(Spells.Nightfall);
			nf.PassiveEffect = null;
			GameManager.Instance.SpellBook.ChangeSpellSet();
			float base_bonus = 1.2f;
			nf.level_req -= 100;
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.Nightfall)
				{
					bonus = new BigNumber(base_bonus).Pow(Convert.ToInt32(GameManager.Instance.CurrentHero.ClassBonusStacks.Value.ToDouble()));
					GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, bonus);
					Gallery.Set(HeroesNames.Prodigy, "3#4");
				}
				else if (nf.active)
				{
					if (s.Type == SpellTypeGroup.Evocation)
					{
						GameManager.Instance.CurrentHero.ClassBonusStacks.Change(1.0);
						bonus *= base_bonus;
						GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, base_bonus);
					}
					else if (s.Type == SpellTypeGroup.Incantation)
					{
						GameManager.Instance.CurrentHero.ClassBonusStacks.Change(10.0);
						BigNumber bigNumber = Mathf.Pow(base_bonus, 10f);
						bonus *= bigNumber;
						GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, bigNumber);
					}
				}
			};
			on_cast_2 = delegate(Spell s)
			{
				if (s.NameKey == Spells.Nightfall)
				{
					GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, 1.0 / bonus);
					bonus = 1.0;
					Debug.Log("nf removed");
					Gallery.Set(HeroesNames.Prodigy, "3#0");
				}
			};
			on_action = delegate
			{
				Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == Spells.Nightfall);
				if (scroll != null)
				{
					scroll.AddProgressBuild(720.0, text: false, shards: false);
				}
			};
			VariableInt batsExile = Statistic.BatsExile;
			batsExile.OnChange = (Action)Delegate.Combine(batsExile.OnChange, on_action);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnPostCast = (Action<Spell>)Delegate.Combine(scrolls2.OnPostCast, on_cast_2);
		};
		challenge.Rules.OtherDescription = "Children of the Night II Description";
		challenge.Rules.Remove = delegate
		{
			VariableInt batsExile = Statistic.BatsExile;
			batsExile.OnChange = (Action)Delegate.Remove(batsExile.OnChange, on_action);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnPostCast = (Action<Spell>)Delegate.Remove(scrolls2.OnPostCast, on_cast_2);
			GameManager.Instance.Scrolls.StopAll();
			GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, 1.0 / bonus);
			bonus = 1.0;
			GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.Nightfall).level_req += 100;
			GameManager.Instance.CurrentHero.ClassBonusStacks.SetValue(0.0);
			GameManager.Instance.CurrentHero.Hero.SpellList.Remove(Spells.Nightfall);
			GameManager.Instance.SpellBook.ChangeSpellSet();
			GameManager.Instance.SpellBook.GetSpell(Spells.Nightfall).PassiveEffect = GameManager.Instance.SpellBook.persistentPassive[Spells.Nightfall];
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e500", "Earn Mana"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.CurrentHero.ExpManaSources, 0.0, 1.5));
		challenge.RewardDescription = "[SourceXP]{50%}";
		Challenges.Add(challenge);
		challenge = new Challenge("[Children of the Night] III", 248, Challenges.Last());
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Exorcist);
			Gallery.Set(HeroesNames.Exorcist, "6#0");
			GameManager.Instance.CurrentHero.Hero.SpellList.Add(Spells.Nightfall);
			Spell nf = GameManager.Instance.SpellBook.GetSpell(Spells.Nightfall);
			nf.PassiveEffect = null;
			GameManager.Instance.SpellBook.ChangeSpellSet();
			Exorcist exorcist = GameManager.Instance.CurrentHero.Hero as Exorcist;
			exorcist.mc.perClick.Change(100);
			float base_bonus = 1.18f;
			nf.level_req -= 100;
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.Nightfall)
				{
					bonus = new BigNumber(base_bonus).Pow(Convert.ToInt32(Progress.Value.ToDouble()));
					GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, bonus);
					Gallery.Set(HeroesNames.Exorcist, "6#6");
				}
			};
			on_cast_2 = delegate(Spell s)
			{
				if (s.NameKey == Spells.Nightfall)
				{
					GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, 1.0 / bonus);
					bonus = 1.0;
					Debug.Log("nf removed");
					Gallery.Set(HeroesNames.Exorcist, "6#0");
				}
			};
			on_hc = delegate
			{
				if (nf.active)
				{
					int num = exorcist.mc.GetHC().ToInt();
					Progress.Change(num);
					BigNumber bigNumber = new BigNumber(base_bonus).Pow(num);
					bonus *= bigNumber;
					GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, bigNumber);
				}
			};
			on_action = delegate
			{
				Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == Spells.Nightfall);
				if (scroll != null)
				{
					scroll.AddProgressBuild(720.0, text: false, shards: false);
				}
			};
			VariableInt batsExile = Statistic.BatsExile;
			batsExile.OnChange = (Action)Delegate.Combine(batsExile.OnChange, on_action);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnPostCast = (Action<Spell>)Delegate.Combine(scrolls2.OnPostCast, on_cast_2);
			Orb orb = GameManager.Instance.Orb;
			orb.OnMega = (Action)Delegate.Combine(orb.OnMega, on_hc);
		};
		challenge.Rules.OtherDescription = "Children of the Night III Description";
		challenge.Rules.Remove = delegate
		{
			VariableInt batsExile = Statistic.BatsExile;
			batsExile.OnChange = (Action)Delegate.Remove(batsExile.OnChange, on_action);
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			ScrollPanel scrolls2 = GameManager.Instance.Scrolls;
			scrolls2.OnPostCast = (Action<Spell>)Delegate.Remove(scrolls2.OnPostCast, on_cast_2);
			Orb orb = GameManager.Instance.Orb;
			orb.OnMega = (Action)Delegate.Remove(orb.OnMega, on_hc);
			(GameManager.Instance.CurrentHero.Hero as Exorcist).mc.perClick.Change(-100);
			GameManager.Instance.Scrolls.StopAll();
			GameManager.Instance.CurrentHero.AbilityPower.Change(0.0, 1.0 / bonus);
			bonus = 1.0;
			GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.Nightfall).level_req += 100;
			GameManager.Instance.CurrentHero.ClassBonusStacks.SetValue(0.0);
			GameManager.Instance.CurrentHero.Hero.SpellList.Remove(Spells.Nightfall);
			GameManager.Instance.SpellBook.ChangeSpellSet();
			GameManager.Instance.SpellBook.GetSpell(Spells.Nightfall).PassiveEffect = GameManager.Instance.SpellBook.persistentPassive[Spells.Nightfall];
			Progress.SetValue(0.0);
		};
		challenge.Objectives = new List<ConditionUnlock>();
		challenge.Objectives.Add(new ConditionUnlockMore("Base.ManaTotal", "1e500", "Earn Mana"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentHero.ExpManaSources;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 1.5;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[SourceXP]{50%}";
		Challenges.Add(challenge);
		challenge = new Challenge("Reality Decompression", 49);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e260", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: true, change_hero: false, change_pet: false, vip_available: false, null, statistic: false);
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Alchemist);
			SetPet(PetNames.Arcanaworg);
			GameManager.Instance.CurrentHero.Hero.SpellList.Remove(Spells.VoidDecompression);
			GameManager.Instance.CurrentHero.Hero.SpellList.Remove(Spells.VoidRadiance);
			GameManager.Instance.CurrentHero.Hero.SpellList.Remove(Spells.Crystallization);
			GameManager.Instance.CurrentHero.Hero.SpellList.Add(Spells.RealityWarping);
			GameManager.Instance.SpellBook.ChangeSpellSet();
			(GameManager.Instance.CurrentPet.Pet as Arcanaworg).ShardsIsOn = false;
			GameManager.Instance.Scrolls.Period = 0f;
			Spell spell = GameManager.Instance.SpellBook.GetSpell(Spells.RealityWarping);
			spell.FullBuild = 1.0;
			spell.level_req -= 100;
			GameManager.Instance.VoidManaManager.Decrease.Change(0.0, -100.0);
			on_action = delegate
			{
				if (GameManager.Instance.VoidMana.Value >= "1e16")
				{
					GameManager.Instance.VoidMana.SetValue(0.0);
				}
			};
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, on_action);
		};
		challenge.Rules.OtherDescription = "Reality Decompression Description";
		challenge.Rules.Remove = delegate
		{
			GameManager.Instance.Scrolls.Period = 1f;
			(GameManager.Instance.CurrentPet.Pet as Arcanaworg).ShardsIsOn = true;
			Spell spell = GameManager.Instance.SpellBook.GetSpell(Spells.RealityWarping);
			spell.FullBuild = 1000.0;
			spell.level_req += 100;
			GameManager.Instance.CurrentHero.Hero.SpellList.Add(Spells.VoidDecompression);
			GameManager.Instance.CurrentHero.Hero.SpellList.Add(Spells.VoidRadiance);
			GameManager.Instance.CurrentHero.Hero.SpellList.Add(Spells.Crystallization);
			GameManager.Instance.CurrentHero.Hero.SpellList.Remove(Spells.RealityWarping);
			GameManager.Instance.SpellBook.ChangeSpellSet();
			GameManager.Instance.VoidManaManager.Decrease.Change(0.0, -0.01);
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, on_action);
		};
		challenge.Objectives.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "100", "Reach Pet level:"));
		challenge.Reward = new ChallengeReward(new SimpleEffect(GameManager.Instance.CurrentHero.ExpBoost, 0.0, 1.5));
		challenge.RewardDescription = "[ActionsXP]{50%}";
		Challenges.Add(challenge);
		challenge = new Challenge("Absorption", 50);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e280", "Earn mysteries"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: false, vip_available: false, null, statistic: false);
		challenge.Bonus = "1e280";
		challenge.Rules.Other = delegate
		{
			SetHero(HeroesNames.Abolisher);
			PetChoose anti = Resources.Load<PetChoose>("AntiDoppel");
			anti.Pet = new AntiDoppel();
			anti.Pet.Init();
			ulong valueInt = GameManager.Instance.CurrentPet.PlayedTime.ValueInt;
			ulong num = GameManager.Instance.CurrentPet.SkipedPlayedTime.Value.ToUlong();
			GameManager.Instance.CurrentPet.PetPanel.Pets.Add(anti);
			SetPet(PetNames.AntiDoppel);
			GameManager.Instance.PetGallery.SetExactly(PetNames.Doppelganger, 2);
			GameManager.Instance.CurrentPet.SkipedPlayedTime.SetValue(num);
			GameManager.Instance.CurrentPet.PlayedTime.SetValue(valueInt);
			GameManager.Instance.CurrentHero.Hero.SpellList.Remove(Spells.MaterialiseDoppelganger);
			GameManager.Instance.CurrentHero.Hero.SpellList.Add(Spells.KarnaphensSpellshroud);
			GameManager.Instance.SpellBook.ChangeSpellSet();
			Progress.SetValue(0.0);
			foreach (Spell availableSpell2 in GameManager.Instance.SpellBook.AvailableSpells)
			{
				if (availableSpell2.UseThisRun.Value > 0.0)
				{
					if (availableSpell2.NameKey == Spells.Silence)
					{
						Progress.Change(-5.0 * availableSpell2.UseThisRun.Value);
					}
					else
					{
						Progress.Change(availableSpell2.UseThisRun.Value);
					}
				}
			}
			Action updateExp = delegate
			{
				float level = 150f + (Progress.Value / 5.0).ToFloat();
				anti.Pet.SetLevel(level);
			};
			updateExp();
			BigNumber multiplier = new BigNumber(1.100000023841858).Pow(GameManager.Instance.SpellBook.GetSpell(Spells.KarnaphensSpellshroud).UseThisRun.Value.ToInt());
			GameManager.Instance.CurrentHero.ExpBoost.Change(0.0, multiplier);
			on_cast = delegate(Spell s)
			{
				if (s.NameKey == Spells.Silence)
				{
					Progress.Change(-5.0);
					updateExp();
				}
				else
				{
					if (s.NameKey == Spells.KarnaphensSpellshroud)
					{
						GameManager.Instance.CurrentHero.ExpBoost.Change(0.0, 1.100000023841858);
					}
					Progress.Change(1.0);
					updateExp();
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 0.5);
		};
		challenge.Rules.OtherDescription = "Absorption Description";
		challenge.Rules.Remove = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			GameManager.Instance.CurrentHero.Hero.SpellList.Add(Spells.MaterialiseDoppelganger);
			GameManager.Instance.CurrentHero.Hero.SpellList.Remove(Spells.KarnaphensSpellshroud);
			PetChoose item = GameManager.Instance.CurrentPet.PetPanel.Pets.Find((PetChoose x) => x.PetName == PetNames.AntiDoppel);
			GameManager.Instance.CurrentPet.PetPanel.Pets.Remove(item);
			Progress.SetValue(0.0);
			BigNumber bigNumber = new BigNumber(1.100000023841858).Pow(GameManager.Instance.SpellBook.GetSpell(Spells.KarnaphensSpellshroud).UseThisRun.Value.ToInt());
			GameManager.Instance.CurrentHero.ExpBoost.Change(0.0, 1.0 / bigNumber);
			GameManager.Instance.Scrolls.IncantationDuration.Change(0.0, 2.0);
		};
		challenge.Conditions2Fail = new List<ConditionUnlock>();
		challenge.Conditions2Fail.Add(new ConditionUnlockVariable(GameManager.Instance.CurrentPet.Level, "199", "Absorption Fail", calculate_des: false, show_progress: false));
		challenge.Objectives.Add(new ConditionUnlockMoreThan(GameManager.Instance.CurrentHero.Level, GameManager.Instance.CurrentPet.Level, "1", "Absoption Goal"));
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.CurrentHero.ExpManaSources;
		simpleEffect.add = 0.0;
		simpleEffect.mult = 1.5;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "[SourceXP]{50%}";
		Challenges.Add(challenge);
		challenge = new Challenge("The Legion", 51);
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Base.Souls", "1e300", "Earn mysteries"));
		challenge.Conditions2Unlock.Add(new ConditionUnlockMore("Achiev.Challenge", "99", "Complete Challenges:"));
		challenge.Rules = new ChallengeRulles(myst: false, change_hero: false, change_pet: true, vip_available: false, null, statistic: false, achieves: true, equipment: true);
		challenge.Bonus = "1";
		challenge.Rules.Other = delegate
		{
			Legion = UnityEngine.Object.Instantiate(Resources.Load<LegionChallenge>("TheLegion"));
			Legion.Init(challenge, Progress.Value.ToInt());
			Legion.gameObject.SetActive(value: true);
		};
		challenge.Rules.OtherDescription = "The Legion Description";
		challenge.Rules.Remove = delegate
		{
			Progress.SetValue(0.0);
			Legion.Deactivate();
			UnityEngine.Object.Destroy(Legion.gameObject);
		};
		List<string> list = new List<string>
		{
			"The Legion Apprentice", "The Legion Druid", "The Legion Demonologist", "The Legion Necromancer", "The Legion Arcanist", "The Legion Prodigy", "The Legion Voidmancer", "The Legion Exorcist", "The Legion Chronomancer", "The Legion Umbramancer",
			"The Legion Alchemist", "The Legion Ironsoul", "The Legion Abolisher"
		};
		challenge.Objectives = new List<ConditionUnlock>
		{
			new ConditionUnlockVariable(Progress, "1", list[0], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "2", list[1], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "3", list[2], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "4", list[3], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "5", list[4], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "6", list[5], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "7", list[6], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "8", list[7], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "9", list[8], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "10", list[9], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "11", list[10], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "12", list[11], calculate_des: false, show_progress: false),
			new ConditionUnlockVariable(Progress, "13", list[12], calculate_des: false, show_progress: false)
		};
		challenge.Reward = new ChallengeReward();
		simpleEffect = new SimpleEffect();
		simpleEffect.target = GameManager.Instance.Craft.BonusEnchantLevel;
		simpleEffect.add = 1.0;
		simpleEffect.mult = 1.0;
		challenge.Reward.effect = simpleEffect;
		challenge.RewardDescription = "The Legion Reward";
		Challenges.Add(challenge);
	}

	public bool StatsIsOn()
	{
		if (ActiveChallenge != null)
		{
			return ActiveChallenge.Rules.Statistic;
		}
		return true;
	}

	public bool GeodeRecheck()
	{
		if (ActiveChallenge != null && (ActiveChallenge.ID % 100 == 17 || ActiveChallenge.ID % 100 == 34))
		{
			return false;
		}
		return true;
	}

	private void CreateButtons()
	{
		ChallengeButtons = new List<ChallengeChoose>();
		for (int i = 0; i < Challenges.Count; i++)
		{
			ChallengeChoose challengeChoose = UnityEngine.Object.Instantiate(ChallengeChoose);
			challengeChoose.Init(DescriptionLabel, Challenges[i]);
			challengeChoose.transform.SetParent(SelectPlace.transform);
			challengeChoose.transform.localPosition = Vector3.zero;
			challengeChoose.transform.localScale = Vector3.one;
			challengeChoose.transform.SetAsLastSibling();
			challengeChoose.manager = (challengeChoose.Challenge.manager = this);
			ChallengeButtons.Add(challengeChoose);
		}
	}

	public void ResetAllChallenges()
	{
		foreach (Challenge challenge in Challenges)
		{
			challenge.Reward.Delete();
			challenge.Completed = false;
		}
	}

	private void update_rules()
	{
		if (ActiveChallenge == null || !active)
		{
			return;
		}
		if (ActiveChallenge.EffectRules != null)
		{
			foreach (IEffect effectRule in ActiveChallenge.EffectRules)
			{
				effectRule.Update();
			}
		}
		if (ActiveChallenge.Rules.TimeLimit != 0L && Statistic.TimeSession.ValueInt >= ActiveChallenge.Rules.TimeLimit)
		{
			FailChallenge();
		}
		if (ActiveChallenge.Conditions2Fail != null && ActiveChallenge.CheckConditions2Fail())
		{
			FailChallenge();
		}
	}

	public void FailChallenge()
	{
		ActiveChallenge.OffSubscribe();
		ActiveChallenge.RemoveRules();
		Message.Open(ActiveChallenge, complete: false);
		Time.timeScale = 0f;
		active = false;
		exile_page.StopChallenge();
		if (onFail != null)
		{
			onFail();
		}
		if (Settings.SoundOn)
		{
			sounds.Fail();
		}
	}

	public void ApplyRewards()
	{
		foreach (Challenge challenge in Challenges)
		{
			if (challenge.Completed && !challenge.Reward.IsApplied && challenge.RewardIsUnlocked())
			{
				challenge.Reward.Apply();
			}
		}
		applied = true;
	}

	public void RemoveRewards()
	{
		if (!applied)
		{
			return;
		}
		foreach (Challenge challenge in Challenges)
		{
			if (challenge.Completed)
			{
				challenge.Reward.Delete();
			}
		}
		applied = false;
	}

	public void StartChallenge()
	{
		StartChallenge(null);
	}

	public void StartChallenge(Action action, Action decline = null)
	{
		if (SelectedChallenge == null)
		{
			return;
		}
		GameManager.Instance.ConfirmWindow.Open(challenge_message.Translate(), delegate
		{
			LaunchChallenge();
			if (action != null)
			{
				action();
			}
		}, "ResetAttributes".Translate(), GameManager.Instance.AttributeManager.ResetAttributesExile, decline);
	}

	public void LaunchChallenge()
	{
		if (trialOfSkill == null)
		{
			trialOfSkill = GameManager.Instance.Trials.GetTrial(Trials.Skill);
		}
		if (trialOfSkill.IsActive)
		{
			trialOfSkill.End();
		}
		if (ActiveChallenge != null)
		{
			OffChallenge();
		}
		ActiveChallenge = SelectedChallenge;
		GameManager.Instance.Reborn.GetSouls();
		GameManager.Instance.Exile();
		ActiveChallenge.ApplyRules();
		GameManager.Instance.RestartMana();
		ActiveChallenge.Subcribe();
		ActiveChallenge.Active = true;
		active = true;
		Close();
		GameManager.Instance.AttributeManager.CheckButon();
		exile_page.StartChallenge();
		if (ActiveChallenge.Completed)
		{
			Analytics.CustomEvent("ChallengeRepeat", new Dictionary<string, object> { { "Name", ActiveChallenge.Name } });
		}
	}

	public void LoadChallenge()
	{
		if (ActiveChallenge != null)
		{
			ActiveChallenge.ApplyRules();
			ActiveChallenge.Subcribe();
			ActiveChallenge.Active = true;
			active = true;
			exile_page.StartChallenge();
		}
	}

	public void OffChallenge()
	{
		if (ActiveChallenge != null)
		{
			ActiveChallenge.RemoveRules();
			SetHero(HeroesNames.Apprentice);
			GameManager.Instance.CurrentPet.Restart();
			ActiveChallenge.OffSubscribe();
			ActiveChallenge.Active = false;
			ActiveChallenge = null;
			Message.Close();
			active = false;
			exile_page.StopChallenge();
			Statistic.UnlockedItems.SetValue(GameManager.Instance.Craft.AvailableItems.Count);
		}
	}

	public void Home()
	{
		GameManager.Instance.ConfirmWindow.Open(challenge_close_message.Translate(), go_home, "ResetAttributes".Translate(), GameManager.Instance.AttributeManager.ResetAttributesExile);
	}

	public void StopChallenge(bool resetAtt = false)
	{
		if (onExit != null)
		{
			onExit();
		}
		if (ActiveChallenge._completed)
		{
			if (!ActiveChallenge.Completed)
			{
				ActiveChallenge.Completed = true;
				ActiveChallenge.Reward.ApplyAction();
			}
			go_home(resetAtt);
		}
		else
		{
			go_home(resetAtt);
		}
	}

	public void ExitChallenge()
	{
		if (ActiveChallenge != null)
		{
			go_home();
		}
	}

	public bool IsChangeHero()
	{
		if (ActiveChallenge != null)
		{
			return ActiveChallenge.Rules.ChangeHero;
		}
		return true;
	}

	public bool IsChangePet()
	{
		if (ActiveChallenge != null)
		{
			return ActiveChallenge.Rules.ChangePet;
		}
		return true;
	}

	private void go_home(bool reset = false)
	{
		if (reset)
		{
			GameManager.Instance.AttributeManager.ResetAttributesExile();
		}
		go_home();
	}

	private void go_home()
	{
		bool completed = ActiveChallenge._completed;
		GameManager.Instance.Reborn.GetSouls();
		OffChallenge();
		GameManager.Instance.Exile();
		CompletedChallenges.SetValue(Challenges.FindAll((Challenge x) => x.Completed).Count);
		Time.timeScale = 1f;
		GameManager.Instance.AttributeManager.CheckButon();
		if (completed && afterComplete != null)
		{
			Debug.Log("after complete");
			afterComplete();
		}
		if (!completed && afterEscape != null)
		{
			afterEscape();
		}
		Message.Close();
	}

	public void UpdateText(Challenge ch)
	{
		exile_page.challenge_label.text = ch.GetProgressDescription();
	}

	public void CompleteChallenge(Challenge ch)
	{
		Message.Open(ch);
		Time.timeScale = 0f;
		active = false;
		exile_page.StopChallenge();
		if (onComplete != null)
		{
			onComplete();
		}
		if (Settings.SoundOn)
		{
			sounds.Success();
		}
	}

	public void RecalculateCompletedCount()
	{
		CompletedChallenges.SetValue(Challenges.FindAll((Challenge x) => x.Completed).Count);
	}

	public void ShowHideCompleted()
	{
		if (ShowCompleted.isOn)
		{
			for (int i = 0; i < ChallengeButtons.Count; i++)
			{
				if (ChallengeButtons[i].Challenge.PrevTier != null)
				{
					ChallengeButtons[i].gameObject.SetActive(ChallengeButtons[i].Challenge.PrevTier.Completed);
				}
				else
				{
					ChallengeButtons[i].gameObject.SetActive(value: true);
				}
			}
			return;
		}
		for (int j = 0; j < ChallengeButtons.Count; j++)
		{
			if (ChallengeButtons[j].Challenge.PrevTier != null)
			{
				ChallengeButtons[j].gameObject.SetActive(ChallengeButtons[j].Challenge.PrevTier.Completed && !ChallengeButtons[j].Challenge.Completed);
			}
			else
			{
				ChallengeButtons[j].gameObject.SetActive(!ChallengeButtons[j].Challenge.Completed);
			}
		}
	}

	public void PostLoad()
	{
		RecalculateCompletedCount();
		ApplyRewards();
		if (trialOfSkill == null)
		{
			trialOfSkill = GameManager.Instance.Trials.GetTrial(Trials.Skill);
		}
		LoadChallenge();
	}

	private void SetHero(HeroesNames key)
	{
		GameManager.Instance.CurrentHero.SetHero(key);
	}

	private void SetPet(PetNames key)
	{
		GameManager.Instance.CurrentPet.SetPet(key);
	}
}
