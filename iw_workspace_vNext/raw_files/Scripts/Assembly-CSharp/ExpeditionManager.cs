using System;
using System.Collections;
using System.Collections.Generic;
using CardGame;
using Newtonsoft.Json;
using UnityEngine;

public class ExpeditionManager : MonoBehaviour
{
	public class Statistic
	{
		public VariableLong Monsters;

		public VariableLong MonstersRealm;

		public Statistic()
		{
			Monsters = new VariableLong(0uL);
			MonstersRealm = new VariableLong(0uL);
		}

		public void Load(ExpeditionSave.SaveStatistic statistic)
		{
			if (statistic == null)
			{
				Monsters.SetValue(0uL);
				MonstersRealm.SetValue(0uL);
			}
			else
			{
				Monsters.SetValue(statistic.monsters);
				MonstersRealm.SetValue(statistic.monstersRealm);
			}
		}
	}

	private static ExpeditionManager _instance;

	public LocationManager Map;

	public LootSystem Loot;

	public KeyManager Keys;

	public IdleInventory IdleInventory;

	public ExpeditionIdle ExpeditionIdle;

	public ExpeditionCharacter Character;

	public CombatLog Log;

	public AlertVisualController AlertVisualController;

	public TrophyDropCap TrophyCap;

	public FoodDropCap FoodCap;

	public ConsumableDropCap ConsumableCap;

	public SpriteAtlas Atlas;

	public AlertController Alerts;

	public VariableFloat Speed;

	public VariableFloat LockedSpeed;

	public VariableFloat WinChanceBonus;

	public VariableFloat WinChanceElixir;

	public VariableInt AddEncounters;

	public VariableFloat LootAmount;

	public VariableFloat XPBonus;

	public ExpeditionStatsPanel StatsPanel;

	public Confirmation Confirmation;

	[SerializeField]
	private Camera mainCamera;

	[SerializeField]
	private Camera cardCamera;

	[SerializeField]
	private Canvas cardCanvas;

	[SerializeField]
	private Canvas infoPanel;

	[SerializeField]
	private Canvas shopPanel;

	[SerializeField]
	private Canvas catalystPanel;

	[SerializeField]
	private AlertConfig alertConfig;

	public bool isActive;

	public Statistic StatisticInfo;

	private VariableInt Level;

	public Action<int> OnWin;

	public Action<Location> OnLocationComplete;

	public Action OnLocationWin;

	public Action OnFight;

	public Func<Location, Location> OnLocationStart;

	public Action OnAlertGenerated;

	public BottomPanelButton sign;

	private const float DEFAULT_LOSE_CHANCE = 0.2f;

	private bool block_save;

