using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
	public class TriumphSave
	{
		public List<int> TriumphFail;
	}

	public SpriteAtlas Atlas;

	public List<Sprite> Gems;

	public RankDefiner RankDefiner;

	public List<AchievementCategory> AchievList;

	public List<Achievement> AllAchievs;

	public List<SecretAchievemet> Secrets;

	public List<Triumph> Triumphs;

	public List<Achievement> RecentlyAchieved;

	public VariableInt AchievUnlocked;

	public VariableInt AchievsPoints;

	public VariableInt AchievSecrets;

	public VariableInt AchievTriumphs;

	public TextMeshProUGUI Label;

	public AchievsWindow Window;

	public RecentlyAchievedScrollControll scroll;

	public FlexTip Tip;

	public UnlockMessage unlock_message;

	public Action<Triumph> OnTriumphUnlock;

	public void PreInit()
	{
		AchievList = new List<AchievementCategory>();
		AllAchievs = new List<Achievement>();
		AchievUnlocked = new VariableInt(0);
		AchievsPoints = new VariableInt(0);
		AchievSecrets = new VariableInt(0);
		AchievTriumphs = new VariableInt(0);
		RecentlyAchieved = new List<Achievement>();
		GameContext.ContextAddResource(ResourceType.Achiev.ToString() + "." + ResourceAchiev.Count, AchievUnlocked);
		GameContext.ContextAddResource(ResourceType.Achiev.ToString() + "." + ResourceAchiev.Points, AchievsPoints);
		GameContext.ContextAddResource(ResourceType.Achiev.ToString() + "." + ResourceAchiev.Secrets, AchievSecrets);
		GameContext.ContextAddResource(ResourceType.Achiev.ToString() + "." + ResourceAchiev.Triumphs, AchievTriumphs);
	}

	public void Init()
	{
		CreateAll(GlobalData.Achievements, GlobalData.Triumphs);
		VariableInt achievUnlocked = AchievUnlocked;
		achievUnlocked.OnChange = (Action)Delegate.Combine(achievUnlocked.OnChange, new Action(RecalculateUnlocked));
		VariableInt achievUnlocked2 = AchievUnlocked;
		achievUnlocked2.OnChange = (Action)Delegate.Combine(achievUnlocked2.OnChange, new Action(update_label));
		TranslationManager instance = TranslationManager.Instance;
		instance.OnChangeLanguage = (Action)Delegate.Combine(instance.OnChangeLanguage, new Action(update_label));
		update_label();
	}

	private void OnDestroy()
	{
		VariableInt achievUnlocked = AchievUnlocked;
		achievUnlocked.OnChange = (Action)Delegate.Remove(achievUnlocked.OnChange, new Action(update_label));
		TranslationManager instance = TranslationManager.Instance;
		instance.OnChangeLanguage = (Action)Delegate.Remove(instance.OnChangeLanguage, new Action(update_label));
	}

	private void RecalculateUnlocked()
	{
		AchievSecrets.SetValue(Secrets.FindAll((SecretAchievemet x) => x.Unlocked).Count);
		AchievTriumphs.SetValue(Triumphs.FindAll((Triumph x) => x.Unlocked).Count);
	}

	public void PreLoad()
	{
		unlock_message.Clear();
		unlock_message.show_message = false;
		foreach (AchievementCategory achiev in AchievList)
		{
			achiev.Restart();
		}
		AchievUnlocked.SetValue(0);
		AchievsPoints.SetValue(0);
	}

	public void PostLoad()
	{
		unlock_message.show_message = true;
		foreach (AchievementCategory achiev in AchievList)
		{
			achiev.Init();
		}
		CheckAllTriumphs();
		Statistic.HeroMaxLevelAllTime.SetValue(Statistic.HeroMaxLevelAllTime.ValueInt);
		Statistic.PetMaxLevelAllTime.SetValue(Statistic.PetMaxLevelAllTime.ValueInt);
		Statistic.TimeOfflineTotal.SetValue(Statistic.TimeOfflineTotal.ValueInt);
		GameManager.Instance.PetGallery.Unlocked.SetValue(GameManager.Instance.PetGallery.Unlocked.ValueInt);
		scroll.SetData(RecentlyAchieved);
		AchievUnlocked.SetValue(AchievUnlocked.ValueInt);
		AchievsPoints.SetValue(AchievsPoints.ValueInt);
		RecalculateUnlocked();
	}

	public void AddRecently(Achievement achieve)
	{
		if (achieve == null)
		{
			Debug.Log("add null");
		}
		RecentlyAchieved.Insert(0, achieve);
		scroll.SetData(RecentlyAchieved);
	}

	private void CreateAll(List<AchievementFormat> ach_info, List<TriumphFormat> triumph_info)
	{
		foreach (AchievementFormat item in ach_info)
		{
			if (string.IsNullOrEmpty(item.Condition))
			{
				item.Condition = "More";
			}
			if (item.Condition == "More")
			{
				SimpleAchievement simpleAchievement = new SimpleAchievement();
				simpleAchievement.Key = (AchievementKey)Enum.Parse(typeof(AchievementKey), item.Key);
				simpleAchievement.Level = int.Parse(item.Level);
				simpleAchievement.Name = item.Name;
				simpleAchievement.Description = item.Description;
				simpleAchievement.Reward = int.Parse(item.Points);
				simpleAchievement.parameter = item.Parameter;
				simpleAchievement.argument = new BigNumber(item.Argument);
				simpleAchievement.Sprite = item.SpriteKey;
				AllAchievs.Add(simpleAchievement);
				AddToCategory(simpleAchievement);
			}
			else
			{
				ConditionalAchievemet conditionalAchievemet = new ConditionalAchievemet();
				conditionalAchievemet.Key = (AchievementKey)Enum.Parse(typeof(AchievementKey), item.Key);
				conditionalAchievemet.Level = int.Parse(item.Level);
				conditionalAchievemet.Name = item.Name;
				conditionalAchievemet.Description = item.Description;
				conditionalAchievemet.Reward = int.Parse(item.Points);
				conditionalAchievemet.condition_key = item.Condition;
				conditionalAchievemet.parameter = item.Parameter;
				conditionalAchievemet.argument = new BigNumber(item.Argument);
				conditionalAchievemet.Sprite = item.SpriteKey;
				AllAchievs.Add(conditionalAchievemet);
				AddToCategory(conditionalAchievemet);
			}
		}
		CreateAllSecrets();
		CreateTriumphs(triumph_info);
	}

	private void addSecret(SecretAchievemet a, string icon)
	{
		if (Secrets == null)
		{
			Secrets = new List<SecretAchievemet>();
		}
		a.Sprite = icon;
		AddToCategory(a, inNew: true);
		AllAchievs.Add(a);
		Secrets.Add(a);
	}

	private void addTriumph(Triumph a)
	{
		if (Triumphs == null)
		{
			Triumphs = new List<Triumph>();
		}
		AddToCategory(a, inNew: true);
		AllAchievs.Add(a);
		Triumphs.Add(a);
	}

	private void AddToCategory(Achievement a, bool inNew = false)
	{
		a.manager = this;
		AchievementCategory achievementCategory = null;
		if (!inNew)
		{
			achievementCategory = AchievList.Find((AchievementCategory x) => x.Key == a.Key);
		}
		if (achievementCategory == null)
		{
			achievementCategory = new AchievementCategory();
			achievementCategory.Key = a.Key;
			AchievList.Add(achievementCategory);
		}
		achievementCategory.Add(a);
	}

	private void update_label()
	{
		Label.text = string.Format("AchievementPointPage".Translate(), AchievUnlocked.ValueInt.ToString("F0"), AchievsPoints.ValueInt);
	}

	public List<Achievement> GetAllAchievements()
	{
		if (GameManager.Instance.Realm.Realms.ValueInt != 0)
		{
			return AllAchievs;
		}
		return AllAchievs.FindAll((Achievement x) => x.Key < AchievementKey.T_Speedrun);
	}

	private void CreateAllSecrets()
	{
		string icon = "Achievement_Secret";
		SecretAchievemet secretAchievemet = new SecretAchievemet("What is it?");
		secretAchievemet.Reward = 10;
		secretAchievemet.Key = AchievementKey.Secret_5;
		secretAchievemet.Condition = () => false;
		secretAchievemet.Sub = delegate
		{
		};
		secretAchievemet.Unsub = delegate
		{
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("More knowledge");
		secretAchievemet.Reward = 10;
		secretAchievemet.Key = AchievementKey.Secret_6;
		secretAchievemet.Condition = () => false;
		secretAchievemet.Sub = delegate
		{
		};
		secretAchievemet.Unsub = delegate
		{
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Have I changed?");
		secretAchievemet.Reward = 10;
		secretAchievemet.Key = AchievementKey.Secret_7;
		secretAchievemet.Condition = () => false;
		secretAchievemet.Sub = delegate
		{
		};
		secretAchievemet.Unsub = delegate
		{
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Wrong place for me");
		secretAchievemet.Reward = 10;
		secretAchievemet.Key = AchievementKey.Secret_1;
		secretAchievemet.Condition = () => Statistic.TimeSession.ValueInt <= 10;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Time to think");
		secretAchievemet.Reward = 10;
		secretAchievemet.Key = AchievementKey.Secret_8;
		secretAchievemet.Condition = () => Statistic.TimeSession.ValueInt >= 600 && Statistic.Clicks.Value < 1.0 && Statistic.TotalBuildings.ValueInt == 0 && Statistic.CastSpell.Value < 1.0 && Statistic.BoughtUpgrades.ValueInt == 0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Shards minimalism");
		secretAchievemet.Reward = 20;
		secretAchievemet.Key = AchievementKey.Secret_2;
		secretAchievemet.Condition = () => GameManager.Instance.Reborn.Convert() != 0.0 && Statistic.Clicks.Value == 15.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Searching/Learning");
		secretAchievemet.Reward = 20;
		secretAchievemet.Key = AchievementKey.Secret_3;
		List<string> names = new List<string>();
		secretAchievemet.Condition = () => names.Count == 15;
		secretAchievemet.OnReset = delegate
		{
			names = new List<string>();
		};
		secretAchievemet.Func = delegate
		{
			if (!names.Contains(GameManager.Instance.CurrentHero.Hero.NameKey.ToString()))
			{
				names.Add(GameManager.Instance.CurrentHero.Hero.NameKey.ToString());
			}
			if (GameManager.Instance.CurrentPet.Pet != null && !names.Contains(GameManager.Instance.CurrentPet.Pet.NameKey.ToString()))
			{
				names.Add(GameManager.Instance.CurrentPet.Pet.NameKey.ToString());
			}
		};
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnSelect = (Action)Delegate.Combine(currentHero.OnSelect, new Action(x.Check));
			PetSlot currentPet = GameManager.Instance.CurrentPet;
			currentPet.OnSelect = (Action)Delegate.Combine(currentPet.OnSelect, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnSelect = (Action)Delegate.Remove(currentHero.OnSelect, new Action(x.Check));
			PetSlot currentPet = GameManager.Instance.CurrentPet;
			currentPet.OnSelect = (Action)Delegate.Remove(currentPet.OnSelect, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Dwarven preparation");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_4;
		secretAchievemet.Condition = () => Statistic.TimeSession.ValueInt >= 86400 && Statistic.CastSpell.Value >= "1e5" && Statistic.AutoClicks.Value >= "1e7" && Statistic.ClickableCollect.Value >= "1e4";
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Much potential left");
		secretAchievemet.Reward = 20;
		secretAchievemet.Key = AchievementKey.Secret_9;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Level.ValueInt == 75 && Statistic.Clicks.Value <= 15.0 && Statistic.AutoClicks.Value <= Statistic.TimeSession.ValueInt + 10 && Statistic.ClickableCollect.ValueInt < 1;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			VariableInt level = GameManager.Instance.CurrentHero.Level;
			level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			VariableInt level = GameManager.Instance.CurrentHero.Level;
			level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Purebred");
		secretAchievemet.Reward = 20;
		secretAchievemet.Key = AchievementKey.Secret_10;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge == null && GameManager.Instance.CurrentHero.Hero.NameKey != HeroesNames.Demonologist && GameManager.Instance.CurrentPet.Level.ValueInt >= 120;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			VariableInt level = GameManager.Instance.CurrentPet.Level;
			level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			VariableInt level = GameManager.Instance.CurrentPet.Level;
			level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Fake ones");
		secretAchievemet.Reward = 20;
		secretAchievemet.Key = AchievementKey.Secret_11;
		secretAchievemet.Condition = () => GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.SyntheticEntity).UseThisRun.Value >= 30.0 && Statistic.ClickableCollect.ValueInt <= 31;
		SecretAchievemet fo = secretAchievemet;
		Action<Spell> on_cast_7 = delegate(Spell x)
		{
			if (x.NameKey == Spells.SyntheticEntity)
			{
				fo.Check();
			}
		};
		secretAchievemet.Sub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast_7);
		};
		secretAchievemet.Unsub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast_7);
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Bad day");
		secretAchievemet.Reward = 20;
		secretAchievemet.Key = AchievementKey.Secret_ch_6;
		int count_fails = 0;
		Action reset = delegate
		{
			count_fails = 0;
		};
		secretAchievemet.Condition = delegate
		{
			count_fails++;
			return count_fails > 2;
		};
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onFail = (Action)Delegate.Combine(challengeManager.onFail, new Action(x.Check));
			ChallengeManager challengeManager2 = GameManager.Instance.ChallengeManager;
			challengeManager2.onComplete = (Action)Delegate.Combine(challengeManager2.onComplete, reset);
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onFail = (Action)Delegate.Remove(challengeManager.onFail, new Action(x.Check));
			ChallengeManager challengeManager2 = GameManager.Instance.ChallengeManager;
			challengeManager2.onComplete = (Action)Delegate.Remove(challengeManager2.onComplete, reset);
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Nostalgia");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_ch_3;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge != null && GameManager.Instance.ChallengeManager.ActiveChallenge.ID % 100 == 1 && GameManager.Instance.ChallengeManager.ActiveChallenge._completed && GameManager.Instance.ChallengeManager.ActiveChallenge.Completed;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Combine(challengeManager.onExit, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Remove(challengeManager.onExit, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Don't Touch This!");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_ch_12;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge != null && GameManager.Instance.ChallengeManager.ActiveChallenge.ID % 100 == 40 && GameManager.Instance.ChallengeManager.ActiveChallenge._completed && Statistic.AutoClicks.Value < 1.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Combine(challengeManager.onExit, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Remove(challengeManager.onExit, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Truest Prodigy");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_ch_4;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge != null && GameManager.Instance.ChallengeManager.ActiveChallenge.ID % 100 == 6 && GameManager.Instance.ChallengeManager.ActiveChallenge._completed && Statistic.TimeSession.ValueInt <= 150;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Combine(challengeManager.onExit, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Remove(challengeManager.onExit, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("9k and 1");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_ch_1;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge != null && GameManager.Instance.ChallengeManager.ActiveChallenge.ID % 100 == 9 && Statistic.CastSpell.Value >= 9001.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Combine(challengeManager.onExit, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Remove(challengeManager.onExit, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Demons purpose");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_ch_8;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge != null && GameManager.Instance.ChallengeManager.ActiveChallenge.ID % 100 == 11 && GameManager.Instance.ChallengeManager.ActiveChallenge._completed && GameManager.Instance.CurrentPet.Pet.NameKey == PetNames.PitLord;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Combine(challengeManager.onExit, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Remove(challengeManager.onExit, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("This Place Is Mine!");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_ch_5;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge != null && GameManager.Instance.ChallengeManager.ActiveChallenge.ID % 100 == 12 && GameManager.Instance.ChallengeManager.ActiveChallenge._completed && GameManager.Instance.CurrentPet.Pet == null;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Combine(challengeManager.onExit, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Remove(challengeManager.onExit, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Too easy for me");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_ch_2;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge != null && GameManager.Instance.ChallengeManager.ActiveChallenge.ID % 100 == 18 && GameManager.Instance.ChallengeManager.ActiveChallenge._completed && Statistic.TimeSession.ValueInt <= 450 && GameManager.Instance.ChallengeManager.ActiveChallenge.Completed;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Combine(challengeManager.onExit, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Remove(challengeManager.onExit, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Forbearance");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_ch_7;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge != null && GameManager.Instance.ChallengeManager.ActiveChallenge.ID % 100 == 19 && GameManager.Instance.ChallengeManager.ActiveChallenge._completed && GameManager.Instance.CurrentHero.ClassBonusStacks.Value < 2.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Combine(challengeManager.onExit, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Remove(challengeManager.onExit, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Mighty group");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_ch_9;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge != null && GameManager.Instance.ChallengeManager.ActiveChallenge.ID % 100 == 20 && GameManager.Instance.ChallengeManager.ActiveChallenge._completed && GameManager.Instance.SpellBook.SpellList.FindAll((Spell x) => x.Use.Value > 0.0).Count >= 56;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Combine(challengeManager.onExit, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Remove(challengeManager.onExit, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("No Rest for the Damned");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_ch_10;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge != null && GameManager.Instance.ChallengeManager.ActiveChallenge.ID % 100 == 23 && GameManager.Instance.ChallengeManager.ActiveChallenge._completed && (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Demonologist || GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Necromancer);
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Combine(challengeManager.onExit, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Remove(challengeManager.onExit, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Cleaner");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_ch_11;
		secretAchievemet.Condition = () => GameManager.Instance.ChallengeManager.ActiveChallenge != null && GameManager.Instance.ChallengeManager.ActiveChallenge.ID % 100 == 42 && GameManager.Instance.ChallengeManager.ActiveChallenge._completed && GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Alchemist;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Combine(challengeManager.onExit, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
			challengeManager.onExit = (Action)Delegate.Remove(challengeManager.onExit, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Gotcha!");
		secretAchievemet.Reward = 10;
		secretAchievemet.Key = AchievementKey.Secret_ch_13;
		secretAchievemet.Condition = () => false;
		secretAchievemet.Sub = delegate
		{
		};
		secretAchievemet.Unsub = delegate
		{
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("In correct order");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_7;
		List<Spell> spells = new List<Spell>();
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Apprentice && spells.Count >= 14;
		secretAchievemet.OnReset = delegate
		{
			spells = new List<Spell>();
		};
		Action<Spell> on_cast = delegate(Spell x)
		{
			if (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Apprentice)
			{
				if (spells.Count == 0)
				{
					spells.Add(x);
				}
				else
				{
					if (spells.Last().level_req >= x.level_req)
					{
						spells = new List<Spell>();
					}
					spells.Add(x);
				}
			}
		};
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast);
			VariableBignumber castSpell = Statistic.CastSpell;
			castSpell.OnChange = (Action)Delegate.Combine(castSpell.OnChange, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast);
			VariableBignumber castSpell = Statistic.CastSpell;
			castSpell.OnChange = (Action)Delegate.Remove(castSpell.OnChange, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Tranquility");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_1;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Druid && Statistic.AutoClicks.Value >= "1e7" && Statistic.Clicks.Value < 100.0 && Statistic.TimeSession.ValueInt >= 3600;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		SecretAchievemet fon;
		secretAchievemet = (fon = new SecretAchievemet("Force of Nothing"));
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_8;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Druid && GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == 4).building.Level.ValueInt == 0 && Statistic.AutoClicks.Value <= 1000.0;
		Action<Spell> on_cast_8 = delegate(Spell x)
		{
			if (x.NameKey == Spells.ForceOfNature)
			{
				fon.Check();
			}
		};
		secretAchievemet.Sub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast_8);
		};
		secretAchievemet.Unsub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast_8);
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Forests of Caliban");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_17;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Druid && GameManager.Instance.CurrentPet.Pet != null && GameManager.Instance.CurrentPet.Pet.NameKey == PetNames.Daemon && GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == 4).building.Level.ValueInt >= 1000 && Math.Abs(GameManager.Instance.CurrentHero.PlayedTime.Value.ToDouble() - GameManager.Instance.CurrentPet.PlayedTime.Value.ToDouble()) <= 10.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("One is not enough");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_2;
		List<string> levels = new List<string>();
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Demonologist && levels.Count >= 4;
		secretAchievemet.OnReset = delegate
		{
			levels = new List<string>();
		};
		secretAchievemet.Func = delegate
		{
			if (GameManager.Instance.CurrentPet.Pet != null && (float)GameManager.Instance.CurrentPet.Level.ValueInt >= 120f && !levels.Contains(GameManager.Instance.CurrentPet.Pet.NameKey.ToString()))
			{
				levels.Add(GameManager.Instance.CurrentPet.Pet.NameKey.ToString());
			}
		};
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			VariableInt level = GameManager.Instance.CurrentPet.Level;
			level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			VariableInt level = GameManager.Instance.CurrentPet.Level;
			level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		SecretAchievemet bsc;
		secretAchievemet = (bsc = new SecretAchievemet("Bad season for crops"));
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_9;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Demonologist && GameManager.Instance.CurrentPet.Pet != null && GameManager.Instance.CurrentPet.Pet.Level.ValueInt == 1 && GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.ReapWhatYouSow).Use.Value >= 10.0;
		Action<Spell> on_cast_9 = delegate(Spell x)
		{
			if (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Demonologist && x.NameKey == Spells.ReapWhatYouSow)
			{
				bsc.Check();
			}
		};
		secretAchievemet.Sub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast_9);
		};
		secretAchievemet.Unsub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast_9);
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Inexperienced");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_18;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Demonologist && GameManager.Instance.CurrentPet.Pet != null && GameManager.Instance.CurrentPet.Pet.Level.ValueInt >= 130 && GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.HellStorm).UseThisRun.Value == 0.0 && GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.UncleanKnowledge).UseThisRun.Value == 0.0 && GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.FireBall).UseThisRun.Value == 0.0 && GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.SummonInfernalThrasher).UseThisRun.Value == 0.0 && GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.SummonHornedIncinerator).UseThisRun.Value == 0.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Patience of the dead");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_3;
		secretAchievemet.Condition = () => (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Necromancer || GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Shaman) && (Statistic.GetClassPlayedTime(HeroesNames.Necromancer) + Statistic.GetClassPlayedTime(HeroesNames.Shaman) >= 1209600 || GameManager.Instance.CurrentHero.PlayedTime.Value >= 604800.0);
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Personal graveyard");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_12;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Necromancer && GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.PlagueZombie).chargeCount >= 250;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("True idler");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_19;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Necromancer && GameManager.Instance.CurrentHero.PlayedTime.ValueInt >= 1800 && (float)(long)(Statistic.TimeIdleSession.ValueInt - GameManager.Instance.CurrentHero.PlayedTime.ValueInt) >= 300f;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Lightning storm");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_4;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Arcanist && GameManager.Instance.SpellBook.GetSpell(Spells.LightningBolt).Use.Value >= "1e4" && GameManager.Instance.SpellBook.GetSpell(Spells.LightningBolt).Use.Value > GameManager.Instance.SpellBook.GetSpell(Spells.MagicMissile).Use.Value;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("The hard way");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_11;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Arcanist && Statistic.CastSpell.Value >= "1e5" && GameManager.Instance.SpellBook.GetSpell(Spells.LightningBolt).Use.Value < 1.0 && GameManager.Instance.SpellBook.GetSpell(Spells.SpellStaffOfChamaon).Use.Value < 1.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("After Void, the Deluge");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_28;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Arcanist && GameManager.Instance.SpellBook.GetSpell(Spells.VoidLure).UseThisRun.Value == 0.0 && Statistic.CastSpell.Value >= "1e5" && Statistic.ClickableCollect.Value >= "1e3";
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("True evocation");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_5;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Prodigy && GameManager.Instance.CurrentHero.Level.ValueInt >= 90 && GameManager.Instance.SpellBook.GetSpell(Spells.KelphiorsBlackBeam).Use.Value >= "1e4" && GameManager.Instance.SpellBook.GetActualSpell(Spells.PrimalPower).Use.Value >= "1e3";
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		SecretAchievemet overdriven;
		secretAchievemet = (overdriven = new SecretAchievemet("Overdriven"));
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_13;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Prodigy && (float)GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == 8).building.Level.ValueInt >= (float)Statistic.TotalBuildings.ValueInt * 0.25f;
		Action<Spell> on_cast_10 = delegate(Spell x)
		{
			if (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Prodigy && x.NameKey == Spells.LeyOverdrive)
			{
				overdriven.Check();
			}
		};
		secretAchievemet.Sub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast_10);
		};
		secretAchievemet.Unsub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast_10);
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Live Fast Die Jung");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_29;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentPet.Pet != null && GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Prodigy && GameManager.Instance.CurrentPet.Pet.NameKey == PetNames.AnimaConstruct && Statistic.TimeSession.ValueInt <= 60 && GameManager.Instance.CurrentHero.Level.ValueInt <= 120;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Embrace the void");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_6;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Voidmancer && GameManager.Instance.SpellBook.GetSpell(Spells.VoidSyphon).Use.Value >= "1e4" && GameManager.Instance.SpellBook.GetSpell(Spells.VoidPrison).Use.Value >= "1e3" && GameManager.Instance.SpellBook.GetSpell(Spells.VoidRadiance).Use.Value >= "1e2" && GameManager.Instance.SpellBook.GetSpell(Spells.VoidAutomaton).Use.Value >= "1e1";
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		SecretAchievemet fe;
		secretAchievemet = (fe = new SecretAchievemet("Futile effort"));
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_14;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Voidmancer && GameManager.Instance.VoidMana.Value < 1.0 && Statistic.AutoClicks.Value >= 100000.0;
		Action<Spell> on_cast_11 = delegate(Spell x)
		{
			if (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Voidmancer && x.NameKey == Spells.EbonTruncheon)
			{
				fe.Check();
			}
		};
		secretAchievemet.Sub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast_11);
		};
		secretAchievemet.Unsub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast_11);
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("One With Nothing");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_33;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Voidmancer && GameManager.Instance.CurrentPet.Pet != null && GameManager.Instance.CurrentPet.Pet.NameKey == PetNames.Hungerer && Statistic.TotalBuildings.ValueInt == 0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Caster of the Church");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_15;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Exorcist && GameManager.Instance.SpellBook.GetSpell(Spells.Smite).Use.Value >= "1e3" && Statistic.CastSpell.Value >= 10000.0 && Statistic.Clicks.Value < 1.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		SecretAchievemet librarian;
		secretAchievemet = (librarian = new SecretAchievemet("Librarian"));
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_16;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Exorcist && GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == 2).building.Level.ValueInt == Statistic.TotalBuildings.ValueInt;
		Action<Spell> on_cast_12 = delegate(Spell x)
		{
			if (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Exorcist && x.NameKey == Spells.HallowedWritings)
			{
				librarian.Check();
			}
		};
		secretAchievemet.Sub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, on_cast_12);
		};
		secretAchievemet.Unsub = delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, on_cast_12);
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Execrated");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_31;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Exorcist && GameManager.Instance.SpellBook.GetSpell(Spells.BattleTrance).Use.Value >= 1.0 && GameManager.Instance.CurrentHero.ClassBonusStacks.Value < 1.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Time stage");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_20;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Chronomancer && Statistic.SkipedTimeSession.Value >= 604800.0 && Statistic.CastSpell.Value >= "1e6" && Statistic.AutoClicks.Value >= "1e7" && Statistic.ClickableCollect.Value >= "1e5";
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Break the Loop");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_21;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Chronomancer && GameManager.Instance.CurrentHero.Level.ValueInt >= 125 && GameManager.Instance.SpellBook.GetSpell(Spells.TimeHelix).Use.Value >= "25" && Statistic.CastSpell.Value >= 100000.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Finite Proper Time");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_32;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Chronomancer && GameManager.Instance.SpellBook.GetSpell(Spells.SingularityBeam).Use.Value >= 1.0 && GameManager.Instance.SpellBook.GetSpell(Spells.TimeHelix).Use.Value >= 1.0 && Statistic.TimeSession.ValueInt <= 30;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Opposites Attract");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_22;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Umbramancer && GameManager.Instance.CurrentPet.Pet != null && GameManager.Instance.CurrentPet.Pet.NameKey == PetNames.RisenGiant && GameManager.Instance.CurrentPet.Level.ValueInt >= 100;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			VariableInt level = GameManager.Instance.CurrentPet.Level;
			level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			VariableInt level = GameManager.Instance.CurrentPet.Level;
			level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Preparation for Darkness");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_23;
		secretAchievemet.Condition = delegate
		{
			if (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Umbramancer)
			{
				BigNumber value = GameContext.GetResource("Shadow.ShadowEnergy").Value;
				if (value >= "1e6")
				{
					return GameManager.Instance.CurrentHero.ClassBonusStacks.Value <= 0.009999999776482582 * value;
				}
				return false;
			}
			return false;
		};
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Syzygy");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_37;
		secretAchievemet.Condition = delegate
		{
			if (GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Umbramancer)
			{
				BigNumber val = GameManager.Instance.SpellBook.GetSpell(Spells.Eclipse).UseThisRun.Value;
				if (val >= 2.0)
				{
					return GameManager.Instance.SpellBook.AvailableSpells.FindAll((Spell x) => x.UseThisRun.Value == val).Count == 3;
				}
				return false;
			}
			return false;
		};
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Reduction of Staff");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_24;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Alchemist && Statistic.TotalBuildings.ValueInt >= 30000 && GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == 5).building.Level.ValueInt <= 1000;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Prism Analysis");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_25;
		secretAchievemet.Condition = () => false;
		secretAchievemet.Sub = delegate
		{
		};
		secretAchievemet.Unsub = delegate
		{
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Philosopher's Moonshine");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_34;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Alchemist && GameManager.Instance.SpellBook.GetSpell(Spells.AnimaSynteta).UseThisRun.Value >= 100.0 && GameManager.Instance.Orb.OrbContainer.GetComponentInChildren<AlchemistOrb>().elixirCount == 0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Fast Soul");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_26;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Ironsoul && Statistic.TimeSession.Value < 2400.0 && GameManager.Instance.CurrentHero.ClassBonusStacks.Value >= 1000.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Iron Mastery");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_27;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Ironsoul && GameManager.Instance.AttributeManager.Panel.mastery.parameter.Level.ValueInt >= 300;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			VariableInt level = GameManager.Instance.AttributeManager.Panel.mastery.parameter.Level;
			level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			VariableInt level = GameManager.Instance.AttributeManager.Panel.mastery.parameter.Level;
			level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Before and after voice");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_35;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Ironsoul && (GameManager.Instance.CurrentHero.Hero as Ironsoul).stances.stanceCount >= 100;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			VariableLong timeSession = Statistic.TimeSession;
			timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Almost Absolute Zero");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_30;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Abolisher && Statistic.EnchantingDustExile.Value >= 1.0 && Statistic.EnchantingDustExile.Value < 1000.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Through Five And Seven Illustrious Gates Of Not");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_36;
		secretAchievemet.Condition = delegate
		{
			Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == Spells.SyphonPower);
			return GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Abolisher && GameManager.Instance.Scrolls.ShardsPassive.Value >= "1e7" && (scroll == null || !scroll.spell.active);
		};
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Touch Nothing in the Underworld");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_38;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Shaman && GameManager.Instance.CurrentPet.Pet != null && GameManager.Instance.CurrentPet.Pet.NameKey == PetNames.Daemon && GameManager.Instance.SpellBook.GetSpell(Spells.SyntheticEntity).Use.Value >= 130.0 && Statistic.AutoClicks.Value < 2.0 && Statistic.Clicks.Value < 2.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Active Blasphemancy");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_39;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Heretic && GameManager.Instance.CurrentHero.ClassBonusStacks.Value > 20000.0 && GameManager.Instance.SpellBook.GetSpell(Spells.HallowedWritings).Use.Value > 0.0 && GameManager.Instance.VoidMana.Value > 10.0 && Statistic.TimeIdleSession.Value < 10.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Hellrazer");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_40;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Oni && GameManager.Instance.SpellBook.GetSpell(Spells.FuriousStrike).Use.Value > 666.0 && Statistic.TotalBuildings.ValueInt == GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == 6).building.Level.ValueInt;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("No Need For Words");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_41;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Archon && Statistic.EnchantingDustExile.Value >= 1.0 && Statistic.ShardsSession.Value < 10.0;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Neverending Stability");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_42;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Temporalist && GameManager.Instance.SpellBook.GetActualSpell(Spells.StabilizeTheFlow).Use.Value >= "1e3" && GameManager.Instance.SpellBook.GetActualSpell(Spells.StabilizeTheFlow).Use.Value < GameManager.Instance.SpellBook.GetSpell(Spells.Revert).Use.Value;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("Shadow of the Sacrifice");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_class_43;
		secretAchievemet.Condition = () => GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Desolator && ShadowEnergyManager.instance.ShadowEnergy.Value > "1e10" && Statistic.TotalBuildings.ValueInt <= 1000;
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("A Persistent Classic");
		secretAchievemet.Reward = 40;
		secretAchievemet.Key = AchievementKey.Secret_spells_1;
		secretAchievemet.Condition = () => GameManager.Instance.SpellBook.GetActualSpell(Spells.TrueSorcery).Use.Value >= "1e4";
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("A Persistent Storm");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_spells_2;
		secretAchievemet.Condition = () => GameManager.Instance.SpellBook.GetActualSpell(Spells.TrueSorcery).Use.Value >= "3e4" && GameManager.Instance.SpellBook.GetActualSpell(Spells.RulesOfNature).Use.Value >= "1e4" && GameManager.Instance.SpellBook.GetActualSpell(Spells.Nightfall).Use.Value >= "5000" && GameManager.Instance.SpellBook.GetActualSpell(Spells.JAMissileStorm).Use.Value >= "2e6";
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("A Persistent Crusade");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_spells_3;
		secretAchievemet.Condition = () => GameManager.Instance.SpellBook.GetActualSpell(Spells.TrueSorcery).Use.Value >= "7e4" && GameManager.Instance.SpellBook.GetActualSpell(Spells.RulesOfNature).Use.Value >= "3e4" && GameManager.Instance.SpellBook.GetActualSpell(Spells.Nightfall).Use.Value >= "7000" && GameManager.Instance.SpellBook.GetActualSpell(Spells.JAMissileStorm).Use.Value >= "7e6";
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("A Persistent Conflux");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_spells_4;
		secretAchievemet.Condition = () => GameManager.Instance.SpellBook.GetActualSpell(Spells.TrueSorcery).Use.Value >= "1e5" && GameManager.Instance.SpellBook.GetActualSpell(Spells.RulesOfNature).Use.Value >= "4e4" && GameManager.Instance.SpellBook.GetActualSpell(Spells.Nightfall).Use.Value >= "8000" && GameManager.Instance.SpellBook.GetActualSpell(Spells.JAMissileStorm).Use.Value >= "1e7";
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("A Persistent Pyre");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_spells_5;
		secretAchievemet.Condition = () => GameManager.Instance.SpellBook.GetActualSpell(Spells.TrueSorcery).Use.Value >= "5e5" && GameManager.Instance.SpellBook.GetActualSpell(Spells.RulesOfNature).Use.Value >= "1e5" && GameManager.Instance.SpellBook.GetActualSpell(Spells.Nightfall).Use.Value >= "1e4" && GameManager.Instance.SpellBook.GetActualSpell(Spells.JAMissileStorm).Use.Value >= "2e7";
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
		secretAchievemet = new SecretAchievemet("A Persistent Magna");
		secretAchievemet.Reward = 50;
		secretAchievemet.Key = AchievementKey.Secret_spells_6;
		secretAchievemet.Condition = () => GameManager.Instance.SpellBook.GetActualSpell(Spells.TrueSorcery).Use.Value >= "5e5" && GameManager.Instance.SpellBook.GetActualSpell(Spells.RulesOfNature).Use.Value >= "1e5" && GameManager.Instance.SpellBook.GetActualSpell(Spells.Nightfall).Use.Value >= "1e4" && GameManager.Instance.SpellBook.GetActualSpell(Spells.JAMissileStorm).Use.Value >= "2e7" && GameManager.Instance.SpellBook.GetActualSpell(Spells.TemperTheSteel).Use.Value >= "1e5";
		secretAchievemet.Sub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(x.Check));
		};
		secretAchievemet.Unsub = delegate(SecretAchievemet x)
		{
			GameManager instance = GameManager.Instance;
			instance.OnExile = (Action)Delegate.Remove(instance.OnExile, new Action(x.Check));
		};
		addSecret(secretAchievemet, icon);
	}

	public TriumphSave SaveTriumphs()
	{
		TriumphSave triumphSave = new TriumphSave();
		triumphSave.TriumphFail = new List<int>();
		foreach (Triumph triumph in Triumphs)
		{
			if (triumph.IsFailed)
			{
				triumphSave.TriumphFail.Add((int)triumph.Key);
			}
		}
		return triumphSave;
	}

	public void LoadTriumps(TriumphSave data)
	{
		if (data == null)
		{
			foreach (Triumph triumph in Triumphs)
			{
				triumph.OnFail();
			}
			return;
		}
		foreach (int v in data.TriumphFail)
		{
			Triumphs.Find((Triumph x) => x.Key == (AchievementKey)v).OnFail();
		}
	}

	public void RefreshTriumphs()
	{
		foreach (Triumph triumph in Triumphs)
		{
			triumph.Refresh();
		}
	}

	public void FailAllTriumphs()
	{
		foreach (Triumph triumph in Triumphs)
		{
			if (!triumph.IsFailed)
			{
				triumph.OnFail();
			}
		}
	}

	public void CheckAllTriumphs()
	{
		foreach (Triumph triumph in Triumphs)
		{
			if (!triumph.IsFailed)
			{
				triumph.CheckFail();
			}
		}
	}

	private Triumph CreateTriumph(AchievementKey key, List<TriumphFormat> triumph_info)
	{
		return new Triumph(key, triumph_info.Find((TriumphFormat x) => x.Key == key.ToString()));
	}

	private void CreateTriumphs(List<TriumphFormat> triumph_info)
	{
		Triumph triumph = CreateTriumph(AchievementKey.T_Speedrun, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e300" && (float)Statistic.TimeRealm.ValueInt <= 3600f;
		triumph.Fail = () => (float)Statistic.TimeRealm.ValueInt > 3600f;
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Combine(timeRealm.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Remove(timeRealm.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Speedrun_2, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e700" && Statistic.TimeRealm.ValueInt <= 86400;
		triumph.Fail = () => Statistic.TimeRealm.ValueInt > 86400;
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Combine(timeRealm.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Remove(timeRealm.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Speedrun_3, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e800" && Statistic.TimeRealm.ValueInt <= 172800;
		triumph.Fail = () => Statistic.TimeRealm.ValueInt > 172800;
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Combine(timeRealm.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Remove(timeRealm.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Speedrun_4, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e900" && Statistic.TimeRealm.ValueInt <= 259200;
		triumph.Fail = () => Statistic.TimeRealm.ValueInt > 259200;
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Combine(timeRealm.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Remove(timeRealm.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Attributes, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e400";
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			AttributeManager attributeManager = GameManager.Instance.AttributeManager;
			attributeManager.OnInvest = (Action)Delegate.Combine(attributeManager.OnInvest, new Action(x.OnFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			AttributeManager attributeManager = GameManager.Instance.AttributeManager;
			attributeManager.OnInvest = (Action)Delegate.Remove(attributeManager.OnInvest, new Action(x.OnFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Trials, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e400";
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			TrialManager trials = GameManager.Instance.Trials;
			trials.OnStart = (Action)Delegate.Combine(trials.OnStart, new Action(x.OnFail));
			CraftingMenu craftingMenu = GameManager.Instance.Craft.window.craftingMenu;
			craftingMenu.OnExperiment = (Action)Delegate.Combine(craftingMenu.OnExperiment, new Action(x.OnFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			TrialManager trials = GameManager.Instance.Trials;
			trials.OnStart = (Action)Delegate.Remove(trials.OnStart, new Action(x.OnFail));
			CraftingMenu craftingMenu = GameManager.Instance.Craft.window.craftingMenu;
			craftingMenu.OnExperiment = (Action)Delegate.Remove(craftingMenu.OnExperiment, new Action(x.OnFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Expedition, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e400";
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			ExpeditionManager instance = ExpeditionManager.Instance;
			instance.OnFight = (Action)Delegate.Combine(instance.OnFight, new Action(x.OnFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			ExpeditionManager instance = ExpeditionManager.Instance;
			instance.OnFight = (Action)Delegate.Remove(instance.OnFight, new Action(x.OnFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Forge, triumph_info);
		triumph.Condition = () => GameManager.Instance.Realm.Realms.ValueInt >= 1 && Reborn.Souls.Value >= "1e400";
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			CraftManager craft = GameManager.Instance.Craft;
			craft.OnEquipItem = (Action)Delegate.Combine(craft.OnEquipItem, new Action(x.OnFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			CraftManager craft = GameManager.Instance.Craft;
			craft.OnEquipItem = (Action)Delegate.Remove(craft.OnEquipItem, new Action(x.OnFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_ManaSource, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e400";
		triumph.Fail = () => GameManager.Instance.Buildings.FindAll((BuildingVisual x) => x.building.Level.ValueInt > 0).Count > 1;
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			VariableInt totalBuildings = Statistic.TotalBuildings;
			totalBuildings.OnChange = (Action)Delegate.Combine(totalBuildings.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			VariableInt totalBuildings = Statistic.TotalBuildings;
			totalBuildings.OnChange = (Action)Delegate.Remove(totalBuildings.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Pet, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e400";
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			PetSlot currentPet = GameManager.Instance.CurrentPet;
			currentPet.OnSelect = (Action)Delegate.Combine(currentPet.OnSelect, new Action(x.OnFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			PetSlot currentPet = GameManager.Instance.CurrentPet;
			currentPet.OnSelect = (Action)Delegate.Remove(currentPet.OnSelect, new Action(x.OnFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Apprentice, triumph_info);
		triumph.Condition = delegate
		{
			RealmUpgrade upgrade = GameManager.Instance.Realm.GetUpgrade(51);
			return (upgrade == null || upgrade.Level.ValueInt <= 0) && Reborn.Souls.Value >= "1e300";
		};
		triumph.Fail = delegate
		{
			RealmUpgrade upgrade = GameManager.Instance.Realm.GetUpgrade(51);
			return (upgrade != null && upgrade.Level.ValueInt > 0) || GameManager.Instance.CurrentHero.Hero.NameKey != HeroesNames.Apprentice;
		};
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Combine(currentHero.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Remove(currentHero.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Shaman, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e500";
		triumph.Fail = delegate
		{
			HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
			return nameKey != HeroesNames.Apprentice && nameKey != HeroesNames.Druid && nameKey != HeroesNames.Necromancer;
		};
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Combine(currentHero.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Remove(currentHero.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Heretic, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e500";
		triumph.Fail = delegate
		{
			HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
			return nameKey != HeroesNames.Apprentice && nameKey != HeroesNames.Voidmancer && nameKey != HeroesNames.Exorcist;
		};
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Combine(currentHero.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Remove(currentHero.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Oni, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e500";
		triumph.Fail = delegate
		{
			HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
			return nameKey != HeroesNames.Apprentice && nameKey != HeroesNames.Demonologist && nameKey != HeroesNames.Ironsoul;
		};
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Combine(currentHero.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Remove(currentHero.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Archon, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e500";
		triumph.Fail = delegate
		{
			HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
			return nameKey != HeroesNames.Apprentice && nameKey != HeroesNames.Arcanist && nameKey != HeroesNames.Abolisher;
		};
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Combine(currentHero.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Remove(currentHero.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Temporalist, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e500";
		triumph.Fail = delegate
		{
			HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
			return nameKey != HeroesNames.Apprentice && nameKey != HeroesNames.Prodigy && nameKey != HeroesNames.Chronomancer;
		};
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Combine(currentHero.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Remove(currentHero.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Desolator, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e500";
		triumph.Fail = delegate
		{
			HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
			return nameKey != HeroesNames.Apprentice && nameKey != HeroesNames.Umbramancer && nameKey != HeroesNames.Alchemist;
		};
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Combine(currentHero.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Remove(currentHero.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Evo, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e700";
		triumph.Sub = delegate(Triumph x)
		{
			x.CheckFailSpell = delegate(Spell y)
			{
				if (y.Type == SpellTypeGroup.Evocation)
				{
					x.OnFail();
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, x.CheckFailSpell);
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, x.CheckFailSpell);
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Inca, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e600";
		triumph.Sub = delegate(Triumph x)
		{
			x.CheckFailSpell = delegate(Spell y)
			{
				if (y.Type == SpellTypeGroup.Incantation)
				{
					x.OnFail();
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, x.CheckFailSpell);
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, x.CheckFailSpell);
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Sum, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e700";
		triumph.Sub = delegate(Triumph x)
		{
			x.CheckFailSpell = delegate(Spell y)
			{
				if (y.Type == SpellTypeGroup.Summoning)
				{
					x.OnFail();
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, x.CheckFailSpell);
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, x.CheckFailSpell);
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Memory, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e550";
		triumph.Fail = () => (GameManager.Instance.Realm.TotalMemories.Value - GameManager.Instance.Realm.Memories.Value).Abs() > 1.0 || (GameManager.Instance.Realmcraft.DelusionsTotal.Value - GameManager.Instance.Realmcraft.Delusions.Value).Abs() >= 1.0;
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Combine(timeRealm.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Remove(timeRealm.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_MM_Augments, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e700";
		triumph.Sub = delegate(Triumph x)
		{
			x.CheckFailSpell = delegate(Spell y)
			{
				if (y.ResetUses && !y.IsAugment && y.NameKey != Spells.MagicMissile)
				{
					x.OnFail();
				}
			};
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, x.CheckFailSpell);
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, x.CheckFailSpell);
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_VMana, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e900";
		triumph.Fail = () => GameManager.Instance.VoidMana.Value >= 1.0 || Statistic.VoidManaRealm.Value >= 1.0 || (ShadowEnergyManager.instance != null && ShadowEnergyManager.instance.ShadowEnergy.Value >= 1.0);
		triumph.Sub = delegate(Triumph x)
		{
			BonusSpawner bonusSpawner = GameManager.Instance.BonusSpawner;
			bonusSpawner.OnGetResource = (Action)Delegate.Combine(bonusSpawner.OnGetResource, new Action(x.CheckFail));
			VariableBignumber voidManaRealm = Statistic.VoidManaRealm;
			voidManaRealm.OnChange = (Action)Delegate.Combine(voidManaRealm.OnChange, new Action(x.CheckFail));
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			BonusSpawner bonusSpawner = GameManager.Instance.BonusSpawner;
			bonusSpawner.OnGetResource = (Action)Delegate.Remove(bonusSpawner.OnGetResource, new Action(x.CheckFail));
			VariableBignumber voidManaRealm = Statistic.VoidManaRealm;
			voidManaRealm.OnChange = (Action)Delegate.Remove(voidManaRealm.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Mem_Source, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e1000";
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			BuildingGilding buildings = GameManager.Instance.Gilding.Buildings;
			buildings.OnSetSpec = (Action)Delegate.Combine(buildings.OnSetSpec, new Action(x.OnFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			BuildingGilding buildings = GameManager.Instance.Gilding.Buildings;
			buildings.OnSetSpec = (Action)Delegate.Remove(buildings.OnSetSpec, new Action(x.OnFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Catas, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e1000";
		triumph.Fail = delegate
		{
			BuildingManager buildingManager = GameManager.Instance.BuildingManager;
			return (buildingManager.TotalGreen.Value - buildingManager.FreeGreenCatalysts.Value).Abs() > 0.10000000149011612 || (buildingManager.TotalBlue.Value - buildingManager.FreeBlueCatalysts.Value).Abs() > 0.10000000149011612 || (buildingManager.TotalRed.Value - buildingManager.FreeRedCatalysts.Value).Abs() > 0.10000000149011612;
		};
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			RealmManager realm = GameManager.Instance.Realm;
			realm.OnRealm = (Action)Delegate.Combine(realm.OnRealm, new Action(x.CheckFail));
			BuildingManager buildingManager = GameManager.Instance.BuildingManager;
			buildingManager.OnInvestCatalysts = (Action<BigNumber>)Delegate.Combine(buildingManager.OnInvestCatalysts, new Action<BigNumber>(x.OnFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			RealmManager realm = GameManager.Instance.Realm;
			realm.OnRealm = (Action)Delegate.Remove(realm.OnRealm, new Action(x.CheckFail));
			BuildingManager buildingManager = GameManager.Instance.BuildingManager;
			buildingManager.OnInvestCatalysts = (Action<BigNumber>)Delegate.Remove(buildingManager.OnInvestCatalysts, new Action<BigNumber>(x.OnFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Casts_Limit, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e1000";
		triumph.Fail = () => Statistic.CastSpell.Value > 100.0;
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber castSpell = Statistic.CastSpell;
			castSpell.OnChange = (Action)Delegate.Combine(castSpell.OnChange, new Action(x.CheckFail));
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			VariableBignumber castSpell = Statistic.CastSpell;
			castSpell.OnChange = (Action)Delegate.Remove(castSpell.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_Minors, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e1100";
		triumph.Fail = delegate
		{
			Dictionary<Gods, God> allGods = GameManager.Instance.Pantheon.GetAllGods();
			int num = 0;
			int num2 = 0;
			foreach (KeyValuePair<Gods, God> item in allGods)
			{
				if (item.Value.Level.ValueInt != 0)
				{
					if (item.Value.Group < 5)
					{
						num2 += item.Value.Level.ValueInt;
					}
					else
					{
						num += item.Value.Level.ValueInt;
					}
				}
			}
			return num < num2;
		};
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.CheckFail));
			VariableBignumber souls2 = Reborn.Souls;
			souls2.OnChange = (Action)Delegate.Combine(souls2.OnChange, new Action(x.Check));
			PantheonManager pantheon = GameManager.Instance.Pantheon;
			pantheon.OnGetExp = (Action)Delegate.Combine(pantheon.OnGetExp, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.CheckFail));
			VariableBignumber souls2 = Reborn.Souls;
			souls2.OnChange = (Action)Delegate.Remove(souls2.OnChange, new Action(x.Check));
			PantheonManager pantheon = GameManager.Instance.Pantheon;
			pantheon.OnGetExp = (Action)Delegate.Remove(pantheon.OnGetExp, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_WithoutExile, triumph_info);
		triumph.Condition = delegate
		{
			RealmUpgrade upgrade = GameManager.Instance.Realm.GetUpgrade(51);
			return (upgrade == null || upgrade.Level.ValueInt <= 0) && Reborn.Souls.Value >= "1e80";
		};
		triumph.Fail = delegate
		{
			RealmUpgrade upgrade = GameManager.Instance.Realm.GetUpgrade(51);
			return (upgrade != null && upgrade.Level.ValueInt > 0) || Reborn.Souls.Value >= 1.0;
		};
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			VariableBignumber souls2 = Reborn.Souls;
			souls2.OnChange = (Action)Delegate.Combine(souls2.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			VariableBignumber souls2 = Reborn.Souls;
			souls2.OnChange = (Action)Delegate.Remove(souls2.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_WithoutExile_App, triumph_info);
		triumph.Condition = delegate
		{
			RealmUpgrade upgrade = GameManager.Instance.Realm.GetUpgrade(51);
			return (upgrade == null || upgrade.Level.ValueInt <= 0) && Reborn.Souls.Value >= "1e50";
		};
		triumph.Fail = delegate
		{
			RealmUpgrade upgrade = GameManager.Instance.Realm.GetUpgrade(51);
			if (upgrade != null && upgrade.Level.ValueInt > 0)
			{
				return true;
			}
			HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
			return Reborn.Souls.Value >= 1.0 || nameKey != HeroesNames.Apprentice;
		};
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			VariableBignumber souls2 = Reborn.Souls;
			souls2.OnChange = (Action)Delegate.Combine(souls2.OnChange, new Action(x.CheckFail));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Combine(currentHero.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			VariableBignumber souls2 = Reborn.Souls;
			souls2.OnChange = (Action)Delegate.Remove(souls2.OnChange, new Action(x.CheckFail));
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Remove(currentHero.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_LimitExiles, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e1000";
		triumph.Fail = () => Statistic.AscendsInRealm.ValueInt >= 12;
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			VariableInt ascendsInRealm = Statistic.AscendsInRealm;
			ascendsInRealm.OnChange = (Action)Delegate.Combine(ascendsInRealm.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			VariableInt ascendsInRealm = Statistic.AscendsInRealm;
			ascendsInRealm.OnChange = (Action)Delegate.Remove(ascendsInRealm.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
		triumph = CreateTriumph(AchievementKey.T_WithoutAscentionDelusion, triumph_info);
		triumph.Condition = () => Reborn.Souls.Value >= "1e1300";
		triumph.Fail = () => GameManager.Instance.Ascension.IsActive || (GameManager.Instance.Realmcraft.DelusionsTotal.Value - GameManager.Instance.Realmcraft.Delusions.Value).Abs() >= 1.0;
		triumph.Sub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Combine(timeRealm.OnChange, new Action(x.CheckFail));
		};
		triumph.Unsub = delegate(Triumph x)
		{
			VariableBignumber souls = Reborn.Souls;
			souls.OnChange = (Action)Delegate.Remove(souls.OnChange, new Action(x.Check));
			VariableLong timeRealm = Statistic.TimeRealm;
			timeRealm.OnChange = (Action)Delegate.Remove(timeRealm.OnChange, new Action(x.CheckFail));
		};
		addTriumph(triumph);
	}
}
