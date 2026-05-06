using System;
using System.Collections;
using System.Collections.Generic;
using CardGame;
using Newtonsoft.Json;
using UnityEngine;

public class CardGameManager : MonoBehaviour
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

		public void Load(CardSave.SaveStatistic statistic)
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

	public static CardGameManager instance;

	public Combat Combat;

	public SkillMap SkillMap;

	public SkillPanel SkillPanel;

	public LocationManager Map;

	public Inventory Inventory;

	public LootSystem Loot;

	public TrophyDropCap TrophyCap;

	public FoodDropCap FoodCap;

	public SkillVEController SkillVE;

	public SkillSfxMap SkillSfx;

	public Autofight Autofight;

	public Deckbuilding Deckbuilding;

	public VoidManaManager Voidmana;

	public KeyManager Keys;

	public SpriteAtlas Atlas;

	public Statistic StatisticInfo;

	[SerializeField]
	private GameObject fightScreen;

	public GameObject preparingScreen;

	[SerializeField]
	private GameObject deckButton;

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
	private VariableInt Level;

	public Action<int> OnWin;

	public Action<Location> OnLocationComplete;

	public Action OnLocationWin;

	public bool isActive;

	public Func<Location, Location> OnLocationStart;

	public Action OnFight;

	private bool block_save;

	public void Init()
	{
		instance = this;
		mainCamera = Camera.main;
		Level = new VariableInt(1);
		Autofight.IdleSpeed = new VariableFloat(1f);
		Autofight.IdleLockedSpeed = new VariableFloat(1f);
		Autofight.IdleEncounter = new VariableInt(0);
		Autofight.IdleWinChance = new VariableFloat(0f);
		StatisticInfo = new Statistic();
		LoadJson();
		SkillMap = new SkillMap();
		SkillMap.Init();
		SkillPanel.Init();
		Map.Init();
		Combat.GetCharacter().Init();
		Voidmana.Init(Combat.GetCharacter());
		EndFight();
		Loot.Init();
		TrophyCap = new TrophyDropCap();
		TrophyCap.Activate();
		FoodCap = new FoodDropCap();
		FoodCap.Activate();
		Deckbuilding.Init();
		Keys.Init();
		CheckDeckButton();
		GameContext.ContextAddResource("Expedition.Level", Level);
		GameContext.ContextAddResource("Expedition.Monsters", StatisticInfo.Monsters);
		GameContext.ContextAddResource("Expedition.MonstersRealm", StatisticInfo.MonstersRealm);
		GameContext.ContextAddResource("Expedition.MaxStage", Map.maxReached);
		GameContext.ContextAddResource("Expedition.IdleSpeed", Autofight.IdleSpeed);
		GameContext.ContextAddResource("Expedition.IdleLockedSpeed", Autofight.IdleLockedSpeed);
		GameContext.ContextAddResource("Expedition.IdleEncounters", Autofight.IdleEncounter);
		GameContext.ContextAddResource("Expedition.IdleWinChance", Autofight.IdleWinChance);
		GameContext.ContextAddResource("Expedition.KeyDrop", Keys.KeyIncome);
		GameContext.ContextAddResource("Expedition.KeyTotal", Keys.keyCounter);
		GameContext.ContextAddResource("Expedition.RewardSize", Loot.RewardSize);
	}

	private void OnDestroy()
	{
		TrophyCap.Deactivate();
		FoodCap.Deactivate();
	}

	public bool CatalystsIsOn()
	{
		return catalystPanel.enabled;
	}

	public void PostLoad()
	{
		Level.SetValue(Level.ValueInt);
		StatisticInfo.Monsters.SetValue(StatisticInfo.Monsters.ValueInt);
		Map.PostLoad();
		CheckDeckButton();
	}

	public void SetLevel(int lvl)
	{
		Level.SetValue(lvl);
	}

	private void LoadJson()
	{
		GlobalData.Skills = JsonConvert.DeserializeObject<List<SkillData>>(Resources.Load<TextAsset>("JsonFiles/Skills").text);
	}

	public void StartGame()
	{
		if (GameManager.Instance.Paragon.ExpeditionIsAvailable && !Settings.BlockInput && !isActive)
		{
			cardCamera.gameObject.SetActive(value: true);
			cardCanvas.enabled = true;
			mainCamera.gameObject.SetActive(value: false);
			infoPanel.transform.position = new Vector3(cardCamera.transform.position.x, cardCamera.transform.position.y, 90f);
			infoPanel.worldCamera = cardCamera;
			shopPanel.transform.position = new Vector3(cardCamera.transform.position.x, cardCamera.transform.position.y, 90f);
			shopPanel.worldCamera = cardCamera;
			catalystPanel.transform.position = new Vector3(cardCamera.transform.position.x, cardCamera.transform.position.y, 90f);
			catalystPanel.worldCamera = cardCamera;
			Settings.BlockInput = true;
			SoundManager.Instance.ChangeMode(SoundManager.ModeList.CardGame);
			Map.gameObject.SetActive(value: true);
			Autofight.sign.TurnOff();
			isActive = true;
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
			Settings.BlockInput = false;
			SoundManager.Instance.ChangeMode(SoundManager.ModeList.MainGame);
		}
	}

	public void StartFight(Location location)
	{
		preparingScreen.SetActive(value: false);
		if (OnLocationStart != null)
		{
			location = OnLocationStart(location);
		}
		Combat.StartLocation(location, Map.GetStartLevel());
		CheckDeckButton();
	}

	public void EndFight()
	{
		Combat.EndFight();
		Loot.ApplyMobLoot();
		preparingScreen.SetActive(value: true);
		CheckDeckButton();
	}

	private void CheckDeckButton()
	{
		bool flag = Combat.GetCharacter().Level >= 10 && !Combat.IsFight;
		if (flag != deckButton.gameObject.activeSelf)
		{
			deckButton.gameObject.SetActive(flag);
		}
	}

	public CardSave Save()
	{
		Character character = Combat.GetCharacter();
		return new CardSave
		{
			lvl = character.Level,
			exp = character.exp,
			loc = Map.GetMaxReachedLevel(),
			lastLoc = Map.LastLocation,
			lastLocInf = Map.LastLocationInf,
			att = character.SaveAttributes(),
			hp = (int)character.HP.ValueFloat,
			fAtt = character.freePoints,
			items = Inventory.Save(),
			statistic = new CardSave.SaveStatistic(StatisticInfo),
			auto = Autofight.Save(),
			deck = Deckbuilding.Save(),
			keys = Keys.Save(),
			keysTotal = Keys.keyCounter.Value,
			exhibits = TrophyCap.Save(),
			food = FoodCap.Save()
		};
	}

	public void Load(CardSave save)
	{
		Character character = Combat.GetCharacter();
		if (save == null)
		{
			Map.Load(0, 0, 0);
			character.LoadLevel(1, 0);
			character.HP.SetValue((int)character.MaxHP.ValueFloat);
			character.LoadAttributes(null, 0);
			Inventory.Load(null);
			StatisticInfo.Load(null);
			Deckbuilding.Load(null);
			Keys.Load(null);
			Keys.keyCounter.SetValue(0.0);
			TrophyCap.Load(null);
			FoodCap.Load(null);
		}
		else
		{
			Map.Load(save.loc, save.lastLoc, save.lastLocInf);
			character.LoadLevel(save.lvl, save.exp);
			character.HP.SetValue(save.hp);
			character.LoadAttributes(save.att, save.fAtt);
			Inventory.Load(save.items);
			StatisticInfo.Load(save.statistic);
			Autofight.Load(save.auto);
			Deckbuilding.Load(save.deck);
			Keys.Load(save.keys);
			Keys.keyCounter.SetValue(save.keysTotal);
			TrophyCap.Load(save.exhibits);
			FoodCap.Load(save.food);
		}
		PostLoad();
	}

	public void SkipTime(int seconds)
	{
		Autofight.Skip(seconds);
		Loot.Offline(seconds);
		TrophyCap.Update(seconds);
		FoodCap.Update(seconds);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return) && !block_save)
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

	public void RealmChange()
	{
		StatisticInfo.MonstersRealm.SetValue(0uL);
		Map.OnRealm();
	}

	public void HardReset()
	{
		Character character = Combat.GetCharacter();
		Map.Load(0, 0, 0);
		character.LoadLevel(1, 0);
		Deckbuilding.Load(null);
		character.InitDeck();
		character.LoadAttributes(null, 0);
		Inventory.Load(null);
		CheckDeckButton();
	}
}
