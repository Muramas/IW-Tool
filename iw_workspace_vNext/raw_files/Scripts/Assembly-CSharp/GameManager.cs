using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	public int BuyPack = 1;

	public VariableComplex Profit;

	public VariableComplex PPS;

	public Relics Relics;

	public VariableInt AlterationSand;

	public VariableFloat AlterationSandIncome;

	public VariableInt Nullifier;

	public SkillManager SkillManager;

	public BuildingManager BuildingManager;

	public HeroSlot CurrentHero;

	public PetSlot CurrentPet;

	public Orb Orb;

	public ScrollPanel Scrolls;

	public Idle Idle;

	public Reborn Reborn;

	public VoidMana VoidManaManager;

	public UpgradeManager UpgradeManager;

	public BonusSpawner BonusSpawner;

	public SpellBook SpellBook;

	public GoodsManager Shop;

	public AchievementManager AchievManager;

	public BuildingLeveling[] BuildingLeveling;

	public SaveData SaveData;

	public VariableComplex OfflineProduction;

	public VariableInt LevelReduction;

	public AnimatedText AnimatedText;

	public Confirmation ConfirmWindow;

	public SettingMenu SettingMenu;

	public BuffManager BuffManager;

	public ChallengeManager ChallengeManager;

	public TrialManager Trials;

	public AttributeManager AttributeManager;

	public Paragons Paragon;

	public StatisticWindow StatsWindow;

	public ResourceManager Resources;

	public CraftManager Craft;

	public MovableBonusSpawner MBSpawner;

	public CorruptionManager CorruptionManager;

	public GildingManager Gilding;

	public Calendar Calendar;

	public ServerRewards ServerRewards;

	public Gallery Gallery;

	public PetGallery PetGallery;

	public InteriorManager Interior;

	public MultipleBuy BuyPackPanel;

	public TutorialManager Tutorial;

	public TimerManager Timers;

	public FlexTip Tip;

	public Links Social;

	public string Version;

	public Action<float> GameTick;

	public Action<float> GameTickReal;

	public Action OnExile;

	public Action OnPostExile;

	private float time_played;

	private Timer timer;

	private float auto_save_timer;

	private float local_save_timer;

	private bool auto_enable = true;

	public bool cloud_inited;

	public CompareSaveWindow compare_window;

	public CloudSave CustomCloud;

	public SaveInfo SaveInfo;

	public SaveLoadingCompare Cap;

	private string cloud_save_storage = "";

	public ManaManager ManaManager;

	public GameObject UpdateInfo;

	public SoundManager SoundManager;

	public LorePage LoreInfo;

	public PantheonManager Pantheon;

	public RealmManager Realm;

	public RealmcraftManager Realmcraft;

	public OfflineProduction Offline;

	public EventManager Event;

	public AscensionManager Ascension;

	public TaskManager Tasks;

	public FamiliarManager Familiars;

	public UpdateNotification Notification;

	public Action OnPostLoad;

	public Action OnPreSave;

	public Action OnPostSave;

	public Action OnPostOffline;

	public Action OnPostTimeSkip;

	public VariableBignumber Mana => ManaManager.Mana;

	public VariableBignumber StartingMana => ManaManager.StartingMana;

	public bool IsLoaded { get; private set; }

	public int UserID => (int)(SteamUser.GetSteamID().m_SteamID % int.MaxValue);

	public string UserName => PlatformAPI.instance.username;

	public List<BuildingVisual> Buildings => BuildingManager.Buildings;

	public VariableComplex PPSFromBuildings => BuildingManager.PPSFromBuildings;

	public VariableBignumber VoidMana => VoidManaManager.Value;

	public string GetUserId()
	{
		return "Steam" + SteamUser.GetSteamID().m_SteamID;
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			SoundManager.Init();
			Init();
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void Init()
	{
		Version = "1.80.1c";
		Cap.On();
		CustomCloud = UnityEngine.Object.FindObjectOfType<CloudSave>();
		Timers.Init();
		SaveData = new SaveData();
		Relics = new Relics();
		ManaManager = new ManaManager();
		SkillManager = new SkillManager();
		Profit = new VariableComplex(0.0);
		PPS = new VariableComplex(0.0);
		OfflineProduction = new VariableComplex(1.0);
		LevelReduction = new VariableInt(0);
		AlterationSand = new VariableInt(0);
		AlterationSandIncome = new VariableFloat(1f);
		Nullifier = new VariableInt(0);
		GameContext.InitContext();
		Event.Init();
		InitAll();
		Tasks = new TaskManager(AchievManager.Atlas.Get("Quest_Complete"));
		Debug.Log("Inited game version " + Version);
	}

	private void Start()
	{
	}

	private void Update()
	{
		if (Time.timeScale != 0f)
		{
			update_time_played();
			ManaManager.ManaChange();
			GameTick?.Invoke(Time.deltaTime);
			GameTickReal?.Invoke(Time.unscaledDeltaTime);
			auto_save();
		}
	}

	private void LateUpdate()
	{
		AddProfit(Time.deltaTime);
	}

	private void auto_save()
	{
		if (local_save_timer >= 60f)
		{
			SaveData.SaveLocal();
			local_save_timer = 0f;
		}
		if (Time.timeScale != 0f)
		{
			local_save_timer += Time.unscaledDeltaTime;
		}
		if (auto_enable)
		{
			if (auto_save_timer >= 900f)
			{
				Save();
				auto_save_timer = 0f;
			}
			if (Time.timeScale != 0f)
			{
				auto_save_timer += Time.unscaledDeltaTime;
			}
		}
	}

	public void AddOfflineMana(TimeSpan span)
	{
		long sec = (long)span.TotalSeconds;
		SaveData.OfflineImediatly(sec);
		IEnumerator routine = wait_1_frame(delegate
		{
			AddProfit(0f);
			BigNumber bigNumber = Idle.GetIdleBonus() * PPSFromBuildings.ApplyModOnVar(PPS.Value) * sec;
			if (Instance.CurrentHero.Hero.NameKey != HeroesNames.Umbramancer && Instance.CurrentHero.Hero.NameKey != HeroesNames.Desolator)
			{
				bigNumber *= OfflineProduction.Value;
			}
			OfflineProductionData offlineProductionData = new OfflineProductionData
			{
				time = (int)((sec > int.MaxValue) ? int.MaxValue : sec),
				mana = bigNumber
			};
			ManaChange(bigNumber);
			SaveData.SkipTimeData data = SaveData.SkipTime(sec);
			offlineProductionData.LoadSkipTime(data);
			Instance.Resources.gatherer.Offline(sec);
			UpgradeManager.CloseActivatedUpgrades();
			Offline.Open(offlineProductionData);
			Idle.Restart();
		});
		StartCoroutine(routine);
	}

	public void AddProfit(float time)
	{
		PPS.SetValue(BuildingManager.GetBuildingProfit());
		Orb.click_profit.SetValue(PPS.Value * Orb.percent_pps.Value + 1.0);
		if (time != 0f)
		{
			ManaManager.Change(PPSFromBuildings.ApplyModOnVar(PPS.Value) * time, msources: true);
		}
	}

	public void ManaChange(BigNumber add)
	{
		ManaManager.Change(add);
	}

	public void VoidManaChange(BigNumber add)
	{
		add *= VoidManaManager.Income.Value;
		VoidMana.Change(add);
		if (BigNumber.Sign(add) == 1)
		{
			Statistic.Change(Statistic.VoidManaAllTime, add);
			Statistic.Change(Statistic.VoidManaRealm, add);
			Statistic.VoidManaSession.Change(add);
			if (VoidMana.Value > Statistic.MaxVoidManaSession.Value)
			{
				Statistic.MaxVoidManaSession.SetValue(VoidMana.Value);
			}
		}
		Instance.BonusSpawner.OnGetResource?.Invoke();
	}

	public void InitAll()
	{
		BuildingManager.Init();
		Statistic.TimeSession.OnChange = null;
		for (int i = 0; i < 5; i++)
		{
			Statistic.ResourcesRealm.Add(new VariableBignumber(0.0));
			Statistic.ResourcesTotal.Add(new VariableBignumber(0.0));
		}
		for (int j = 0; j < 7; j++)
		{
			Statistic.UnlockedByTiers.Add(new VariableInt(0));
		}
		Statistic.ExileReset();
		Idle.Init();
		Orb.Init();
		VoidManaManager.Init();
		BonusSpawner.Init(VoidManaManager.VoidCore);
		GameContext.GenerateContext();
		Reborn.Init();
		Idle.PostInit();
		Scrolls.Init();
		CurrentPet.Init();
		CurrentHero.HeroPanel.InitMap();
		CurrentHero.HeroPanel.Init();
		Gallery.Init();
		PetGallery.Init();
		Interior = new InteriorManager();
		Interior.Init();
		CurrentHero.Init();
		AchievManager.PreInit();
		AttributeManager.Init();
		CurrentPet.PanelInit();
		SpellBook.Init();
		SpellBook.ChangeSpellSet();
		Pantheon.PreInit();
		ExpeditionManager.Instance.Init();
		MBSpawner.Init();
		Craft.Init();
		AchievManager.Init();
		Trials.InitContext();
		UpgradeManager.Init();
		BuffManager.Init();
		Resources.Init();
		Pantheon.Init();
		Ascension.Init();
		Gilding.Init();
		Craft.InitItems();
		ChallengeManager.Init();
		Trials.Init();
		Realm.Init();
		Realmcraft.Init();
		Paragon.Init();
		CorruptionManager.Init();
		Familiars.Init();
		CurrentHero.HeroPanel.PostInit();
		Shop.Init();
		GameContext.AddPromoResources();
		SteamManager.instance.Subscribe();
	}

	public void Exile()
	{
		Ascension.ResetAll();
		Gilding.PreExile();
		Event.Remove();
		Calendar.Remove();
		ServerRewards.RemoveBuffs();
		AttributeManager.Restart();
		Pantheon.PreLoad();
		Trials.PreExile();
		Realm.ResetOnExile();
		Realmcraft.ResetOnExile();
		BuildingManager.ResetBuildings();
		Craft.weapons.RemoveEffect();
		Craft.RemoveEffects();
		Shop.OffVip();
		BuffManager.OffBuffs();
		Familiars.PreLoad();
		Restart();
		Reborn.OnSoulsChange();
		ChallengeManager.ApplyRewards();
		Paragon.CheckLevel();
		Shop.Activate();
		BuffManager.OnBuffs();
		RestartMana();
		Craft.weapons.ResetAll();
		Realm.ApplyAll();
		Realmcraft.PostExile();
		Pantheon.PostLoad();
		Trials.PostExile();
		AttributeManager.PostReset();
		Calendar.Apply();
		Event.Apply();
		ServerRewards.ApplyBuffs();
		Craft.ApplyEffects();
		Craft.ApplyEnchBonus();
		Familiars.PostLoad();
		UpgradeManager.CloseActivatedUpgrades();
		CurrentHero.Hero.UpdateExp();
		Gilding.CheckAvailable(reset: true);
		Gallery.Unlocked.OnChange?.Invoke();
	}

	public void RealmChange(Action onReset = null)
	{
		Reborn.BeginBlockUpdate();
		Realmcraft.SaveHistory();
		Realm.PreReset();
		Ascension.ResetAll();
		Calendar.Remove();
		Event.Remove();
		ServerRewards.RemoveBuffs();
		Craft.weapons.RemoveEffect();
		Craft.RemoveEffects();
		Shop.OffVip();
		BuffManager.OnRealmChange();
		ChallengeManager.OffChallenge();
		ChallengeManager.RemoveRewards();
		Pantheon.PreLoad();
		Pantheon.ResetOnRealmChange();
		Realmcraft.PreRealmReset();
		Gilding.RealmChange();
		Craft.Proficiency.ResetFull();
		BuildingManager.ResetBuildingsRealm();
		Scrolls.ChangeRealm();
		Craft.RealmReset();
		Familiars.PreLoad();
		Realm.RemoveAll();
		PPSFromBuildings.Reset();
		Instance.CurrentHero.ClassBonusStacks.SetValue(0.0);
		Statistic.RealmReset();
		Paragon.RealmReset();
		Resources.RealmReset();
		AttributeManager.RealmReset();
		Trials.RealmReset();
		ExpeditionManager.Instance.RealmChange();
		CurrentHero.Hero.UpdateExp();
		UpgradeManager.Restart();
		CurrentPet.Restart();
		CurrentHero.Restart();
		SpellBook.ResetAll(resetAll: true);
		OfflineProduction.SetValue(1.0);
		AddProfit(0f);
		Orb.Restart();
		Idle.Restart();
		VoidManaManager.Restart();
		BonusSpawner.Restart();
		PPS.Reset();
		BuildingManager.ResetBuildings();
		Scrolls.SpellsReset();
		Scrolls.Restart();
		Reborn.RestartSoulPower();
		CurrentHero.Hero.UpdateExp();
		Trials.PostRealmReset();
		Shop.Activate();
		BuffManager.OnBuffs();
		RestartMana();
		Realm.PostReset();
		AttributeManager.PostReset();
		Craft.weapons.ResetAll();
		Realm.ApplyAll();
		Realmcraft.PostRealmReset();
		Reborn.ChangeRealm(Realmcraft.Active);
		Reborn.EndBlockUpdate();
		onReset?.Invoke();
		Paragon.CheckLevel();
		ChallengeManager.ApplyRewards();
		Craft.ApplyEffects();
		Calendar.Apply();
		Event.Apply();
		ServerRewards.ApplyBuffs();
		Familiars.PostLoad();
		CurrentHero.PostLoad();
		CurrentPet.PostLoad();
		Gilding.PostLoad();
		Pantheon.PostLoad();
		UpgradeManager.CloseActivatedUpgrades();
		Instance.AchievManager.CheckAllTriumphs();
	}

	public void Restart(bool isLoading = false)
	{
		if (isLoading)
		{
			ChallengeManager.OffChallenge();
		}
		ChallengeManager.RemoveRewards();
		Scrolls.SpellsReset(isLoading);
		PPSFromBuildings.Reset();
		Statistic.ExileReset();
		BuildingManager.ResetBuildings();
		UpgradeManager.Restart(isLoading);
		CurrentPet.Restart();
		CurrentHero.Restart();
		SpellBook.ResetAll();
		OfflineProduction.SetValue(1.0);
		AddProfit(0f);
		Orb.Restart();
		Idle.Restart();
		BonusSpawner.Restart();
		PPS.Reset();
		BuildingManager.ResetBuildings();
		VoidManaManager.Restart();
		Scrolls.Restart();
		Reborn.RestartSoulPower();
	}

	public void PreLoad()
	{
		IsLoaded = false;
		Tutorial.DisableAll();
		Idle.ActivateIdle();
		Tasks.PreLoad();
		AttributeManager.Restart();
		ChallengeManager.RemoveRewards();
		AchievManager.PreLoad();
		Shop.OffVip();
		BuffManager.OffBuffs();
		BuffManager.Restart();
		Reborn.DeleteSoulPower();
		Paragon.PreLoad();
		Craft.RemoveEnchBonus();
		Craft.weapons.RemoveEffect();
		Craft.PreLoad();
		Calendar.Remove();
		ServerRewards.RemoveBuffs();
		Trials.PreLoad();
		Pantheon.PreLoad();
		Realm.RemoveAll();
		Realmcraft.PreLoad();
		Ascension.Deactivate();
		Gilding.PreLoad();
		Event.PreLoad();
		Familiars.PreLoad();
		SpellBook.Enhancements.ResetAll();
	}

	public void PostLoad()
	{
		Time.timeScale = 1f;
		ChallengeManager.RemoveRewards();
		SettingMenu.LoadSettings();
		foreach (Scroll scroll in Scrolls.Scrolls)
		{
			scroll.UpdateVisual();
		}
		Paragon.PostLoad();
		VoidManaManager.ResetValue();
		BonusSpawner.DisableAll();
		Shop.Activate();
		BuffManager.OnBuffs();
		CurrentHero.Hero.UpdateExp();
		SettingMenu.PostLoad();
		AchievManager.PostLoad();
		Trials.PostLoad();
		Reborn.ApplySouls();
		Craft.PostLoad();
		UpgradeManager.PostLoad();
		SpellBook.PostLoad();
		BuyPackPanel.SetState(BuyPack);
		AttributeManager.PostReset();
		Calendar.Apply();
		Event.PostLoad();
		ServerRewards.ApplyBuffs();
		auto_enable = true;
		MBSpawner.bonus.Off();
		Pantheon.PostLoad();
		Realm.ApplyAll();
		Realmcraft.PostLoad();
		SpellBook.ChangeSpellSet();
		Gilding.PostLoad();
		CurrentHero.PostLoad();
		CurrentPet.PostLoad();
		Ascension.PostLoad();
		ChallengeManager.PostLoad();
		Familiars.PostLoad();
		Shop.special.PostLoad();
		AddProfit(0f);
		IsLoaded = true;
		if (OnPostLoad != null)
		{
			OnPostLoad();
		}
		Tutorial.ShowOnLoad();
		ServerRewards.Get();
	}

	public void PostOffline()
	{
		Realmcraft.PostOffline();
		Tasks.PostLoad();
		Interior.Backs.Unlocked.OnChange?.Invoke();
		OnPostOffline?.Invoke();
	}

	private void update_time_played()
	{
		if (time_played >= 1f)
		{
			int num = Mathf.FloorToInt(time_played);
			Statistic.TimeTotal.Change(num);
			Statistic.TimeRealm.Change(num);
			Statistic.TimeSession.Change(num);
			CurrentHero.PlayedTime.Change(num);
			Statistic.ChangeClassTime(CurrentHero.Hero.NameKey, num);
			if (Instance.Ascension.IsActive)
			{
				Statistic.ChangeClassTime(Instance.Ascension.Choosen, num);
			}
			if (CurrentPet.Pet != null)
			{
				CurrentPet.PlayedTime.Change(num);
			}
			if (CurrentPet.SecondPetIsAcitve())
			{
				CurrentPet.PetPanel.secondSlot.PlayedTime.Change(num);
			}
			if (Idle.IdleIsActive)
			{
				Statistic.TimeIdleSession.Change(num);
				Statistic.TimeIdleRealm.Change(num);
				Statistic.TimeIdleTotal.Change(num);
			}
			if (Time.timeScale != 1f)
			{
				int num2 = (int)((float)num * Time.timeScale) - num;
				if (num2 > 0)
				{
					Statistic.SkipedTimeSession.Change(num2);
					Statistic.SkipedTimeTotal.Change(num2);
					Statistic.SkipedTimeRealm.Change(num2);
					CurrentHero.SkipedPlayedTime.Change(num2);
					if (CurrentPet.Pet != null)
					{
						CurrentPet.SkipedPlayedTime.Change(num2);
					}
					if (CurrentPet.SecondPetIsAcitve())
					{
						CurrentPet.PetPanel.secondSlot.SkipedPlayedTime.Change(num2);
					}
				}
			}
			time_played -= num;
		}
		time_played += Time.unscaledDeltaTime;
	}

	public void Save()
	{
		SaveData.SaveLocal();
		CustomCloud.SetUserData(SaveData.ToJson());
		EnableAutoSave();
	}

	public void LoadWebGL()
	{
		PlatformAPI.instance.OnLoad();
		Cap.Off();
		SaveInfo.Open(SaveInfo.load_complete);
	}

	public void LoadCloudToCompare(string save_cloud)
	{
		Debug.Log("load compare");
		Cap.Off();
		cloud_save_storage = save_cloud;
		SaveData saveData = SaveData.GetSaveData(save_cloud);
		SaveData saveData2 = SaveData.GetSaveData();
		if (saveData2 == null)
		{
			SaveData.LoadSaveJson(save_cloud);
			PlatformAPI.instance.OnLoad();
			SaveInfo.Open(SaveInfo.load_complete);
		}
		else if (Math.Abs((float)saveData2.TimeTotal - (float)saveData.TimeTotal) > 600f && saveData2.Souls > 1.0)
		{
			if (!cloud_inited)
			{
				auto_enable = false;
			}
			compare_window.Open(saveData2, saveData);
			Settings.MusicOn = false;
			SettingMenu.LoadSettings();
		}
		else if (saveData2.TimeTotal > saveData.TimeTotal)
		{
			LoadWebGL();
		}
		else
		{
			SaveData.LoadSaveJson(save_cloud);
			PlatformAPI.instance.OnLoad();
			SaveInfo.Open(SaveInfo.load_complete);
		}
	}

	public void LoadCloud()
	{
		if (!SaveData.LoadSaveJson(cloud_save_storage))
		{
			Debug.Log("incorrect save in cloud");
			if (!SaveData.LoadLocal())
			{
				LoadEmpty();
			}
		}
		PlatformAPI.instance.OnLoad();
		SaveInfo.Open(SaveInfo.load_complete);
		cloud_save_storage = null;
	}

	public void ChangeUser()
	{
		Shop.Close();
		if (UserID != 0)
		{
			CustomCloud.UpdateUrl();
			CustomCloud.GetUserData();
			ServerRewards.Get();
		}
	}

	public void Load()
	{
		if (!SaveData.LoadLocal())
		{
			CustomCloud.GetUserData();
		}
		else
		{
			Cap.Off();
		}
		PlatformAPI.instance.OnLoad();
	}

	public void LoadEmpty()
	{
		Restart();
		AchievManager.PreLoad();
		Shop.Activate();
		CurrentHero.Hero.UpdateExp();
		UpgradeManager.Restart();
		AddProfit(0f);
		PostLoad();
		local_save_timer = -120f;
		Tutorial.ShowOnLoad();
		Settings.MusicOn = true;
		SettingMenu.LoadSettings();
		Cap.Off();
	}

	public void HardReset()
	{
		bool resetShop = false;
		ConfirmWindow.Open("HardResetTooltip".Translate(), delegate
		{
			SaveData.NewGame(resetShop);
			Gilding.Clear();
		}, "HardResetToggle".Translate(), delegate
		{
			resetShop = true;
		});
	}

	private IEnumerator wait_1_frame(Action action)
	{
		yield return null;
		yield return null;
		action();
	}

	public void SkipTime(int time)
	{
		SkipTime(time, insec: false);
	}

	public void SkipTime(BigNumber time, bool insec, bool real = true, bool showMessage = true, bool tw = false, bool stopSpells = false)
	{
		if (!insec)
		{
			time *= (BigNumber)3600.0;
		}
		if (VoidManaManager.enabled)
		{
			VoidMana.SetValue(0.0);
		}
		if (stopSpells)
		{
			Scrolls.StopOnWarp();
		}
		int num = ((time > 2000000000.0) ? 2000000000 : time.ToInt());
		if (tw || real)
		{
			Idle.ActivateIdle();
			Familiars.AddExp(num);
		}
		AddProfit(0f);
		BigNumber bigNumber = OfflineProduction.Value * PPS.Value * time;
		ManaChange(bigNumber);
		if (real)
		{
			Idle.Restart();
		}
		OfflineProductionData offlineProductionData = new OfflineProductionData();
		offlineProductionData.time = num;
		offlineProductionData.mana = bigNumber;
		SaveData.SkipTimeData data = SaveData.SkipTime(time, real, tw);
		offlineProductionData.LoadSkipTime(data);
		if (showMessage)
		{
			Offline.Open(offlineProductionData);
		}
		OnPostTimeSkip?.Invoke();
	}

	public void InfShards()
	{
		Scrolls.Period = 0.001f;
	}

	public void InfVoids()
	{
		VoidManaManager.VoidCore.TimeInterval.Change(0.0, 0.001);
	}

	public void TestSpellEff()
	{
		Scrolls.IncantationEfficiency.Change(0.0, 100.0);
	}

	public void TestSpellDur()
	{
		Scrolls.IncantationDuration.Change(0.0, 100.0);
	}

	public void RestartMana()
	{
		ManaManager.Restart();
	}

	public void EnableAutoSave()
	{
		if (!auto_enable)
		{
			auto_enable = true;
		}
	}
}
