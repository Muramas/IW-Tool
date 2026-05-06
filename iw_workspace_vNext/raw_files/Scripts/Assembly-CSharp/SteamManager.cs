using System;
using Steamworks;
using UnityEngine;

internal class SteamManager : MonoBehaviour
{
	public static SteamManager instance;

	public SteamBaseManager baseManger;

	public SteamCloud cloud;

	private SteamStatsAndAchievements statsAndAchievements;

	private Callback<GameOverlayActivated_t> gameOverlayActivatedCallback;

	public Action OnOverlayClosedEvent;

	private void Awake()
	{
		instance = this;
		baseManger = UnityEngine.Object.FindObjectOfType<SteamBaseManager>();
		baseManger.Init();
		if (SteamBaseManager.Initialized)
		{
			Debug.Log(SteamFriends.GetPersonaName());
			gameOverlayActivatedCallback = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
		}
	}

	private void Start()
	{
		cloud = UnityEngine.Object.FindObjectOfType<SteamCloud>();
		statsAndAchievements = UnityEngine.Object.FindObjectOfType<SteamStatsAndAchievements>();
		statsAndAchievements.Init();
		InitAchivements();
	}

	private void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
	{
		if (pCallback.m_bActive == 1)
		{
			Debug.Log("Steam overlay opened");
			OnOverlayOpened();
		}
		else
		{
			Debug.Log("Steam overlay closed");
			OnOverlayClosed();
		}
	}

	private void OnOverlayOpened()
	{
	}

	private void OnOverlayClosed()
	{
		VerifyDLC();
		OnOverlayClosedEvent?.Invoke();
	}

	public void UnlockAchievement(string key)
	{
		statsAndAchievements.UnlockAchievement(key);
	}

	public bool IsCatcherPaskDLCUnlocked()
	{
		bool result = SteamApps.BIsDlcInstalled(new AppId_t(3856390u));
		Debug.Log("IsCatcherPackDLCUnlocked: " + result);
		return result;
	}

	public bool IsExpeditionPackDLCUnlocked()
	{
		bool result = SteamApps.BIsDlcInstalled(new AppId_t(3939820u));
		Debug.Log("IsExpeditionPackDLCUnlocked: " + result);
		return result;
	}

	public bool IsGathererPackDLCUnlocked()
	{
		bool result = SteamApps.BIsDlcInstalled(new AppId_t(4181890u));
		Debug.Log("IsGathererPackDLCUnlocked: " + result);
		return result;
	}

	public bool IsDDBought()
	{
		bool result = SteamApps.BIsSubscribedApp(new AppId_t(3769070u));
		Debug.Log("IsDDBought: " + result);
		return result;
	}

	public void VerifyDLC()
	{
		if (!(GameManager.Instance == null) && GameManager.Instance.IsLoaded)
		{
			if (IsCatcherPaskDLCUnlocked())
			{
				GameManager.Instance.Shop.GetCatcherPack();
			}
			else
			{
				GameManager.Instance.Shop.TakeBackCatcherPack();
			}
			if (IsExpeditionPackDLCUnlocked())
			{
				GameManager.Instance.Shop.GetExpeditionPack();
			}
			else
			{
				GameManager.Instance.Shop.TakeBackExpeditionPack();
			}
			if (IsGathererPackDLCUnlocked())
			{
				GameManager.Instance.Shop.GetGathererPack();
			}
			else
			{
				GameManager.Instance.Shop.TakeBackGathererPack();
			}
			if (IsDDBought())
			{
				GameManager.Instance.Shop.GetDDGame();
			}
			else
			{
				GameManager.Instance.Shop.TakeBackDDGame();
			}
		}
	}

