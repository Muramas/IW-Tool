using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CardGame;
using Newtonsoft.Json;
using Unity.IO.Compression;
using UnityEngine;

public class SaveData
{
	public class AchievementSave
	{
		public AchievementKey key;

		public int level;

		public AchievementSave()
		{
			key = AchievementKey.None;
			level = 0;
		}

		public AchievementSave(AchievementKey _key, int _level)
		{
			key = _key;
			level = _level;
		}
	}

	public class SkipTimeData
	{
		public int rune;

		public int attributes;

		public int red;

		public int blue;

		public int green;

		public int yellow;

		public BigNumber edust;
	}

	private class SaveFileMeta
	{
		public int ID;

		public string Name;

		public DateTime Time;

		public string Path;

		public SaveFileMeta(string[] strs, string name)
		{
			ID = int.Parse(strs[1]);
			Name = name;
			Time = DateTime.Parse(strs[2]);
		}
	}

	public class IntIntPair
	{
		public int ID;

		public int Value;

		public IntIntPair()
		{
			ID = (Value = 0);
		}

		public IntIntPair(int _key, int _value)
		{
			ID = _key;
			Value = _value;
		}
	}

	public class IntBignumberPair
	{
		public int ID;

		public BigNumber Value;

		public IntBignumberPair()
		{
			ID = 0;
			Value = 0.0;
		}

		public IntBignumberPair(int _key, BigNumber _value)
		{
			ID = _key;
			Value = _value;
		}
	}

	public class SpellUse
	{
		public Spells key;

		public double value;

		public double aver;

		public SpellUse()
		{
			key = Spells.None;
			value = 0.0;
			aver = 0.0;
		}

		public SpellUse(Spells _k, double _v, double _a = 0.0)
		{
			key = _k;
			value = _v;
			aver = _a;
		}
	}

	[Serializable]
	public class FloatFloatPair
	{
		public float key;

		public float value;

		public FloatFloatPair()
		{
			key = 0f;
			value = 0f;
		}

		public FloatFloatPair(float _k, float _v)
		{
			key = _k;
			value = _v;
		}
	}

	public class IntFloatPair
	{
		public int key;

		public float value;

		public IntFloatPair()
		{
			key = 0;
			value = 0f;
		}

		public IntFloatPair(int _k, float _v)
		{
			key = _k;
			value = _v;
		}
	}

	public DateTime SaveTime;

	public HeroesNames Hero;

	public PetNames Pet = PetNames.None;

	public BigNumber PetExp = new BigNumber(0.0);

	public BigNumber CharExp = new BigNumber(0.0);

	public BigNumber CharExpMult = new BigNumber(1.0);

	public BigNumber Mana = new BigNumber(0.0);

	public BigNumber VMana = new BigNumber(0.0);

	public BigNumber Souls = new BigNumber(0.0);

	public int FPS = 1;

	public bool NumberFormat;

	public bool FloorMultibuy;

	public bool SoundOn = true;

	public bool MusicOn = true;

	public bool VoidSFX = true;

	public bool ClassChooiseMessage = true;

	public bool RedColorCost = true;

	public bool AutoClose = true;

	public bool FlyingText = true;

	public bool Particles = true;

	public bool OrbParticles = true;

	public bool BackParticles = true;

	public bool ThrowShards;

	public bool Quotes = true;

	public bool ShowComics = true;

	public bool CTips = true;

	public bool MirrorChar;

	public bool MirrorPet;

	public bool Cursor = true;

	public bool SeasonalVFX = true;

	public float MusicVolume = 1f;

	public float SoundVolume = 1f;

	public int Language;

	public int BuyPack = 1;

	public int Ascends;

	public int AscendsRealm;

	public int TotalBuildings;

	public BigNumber ManaAllTime = new BigNumber(0.0);

	public BigNumber ManaRealm = new BigNumber(0.0);

	public BigNumber ManaSession = new BigNumber(0.0);

	public BigNumber VoidManaAllTime = new BigNumber(0.0);

	public BigNumber VoidManaRealm = new BigNumber(0.0);

	public BigNumber VoidManaSession = new BigNumber(0.0);

	public BigNumber MaxVMSession = new BigNumber(0.0);

	public BigNumber ClicksTotal = 0.0;

	public BigNumber ClicksRealm = 0.0;

	public BigNumber Clicks = 0.0;

	public BigNumber AutoClicksTotal = 0.0;

	public BigNumber AutoClicksRealm = 0.0;

	public BigNumber AutoClicks = 0.0;

	public double AverageClicks;

	public BigNumber CastSpellTotal = 0.0;

	public BigNumber CastSpellRealm = 0.0;

	public BigNumber CastSpell = 0.0;

	public BigNumber ShardsTotal = new BigNumber(0.0);

	public BigNumber ShardsRealm = new BigNumber(0.0);

	public BigNumber ShardsSession = new BigNumber(0.0);

	public ulong ClickableCollect;

	public ulong ClickableCollectRealm;

	public ulong ClickableCollectTotal;

	public BigNumber AverageEntities = 0.0;

	public BigNumber CTTotal = new BigNumber(0.0);

	public BigNumber HCTotal = new BigNumber(0.0);

	public BigNumber LSTotal = new BigNumber(0.0);

	public List<ulong> ClassTime = new List<ulong>();

	public ulong HeroPlayedTime;

	public ulong PetPlayedTime;

	public BigNumber HeroSkipedPlayedTime = 0.0;

	public BigNumber PetSkipedPlayedTime = 0.0;

	public ulong TimeTotal;

	public ulong TimeRealm;

	public ulong TimeSession;

	public BigNumber SkipedTimeTotal = 0.0;

	public BigNumber SkipedTimeRealm = 0.0;

	public BigNumber SkipedTimeSession = 0.0;

	public ulong TimeIdleTotal;

	public ulong TimeIdleRealm;

	public ulong TimeIdleSession;

	public ulong TimeOfflineTotal;

	public ulong TimeOfflineRealm;

	public ulong TimeOfflineSession;

	public int BoughtUpgrades;

	public int PetMaxLevel;

	public int PetMaxLevelAllTime;

	public int HeroMaxLevelAllTime;

	public int ApprenticeMaxLevelRealm;

	public int Bats;

	public int BatsE;

	public int BatsR;

	public int BatsOnly;

	public BigNumber EDE = 0.0;

	public List<int> Upgrades;

	public int[] BuildingLevels = new int[9];

	public BuildingManager.CatalystSave Catalysts;

	public Spells[] ChoosenSpells = new Spells[7];

	public BigNumber ShardsPool = 0.0;

	public string[] SpellShards = new string[7];

	public int[] Autocast = new int[7];

	public int Stance;

	public List<IntFloatPair> OtherSpellShards = new List<IntFloatPair>();

	public BigNumber AccumCasts;

	public List<SpellUse> SpellUses;

	public List<SpellUse> SpellUsesTR;

	public List<IntBignumberPair> Resources;

	public List<IntIntPair> Jars;

	public CraftSave Craft;

	public GildingManager.GildingSave Gilding;

	public BigNumber ResCollected;

	public List<BigNumber> TCollectedRes = new List<BigNumber>();

	public List<BigNumber> RCollectedRes = new List<BigNumber>();

	public DropToSave DropMB;

	public CorruptionManager.SaveData Corruption;

	public GamblingToSave Gamble;

	public EventManager.EventSave EventSave;

	public SetsPanel.SaveData SpellPresets;

	public ItemPresetWindow.SaveData ItemPresets;

	public ItemPresetWindow.ItemPresetMap ItemPresetsMap;

	public List<AchievementSave> AchievementsSave;

	public List<AchievementSave> RAchieves;

	public AchievementManager.TriumphSave Triumphs;

	public GalleryToSave GalleryH;

	public GalleryToSave GalleryP;

	public InteriorManager.SaveData Interior;

	public int ActiveChID = -1;

	public List<int> CompletedChIDs;

	public BigNumber ChProgress = 0.0;

	public TrialSave Trial;

	public PantheonManager.SaveData Pantheon;

	public GoodsManager.SaveData Shop;

	public List<IntIntPair> Buffs;

	public int AttTotal;

	public int AttSearched;

	public int AttFree;

	public int AttResets = 3;

	public double AttProgress;

	public int Int;

	public int Ins;

	public int Scr;

	public int Wis;

	public int Dom;

	public int Pat;

	public int Mas;

	public int Emp;

	public int Ver;

	public int Paragon;

	public BigNumber ClassBonusStacks;

	public BigNumber ClassSubRes;

	public List<int> Social;

	public RealmManager.SaveData Memories;

	public RealmcraftManager.SaveData Realm;

	public AscensionManager.SaveData Ascention;

	public ExpeditionSave Card;

	public TaskManager.SaveData Quests;

	public FamiliarManager.SaveData Familiars;

	public int SaveVersion;

	public int VipTier;

	public int Relics;

	public int Real;

	public int Coins;

	public int Nullifiers;

	public int DECore = 1;

	public List<int> Warps;

	public int GM;

	public bool Catcher;

	public int CatcherUpgrade;

	public bool EVip3;

	public List<int> RPacks;

	[NonSerialized]
	public Action OnSave;

	[NonSerialized]
	public Action OnLoad;