	public static ExpeditionManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindFirstObjectByType<ExpeditionManager>();
			}
			return _instance;
		}
	}

	private void Awake()
	{
		_instance = this;
	}

	public void Init()
	{
		mainCamera = Camera.main;
		Level = new VariableInt(1);
		Speed = new VariableFloat(1f);
		LockedSpeed = new VariableFloat(1f);
		AddEncounters = new VariableInt(0);
		WinChanceBonus = new VariableFloat(0f);
		WinChanceElixir = new VariableFloat(1f);
		LootAmount = new VariableFloat(1f);
		XPBonus = new VariableFloat(1f);
		StatisticInfo = new Statistic();
		LoadJson();
		Map.Init();
		Character.Init();
		Loot.Init();
		TrophyCap = new TrophyDropCap();
		TrophyCap.Activate();
		FoodCap = new FoodDropCap();
		FoodCap.Activate();
		ConsumableCap = new ConsumableDropCap();
		ConsumableCap.Activate();
		TrophyDropCap trophyCap = TrophyCap;
		trophyCap.OnCurrentChanged = (Action)Delegate.Combine(trophyCap.OnCurrentChanged, new Action(RecalculateLootAmount));
		FoodDropCap foodCap = FoodCap;
		foodCap.OnCurrentChanged = (Action)Delegate.Combine(foodCap.OnCurrentChanged, new Action(RecalculateLootAmount));
		ConsumableDropCap consumableCap = ConsumableCap;
		consumableCap.OnCurrentChanged = (Action)Delegate.Combine(consumableCap.OnCurrentChanged, new Action(RecalculateLootAmount));
		Keys.Init();
		IdleInventory.Init();
		Alerts = new AlertController();
		Alerts.Init(alertConfig);
		GameContext.ContextAddResource("Expedition.Level", Level);
		GameContext.ContextAddResource("Expedition.Monsters", StatisticInfo.Monsters);
		GameContext.ContextAddResource("Expedition.MonstersRealm", StatisticInfo.MonstersRealm);
		GameContext.ContextAddResource("Expedition.MaxStage", Map.maxReached);
		GameContext.ContextAddResource("Expedition.IdleSpeed", Speed);
		GameContext.ContextAddResource("Expedition.IdleLockedSpeed", LockedSpeed);
		GameContext.ContextAddResource("Expedition.IdleEncounters", AddEncounters);
		GameContext.ContextAddResource("Expedition.IdleWinChance", WinChanceBonus);
		GameContext.ContextAddResource("Expedition.IdleWinChanceElixir", WinChanceElixir);
		GameContext.ContextAddResource("Expedition.KeyDrop", Keys.KeyIncome);
		GameContext.ContextAddResource("Expedition.KeyTotal", Keys.keyCounter);
		GameContext.ContextAddResource("Expedition.RewardSize", Loot.RewardSize);
		GameContext.ContextAddResource("Expedition.RewardChance", Loot.RewardChance);
		GameContext.ContextAddResource("Expedition.IdleLootAmount", LootAmount);
		GameContext.ContextAddResource("Expedition.XPBonus", XPBonus);
	}

	private void OnDestroy()
	{
		if (Alerts != null)
		{
			Alerts.Deinit();
		}
		if (_instance == this)
		{
			_instance = null;
		}
		if (TrophyCap != null)
		{
			TrophyDropCap trophyCap = TrophyCap;
			trophyCap.OnCurrentChanged = (Action)Delegate.Remove(trophyCap.OnCurrentChanged, new Action(RecalculateLootAmount));
			TrophyCap.Deactivate();
		}
		if (FoodCap != null)
		{
			FoodDropCap foodCap = FoodCap;
			foodCap.OnCurrentChanged = (Action)Delegate.Remove(foodCap.OnCurrentChanged, new Action(RecalculateLootAmount));
			FoodCap.Deactivate();
		}
		if (ConsumableCap != null)
		{
			ConsumableDropCap consumableCap = ConsumableCap;
			consumableCap.OnCurrentChanged = (Action)Delegate.Remove(consumableCap.OnCurrentChanged, new Action(RecalculateLootAmount));
			ConsumableCap.Deactivate();
		}
	}

	private void Update()
	{
		if (isActive && Input.GetKeyDown(KeyCode.Escape) && !CatalystsIsOn())
		{
			ExitGame();
		}
		else if (Input.GetKeyDown(KeyCode.Return) && !block_save)
		{
			GameManager.Instance.Save();
			block_save = true;
			StartCoroutine(wait(10f, delegate
			{
				block_save = false;
			}));
		}
	}

	private IEnumerator wait(float t, Action action)
	{
		yield return new WaitForSecondsRealtime(t);
		action();
	}

	private void LoadJson()
	{
		GlobalData.Skills = JsonConvert.DeserializeObject<List<SkillData>>(Resources.Load<TextAsset>("JsonFiles/Skills").text);
	}

	public void SetLevel(int lvl)
	{
		Level.SetValue(lvl);
	}

	public bool CatalystsIsOn()
	{
		return catalystPanel.enabled;
	}

	public void PostLoad()
	{
		SetLevel(Character.Level);
		StatisticInfo.Monsters.SetValue(StatisticInfo.Monsters.ValueInt);
		Map.PostLoad();
		RecalculateLootAmount();
		Alerts.PostLoad();
		AlertVisualController.Refresh();
	}

	private void RecalculateLootAmount()
	{
		float num = ((TrophyCap.Current > 0) ? ((float)TrophyCap.Current / (float)TrophyCap.Cap) : 0f);
		float num2 = ((ConsumableCap.Current > 0) ? ((float)ConsumableCap.Current / (float)ConsumableCap.Cap) : 0f);
		float value;
		if (GameManager.Instance.Paragon.FamiliarsIsAvailable)
		{
			float num3 = ((FoodCap.Current > 0) ? ((float)FoodCap.Current / (float)FoodCap.Cap) : 0f);
			value = (num + num3 + num2) / 3f;
		}
		else
		{
			value = (num + num2) / 2f;
		}
		LootAmount.SetValue(value);
	}

	public void StartGame()
	{
		if (GameManager.Instance.Paragon.ExpeditionIsAvailable && !Settings.BlockInput && !isActive)
		{
			cardCamera.gameObject.SetActive(value: true);
			mainCamera.gameObject.SetActive(value: false);
			cardCanvas.enabled = true;
			infoPanel.transform.position = new Vector3(cardCamera.transform.position.x, cardCamera.transform.position.y, 90f);
			infoPanel.worldCamera = cardCamera;
			shopPanel.transform.position = new Vector3(cardCamera.transform.position.x, cardCamera.transform.position.y, 90f);
			shopPanel.worldCamera = cardCamera;
			catalystPanel.transform.position = new Vector3(cardCamera.transform.position.x, cardCamera.transform.position.y, 90f);
			catalystPanel.worldCamera = cardCamera;
			Settings.BlockInput = true;
			SoundManager.Instance.ChangeMode(SoundManager.ModeList.CardGame);
			isActive = true;
			ExpeditionIdle.RefreshUIState();
			ExpeditionIdle.RefreshMobDisplay();
		}
	}

	public void ExitGame()
	{
		if (isActive)
		{
			isActive = false;
			Map.gameObject.SetActive(value: false);
			mainCamera.gameObject.SetActive(value: true);
			cardCamera.gameObject.SetActive(value: false);
			cardCanvas.enabled = false;
			infoPanel.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 90f);
			infoPanel.worldCamera = mainCamera;
			shopPanel.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 90f);
			shopPanel.worldCamera = mainCamera;
			catalystPanel.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 90f);
			catalystPanel.worldCamera = mainCamera;
			Loot.ApplyMobLoot();
			Settings.BlockInput = false;
			SoundManager.Instance.ChangeMode(SoundManager.ModeList.MainGame);
		}
	}

	public void StartFight(Location location)
	{
		ExpeditionIdle.StartExpedition(location);
	}

	public ExpeditionSave Save()
	{
		return new ExpeditionSave
		{
			lvl = Character.Level,
			exp = Character.exp,
			loc = Map.GetMaxReachedLevel(),
			lastLoc = Map.LastLocation,
			lastLocInf = Map.LastLocationInf,
			statistic = new ExpeditionSave.SaveStatistic(StatisticInfo),
			keys = Keys.Save(),
			keysTotal = Keys.keyCounter.Value,
			exhibits = TrophyCap.Save(),
			food = FoodCap.Save(),
			consumableDropCap = ConsumableCap.Save(),
			idleItems = IdleInventory.Save(),
			completedLocations = Map.SaveCompletedLocations(),
			idleState = ExpeditionIdle.GetSaveData(),
			alerts = Alerts.Save()
		};
	}

	public void Load(ExpeditionSave save)
	{
		ExpeditionIdle.GiveUp();
		if (save == null)
		{
			Map.Load(0, 0, 0);
			Character.LoadLevel(1, 0);
			IdleInventory.Load(null);
			StatisticInfo.Load(null);
			Keys.Load(null);
			Keys.keyCounter.SetValue(0.0);
			ExpeditionIdle.SetAutoToggle(value: false);
			TrophyCap.Load(null);
			FoodCap.Load(null);
			ConsumableCap.Load(null);
			Alerts.Load(null);
		}
		else
		{
			Map.Load(save.loc, save.lastLoc, save.lastLocInf, save.completedLocations);
			Character.LoadLevel(save.lvl, save.exp);
			IdleInventory.Load(save.idleItems);
			StatisticInfo.Load(save.statistic);
			Keys.Load(save.keys);
			Keys.keyCounter.SetValue(save.keysTotal);
			ExpeditionIdle.SetAutoToggle(save.idleState != null && save.idleState.auto);
			TrophyCap.Load(save.exhibits);
			FoodCap.Load(save.food);
			ConsumableCap.Load(save.consumableDropCap);
			Alerts.Load(save.alerts);
			ExpeditionIdle.LoadSaveData(save.idleState);
			if (!ExpeditionIdle.IsRunning || !Alerts.IsAlertRunning)
			{
				Alerts.CheckAndGenerate();
			}
		}
		PostLoad();
	}

	public void SkipTime(int seconds)
	{
		ExpeditionIdle.RequestSkip(seconds);
	}

	public void TickRewardsVirtual(float dt)
	{
		Loot.Offline(dt);
		TrophyCap.Update(dt);
		FoodCap.Update(dt);
		ConsumableCap.Update(dt);
	}

	public void RealmChange()
	{
		StatisticInfo.MonstersRealm.SetValue(0uL);
		Map.OnRealm();
	}

	public void HardReset()
	{
		ExpeditionIdle.GiveUp();
		Map.Load(0, 0, 0);
		Character.LoadLevel(1, 0);
		IdleInventory.Load(null);
	}

	public int GetTotalEncounters()
	{
		int num = Alerts.GetEncounterCount(GetCurrentLocation());
		if (num == 0)
		{
			num = 10;
		}
		return num + AddEncounters.ValueInt;
	}

	public float GetWinChance(Location loc)
	{
		if (WinChanceBonus.ValueFloat > 100f)
		{
			return 1f;
		}
		int level = Character.Level;
		float num = 0.2f + (float)(((loc != null) ? loc.Level : 0) - level) * 0.015f;
		if (num < 0.05f)
		{
			num = 0.05f;
		}
		float num2 = WinChanceBonus.ValueFloat / 100f + WinChanceElixir.ValueFloat - 1f;
		num -= num2;
		if (num < 0f)
		{
			num = 0f;
		}
		if (GameManager.Instance.Shop.AutoExpedition > 1)
		{
			num /= 2f;
		}
		return 1f - num;
	}

	public float GetWinChanceDefault()
	{
		if (WinChanceBonus.ValueFloat > 100f)
		{
			return 1f;
		}
		float num = 0.2f;
		num -= WinChanceBonus.ValueFloat / 100f + WinChanceElixir.ValueFloat - 1f;
		if (num < 0f)
		{
			num = 0f;
		}
		if (GameManager.Instance.Shop.AutoExpedition > 1)
		{
			num /= 2f;
		}
		return 1f - num;
	}

	public float GetSpeed(Location loc)
	{
		float num = Speed.ValueFloat;
		if (loc.IsKeyLocked)
		{
			num *= LockedSpeed.ValueFloat;
			if (GameManager.Instance.Shop.AutoExpedition > 1)
			{
				num *= 2f;
			}
		}
		return num * GetLevelSpeedModifier((loc != null) ? loc.Level : 0);
	}

	public float GetSpeedDefault()
	{
		return Speed.ValueFloat;
	}

	public float GetLevelSpeedModifier(int locLevel)
	{
		if (locLevel > 100)
		{
			locLevel = 100;
		}
		int num = locLevel - Character.Level;
		if (num == 0)
		{
			return 1f;
		}
		float num2 = 2f;
		if (num <= -40)
		{
			return num2;
		}
		if (num <= 0)
		{
			return Mathf.Lerp(num2, 1f, (float)(num + 40) / 40f);
		}
		return 1f;
	}

	public Location GetCurrentLocation()
	{
		if (ExpeditionIdle.IsRunning && ExpeditionIdle.CurrentLocation != null)
		{
			return ExpeditionIdle.CurrentLocation;
		}
		return Map.Selected;
	}

	public float GetXPReward()
	{
		Location currentLocation = GetCurrentLocation();
		float num = XPBonus.ValueFloat * Alerts.GetXPMultiplier();
		if (!(currentLocation != null))
		{
			return num;
		}
		return num * GetXPRewardModifier(currentLocation.Level);
	}

	public float GetWinChance()
	{
		Location currentLocation = GetCurrentLocation();
		if (!(currentLocation != null))
		{
			return GetWinChanceDefault();
		}
		return GetWinChance(currentLocation);
	}

	public float GetSpeed()
	{
		Location currentLocation = GetCurrentLocation();
		if (!(currentLocation != null))
		{
			return GetSpeedDefault();
		}
		return GetSpeed(currentLocation);
	}

	public float GetRewardChance()
	{
		Location currentLocation = GetCurrentLocation();
		float valueFloat = Loot.RewardChance.ValueFloat;
		if (!(currentLocation != null))
		{
			return valueFloat;
		}
		return valueFloat * GetRewardModifier(currentLocation.Level);
	}

	private int GetDisplayLevel(Location loc)
	{
		if (loc == null)
		{
			return 0;
		}
		if (ExpeditionIdle.IsRunning)
		{
			return loc.Level;
		}
		int levelOverrideForDisplay = Alerts.GetLevelOverrideForDisplay(loc);
		if (levelOverrideForDisplay <= 0)
		{
			return loc.StartLevel;
		}
		return levelOverrideForDisplay;
	}

	public int GetTotalEncountersDisplay()
	{
		if (ExpeditionIdle.IsRunning)
		{
			return GetTotalEncounters();
		}
		int num = Alerts.GetEncounterCountForDisplay(GetCurrentLocation());
		if (num == 0)
		{
			num = 10;
		}
		return num + AddEncounters.ValueInt;
	}

	public float GetWinChanceDisplay()
	{
		if (ExpeditionIdle.IsRunning)
		{
			return GetWinChance();
		}
		Location currentLocation = GetCurrentLocation();
		if (currentLocation == null)
		{
			return GetWinChanceDefault();
		}
		int displayLevel = GetDisplayLevel(currentLocation);
		if (WinChanceBonus.ValueFloat > 100f)
		{
			return 1f;
		}
		float num = 0.2f + (float)(displayLevel - Character.Level) * 0.015f;
		if (num < 0.05f)
		{
			num = 0.05f;
		}
		float num2 = WinChanceBonus.ValueFloat / 100f + WinChanceElixir.ValueFloat - 1f;
		num -= num2;
		if (num < 0f)
		{
			num = 0f;
		}
		if (GameManager.Instance.Shop.AutoExpedition > 1)
		{
			num /= 2f;
		}
		return 1f - num;
	}

	public float GetXPRewardDisplay()
	{
		if (ExpeditionIdle.IsRunning)
		{
			return GetXPReward();
		}
		Location currentLocation = GetCurrentLocation();
		float valueFloat = XPBonus.ValueFloat;
		if (!(currentLocation != null))
		{
			return valueFloat;
		}
		return valueFloat * GetXPRewardModifier(GetDisplayLevel(currentLocation));
	}

	public float GetRewardChanceDisplay()
	{
		if (ExpeditionIdle.IsRunning)
		{
			return GetRewardChance();
		}
		Location currentLocation = GetCurrentLocation();
		float valueFloat = Loot.RewardChance.ValueFloat;
		if (!(currentLocation != null))
		{
			return valueFloat;
		}
		return valueFloat * GetRewardModifier(GetDisplayLevel(currentLocation));
	}

	public float GetSpeedDisplay()
	{
		if (ExpeditionIdle.IsRunning)
		{
			return GetSpeed();
		}
		Location currentLocation = GetCurrentLocation();
		if (currentLocation == null)
		{
			return GetSpeedDefault();
		}
		float num = Speed.ValueFloat;
		if (currentLocation.IsKeyLocked)
		{
			num *= LockedSpeed.ValueFloat;
			if (GameManager.Instance.Shop.AutoExpedition > 1)
			{
				num *= 2f;
			}
		}
		return num * GetLevelSpeedModifier(GetDisplayLevel(currentLocation));
	}

	public float GetRewardModifier(int locLevel)
	{
		int num = locLevel - Character.Level;
		if (num <= -40)
		{
			return 0f;
		}
		if (num <= 0)
		{
			return Mathf.Lerp(0f, 1f, (float)(num + 40) / 40f);
		}
		return 1f + (float)num * 0.02f;
	}

	public float GetXPRewardModifier(int locLevel)
	{
		int num = locLevel - Character.Level;
		if (num <= -40)
		{
			return 0f;
		}
		if (num <= 0)
		{
			return Mathf.Lerp(0f, 1f, (float)(num + 40) / 40f);
		}
		return 1f + (float)num * 0.01f;
	}
}