	private void InitAchivements()
	{
		statsAndAchievements.AddAchievement("FirstStep");
		statsAndAchievements.AddAchievement("Apprentice_0");
		statsAndAchievements.AddAchievement("Druid_0");
		statsAndAchievements.AddAchievement("Demonologist_0");
		statsAndAchievements.AddAchievement("Necromancer_0");
		statsAndAchievements.AddAchievement("Arcanist_0");
		statsAndAchievements.AddAchievement("Prodigy_0");
		statsAndAchievements.AddAchievement("Voidmancer_0");
		statsAndAchievements.AddAchievement("Exorcist_0");
		statsAndAchievements.AddAchievement("Chronomancer_0");
		statsAndAchievements.AddAchievement("Umbramancer_0");
		statsAndAchievements.AddAchievement("Alchemist_0");
		statsAndAchievements.AddAchievement("Ironsoul_0");
		statsAndAchievements.AddAchievement("Abolisher_0");
		statsAndAchievements.AddAchievement("Shaman_0");
		statsAndAchievements.AddAchievement("Heretic_0");
		statsAndAchievements.AddAchievement("Oni_0");
		statsAndAchievements.AddAchievement("Archon_0");
		statsAndAchievements.AddAchievement("Temporalist_0");
		statsAndAchievements.AddAchievement("Desolator_0");
		statsAndAchievements.AddAchievement("Pixie_0");
		statsAndAchievements.AddAchievement("ZombieWarrior_0");
		statsAndAchievements.AddAchievement("Daemon_0");
		statsAndAchievements.AddAchievement("Homunculus_0");
		statsAndAchievements.AddAchievement("Golem_0");
		statsAndAchievements.AddAchievement("Spellhound_0");
		statsAndAchievements.AddAchievement("Voidfiend_0");
		statsAndAchievements.AddAchievement("Devourer_0");
		statsAndAchievements.AddAchievement("Geode_0");
		statsAndAchievements.AddAchievement("HolySpirit_0");
		statsAndAchievements.AddAchievement("ShadowStalker_0");
		statsAndAchievements.AddAchievement("Ent_0");
		statsAndAchievements.AddAchievement("RisenGiant_0");
		statsAndAchievements.AddAchievement("Simulacrum_0");
		statsAndAchievements.AddAchievement("PitLord_0");
		statsAndAchievements.AddAchievement("AnimaConstruct_0");
		statsAndAchievements.AddAchievement("Voidterror_0");
		statsAndAchievements.AddAchievement("Hungerer_0");
		statsAndAchievements.AddAchievement("Archivist_0");
		statsAndAchievements.AddAchievement("LeyKeeper_0");
		statsAndAchievements.AddAchievement("Ebonsand_0");
		statsAndAchievements.AddAchievement("Arcanaworg_0");
		statsAndAchievements.AddAchievement("HeraldOfRot_0");
		statsAndAchievements.AddAchievement("LivingSin_0");
		statsAndAchievements.AddAchievement("MechanosApexis_0");
		statsAndAchievements.AddAchievement("VoidlightAmalgam_0");
		statsAndAchievements.AddAchievement("NixInstability_0");
		statsAndAchievements.AddAchievement("GreaterChimaera_0");
		statsAndAchievements.AddAchievement("Ascends_0");
		statsAndAchievements.AddAchievement("Ascends_5");
		statsAndAchievements.AddAchievement("PlayedTime_3");
		statsAndAchievements.AddAchievement("Challenges_0");
		statsAndAchievements.AddAchievement("ItemsUnlocked_12");
		statsAndAchievements.AddAchievement("Unlock_Att");
		statsAndAchievements.AddAchievement("Unlock_Trials");
		statsAndAchievements.AddAchievement("Unlock_Items");
		statsAndAchievements.AddAchievement("Unlock_Ench");
		statsAndAchievements.AddAchievement("Unlock_Weapon");
		statsAndAchievements.AddAchievement("Unlock_Pantheon");
		statsAndAchievements.AddAchievement("Unlock_MinorPantheon");
		statsAndAchievements.AddAchievement("Unlock_VoidMemetic");
		statsAndAchievements.AddAchievement("Unlock_CataMemetic");
		statsAndAchievements.AddAchievement("Unlock_SourceMemetic");
		statsAndAchievements.AddAchievement("Unlock_Ascension");
		statsAndAchievements.AddAchievement("Challenges_9");
		statsAndAchievements.AddAchievement("ExpeditionLevel_4");
		statsAndAchievements.AddAchievement("Realm_0");
		statsAndAchievements.AddAchievement("Animatealia_4");
		statsAndAchievements.AddAchievement("Altermutus_4");
		statsAndAchievements.AddAchievement("Ardourium_4");
		statsAndAchievements.AddAchievement("Veritallios_4");
		statsAndAchievements.AddAchievement("Procreogenus_4");
		statsAndAchievements.AddAchievement("Tempoaeverum_4");
		statsAndAchievements.AddAchievement("Chaos_4");
		statsAndAchievements.AddAchievement("Cerebros_4");
		statsAndAchievements.AddAchievement("Legacy_10");
		statsAndAchievements.AddAchievement("Legacy_25");
		statsAndAchievements.AddAchievement("Legacy_50");
		statsAndAchievements.AddAchievement("Legacy_75");
		statsAndAchievements.AddAchievement("Legacy_100");
	}