	private string emptySave = "H4sIAAAAAAAEAOy9B2AcSZYlJi9tynt/SvVK1+B0oQiAYBMk2JBAEOzBiM3mkuwdaUcjKasqgcplVmVdZhZAzO2dvPfee++999577733ujudTif33/8/XGZkAWz2zkrayZ4hgKrIHz9+fB8/In7xR6+zy/xNscg/evTR3s7e/vbO/e2dvTe79x/t7j26f++nPhp99O28rj56tDP66GXefvRod2dHfj19t/ro0S/+6Its2RZNk1GLMX1Bn1bLfEkNd37J6KOTeVbfph19md3Y6Cdv1ep1tS6bG1s9e/mahjL66MV6McnrZ1W9yOib86xscvqyrKr6i3XZFpP19UeP2nqdM9zl7Mul+fOLdVNM8ae+85NVMXv97Pc2X5+UWdOczKuqaPIvcsLiIjdfvcpnJ1VZ1SdV05rPjtdtdVJWjW30rLwulhdv8ne2ycusbotpmTfmgy/rSe+zJ9n0be/DN/O6unpNMzFrLLo/sa5a1+L1vLo6qRbF1H5y8qZY2T++KOqa8CUI9n35iBlCPzlZ103lNQB9frIq1+CsXUwAEzD45Mn6+iUhzBNx3EzzJRDcsb+/yrNywR+8qdqsfLIuyhkRRdqAF47LUjj3FtylwG7R8jVNV1Etb2yLGX8fLEz722FiWt8Wmy+ydz/5xW0bn5TF9G3DVL1l29shLW1vbCbcfnsUXPvboeHa39j0JGva16ucpvBWmNjmt0PENr+xpYjn7ZCQtrfDQNq+F19kkzInBVXmU3za/9CJZfcbRR/fvLndUL59crt2z1/frh2rXZHG7+0+GO28z3/fF0v3ssyu85nA2D1gS+d/tCOtXr8tVvnM/+IGzAjMe7+DRjpuGgz/qcQ3f9qJxQcC33vpJt6w7W/JS7b9bfkJbc9mpcFnx33i6Xb9xMLUz748Py+LZedV/TB8Wz/0ATyp1hfz9qvVRZ3NcjEYNAGkI5/nl3nJBsf722pwndze57BPq1VN4yqmufnWTAWsbit94JdT+9sr+9uXy/Ka/zh9enoj0RzW3/s+bKRYPe4Sn8X49iQjIl03Les77w8AuB0vtMc3Njm/uUn7xY1Nzm9u0r66scn5TU2gC8j7avIl61/Qgh3XoX+IUNzQuEnf+2iHPN/+/78v1mWawXfz5wIA2mw5FS76sp3ndQgQb06n6wVsws12id/9qjFcYP9880o+eJU31bqe8ve/+KOzp8yHP5mV645W2QtB3yPCSPO992t+7/2a779f8/u3bE4D/05Wx8a80x+X/cjhbj9y+O0A6EmdnbcsO6TjyvL0FhHL57du+bKuLmrSTWfLyxxcw02mlQhodxz39u71R7L34GF/LHv3d/uj2Tu4j/GctflCOef0F62hteWPL6o1sPrFFif6HaJC8Q/ik1/80Xer+q1i+LqsmE9/iXC8Ova/hFkvz+rp/HNRTArC/fVRrbx5I2VMw9upqC/ot6wsfkC93IbscKDR6HUO1wQU2EXgCu19WTLJ39QZYpxfTGJN//Cn0oB/5jf2QZ3sfu039772m/e+5pusEjOaaJ1VDP9JtVx3lNFupEsv9PrFH03Xdc1f3aTGbzGrRtN944BHHy1Iz+b19W1YpWEdbUi6sF4C40fsru4teOgmh8c2faWqeVPzn6NvYT7+P4Ln07paffEEZG/FEft0/PDgwf2D+yS+q6zOFh0jDB6v6nq9atU9xWs13OPx/U8f7u4djD6CVtim16csCzzj2UL0Iuv1bEEBDV6d8XcT/neayY+1/JB02AX/+5b/rRX/Wj1TgnR6SeNAdg3A8gvNW+TIN8mvhIBaBPD023wpHzd5Ll03y+qKf5llLQH5iNyU3W3+35udnUf8P/JJ+iNsVtkVDX5vf49keffgHg12mXGC75Rl4iNtwt0YCXxJyjgXx2RVS3BCX/2iddYUquFhVG5udTydFyQ9Cxp7I4Nnd0U/Nj5pXawXqznD0d+fZUWJL3kKiDfr62+z9hb6/RL38cvOx2dLGlKBzNMv/mjC2aRf/FE2bQs0INqtl2U1fSsGkFpX9UQaFO2lYt9pMV8D4GYoU851NRshMS0A4GQOGw2WoxTbqsxJ6PCRkuJk7tniG5RLXYjemxo4yjqkDi1o+aguNOpR7jj49N7BPUB8m183bPyIpzLA2hEJIkz29TfWsI2z95QcZBX6k8XqTQFgQIz+uOcSmiVnDnfwKzCkX15nS0Hlxbosi/PC8PzTU5LPnDH4blbD/Ir8Uv+ff8EtKHyZztGNySvK3xoVcRP4I0SafFa0JuB7hUSikpT4eaqUmhFXXcunV3n+Vn7/JYoylAvrCEstyh608mk/F37v4fjew4P7Ow8PfuqjXwKTeH6uciDTJwrnlyjJ2cYyD7ReWoT+eM2+k84T/f2sznVQbQvPCsK1y385vmBeOGM2wE/B9fVUpuK7hVK2WvDPl5m0+yKTz08XK/75kzp1lB7OLpRqnC55Ah+AYpdbpcvwwuv1hK3GDW1fVzILdoK/yBeVkPoXM6MsBEH/480ArwpihVs3Z7LfurWLujGH/Nr1a54NZRjNkByrUvjoI0+e5SVQdrHMGxaGX2xeesqC9hF4hvPaxjL9Xvm1WUwxPukOe+LWc1/m9cU1IwvKk4uC10p1XvN3Mq2kcSzrPg9/P1ue85/nxEz8y3ylTvB0RrB295gMGb78xfBkd9gr3WEPc4f1Af17n//9VIhUuLiiaTMiJuVD8PKiWsKwyHyaP0ySht6b5dDL/dSFDZ1FLYFm+O128UAxg522WoosArG4iOEDxE6euoppqJOqUEkaVlG3V0mqEAWb/ycAAP//IF+cB1UbAAA=";

	[JsonIgnore]
	private string main = "save";

	[JsonIgnore]
	private string back = "save_backup";

	public SaveData()
	{
		CreateFolder();
	}

	private void prepare_data()
	{
		GameManager.Instance.OnPreSave?.Invoke();
		GameManager.Instance.AttributeManager.PreSave();
		try
		{
			SaveTime = TimeUtils.GetTime();
			Hero = GameManager.Instance.CurrentHero.Hero.NameKey;
			Pet = ((GameManager.Instance.CurrentPet.Pet == null) ? PetNames.None : GameManager.Instance.CurrentPet.Pet.NameKey);
			PetExp = ((GameManager.Instance.CurrentPet.Pet == null) ? ((BigNumber)0.0) : GameManager.Instance.CurrentPet.Pet.TotalExp.Value);
			CharExp = GameManager.Instance.CurrentHero.ExpFlat.Value;
			CharExpMult = GameManager.Instance.CurrentHero.ExpStack.Value;
			if (Hero == HeroesNames.Ironsoul)
			{
				Stance = (int)(GameManager.Instance.CurrentHero.Hero as Ironsoul).active;
			}
			else if (Hero == HeroesNames.Oni)
			{
				Stance = (int)(GameManager.Instance.CurrentHero.Hero as Oni).active;
			}
			else if (Hero == HeroesNames.Alchemist || Hero == HeroesNames.Desolator)
			{
				Stance = (int)(GameManager.Instance.CurrentHero.Hero as Alchemist).active;
			}
			else if (Hero == HeroesNames.Artificer)
			{
				Stance = (GameManager.Instance.CurrentHero.Hero as Artificer).Parts.Value.ToInt();
			}
			else if (Hero == HeroesNames.Shapeshifter)
			{
				Stance = (int)(GameManager.Instance.CurrentHero.Hero as Shapeshifter).Current;
			}
			Mana = GameManager.Instance.Mana.Value;
			if (ShadowEnergyManager.instance.isActivated())
			{
				VMana = 0.0;
			}
			else
			{
				VMana = GameManager.Instance.VoidMana.Value;
			}
			Souls = Reborn.Souls.Value;
			BuyPack = GameManager.Instance.BuyPack;
		}
		catch (Exception ex)
		{
			Debug.Log("null step 1");
			Debug.Log(ex.Message);
			Debug.Log(ex.StackTrace);
			throw;
		}
		try
		{
			prepare_settings();
			prepare_statistic();
			prepare_buildings();
			prepare_upgrades();
			prepare_shards();
			prepare_spells();
			SpellPresets = GameManager.Instance.SpellBook.SetsPanel.Save();
			prepare_achievements();
			GalleryH = GameManager.Instance.Gallery.Save();
			GalleryP = GameManager.Instance.PetGallery.Save();
			Interior = GameManager.Instance.Interior.Save();
			prepare_challenges();
			Trial = GameManager.Instance.Trials.Save();
			Pantheon = GameManager.Instance.Pantheon.Save();
			prepare_shop();
			prepare_familiars();
			prepare_attributes();
		}
		catch (Exception ex2)
		{
			Debug.Log("null step 2");
			Debug.Log(ex2.Message);
			Debug.Log(ex2.StackTrace);
			throw;
		}
		try
		{
			Paragon = GameManager.Instance.Paragon.MaxLevel;
			ClassBonusStacks = GameManager.Instance.CurrentHero.ClassBonusStacks.Value;
			ClassSubRes = new SubResourceSave().Save();
			prepare_craft();
			ItemPresetsMap = GameManager.Instance.Craft.window.presets.SaveSetsLegacy();
			ItemPresets = GameManager.Instance.Craft.window.presets.Save();
			DropMB = GameManager.Instance.MBSpawner.Save();
			Corruption = GameManager.Instance.CorruptionManager.Save();
			Gamble = GameManager.Instance.Craft.window.craftingMenu.Save();
		}
		catch (Exception ex3)
		{
			Debug.Log("null step 3");
			Debug.Log(ex3.Message);
			Debug.Log(ex3.StackTrace);
			throw;
		}
		GameManager.Instance.AttributeManager.PostSave();
		EventSave = GameManager.Instance.Event.Save();
		if (OnSave != null)
		{
			OnSave();
		}
		SaveVersion = 83;
	}

	private void prepare_settings()
	{
		FPS = Settings.TargetFPS;
		NumberFormat = Settings.ScientificNumber;
		FloorMultibuy = Settings.FloorMultiBuy;
		SoundOn = Settings.SoundOn;
		MusicOn = Settings.MusicOn;
		VoidSFX = Settings.VoidSFX;
		ClassChooiseMessage = Settings.ConfirmMessageChoose;
		RedColorCost = Settings.RedCost;
		AutoClose = Settings.AutoClose;
		FlyingText = Settings.FlyingText;
		Particles = Settings.Particles;
		OrbParticles = Settings.OrbParticles;
		BackParticles = Settings.BackParticles;
		ThrowShards = Settings.ThrowShards;
		Quotes = Settings.Quotes;
		CTips = Settings.ColoredTips;
		ShowComics = Settings.ShowComics;
		MirrorChar = Settings.MirrorChar;
		MirrorPet = Settings.MirrorPet;
		Cursor = Settings.Cursor;
		SeasonalVFX = Settings.SeasonalVFX;
		MusicVolume = Settings.MusicVolume;
		SoundVolume = Settings.SoundVolume;
	}

	private void prepare_statistic()
	{
		Ascends = Statistic.Ascends.ValueInt;
		AscendsRealm = Statistic.AscendsInRealm.ValueInt;
		TotalBuildings = Statistic.TotalBuildings.ValueInt;
		ManaAllTime = Statistic.ManaAllTime.Value;
		ManaRealm = Statistic.ManaRealm.Value;
		ManaSession = Statistic.ManaSession.Value;
		VoidManaAllTime = Statistic.VoidManaAllTime.Value;
		VoidManaRealm = Statistic.VoidManaRealm.Value;
		VoidManaSession = Statistic.VoidManaSession.Value;
		MaxVMSession = Statistic.MaxVoidManaSession.Value;
		ClicksTotal = Statistic.ClicksTotal.Value;
		ClicksRealm = Statistic.ClicksRealm.Value;
		Clicks = Statistic.Clicks.Value;
		AutoClicksTotal = Statistic.AutoClicksTotal.Value;
		AutoClicksRealm = Statistic.AutoClicksRealm.Value;
		AutoClicks = Statistic.AutoClicks.Value;
		AverageClicks = GameManager.Instance.Orb.getAutoclickPerSec;
		ShardsTotal = Statistic.ShardsTotal.Value;
		ShardsRealm = Statistic.ShardsRealm.Value;
		ShardsSession = Statistic.ShardsSession.Value;
		CastSpellTotal = Statistic.CastSpellTotal.Value;
		CastSpellRealm = Statistic.CastSpellRealm.Value;
		CastSpell = Statistic.CastSpell.Value;
		ClickableCollectTotal = Statistic.ClickableCollectTotal.ValueInt;
		ClickableCollectRealm = Statistic.ClickableCollectRealm.ValueInt;
		ClickableCollect = Statistic.ClickableCollect.ValueInt;
		AverageEntities = GameManager.Instance.BonusSpawner.Average.GetValue();
		CTTotal = Statistic.CTTotal.Value;
		HCTotal = Statistic.HCTotal.Value;
		LSTotal = Statistic.LSTotal.Value;
		ClassTime = new List<ulong>();
		for (int i = 0; i < Statistic.ClassTime.Count; i++)
		{
			ClassTime.Add(Statistic.ClassTime[i].ValueInt);
		}
		TimeTotal = Statistic.TimeTotal.ValueInt;
		TimeRealm = Statistic.TimeRealm.ValueInt;
		TimeSession = Statistic.TimeSession.ValueInt;
		SkipedTimeTotal = Statistic.SkipedTimeTotal.Value;
		SkipedTimeRealm = Statistic.SkipedTimeRealm.Value;
		SkipedTimeSession = Statistic.SkipedTimeSession.Value;
		TimeIdleTotal = Statistic.TimeIdleTotal.ValueInt;
		TimeIdleRealm = Statistic.TimeIdleRealm.ValueInt;
		TimeIdleSession = Statistic.TimeIdleSession.ValueInt;
		TimeOfflineTotal = Statistic.TimeOfflineTotal.ValueInt;
		TimeOfflineRealm = Statistic.TimeOfflineRealm.ValueInt;
		TimeOfflineSession = Statistic.TimeOfflineSession.ValueInt;
		BoughtUpgrades = Statistic.BoughtUpgrades.ValueInt;
		PetMaxLevel = Statistic.PetMaxLevel.ValueInt;
		PetMaxLevelAllTime = Statistic.PetMaxLevelAllTime.ValueInt;
		HeroMaxLevelAllTime = Statistic.HeroMaxLevelAllTime.ValueInt;
		ApprenticeMaxLevelRealm = Statistic.ApprenticeMaxLevelRealm.ValueInt;
		Bats = Statistic.Collectables.ValueInt;
		BatsR = Statistic.CollectablesRealm.ValueInt;
		BatsE = Statistic.CollectablessExile.ValueInt;
		BatsOnly = Statistic.BatsExile.ValueInt;
		EDE = Statistic.EnchantingDustExile.Value;
		HeroPlayedTime = GameManager.Instance.CurrentHero.PlayedTime.ValueInt;
		PetPlayedTime = GameManager.Instance.CurrentPet.PlayedTime.ValueInt;
		HeroSkipedPlayedTime = GameManager.Instance.CurrentHero.SkipedPlayedTime.Value;
		PetSkipedPlayedTime = GameManager.Instance.CurrentPet.SkipedPlayedTime.Value;
	}

	private void prepare_buildings()
	{
		BuildingLevels = new int[9];
		int i;
		for (i = 0; i < BuildingLevels.Length - 1; i++)
		{
			BuildingLevels[i] = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier - 1 == i).building.Level.ValueInt;
		}
		BuildingLevels[BuildingLevels.Length - 1] = GameManager.Instance.BuildingManager.special.building.Level.ValueInt;
		Catalysts = GameManager.Instance.BuildingManager.SaveCatalysts();
	}

	private void prepare_upgrades()
	{
		List<Upgrade> activeUpgradeList = GameManager.Instance.UpgradeManager.ActiveUpgradeList;
		Upgrades = new List<int>();
		for (int i = 0; i < activeUpgradeList.Count; i++)
		{
			Upgrade upgrade = activeUpgradeList[i];
			if (upgrade != null)
			{
				Upgrades.Add(int.Parse(upgrade.ID));
			}
		}
	}

	private void prepare_shards()
	{
		int count = GameManager.Instance.Scrolls.Scrolls.Count;
		ChoosenSpells = new Spells[count];
		SpellShards = new string[count];
		Autocast = new int[count];
		Scroll scroll = null;
		ShardsPool = GameManager.Instance.Scrolls.ShardsPool.Pool;
		for (int i = 0; i < ChoosenSpells.Length; i++)
		{
			Spell spell;
			if (GameManager.Instance.Scrolls.Scrolls.Count > i)
			{
				scroll = GameManager.Instance.Scrolls.Scrolls[i];
				spell = scroll.spell;
			}
			else
			{
				spell = null;
			}
			if (spell != null)
			{
				ChoosenSpells[i] = spell.NameKey;
				SpellShards[i] = spell.GetAllProgress.ToString("F2", CultureInfo.InvariantCulture);
				if (scroll != null)
				{
					Autocast[i] = scroll.GetAutoMode();
				}
			}
			else
			{
				ChoosenSpells[i] = Spells.None;
				SpellShards[i] = 0f.ToString();
				Autocast[i] = 0;
			}
		}
		OtherSpellShards = new List<IntFloatPair>();
		foreach (Spell spell2 in GameManager.Instance.SpellBook.SpellList)
		{
			if (spell2.GetAllProgress > 0f && !ChoosenSpells.Contains(spell2.NameKey))
			{
				OtherSpellShards.Add(new IntFloatPair((int)spell2.NameKey, spell2.GetAllProgress));
			}
		}
	}

	private void prepare_spells()
	{
		List<Spell> spellList = GameManager.Instance.SpellBook.SpellList;
		SpellUses = new List<SpellUse>();
		SpellUsesTR = new List<SpellUse>();
		bool flag = GameManager.Instance.ChallengeManager.ActiveChallenge != null;
		Spell spell;
		for (int i = 0; i < spellList.Count; i++)
		{
			spell = spellList[i];
			double num = spell.Use.GetInternalValue.ToDouble();
			if (num > 0.0)
			{
				if (flag && spell.ResetUses && GameManager.Instance.Scrolls.Scrolls.Any((Scroll x) => x.spell != null && x.spell.NameKey == spell.NameKey && x.active))
				{
					num -= 1.0;
				}
				SpellUses.Add(new SpellUse(spell.NameKey, num, spell.useCounter.GetValue()));
			}
			num = spell.UseThisRun.Value.ToDouble();
			if (!(num > 0.0))
			{
				continue;
			}
			if (flag && spell.ResetUses && GameManager.Instance.Scrolls.Scrolls.FirstOrDefault((Scroll x) => x.spell != null && x.spell.NameKey == spell.NameKey && x.active) != null)
			{
				num -= 1.0;
				CastSpell -= (BigNumber)1.0;
				if (GameManager.Instance.ChallengeManager.StatsIsOn())
				{
					CastSpellTotal -= (BigNumber)1.0;
				}
			}
			SpellUsesTR.Add(new SpellUse(spell.NameKey, num, spell.useThisRunCounter.GetValue()));
		}
		AccumCasts = GameManager.Instance.Scrolls.AccumCastCount.Value;
	}

	private void prepare_achievements()
	{
		List<AchievementCategory> achievList = GameManager.Instance.AchievManager.AchievList;
		AchievementsSave = new List<AchievementSave>();
		for (int i = 0; i < achievList.Count; i++)
		{
			int num = 0;
			for (int j = 0; j < achievList[i].Row.Count && achievList[i].Row[j].Unlocked; j++)
			{
				num++;
			}
			if (num != 0)
			{
				AchievementsSave.Add(new AchievementSave(achievList[i].Key, num));
			}
		}
		RAchieves = new List<AchievementSave>(10);
		for (int k = 0; k < 10 && GameManager.Instance.AchievManager.RecentlyAchieved.Count > k; k++)
		{
			Achievement achievement = GameManager.Instance.AchievManager.RecentlyAchieved[k];
			if (achievement != null)
			{
				RAchieves.Add(new AchievementSave(achievement.Key, achievement.Level));
			}
		}
		Triumphs = GameManager.Instance.AchievManager.SaveTriumphs();
	}

	private void prepare_challenges()
	{
		if (GameManager.Instance.ChallengeManager.ActiveChallenge == null)
		{
			ActiveChID = -1;
		}
		else
		{
			ActiveChID = GameManager.Instance.ChallengeManager.ActiveChallenge.ID;
		}
		ChProgress = GameManager.Instance.ChallengeManager.Progress.Value;
		List<Challenge> challenges = GameManager.Instance.ChallengeManager.Challenges;
		CompletedChIDs = new List<int>();
		for (int i = 0; i < challenges.Count; i++)
		{
			if (challenges[i].Completed)
			{
				CompletedChIDs.Add(challenges[i].ID);
			}
		}
	}

	private void prepare_shop()
	{
		Shop = GameManager.Instance.Shop.Save();
		Social = GameManager.Instance.Social.Save();
		Buffs = null;
		Memories = GameManager.Instance.Realm.Save();
		Realm = GameManager.Instance.Realmcraft.Save();
		Ascention = GameManager.Instance.Ascension.Save();
		Card = ExpeditionManager.Instance.Save();
		Quests = GameManager.Instance.Tasks.Save();
	}

	private void prepare_familiars()
	{
		Familiars = GameManager.Instance.Familiars.Save();
	}

	private void prepare_attributes()
	{
		AttTotal = GameManager.Instance.AttributeManager.Total.ValueInt;
		AttSearched = GameManager.Instance.AttributeManager.Searched.ValueInt;
		AttFree = GameManager.Instance.AttributeManager.Free.ValueInt;
		AttResets = GameManager.Instance.AttributeManager.Resets;
		AttProgress = GameManager.Instance.AttributeManager.progress;
		Int = GameManager.Instance.AttributeManager.Int;
		if (Int < 0)
		{
			Int = 0;
		}
		Ins = GameManager.Instance.AttributeManager.Ins;
		if (Ins < 0)
		{
			Ins = 0;
		}
		Scr = GameManager.Instance.AttributeManager.Scr;
		if (Scr < 0)
		{
			Scr = 0;
		}
		Wis = GameManager.Instance.AttributeManager.Wis;
		if (Wis < 0)
		{
			Wis = 0;
		}
		Dom = GameManager.Instance.AttributeManager.Dom;
		if (Dom < 0)
		{
			Dom = 0;
		}
		Pat = GameManager.Instance.AttributeManager.Pat;
		if (Pat < 0)
		{
			Pat = 0;
		}
		Mas = GameManager.Instance.AttributeManager.Mas;
		if (Mas < 0)
		{
			Mas = 0;
		}
		Emp = GameManager.Instance.AttributeManager.Emp;
		if (Emp < 0)
		{
			Emp = 0;
		}
		Ver = GameManager.Instance.AttributeManager.Ver;
		if (Ver < 0)
		{
			Ver = 0;
		}
		int num = Int + Ins + Scr + Wis + Dom + Pat + Mas + Emp + Ver;
		if (num + AttFree < AttSearched)
		{
			AttFree = AttSearched - num;
		}
	}

	private void prepare_craft()
	{
		Resources = GameManager.Instance.Craft.SaveResources();
		Jars = GameManager.Instance.Resources.SaveJars();
		ResCollected = Statistic.ResourcesCollected.Value;
		TCollectedRes = new List<BigNumber>();
		for (int i = 0; i < Statistic.ResourcesTotal.Count; i++)
		{
			TCollectedRes.Add(Statistic.ResourcesTotal[i].Value);
		}
		RCollectedRes = new List<BigNumber>();
		for (int j = 0; j < Statistic.ResourcesRealm.Count; j++)
		{
			RCollectedRes.Add(Statistic.ResourcesRealm[j].Value);
		}
		Craft = new CraftSave();
		Craft.Save();
		Gilding = GameManager.Instance.Gilding.Save();
	}

	public SaveData GetSaveData(string s)
	{
		SaveData saveData = null;
		if (s == null)
		{
			Debug.Log("empry string");
			return null;
		}
		try
		{
			return JsonConvert.DeserializeObject<SaveData>(s);
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message);
			s = s.Replace("ItemPresets", "ItemPresetsLegacy");
			try
			{
				return JsonConvert.DeserializeObject<SaveData>(s);
			}
			catch
			{
				Debug.Log(s);
				Debug.Log("json error");
				return null;
			}
		}
	}

	public void load_save(SaveData data)
	{
		SaveTime = data.SaveTime;
		Hero = data.Hero;
		Pet = data.Pet;
		PetExp = data.PetExp;
		CharExp = data.CharExp;
		CharExpMult = data.CharExpMult;
		Stance = data.Stance;
		Mana = data.Mana;
		VMana = data.VMana;
		Souls = data.Souls;
		FPS = data.FPS;
		NumberFormat = data.NumberFormat;
		FloorMultibuy = data.FloorMultibuy;
		SoundOn = data.SoundOn;
		MusicOn = data.MusicOn;
		VoidSFX = data.VoidSFX;
		ClassChooiseMessage = data.ClassChooiseMessage;
		AutoClose = data.AutoClose;
		RedColorCost = data.RedColorCost;
		FlyingText = data.FlyingText;
		Particles = data.Particles;
		OrbParticles = data.OrbParticles;
		BackParticles = data.BackParticles;
		ThrowShards = data.ThrowShards;
		Quotes = data.Quotes;
		CTips = data.CTips;
		ShowComics = data.ShowComics;
		MirrorChar = data.MirrorChar;
		MirrorPet = data.MirrorPet;
		Cursor = data.Cursor;
		SeasonalVFX = data.SeasonalVFX;
		MusicVolume = data.MusicVolume;
		SoundVolume = data.SoundVolume;
		BuyPack = data.BuyPack;
		Ascends = data.Ascends;
		AscendsRealm = data.AscendsRealm;
		TotalBuildings = data.TotalBuildings;
		ManaAllTime = data.ManaAllTime;
		ManaRealm = data.ManaRealm;
		ManaSession = data.ManaSession;
		VoidManaAllTime = data.VoidManaAllTime;
		VoidManaRealm = data.VoidManaRealm;
		VoidManaSession = data.VoidManaSession;
		MaxVMSession = data.MaxVMSession;
		ClicksTotal = data.ClicksTotal;
		ClicksRealm = data.ClicksRealm;
		Clicks = data.Clicks;
		AutoClicksTotal = data.AutoClicksTotal;
		AutoClicksRealm = data.AutoClicksRealm;
		AutoClicks = data.AutoClicks;
		AverageClicks = data.AverageClicks;
		CastSpellTotal = data.CastSpellTotal;
		CastSpellRealm = data.CastSpellRealm;
		CastSpell = data.CastSpell;
		ShardsTotal = data.ShardsTotal;
		ShardsRealm = data.ShardsRealm;
		ShardsSession = data.ShardsSession;
		ClickableCollectTotal = data.ClickableCollectTotal;
		ClickableCollectRealm = data.ClickableCollectRealm;
		ClickableCollect = data.ClickableCollect;
		AverageEntities = data.AverageEntities;
		BatsOnly = data.BatsOnly;
		CTTotal = data.CTTotal;
		HCTotal = data.HCTotal;
		LSTotal = data.LSTotal;
		ClassTime = data.ClassTime;
		TimeTotal = data.TimeTotal;
		TimeRealm = data.TimeRealm;
		TimeSession = data.TimeSession;
		SkipedTimeTotal = data.SkipedTimeTotal;
		SkipedTimeRealm = data.SkipedTimeRealm;
		SkipedTimeSession = data.SkipedTimeSession;
		TimeIdleTotal = data.TimeIdleTotal;
		TimeIdleRealm = data.TimeIdleRealm;
		TimeIdleSession = data.TimeIdleSession;
		TimeOfflineTotal = data.TimeOfflineTotal;
		TimeOfflineRealm = data.TimeOfflineRealm;
		TimeOfflineSession = data.TimeOfflineSession;
		BoughtUpgrades = data.BoughtUpgrades;
		PetMaxLevel = data.PetMaxLevel;
		PetMaxLevelAllTime = data.PetMaxLevelAllTime;
		HeroMaxLevelAllTime = data.HeroMaxLevelAllTime;
		ApprenticeMaxLevelRealm = data.ApprenticeMaxLevelRealm;
		Bats = data.Bats;
		BatsR = data.BatsR;
		BatsE = data.BatsE;
		EDE = data.EDE;
		HeroPlayedTime = data.HeroPlayedTime;
		PetPlayedTime = data.PetPlayedTime;
		HeroSkipedPlayedTime = data.HeroSkipedPlayedTime;
		PetSkipedPlayedTime = data.PetSkipedPlayedTime;
		BuildingLevels = data.BuildingLevels;
		Catalysts = data.Catalysts;
		Upgrades = data.Upgrades;
		ChoosenSpells = data.ChoosenSpells;
		SpellShards = data.SpellShards;
		ShardsPool = data.ShardsPool;
		AccumCasts = data.AccumCasts;
		SpellUses = data.SpellUses;
		SpellUsesTR = data.SpellUsesTR;
		Autocast = data.Autocast;
		OtherSpellShards = data.OtherSpellShards;
		SpellPresets = data.SpellPresets;
		AchievementsSave = data.AchievementsSave;
		RAchieves = data.RAchieves;
		Triumphs = data.Triumphs;
		GalleryH = data.GalleryH;
		GalleryP = data.GalleryP;
		Interior = data.Interior;
		ActiveChID = data.ActiveChID;
		ChProgress = data.ChProgress;
		CompletedChIDs = data.CompletedChIDs;
		Trial = data.Trial;
		Pantheon = data.Pantheon;
		if (data.SaveVersion < 44 && data.Shop == null)
		{
			data.Shop = new GoodsManager.SaveData();
			data.Shop.VipTier = data.VipTier;
			data.Shop.EVip3 = data.EVip3;
			data.Shop.Relics = data.Relics;
			data.Shop.Real = data.Real;
			data.Shop.Spent = 0;
			data.Shop.Sand = data.Coins;
			data.Shop.Nullifiers = data.Nullifiers;
			data.Shop.DECore = data.DECore;
			data.Shop.Warps = data.Warps;
			data.Shop.GM = data.GM;
			data.Shop.Catcher = data.Catcher;
			data.Shop.CatcherUpgrade = data.CatcherUpgrade;
			data.Shop.RPacks = data.RPacks;
		}
		Shop = data.Shop;
		Buffs = data.Buffs;
		Social = data.Social;
		Memories = data.Memories;
		Realm = data.Realm;
		Ascention = data.Ascention;
		Familiars = data.Familiars;
		AttTotal = data.AttTotal;
		AttSearched = data.AttSearched;
		AttFree = data.AttFree;
		AttResets = data.AttResets;
		AttProgress = data.AttProgress;
		Int = data.Int;
		Ins = data.Ins;
		Scr = data.Scr;
		Wis = data.Wis;
		Dom = data.Dom;
		Pat = data.Pat;
		Mas = data.Mas;
		Emp = data.Emp;
		Ver = data.Ver;
		Paragon = data.Paragon;
		ClassBonusStacks = data.ClassBonusStacks;
		ClassSubRes = data.ClassSubRes;
		ResCollected = data.ResCollected;
		Resources = data.Resources;
		TCollectedRes = data.TCollectedRes;
		RCollectedRes = data.RCollectedRes;
		Craft = data.Craft;
		Gilding = data.Gilding;
		Jars = data.Jars;
		ItemPresets = data.ItemPresets;
		ItemPresetsMap = data.ItemPresetsMap;
		DropMB = data.DropMB;
		Corruption = data.Corruption;
		Gamble = data.Gamble;
		EventSave = data.EventSave;
		Card = data.Card;
		Quests = data.Quests;
		SaveVersion = data.SaveVersion;
		Apply();
	}

	public void Apply()
	{
		GameManager.Instance.PreLoad();
		GameManager.Instance.Restart(isLoading: true);
		if (OnLoad != null)
		{
			OnLoad();
		}
		Settings.TargetFPS = FPS;
		Settings.ScientificNumber = NumberFormat;
		Settings.FloorMultiBuy = FloorMultibuy;
		Settings.SoundOn = SoundOn;
		Settings.MusicOn = MusicOn;
		Settings.VoidSFX = VoidSFX;
		Settings.ConfirmMessageChoose = ClassChooiseMessage;
		Settings.RedCost = RedColorCost;
		Settings.AutoClose = AutoClose;
		Settings.FlyingText = FlyingText;
		Settings.Particles = Particles;
		Settings.OrbParticles = OrbParticles;
		Settings.BackParticles = BackParticles;
		Settings.ThrowShards = ThrowShards;
		Settings.Quotes = Quotes;
		Settings.ColoredTips = CTips;
		Settings.ShowComics = ShowComics;
		Settings.MirrorChar = MirrorChar;
		Settings.MirrorPet = MirrorPet;
		if (GameManager.Instance.CurrentHero.OnMirror != null)
		{
			GameManager.Instance.CurrentHero.OnMirror();
		}
		if (GameManager.Instance.CurrentPet.OnMirror != null)
		{
			GameManager.Instance.CurrentPet.OnMirror();
		}
		Settings.Cursor = Cursor;
		Settings.SeasonalVFX = SeasonalVFX;
		Settings.MusicVolume = MusicVolume;
		Settings.SoundVolume = SoundVolume;
		GameManager.Instance.BuyPack = BuyPack;
		GameManager.Instance.ManaManager.Reset(Mana);
		Reborn.Souls.SetValue(Souls);
		Statistic.Ascends.SetValue(Ascends);
		Statistic.AscendsInRealm.SetValue(AscendsRealm);
		Statistic.TotalBuildings.SetValue(TotalBuildings);
		Statistic.ManaAllTime.SetValue(ManaAllTime);
		Statistic.ManaRealm.SetValue(ManaRealm);
		Statistic.ManaSession.SetValue(ManaSession);
		Statistic.VoidManaAllTime.SetValue(VoidManaAllTime);
		Statistic.VoidManaRealm.SetValue(VoidManaRealm);
		Statistic.VoidManaSession.SetValue(VoidManaSession);
		Statistic.MaxVoidManaSession.SetValue(MaxVMSession);
		Statistic.ClicksTotal.SetValue(ClicksTotal);
		Statistic.ClicksRealm.SetValue(ClicksRealm);
		Statistic.Clicks.SetValue(Clicks);
		Statistic.AutoClicksTotal.SetValue(AutoClicksTotal);
		Statistic.AutoClicksRealm.SetValue(AutoClicksRealm);
		Statistic.AutoClicks.SetValue(AutoClicks);
		GameManager.Instance.Orb.getAutoclickPerSec = AverageClicks;
		Statistic.CastSpellTotal.SetValue(CastSpellTotal);
		Statistic.CastSpellRealm.SetValue(CastSpellRealm);
		Statistic.CastSpell.SetValue(CastSpell);
		Statistic.ShardsSession.SetValue(ShardsSession);
		Statistic.ShardsTotal.SetValue(ShardsTotal);
		Statistic.ShardsRealm.SetValue(ShardsRealm);
		Statistic.ClickableCollectTotal.SetValue(ClickableCollectTotal);
		Statistic.ClickableCollectRealm.SetValue(ClickableCollectRealm);
		Statistic.ClickableCollect.SetValue(ClickableCollect);
		GameManager.Instance.BonusSpawner.Average.Load(AverageEntities);
		Statistic.CTTotal.SetValue(CTTotal);
		Statistic.HCTotal.SetValue(HCTotal);
		Statistic.LSTotal.SetValue(LSTotal);
		for (int i = 0; i < ClassTime.Count; i++)
		{
			Statistic.ClassTime[i].SetValue(ClassTime[i]);
		}
		Statistic.TimeTotal.SetValue(TimeTotal);
		Statistic.TimeRealm.SetValue(TimeRealm);
		Statistic.TimeSession.SetValue(TimeSession);
		Statistic.SkipedTimeTotal.SetValue(SkipedTimeTotal);
		Statistic.SkipedTimeRealm.SetValue(SkipedTimeRealm);
		Statistic.SkipedTimeSession.SetValue(SkipedTimeSession);
		Statistic.TimeIdleTotal.SetValue(TimeIdleTotal);
		Statistic.TimeIdleRealm.SetValue(TimeIdleRealm);
		Statistic.TimeIdleSession.SetValue(TimeIdleSession);
		Statistic.TimeOfflineTotal.SetValue(TimeOfflineTotal);
		Statistic.TimeOfflineRealm.SetValue(TimeOfflineRealm);
		Statistic.TimeOfflineSession.SetValue(TimeOfflineSession);
		Statistic.BoughtUpgrades.SetValue(BoughtUpgrades);
		Statistic.PetMaxLevel.SetValue(PetMaxLevel);
		Statistic.PetMaxLevelAllTime.SetValue(PetMaxLevelAllTime);
		Statistic.HeroMaxLevelAllTime.SetValue(HeroMaxLevelAllTime);
		Statistic.ApprenticeMaxLevelRealm.SetValue(ApprenticeMaxLevelRealm);
		Statistic.Collectables.SetValue(Bats);
		Statistic.CollectablesRealm.SetValue(BatsR);
		Statistic.BatsExile.SetValue(BatsOnly);
		Statistic.CollectablessExile.SetValue(BatsE);
		Statistic.EnchantingDustExile.SetValue(EDE);
		int j;
		for (j = 0; j < BuildingLevels.Length; j++)
		{
			BuildingVisual buildingVisual = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier - 1 == j);
			if (buildingVisual == null)
			{
				buildingVisual = GameManager.Instance.BuildingManager.special;
			}
			buildingVisual.building.Level.SetValue(BuildingLevels[j]);
			buildingVisual.Recalculate();
		}
		GameManager.Instance.BuildingManager.LoadCatalysts(Catalysts);
		GameManager.Instance.AddProfit(0f);
		GameManager.Instance.Orb.Restart();
		GameManager.Instance.CurrentHero.SetHero(Hero);
		GameManager.Instance.CurrentPet.SetPet(Pet);
		GameManager.Instance.CurrentHero.PlayedTime.SetValue(HeroPlayedTime);
		GameManager.Instance.CurrentHero.ExpFlat.SetValue(CharExp);
		GameManager.Instance.CurrentHero.ExpStack.SetValue((CharExpMult > 1.0) ? CharExpMult : ((BigNumber)1.0));
		GameManager.Instance.CurrentPet.PlayedTime.SetValue(PetPlayedTime);
		GameManager.Instance.CurrentHero.SkipedPlayedTime.SetValue(HeroSkipedPlayedTime);
		GameManager.Instance.CurrentPet.SkipedPlayedTime.SetValue(PetSkipedPlayedTime);
		if (GameManager.Instance.CurrentPet.Pet != null)
		{
			GameManager.Instance.CurrentPet.Pet.RecalculateLevel(PetExp);
		}
		if (Hero == HeroesNames.Ironsoul)
		{
			(GameManager.Instance.CurrentHero.Hero as Ironsoul).LoadStance(Stance);
		}
		else if (Hero == HeroesNames.Oni)
		{
			(GameManager.Instance.CurrentHero.Hero as Oni).LoadStance(Stance);
		}
		else if (Hero == HeroesNames.Alchemist || Hero == HeroesNames.Desolator)
		{
			(GameManager.Instance.CurrentHero.Hero as Alchemist).LoadElixir(Stance);
		}
		else if (Hero == HeroesNames.Artificer)
		{
			(GameManager.Instance.CurrentHero.Hero as Artificer).Parts.SetValue(Stance);
		}
		else if (Hero == HeroesNames.Shapeshifter)
		{
			(GameManager.Instance.CurrentHero.Hero as Shapeshifter).ActivateForm(Stance);
		}
		List<Upgrade> upgradeList = GameManager.Instance.UpgradeManager.UpgradeList;
		int i2;
		for (i2 = 0; i2 < Upgrades.Count; i2++)
		{
			int num = -1;
			num = upgradeList.FindIndex((Upgrade x) => x.ID == Upgrades[i2].ToString());
			if (num >= 0)
			{
				upgradeList[num].Load();
			}
		}
		GameManager.Instance.UpgradeManager.UpdateScroll();
		List<Spell> spellList = GameManager.Instance.SpellBook.SpellList;
		if (SpellUses != null)
		{
			foreach (SpellUse s in SpellUses)
			{
				Spell spell = spellList.Find((Spell x) => x.NameKey == s.key);
				spell.Use.SetValue(s.value);
				spell.useCounter.SetValue(s.aver);
			}
		}
		if (SpellUsesTR != null)
		{
			foreach (SpellUse s2 in SpellUsesTR)
			{
				Spell spell2 = spellList.Find((Spell x) => x.NameKey == s2.key);
				spell2.UseThisRun.SetValue(s2.value);
				spell2.useThisRunCounter.SetValue(s2.aver);
			}
		}
		GameManager.Instance.Scrolls.AccumCastCount.SetValue(AccumCasts);
		ChallengeManager challengeManager = GameManager.Instance.ChallengeManager;
		challengeManager.ResetAllChallenges();
		challengeManager.ActiveChallenge = challengeManager.Challenges.Find((Challenge x) => x.ID == ActiveChID);
		challengeManager.Progress.SetValue(ChProgress);
		for (int num2 = 0; num2 < challengeManager.Challenges.Count; num2++)
		{
			if (CompletedChIDs == null)
			{
				challengeManager.Challenges[num2].Completed = false;
			}
			else
			{
				challengeManager.Challenges[num2].Completed = CompletedChIDs.Contains(challengeManager.Challenges[num2].ID);
			}
		}
		GameManager.Instance.AttributeManager.Total.SetValue(AttTotal);
		GameManager.Instance.AttributeManager.Searched.SetValue(AttSearched);
		GameManager.Instance.AttributeManager.Free.SetValue(AttFree);
		GameManager.Instance.AttributeManager.Resets = AttResets;
		GameManager.Instance.AttributeManager.LoadProgress(AttProgress);
		GameManager.Instance.AttributeManager.Int = Int;
		GameManager.Instance.AttributeManager.Ins = Ins;
		GameManager.Instance.AttributeManager.Scr = Scr;
		GameManager.Instance.AttributeManager.Wis = Wis;
		GameManager.Instance.AttributeManager.Dom = Dom;
		GameManager.Instance.AttributeManager.Pat = Pat;
		GameManager.Instance.AttributeManager.Mas = Mas;
		GameManager.Instance.AttributeManager.Emp = Emp;
		GameManager.Instance.AttributeManager.Ver = Ver;
		GameManager.Instance.Paragon.MaxLevel = Paragon;
		GameManager.Instance.CurrentHero.ClassBonusStacks.SetValue(ClassBonusStacks);
		new SubResourceSave().Load(ClassSubRes);
		GameManager.Instance.Shop.Load(Shop);
		if (Buffs != null)
		{
			List<Buff> all = GameManager.Instance.BuffManager.GetAll();
			foreach (IntIntPair saved_buff in Buffs)
			{
				Buff buff = all.Find((Buff x) => x.ID == saved_buff.ID);
				if (buff != null)
				{
					buff.SetTime(saved_buff.Value);
				}
			}
		}
		GameManager.Instance.Social.Load(Social);
		GameManager.Instance.Craft.LoadResources(Resources);
		if (SaveVersion < 8)
		{
			for (int num3 = 1; num3 < 5; num3++)
			{
				GameManager.Instance.Craft.map[(CraftResource)num3].Change(2000.0);
			}
		}
		GameManager.Instance.Resources.LoadJars(Jars);
		Statistic.ResourcesCollectedRealm.SetValue(0.0);
		Statistic.ResourcesCollectedTotal.SetValue(0.0);
		Statistic.ResourcesCollected.SetValue(ResCollected);
		BigNumber value = 0.0;
		if (TCollectedRes != null)
		{
			for (int num4 = 0; num4 < TCollectedRes.Count && num4 < 4; num4++)
			{
				Statistic.ResourcesTotal[num4].SetValue(TCollectedRes[num4]);
				value += TCollectedRes[num4];
			}
			if (TCollectedRes.Count == 5)
			{
				Statistic.ResourcesTotal[4].SetValue(TCollectedRes[4]);
			}
		}
		Statistic.ResourcesCollectedTotal.SetValue(value);
		value = 0.0;
		if (RCollectedRes != null)
		{
			for (int num5 = 0; num5 < RCollectedRes.Count && num5 < 4; num5++)
			{
				Statistic.ResourcesRealm[num5].SetValue(RCollectedRes[num5]);
				value += RCollectedRes[num5];
			}
			if (RCollectedRes.Count == 5)
			{
				Statistic.ResourcesRealm[4].SetValue(RCollectedRes[4]);
			}
		}
		Statistic.ResourcesCollectedRealm.SetValue(value);
		if (Craft != null)
		{
			Craft.Load();
		}
		else
		{
			Craft = new CraftSave();
			Craft.LoadEmpty();
		}
		GameManager.Instance.AchievManager.PreLoad();
		List<AchievementCategory> achievList = GameManager.Instance.AchievManager.AchievList;
		if (AchievementsSave != null)
		{
			foreach (AchievementSave a in AchievementsSave)
			{
				achievList.Find((AchievementCategory x) => x.Key == a.key).Load(a.level);
			}
		}
		GameManager.Instance.AchievManager.RecentlyAchieved = new List<Achievement>();
		if (RAchieves != null)
		{
			int i3;
			for (i3 = 0; i3 < RAchieves.Count; i3++)
			{
				Achievement achievement = GameManager.Instance.AchievManager.AllAchievs.Find((Achievement x) => x.Key == RAchieves[i3].key && x.Level == RAchieves[i3].level);
				if (achievement != null)
				{
					GameManager.Instance.AchievManager.RecentlyAchieved.Add(achievement);
				}
			}
		}
		GameManager.Instance.AchievManager.LoadTriumps(Triumphs);
		GameManager.Instance.Gilding.Load(Gilding);
		GameManager.Instance.MBSpawner.Load(DropMB);
		GameManager.Instance.CorruptionManager.Load(Corruption);
		GameManager.Instance.Craft.window.craftingMenu.Load(Gamble);
		GameManager.Instance.Realm.Load(Memories);
		GameManager.Instance.Realmcraft.Load(Realm);
		if (Memories == null || Memories.Realms == 0L)
		{
			Settings.ShowComics = true;
		}
		GameManager.Instance.Ascension.Load(Ascention);
		VersionCorrection();
		GameManager.Instance.Gallery.Load(GalleryH);
		GameManager.Instance.PetGallery.Load(GalleryP);
		GameManager.Instance.Interior.Load(Interior);
		GameManager.Instance.Pantheon.Load(Pantheon);
		GameManager.Instance.Trials.Load(Trial);
		GameManager.Instance.Event.Load(EventSave);
		GameManager.Instance.Tasks.Load(Quests);
		_ = GameManager.Instance.SpellBook;
		GameManager.Instance.Familiars.Load(Familiars);
		GameManager.Instance.PostLoad();
		GameManager.Instance.Craft.window.presets.Load(ItemPresets, ItemPresetsMap);
		LoadSpellOnPanel();
		ExpeditionManager.Instance.Load(Card);
		GameManager.Instance.Mana.SetValue(Mana);
		long num6 = TimeUtils.GetTime().ToUniversalTime().Ticks - DateTime.SpecifyKind(SaveTime, DateTimeKind.Utc).Ticks;
		if (num6 >= 1)
		{
			TimeSpan span = new TimeSpan(num6);
			LoadVmana(span.TotalSeconds);
			GameManager.Instance.AddOfflineMana(span);
		}
		else
		{
			GameManager.Instance.VoidMana.SetValue(VMana);
		}
		PostLoadVersionCorrection();
		GameManager.Instance.PostOffline();
		GameManager.Instance.Cap.Off();
	}

	private void LoadSpellOnPanel()
	{
		GameManager.Instance.Scrolls.ShardsPool.Pool = ShardsPool;
		List<SpellChoose> spellChooses = GameManager.Instance.SpellBook.SpellChooses;
		for (int i = 0; i < ChoosenSpells.Length; i++)
		{
			if (ChoosenSpells[i] != Spells.None)
			{
				GameManager.Instance.Scrolls.ChoosePanel.scroll = GameManager.Instance.Scrolls.Scrolls[i];
				Spells key = GameManager.Instance.SpellBook.Enhancements.Upgrade(ChoosenSpells[i]);
				SpellChoose spellChoose = spellChooses.Find((SpellChoose x) => x.Spell != null && x.Spell.NameKey == key);
				if (spellChoose != null)
				{
					if (SpellShards[i] != null)
					{
						spellChoose.Spell.ResetShards();
						spellChoose.Spell.Load(float.Parse(SpellShards[i], CultureInfo.InvariantCulture));
					}
					else
					{
						Debug.LogError("load error. not exist spell");
					}
					spellChoose.Choose(onLoad: true);
				}
				if (Autocast != null && Autocast.Length > i)
				{
					GameManager.Instance.Scrolls.Scrolls[i].SetAutoMode(Autocast[i], isLoading: true);
				}
			}
			else
			{
				GameManager.Instance.Scrolls.Scrolls[i].Restart();
			}
		}
		List<Spell> spellList = GameManager.Instance.SpellBook.SpellList;
		if (OtherSpellShards != null)
		{
			foreach (IntFloatPair s in OtherSpellShards)
			{
				spellList.Find((Spell x) => x.NameKey == (Spells)s.key).Load(s.value);
			}
		}
		GameManager.Instance.SpellBook.SetsPanel.Load(SpellPresets);
	}

	public void OfflineImediatly(float time, bool spells = true)
	{
		double getAutoclickPerSec = GameManager.Instance.Orb.getAutoclickPerSec;
		if (getAutoclickPerSec > 0.0)
		{
			BigNumber bigNumber = getAutoclickPerSec * (double)time;
			Statistic.AutoClicks.Change(bigNumber);
			Statistic.Change(Statistic.AutoClicksTotal, bigNumber);
			Statistic.Change(Statistic.AutoClicksRealm, bigNumber);
		}
		BigNumber value = GameManager.Instance.BonusSpawner.Average.GetValue();
		if (value > 0.0)
		{
			int num = ((time > 2.1474836E+09f) ? int.MaxValue : ((int)time));
			int num2 = (value * num).ToInt();
			Statistic.ClickableCollect.Change(num2);
			Statistic.Change(Statistic.ClickableCollectTotal, num2);
			Statistic.Change(Statistic.ClickableCollectRealm, num2);
			GameManager.Instance.BonusSpawner.RestartCounter();
		}
		if (spells)
		{
			GameManager.Instance.SpellBook.Offline(time);
		}
	}

	public SkipTimeData SkipTime(BigNumber time, bool real = true, bool tw = false)
	{
		int num = ((time > 86400.0) ? 86400 : time.ToInt());
		int num2 = time.ToInt();
		if (real)
		{
			GameManager.Instance.Event.Offline(num);
		}
		if (real || tw)
		{
			ShardsOffline(num);
			GameManager.Instance.Trials.PreOffline();
		}
		if (GameManager.Instance.GameTick != null)
		{
			GameManager.Instance.GameTick(time.ToFloat());
		}
		if (real && GameManager.Instance.GameTickReal != null)
		{
			GameManager.Instance.GameTickReal(time.ToFloat());
		}
		if (real)
		{
			apply_time_change(time.ToUlong());
		}
		else
		{
			apply_skipped_time_change(time);
		}
		GameManager.Instance.CurrentPet.Pet?.OfflineWork(num2, real);
		if (GameManager.Instance.CurrentPet.SecondPetIsAcitve())
		{
			GameManager.Instance.CurrentPet.PetPanel.secondSlot.Pet?.OfflineWork(num2, real);
		}
		if (!real && !tw)
		{
			OfflineImediatly(time.ToFloat(), spells: false);
			return null;
		}
		int seconds = ((time > 604800.0) ? 604800 : time.ToInt());
		if (tw)
		{
			OfflineImediatly(time.ToFloat());
		}
		SkipTimeData skipTimeData = new SkipTimeData();
		GameManager.Instance.Pantheon.Offline(time);
		GameManager.Instance.MBSpawner.Offline(num);
		skipTimeData.rune = GameManager.Instance.Trials.Offline(num2);
		skipTimeData.attributes = GameManager.Instance.AttributeManager.Offline(time);
		ExpeditionManager.Instance.SkipTime(seconds);
		if (GameManager.Instance.Paragon.GildingIsAvailable)
		{
			GameManager.Instance.Gilding.EchoSpawner.Offline(num);
		}
		if (real)
		{
			GameManager.Instance.CorruptionManager.Offline(num);
			if (GameManager.Instance.Paragon.ItemsIsAvailable && Jars != null)
			{
				ResourceManager.OfflineData offlineData = GameManager.Instance.Resources.OfflineProgress(num2);
				skipTimeData.red = offlineData.red;
				skipTimeData.blue = offlineData.blue;
				skipTimeData.green = offlineData.green;
				skipTimeData.yellow = offlineData.yellow;
				skipTimeData.edust = offlineData.edust;
			}
			else
			{
				GameManager.Instance.Resources.Off();
			}
		}
		return skipTimeData;
	}

	public void ShardsOffline(int sec)
	{
		BigNumber shards = GameManager.Instance.Scrolls.ShardsPassive.Value * GameManager.Instance.Scrolls.Period * sec * GameManager.Instance.Scrolls.ShardsPool.Efficiency.Value;
		GameManager.Instance.Scrolls.ShardsPool.Insert(shards);
	}

	public void apply_time_change(ulong sec)
	{
		Statistic.TimeTotal.Change(sec);
		Statistic.TimeRealm.Change(sec);
		Statistic.TimeSession.Change(sec);
		Statistic.TimeIdleTotal.Change(sec);
		Statistic.TimeIdleRealm.Change(sec);
		Statistic.TimeIdleSession.Change(sec);
		Statistic.TimeOfflineTotal.Change(sec);
		Statistic.TimeOfflineRealm.Change(sec);
		Statistic.TimeOfflineSession.Change(sec);
		GameManager.Instance.CurrentHero.PlayedTime.Change(sec);
		if (GameManager.Instance.CurrentPet.Pet != null)
		{
			GameManager.Instance.CurrentPet.PlayedTime.Change(sec);
		}
	}

	public void apply_skipped_time_change(BigNumber sec)
	{
		Statistic.SkipedTimeSession.Change(sec);
		Statistic.SkipedTimeRealm.Change(sec);
		Statistic.SkipedTimeTotal.Change(sec);
		GameManager.Instance.CurrentHero.SkipedPlayedTime.Change(sec);
		if (GameManager.Instance.CurrentPet.Pet != null)
		{
			GameManager.Instance.CurrentPet.SkipedPlayedTime.Change(sec);
		}
		if (GameManager.Instance.CurrentPet.SecondPetIsAcitve())
		{
			GameManager.Instance.CurrentPet.PetPanel.secondSlot.SkipedPlayedTime.Change(sec);
		}
	}

	private void PostLoadVersionCorrection()
	{
		if (SaveVersion >= 82)
		{
			return;
		}
		List<int> rPacks = GameManager.Instance.Shop.Visual.RPacks;
		if (rPacks != null && rPacks.Count > 7 && rPacks[7] > 0)
		{
			GameManager.Instance.Relics.Real.Change(5 * rPacks[7]);
		}
		if (SaveVersion >= 81)
		{
			return;
		}
		February.SaveData february = GameManager.Instance.Event.february;
		if (february != null && february.Year == DateTime.Now.Year)
		{
			int num = Mathf.FloorToInt(february.Exp / 100f);
			BattlepassRewards battlepassRewards = new BattlepassRewards();
			for (int i = 0; i < num && i < battlepassRewards.rewards.Count; i++)
			{
				if (battlepassRewards.rewards[i].Resource == "ReliqRarity")
				{
					GameManager.Instance.Familiars.AddReliquary(FamiliarChests.Rarity, battlepassRewards.rewards[i].Value);
				}
			}
		}
		if (SaveVersion >= 80)
		{
			return;
		}
		CatcherManager catcherManager = GameManager.Instance.Shop.catcherManager;
		if (catcherManager.selectedId == 11)
		{
			catcherManager.SetActive(0);
		}
		if (SaveVersion >= 79)
		{
			return;
		}
		List<AchievementKey> gods = new List<AchievementKey>
		{
			AchievementKey.Animatealia,
			AchievementKey.Altermutus,
			AchievementKey.Ardourium,
			AchievementKey.Veritallios,
			AchievementKey.Procreogenus,
			AchievementKey.Tempoaeverum,
			AchievementKey.Chaos,
			AchievementKey.Cerebros
		};
		List<AchievementKey> minorGods = new List<AchievementKey>
		{
			AchievementKey.Cantopotensa,
			AchievementKey.Peritocapia,
			AchievementKey.Vastognicia,
			AchievementKey.Robuqueisa,
			AchievementKey.Contraligia,
			AchievementKey.Mundigenia
		};
		int num2 = 0;
		int num3 = 0;
		foreach (AchievementCategory item in GameManager.Instance.AchievManager.AchievList.FindAll((AchievementCategory x) => gods.Contains(x.Key)))
		{
			Achievement achievement = item.Row.FindLast((Achievement x) => x.Unlocked);
			if (achievement != null && achievement.GetArgument().ToInt() > num2)
			{
				num2 = achievement.GetArgument().ToInt();
			}
		}
		foreach (AchievementCategory item2 in GameManager.Instance.AchievManager.AchievList.FindAll((AchievementCategory x) => minorGods.Contains(x.Key)))
		{
			Achievement achievement2 = item2.Row.FindLast((Achievement x) => x.Unlocked);
			if (achievement2 != null && achievement2.GetArgument().ToInt() > num3)
			{
				num3 = achievement2.GetArgument().ToInt();
			}
		}
		PantheonManager pantheon = GameManager.Instance.Pantheon;
		if (pantheon.MaxLevel.ValueInt < num2)
		{
			pantheon.MaxLevel.SetValue(num2);
		}
		if (pantheon.MinorMaxLevel.ValueInt < num3)
		{
			pantheon.MinorMaxLevel.SetValue(num3);
		}
		if (SaveVersion >= 78)
		{
			return;
		}
		foreach (KeyValuePair<Gods, God> allGod in GameManager.Instance.Pantheon.GetAllGods())
		{
			int valueInt = allGod.Value.Level.ValueInt;
			if (valueInt > allGod.Value.MaxLevelRealm)
			{
				allGod.Value.MaxLevelRealm = valueInt;
			}
			if (valueInt > allGod.Value.MaxLevel)
			{
				allGod.Value.MaxLevel = valueInt;
			}
		}
		if (SaveVersion >= 77)
		{
			return;
		}
		GameManager.Instance.Realmcraft.ResetSoft();
		if (SaveVersion >= 63)
		{
			return;
		}
		List<int> rPacks2 = GameManager.Instance.Shop.Visual.RPacks;
		if (rPacks2.Count > 6 && (rPacks2[5] > 0 || rPacks2[6] > 0))
		{
			string text = "The Alteration and Crafting packs rewards have been increased to better represent their cost. The following difference has been awarded to you:";
			int num4 = rPacks2[5];
			int num5 = 0;
			if (num4 > 0)
			{
				GameManager.Instance.AlterationSand.Change(100 * num4);
				num5 = 40 * num4;
				GameManager.Instance.Relics.Real.Change(40 * num4);
				text = text + "\n-<sprite=1> Alteration sand " + 100 * num4;
			}
			num4 = rPacks2[6];
			if (num4 > 0)
			{
				num5 += 60 * num4;
				GameManager.Instance.Relics.Real.Change(60 * num4);
			}
			if (num5 > 0)
			{
				text = text + "\n-<sprite=0> Relics " + num5;
			}
			GameManager.Instance.Notification.SetText(text);
		}
		if (SaveVersion < 57)
		{
			GameManager.Instance.Gallery.Unlocked.OnChange?.Invoke();
			GameManager.Instance.PetGallery.Unlocked.OnChange?.Invoke();
			Statistic.UnlockedItems.OnChange?.Invoke();
		}
	}

	private void VersionCorrection()
	{
		if (SaveVersion >= 83)
		{
			return;
		}
		if (Card?.items != null && Card.items.Count > 0)
		{
			Dictionary<int, (int, int)> dictionary = new Dictionary<int, (int, int)>
			{
				{
					0,
					(4, 10)
				},
				{
					1,
					(4, 5)
				},
				{
					5,
					(5, 10)
				},
				{
					2,
					(5, 5)
				},
				{
					8,
					(0, 10)
				},
				{
					9,
					(0, 5)
				},
				{
					6,
					(1, 10)
				},
				{
					11,
					(1, 5)
				},
				{
					10,
					(2, 10)
				},
				{
					7,
					(2, 5)
				},
				{
					4,
					(3, 10)
				},
				{
					3,
					(3, 5)
				}
			};
			Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
			foreach (Inventory.ItemSaveData item in Card.items)
			{
				if (dictionary.TryGetValue(item.ID, out var value) && item.Amount > 0)
				{
					if (!dictionary2.ContainsKey(value.Item1))
					{
						dictionary2[value.Item1] = 0;
					}
					dictionary2[value.Item1] += item.Amount / value.Item2;
				}
			}
			Card.idleItems = new List<IdleInventory.SaveData>();
			foreach (KeyValuePair<int, int> item2 in dictionary2)
			{
				if (item2.Value > 0)
				{
					Card.idleItems.Add(new IdleInventory.SaveData(item2.Key, item2.Value));
				}
			}
		}
		if (SaveVersion >= 76)
		{
			return;
		}
		if (EventSave != null && EventSave.currentCurrencyStats != null)
		{
			EventSave.currentCurrencyStats.currencyName = "Eggs";
			EventSave.currentCurrencyStats.amount = EventSave.eggs;
		}
		if (SaveVersion >= 75)
		{
			return;
		}
		if (Memories != null && Memories.Upgrades != null)
		{
			BigNumber bigNumber = 1000.0;
			BigNumber bigNumber2 = 500.0;
			BigNumber bigNumber3 = 0.0;
			int num = 0;
			if (Memories.Upgrades.ContainsKey(7))
			{
				num = Memories.Upgrades[7];
				if (num > 0)
				{
					bigNumber3 = (2.0 * bigNumber + bigNumber2 * (num - 1)) * num / 2.0;
				}
			}
			if (Memories.Upgrades.ContainsKey(8))
			{
				num = Memories.Upgrades[8];
				if (num > 0)
				{
					bigNumber3 += (2.0 * bigNumber + bigNumber2 * (num - 1)) * num / 2.0;
				}
			}
			if (bigNumber3 >= 1.0)
			{
				GameManager.Instance.Realm.MemoriesSwitch.Change(bigNumber3);
			}
		}
		if (SaveVersion >= 74)
		{
			return;
		}
		GameManager.Instance.Realm.window.ResetAll();
		if (SaveVersion >= 72)
		{
			return;
		}
		if (Shop != null)
		{
			GoodsManager shop = GameManager.Instance.Shop;
			if (shop.Visual.RPacks[2] > 0)
			{
				shop.ActivateResourcePack(30000 * shop.Visual.RPacks[2]);
			}
			if (shop.Visual.RPacks[3] > 0)
			{
				shop.ActivateResourcePack(80000 * shop.Visual.RPacks[3]);
			}
			if (shop.Visual.RPacks[6] > 0)
			{
				shop.ActivateResourcePack(80000 * shop.Visual.RPacks[6]);
			}
			int num2 = 0;
			if (Shop.VipTier > 0)
			{
				num2 += shop.Vip1.Cost;
				if (Shop.VipTier > 1)
				{
					num2 += shop.Vip2.Cost;
					if (Shop.VipTier > 2)
					{
						num2 += shop.Vip3.Cost;
						if (Shop.VipTier > 3)
						{
							num2 += shop.Vip4.Cost;
							if (Shop.VipTier > 4)
							{
								num2 += shop.Vip5.Cost;
								if (Shop.VipTier > 5)
								{
									num2 += shop.Vip6.Cost;
								}
							}
						}
					}
				}
			}
			if (Shop.Catcher)
			{
				num2 += 40;
				if (Shop.CatcherUpgrade > 0)
				{
					num2 += 20;
				}
			}
			if (Shop.AutoExpedition > 0)
			{
				num2 += 40;
				if (Shop.AutoExpedition > 1)
				{
					num2 += 20;
				}
			}
			if (Shop.Gatherer > 0)
			{
				num2 += 40;
				if (Shop.Gatherer > 1)
				{
					num2 += 40;
				}
			}
			Shop.Spent = num2;
			GameManager.Instance.Relics.Spent.SetValue(num2);
		}
		if (SaveVersion < 68 && Gilding != null && Gilding.Buildings != null)
		{
			BigNumber bigNumber4 = 0.0;
			AchievementSave achievementSave = AchievementsSave.Find((AchievementSave x) => x.key == AchievementKey.MemeticIngots);
			if (achievementSave != null)
			{
				AchievementCategory achievementCategory = GameManager.Instance.AchievManager.AchievList.Find((AchievementCategory x) => x.Key == AchievementKey.MemeticIngots);
				if (achievementCategory != null)
				{
					int num3 = achievementSave.level - 1;
					if (num3 > 0 && num3 < achievementCategory.Row.Count)
					{
						bigNumber4 = (achievementCategory.Row[num3] as SimpleAchievement).argument;
					}
				}
			}
			bigNumber4 -= GameManager.Instance.Gilding.Buildings.brickTotal.Value;
			if (bigNumber4 > 1.0)
			{
				GameManager.Instance.Gilding.Buildings.AddBricks(bigNumber4);
			}
		}
		if (SaveVersion >= 65)
		{
			return;
		}
		_ = GameManager.Instance.ChallengeManager.CompletedChallenges.ValueInt;
		Statistic.BatsExile.SetValue(Statistic.CollectablessExile.ValueInt);
		if (SaveVersion >= 64)
		{
			return;
		}
		int valueInt = GameManager.Instance.ChallengeManager.CompletedChallenges.ValueInt;
		Statistic.Ascends.Change(valueInt * 2);
		if (SaveVersion >= 62)
		{
			return;
		}
		if (GalleryH.Save.ContainsKey(102))
		{
			GalleryToSave.Data data = GalleryH.Save[102];
			if (data.A == "102#1" && !data.IDs.Contains(1))
			{
				data.A = "102#0";
			}
		}
		Settings.Cursor = true;
		if (SaveVersion >= 61)
		{
			return;
		}
		if (GameManager.Instance.Shop.VipTier >= 6)
		{
			GameManager.Instance.Shop.AutoExpedition = 1;
		}
		if (SaveVersion >= 60)
		{
			return;
		}
		List<Triumph> triumphs = GameManager.Instance.AchievManager.Triumphs;
		triumphs.Find((Triumph x) => x.Key == AchievementKey.T_Catas).OnFail();
		triumphs.Find((Triumph x) => x.Key == AchievementKey.T_Casts_Limit).OnFail();
		triumphs.Find((Triumph x) => x.Key == AchievementKey.T_Minors).OnFail();
		triumphs.Find((Triumph x) => x.Key == AchievementKey.T_MM_Augments).OnFail();
		triumphs.Find((Triumph x) => x.Key == AchievementKey.T_Mem_Source).OnFail();
		if (SaveVersion >= 59)
		{
			return;
		}
		GameManager.Instance.Realm.ResetMemoriesLegacy();
		if (SaveVersion >= 56)
		{
			return;
		}
		Trial.totalCompleted = Mathf.Max(Trial.completed, Trial.totalCompleted);
		if (Gilding == null)
		{
			return;
		}
		BigNumber resource = Gilding.resource;
		GildingManager gilding = GameManager.Instance.Gilding;
		resource += gilding.Void.splintersInversted.Value;
		resource += gilding.Catas.GetAllInvested();
		resource += gilding.Buildings.brickTotal.Value * 100.0;
		gilding.ResourceTotal.SetValue(resource);
		Debug.Log("total splinters: " + resource.ToReadableString());
		if (SaveVersion >= 49)
		{
			return;
		}
		RedirectSaves();
		if (SaveVersion >= 43)
		{
			return;
		}
		GameManager.Instance.AchievManager.Triumphs.Find((Triumph x) => x.Key == AchievementKey.T_Evo).OnFail();
		GameManager.Instance.AchievManager.Triumphs.Find((Triumph x) => x.Key == AchievementKey.T_Inca).OnFail();
		GameManager.Instance.AchievManager.Triumphs.Find((Triumph x) => x.Key == AchievementKey.T_Sum).OnFail();
		if (SaveVersion >= 37)
		{
			return;
		}
		if (Catalysts != null)
		{
			BigNumber value2 = GameManager.Instance.BuildingManager.FreeGreenCatalysts.Value;
			BigNumber value3 = GameManager.Instance.BuildingManager.FreeBlueCatalysts.Value;
			BigNumber value4 = GameManager.Instance.BuildingManager.FreeRedCatalysts.Value;
			foreach (BuildingVisual building in GameManager.Instance.BuildingManager.Buildings)
			{
				value2 += (BigNumber)GameManager.Instance.BuildingManager.GetSumm(building.building.ACatalyst);
				value3 += (BigNumber)GameManager.Instance.BuildingManager.GetSumm(building.building.MCatalyst);
				value4 += (BigNumber)GameManager.Instance.BuildingManager.GetSumm(building.building.RCatalyst);
			}
			GameManager.Instance.BuildingManager.TotalGreen.SetValue(value2);
			GameManager.Instance.BuildingManager.TotalBlue.SetValue(value3);
			GameManager.Instance.BuildingManager.TotalRed.SetValue(value4);
			GameManager.Instance.BuildingManager.CatalystTrade.ManualReset();
			GameManager.Instance.BuildingManager.FreeGreenCatalysts.SetValue(value2);
			GameManager.Instance.BuildingManager.FreeRedCatalysts.SetValue(value4);
			GameManager.Instance.BuildingManager.FreeBlueCatalysts.SetValue(value3);
			GameManager.Instance.BuildingManager.CatalystAmount.SetValue(value2 + value4 + value3);
		}
		if (Memories != null)
		{
			int num4 = 0;
			if (Memories.Upgrades.ContainsKey(21))
			{
				num4 = Memories.Upgrades[21];
			}
			if (num4 != 0)
			{
				GameManager.Instance.Realm.Memories.Change((2000 + 250 * (num4 - 1)) * num4 / 2);
			}
		}
		if (Gilding != null)
		{
			BigNumber addendum = GameManager.Instance.Gilding.Resource.Value + GameManager.Instance.Gilding.Void.GetAllInvested();
			GameManager.Instance.Gilding.Resource.Change(addendum);
		}
		if (SaveVersion >= 36)
		{
			return;
		}
		if (Gilding != null)
		{
			BigNumber bigNumber5 = 0.0;
			foreach (KeyValuePair<int, TrapBonus.TrapSave> trap in Gilding.Void.Traps)
			{
				bigNumber5 += (BigNumber)(50f * ((Mathf.Pow(1.5f, trap.Value.Max) - 1f) / 0.5f - (Mathf.Pow(1.4f, trap.Value.Max) - 1f) / 0.4f));
			}
			if (bigNumber5 > 1.0)
			{
				GameManager.Instance.Gilding.Resource.Change(bigNumber5);
			}
		}
		if (SaveVersion >= 35)
		{
			return;
		}
		if (Card != null && Card.keys != null && Card.keys.Count > 0)
		{
			BigNumber value5 = 0.0;
			foreach (KeyValuePair<string, int> key in Card.keys)
			{
				value5 += (BigNumber)key.Value;
			}
			if (BigNumber.Sign(ExpeditionManager.Instance.Keys.keyCounter.Value) < 0)
			{
				value5 -= ExpeditionManager.Instance.Keys.keyCounter.Value;
			}
			ExpeditionManager.Instance.Keys.keyCounter.SetValue(value5);
		}
		if (SaveVersion < 32)
		{
			GameManager.Instance.AttributeManager.Searched.SetValue(Int + Ins + Scr + Wis + Dom + Pat + Mas + Emp + Ver + AttFree);
		}
		if (SaveVersion >= 31)
		{
			return;
		}
		GameManager.Instance.AttributeManager.ResetAttributesAll();
		GameManager.Instance.AttributeManager.Free.SetValue(GameManager.Instance.AttributeManager.TotalInRealm());
		GameManager.Instance.Paragon.MaxLevel = GameManager.Instance.Paragon.MystParagons.Check();
		if (SaveVersion >= 27)
		{
			return;
		}
		Statistic.ManaRealm.SetValue(Statistic.ManaAllTime.Value);
		Statistic.VoidManaRealm.SetValue(Statistic.VoidManaAllTime.Value);
		Statistic.ClicksRealm.SetValue(Statistic.ClicksTotal.Value);
		Statistic.AutoClicksRealm.SetValue(Statistic.AutoClicksTotal.Value);
		Statistic.CastSpellRealm.SetValue(Statistic.CastSpellTotal.Value);
		Statistic.ShardsRealm.SetValue(Statistic.ShardsTotal.Value);
		Statistic.ClickableCollectRealm.SetValue(Statistic.ClickableCollectTotal.ValueInt);
		Statistic.TimeRealm.SetValue(Statistic.TimeTotal.ValueInt);
		Statistic.SkipedTimeRealm.SetValue(Statistic.SkipedTimeTotal.Value);
		Statistic.TimeIdleRealm.SetValue(Statistic.TimeIdleTotal.ValueInt);
		for (int num5 = 0; num5 < Statistic.ResourcesRealm.Count; num5++)
		{
			Statistic.ResourcesRealm[num5].SetValue(Statistic.ResourcesTotal[num5].Value);
		}
		Statistic.CollectablesRealm.SetValue(Statistic.Collectables.ValueInt);
		GameManager.Instance.BuildingManager.CatalystAmountTotal.SetValue(GameManager.Instance.BuildingManager.CatalystAmount.Value);
		AttributeManager attributeManager = GameManager.Instance.AttributeManager;
		attributeManager.ResetAttributesExile();
		attributeManager.Total.SetValue(attributeManager.Searched.ValueInt);
		attributeManager.Free.SetValue(attributeManager.Searched.ValueInt);
		GameManager.Instance.Trials.TotalCompleted.SetValue(GameManager.Instance.Trials.Completed.ValueInt);
		if (SaveVersion >= 25)
		{
			return;
		}
		if (Paragon > 65)
		{
			Paragon = 64;
		}
		foreach (BuildingVisual building2 in GameManager.Instance.BuildingManager.Buildings)
		{
			GameManager.Instance.BuildingManager.CatalystAmount.Change(GameManager.Instance.BuildingManager.GetSumm(building2.building.RCatalyst));
		}
		GameManager.Instance.BuildingManager.CatalystAmount.Change(GameManager.Instance.BuildingManager.FreeRedCatalysts.Value);
	}

	public string ToJson()
	{
		prepare_data();
		return JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore
		});
	}

	public string Export()
	{
		return GUIUtility.systemCopyBuffer = ZipString(ToJson());
	}

	public bool Import(string text)
	{
		if (text == string.Empty)
		{
			text = GUIUtility.systemCopyBuffer;
		}
		string save = null;
		try
		{
			save = UnzipString(text);
		}
		catch
		{
			Debug.LogError("wrong format");
		}
		return LoadSaveJson(save);
	}

	public bool LoadSaveJson(string save)
	{
		SaveData saveData = GetSaveData(save);
		if (saveData != null)
		{
			DateTime time = TimeUtils.GetTime();
			if (saveData.SaveTime > time)
			{
				saveData.SaveTime = time;
			}
			load_save(saveData);
			return true;
		}
		return false;
	}

	public void NewGame(bool resetGoods)
	{
		SaveData saveData = GetSaveData(UnzipString(emptySave));
		saveData.SaveVersion = 74;
		saveData.SaveTime = DateTime.UtcNow;
		if (!resetGoods)
		{
			saveData.Shop = GameManager.Instance.Shop.Save();
			saveData.Social = GameManager.Instance.Social.Save();
			saveData.Shop.Special = null;
			saveData.Quests = GameManager.Instance.Tasks.SavePremiumOnly();
			saveData.GalleryH = GameManager.Instance.Gallery.Save();
			saveData.GalleryP = GameManager.Instance.PetGallery.Save();
			saveData.Interior = GameManager.Instance.Interior.Save();
		}
		else
		{
			saveData.Shop = null;
		}
		Statistic.Reset();
		load_save(saveData);
	}

	public static void CopyTo(Stream src, Stream dest)
	{
		byte[] array = new byte[4096];
		int count;
		while ((count = src.Read(array, 0, array.Length)) != 0)
		{
			dest.Write(array, 0, count);
		}
	}

	public static byte[] Zip(string str)
	{
		using MemoryStream src = new MemoryStream(Encoding.UTF8.GetBytes(str));
		using MemoryStream memoryStream = new MemoryStream();
		using (Unity.IO.Compression.GZipStream dest = new Unity.IO.Compression.GZipStream(memoryStream, Unity.IO.Compression.CompressionMode.Compress))
		{
			CopyTo(src, dest);
		}
		return memoryStream.ToArray();
	}

	public static byte[] ZipSystem(string str)
	{
		using MemoryStream src = new MemoryStream(Encoding.UTF8.GetBytes(str));
		using MemoryStream memoryStream = new MemoryStream();
		using (System.IO.Compression.GZipStream dest = new System.IO.Compression.GZipStream(memoryStream, System.IO.Compression.CompressionMode.Compress))
		{
			CopyTo(src, dest);
		}
		return memoryStream.ToArray();
	}

	public static string ZipString(string str)
	{
		return Convert.ToBase64String(Zip(str));
	}

	public static string ZipStringSystem(string str)
	{
		return Convert.ToBase64String(ZipSystem(str));
	}

	public static string Unzip(byte[] bytes)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (MemoryStream stream = new MemoryStream(bytes))
		{
			using Unity.IO.Compression.GZipStream src = new Unity.IO.Compression.GZipStream(stream, Unity.IO.Compression.CompressionMode.Decompress);
			CopyTo(src, memoryStream);
		}
		return Encoding.UTF8.GetString(memoryStream.ToArray());
	}

	public static string UnzipSystem(byte[] bytes)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (MemoryStream stream = new MemoryStream(bytes))
		{
			using System.IO.Compression.GZipStream src = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
			CopyTo(src, memoryStream);
		}
		return Encoding.UTF8.GetString(memoryStream.ToArray());
	}

	public static string UnzipString(string str)
	{
		return Unzip(Convert.FromBase64String(str));
	}

	public static string UnzipStringSystem(string str)
	{
		return UnzipSystem(Convert.FromBase64String(str));
	}

	[DllImport("__Internal")]
	private static extern void SyncFiles();

	[DllImport("__Internal")]
	private static extern void WindowAlert(string message);

	private string GetFileName(string name)
	{
		return $"{GetDirectory()}/{name}.dat";
	}

	private string GetDirectory()
	{
		return string.Concat(Application.persistentDataPath + "/", GameManager.Instance.UserID.ToString(), "/");
	}

	private void RedirectSaves()
	{
		try
		{
			string persistentDataPath = Application.persistentDataPath;
			foreach (string item in Directory.EnumerateFiles(Application.persistentDataPath, "*.dat"))
			{
				string text = item.Replace(Application.persistentDataPath, "");
				string[] array = text.Split('_');
				if (!(array[0] == "\\save") && array.Length >= 2)
				{
					persistentDataPath = Application.persistentDataPath + "/" + array[0];
					Debug.Log(persistentDataPath);
					Debug.Log(array[0]);
					if (!Directory.Exists(persistentDataPath))
					{
						Directory.CreateDirectory(persistentDataPath);
					}
					Directory.Move(item, persistentDataPath + "/" + text.Replace(array[0] + "_", ""));
				}
			}
		}
		catch
		{
			Debug.Log("already moved saves");
		}
	}

	private void CreateFolder()
	{
		string directory = GetDirectory();
		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}
	}

	private void SaveLocal(string data, string name)
	{
		data = ZipStringSystem(data);
		SteamManager.instance.cloud.FileWrite(name, data);
		string fileName = GetFileName(name);
		try
		{
			File.WriteAllText(fileName, data);
		}
		catch (Exception ex)
		{
			PlatformSafeMessage("Failed to Save: " + ex.Message);
		}
	}

	private void BackUps()
	{
		CheckSaveSlot(3, 3);
		CheckSaveSlot(2, 2);
		CheckSaveSlot(1, 1);
		string fileName = GetFileName(main + "_0");
		string fileName2 = GetFileName(back);
		if (File.Exists(fileName))
		{
			if ((DateTime.UtcNow - File.GetCreationTimeUtc(fileName)).TotalHours > 6.0)
			{
				File.Delete(fileName);
				File.Copy(fileName2, fileName);
			}
		}
		else if (File.Exists(fileName2))
		{
			File.Copy(fileName2, fileName);
		}
	}

	private void CopyToBackUp()
	{
		if (File.Exists(GetFileName(main)))
		{
			if (File.Exists(GetFileName(back)))
			{
				File.Delete(GetFileName(back));
			}
			File.Copy(GetFileName(main), GetFileName(back));
		}
	}

	private void CreateBackupByDate()
	{
		if (!File.Exists(GetFileName(main)))
		{
			return;
		}
		string fileName = GetFileName("save_" + SaveTime.DayOfYear);
		bool flag = false;
		if (File.Exists(fileName))
		{
			if ((DateTime.UtcNow - File.GetCreationTimeUtc(fileName)).TotalDays > 1.0)
			{
				File.Delete(fileName);
				flag = true;
			}
		}
		else
		{
			flag = true;
		}
		if (flag)
		{
			File.Copy(GetFileName(main), fileName);
		}
	}

	private void CheckSaveSlot(int id, int days)
	{
		string fileName = GetFileName(main + "_" + id);
		if (File.Exists(fileName))
		{
			TimeSpan timeSpan = DateTime.UtcNow - File.GetCreationTimeUtc(fileName);
			string fileName2 = GetFileName(main + "_" + (id - 1));
			if (timeSpan.TotalDays >= (double)days && File.Exists(fileName2))
			{
				File.Delete(fileName);
				File.Copy(fileName2, fileName);
			}
		}
		else
		{
			string fileName3 = GetFileName(back);
			if (File.Exists(GetFileName(main + "_" + (id - 1))) && File.Exists(fileName3))
			{
				File.Copy(fileName3, fileName);
			}
		}
	}

	private string LoadLocal(string name)
	{
		string text = null;
		text = SteamManager.instance.cloud.FileRead(name);
		if (text != null)
		{
			string text2;
			try
			{
				text2 = UnzipStringSystem(text);
			}
			catch
			{
				text2 = text;
				Debug.Log("catch after unzip");
			}
			text = text2;
		}
		if (text == null)
		{
			Debug.Log("load default");
			text = loadPersistentLocal(name);
		}
		return text;
	}

	private string loadPersistentLocal(string name)
	{
		string text = null;
		string fileName = GetFileName(name);
		try
		{
			if (File.Exists(fileName))
			{
				text = File.ReadAllText(fileName);
			}
			else
			{
				fileName = $"{Application.persistentDataPath}/{name}.dat";
				if (File.Exists(fileName))
				{
					text = File.ReadAllText(fileName);
				}
				else
				{
					Debug.Log(fileName + " file doesn't exist");
				}
			}
		}
		catch (Exception ex)
		{
			PlatformSafeMessage("Failed to Load: " + ex.Message);
		}
		if (text != null)
		{
			string text2;
			try
			{
				text2 = UnzipStringSystem(text);
			}
			catch
			{
				text2 = text;
				Debug.Log("catch after unzip");
			}
			text = text2;
		}
		return text;
	}

	public void SaveLocal()
	{
		prepare_data();
		string data = JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore
		});
		CopyToBackUp();
		SaveLocal(data, main);
		CreateBackupByDate();
	}

	public SaveData GetSaveData()
	{
		return GetSaveData(LoadLocal(main)) ?? GetSaveData(LoadLocal(back)) ?? null;
	}

	public string GetLocalSave()
	{
		return LoadLocal(main) ?? LoadLocal(back) ?? null;
	}

	public bool LoadLocal()
	{
		string text = LoadLocal(main);
		bool result = false;
		if (GetSaveData(text) != null)
		{
			result = LoadSaveJson(text);
		}
		else
		{
			Debug.Log("local save load error");
			text = LoadLocal(back);
			if (GetSaveData(text) != null)
			{
				result = LoadSaveJson(text);
			}
			else
			{
				Debug.Log("local save back up load error");
			}
		}
		return result;
	}

	public void LoadVmana(double seconds)
	{
		if (seconds <= 0.0 || VMana < 1.0)
		{
			GameManager.Instance.VoidMana.SetValue(0.0);
			return;
		}
		if (!GameManager.Instance.VoidManaManager.enabled)
		{
			GameManager.Instance.VoidMana.SetValue(VMana);
			return;
		}
		GameManager.Instance.VoidManaManager.Decrease.SetValue(GameManager.Instance.VoidManaManager.DecreasePercent);
		double num = 1.0 - GameManager.Instance.VoidManaManager.Decrease.Value.ToDouble();
		if ((1.0 / VMana).Log_a(num) <= seconds)
		{
			GameManager.Instance.VoidMana.SetValue(0.0);
		}
		else
		{
			GameManager.Instance.VoidMana.SetValue(VMana * new BigNumber(num).Pow(seconds));
		}
	}

	private void PlatformSafeMessage(string message)
	{
		if (Application.platform == RuntimePlatform.WebGLPlayer)
		{
			WindowAlert(message);
		}
		else
		{
			Debug.Log(message);
		}
	}
}