	public void Subscribe()
	{
		VariableInt boughtUpgrades = Statistic.BoughtUpgrades;
		boughtUpgrades.OnChange = (Action)Delegate.Combine(boughtUpgrades.OnChange, new Action(FirstStepCheck));
		Paragons paragon = GameManager.Instance.Paragon;
		paragon.onChangeLVL = (Action)Delegate.Combine(paragon.onChangeLVL, new Action(OnChangeParagon));
		Paragons paragon2 = GameManager.Instance.Paragon;
		paragon2.onChangeLegacyLVL = (Action)Delegate.Combine(paragon2.onChangeLegacyLVL, new Action(OnChangeLegacy));
		GameManager gameManager = GameManager.Instance;
		gameManager.OnPostLoad = (Action)Delegate.Combine(gameManager.OnPostLoad, new Action(VerifyDLC));
	}

	private void FirstStepCheck()
	{
		if (Statistic.BoughtUpgrades.ValueInt > 0)
		{
			statsAndAchievements.UnlockAchievement("FirstStep");
			VariableInt boughtUpgrades = Statistic.BoughtUpgrades;
			boughtUpgrades.OnChange = (Action)Delegate.Remove(boughtUpgrades.OnChange, new Action(FirstStepCheck));
		}
	}

	private void OnChangeParagon()
	{
		if (!(GameManager.Instance.Realmcraft.Active?.ID == "myst"))
		{
			Paragons paragon = GameManager.Instance.Paragon;
			if (paragon.AttributesIsAvailable)
			{
				statsAndAchievements.UnlockAchievement("Unlock_Att");
			}
			if (paragon.TrialsIsAvailable)
			{
				statsAndAchievements.UnlockAchievement("Unlock_Trials");
			}
			if (paragon.ItemsIsAvailable)
			{
				statsAndAchievements.UnlockAchievement("Unlock_Items");
			}
			if (paragon.EnchantingIsAvailable)
			{
				statsAndAchievements.UnlockAchievement("Unlock_Ench");
			}
			if (paragon.WeaponIsAvailable)
			{
				statsAndAchievements.UnlockAchievement("Unlock_Weapon");
			}
			if (paragon.PantheonIsAvailable)
			{
				statsAndAchievements.UnlockAchievement("Unlock_Pantheon");
			}
			if (paragon.PantheonMinorsIsAvailable)
			{
				statsAndAchievements.UnlockAchievement("Unlock_MinorPantheon");
			}
			if (paragon.GildingIsAvailable)
			{
				statsAndAchievements.UnlockAchievement("Unlock_VoidMemetic");
			}
			if (paragon.GildingCatasIsAvailable)
			{
				statsAndAchievements.UnlockAchievement("Unlock_CataMemetic");
			}
			if (paragon.GildingBuildingsIsAvailable)
			{
				statsAndAchievements.UnlockAchievement("Unlock_SourceMemetic");
			}
			if (paragon.AscensionIsAvailable)
			{
				statsAndAchievements.UnlockAchievement("Unlock_Ascension");
			}
		}
	}

	private void OnChangeLegacy()
	{
		Paragons paragon = GameManager.Instance.Paragon;
		if (paragon.LevelPermanent >= 10)
		{
			statsAndAchievements.UnlockAchievement("Legacy_10");
		}
		if (paragon.LevelPermanent >= 25)
		{
			statsAndAchievements.UnlockAchievement("Legacy_25");
		}
		if (paragon.LevelPermanent >= 50)
		{
			statsAndAchievements.UnlockAchievement("Legacy_50");
		}
		if (paragon.LevelPermanent >= 75)
		{
			statsAndAchievements.UnlockAchievement("Legacy_75");
		}
		if (paragon.LevelPermanent >= 100)
		{
			statsAndAchievements.UnlockAchievement("Legacy_100");
		}
	}
}
