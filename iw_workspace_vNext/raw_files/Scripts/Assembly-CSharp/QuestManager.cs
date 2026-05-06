using System.Collections.Generic;

public class QuestManager
{
	private List<Quest> quests;

	private List<Quest> questsExpedition;

	private List<Quest> questsCraft;

	private List<Quest> questsEnchant;

	private List<Quest> questsT2;

	private List<Quest> questsGods;

	private List<Quest> questsGods2;

	private List<Quest> questsGods3;

	public void Init()
	{
		quests = new List<Quest>
		{
			new QuestStat(0, "TaskAutoclicks", Statistic.AutoClicks, "10000", 0.43458f),
			new QuestStat(1, "TaskVoidEntities", Statistic.ClickableCollect, "20", 0.72f),
			new QuestStat(2, "TaskCollectables", Statistic.Collectables, "7", 0.1717f),
			new QuestCatas(3, "TaskCataShards", GameManager.Instance.BuildingManager.CatalystAmount, "100", 0.10835f),
			new QuestStat(4, "TaskPetLevel", GameManager.Instance.CurrentPet.Level, "60", 0.2589f),
			new QuestStat(5, "TaskManaSources", Statistic.TotalBuildings, "2500", 0.50932f),
			new QuestCritAuto(6, "TaskPerformCrits", "500", 0.468307f),
			new QuestCast(7, "TaskCast", "2000", 1f),
			new QuestCastType(8, "TaskCastEvo", SpellTypeGroup.Evocation, "1500", 1f),
			new QuestCastType(9, "TaskCastInca", SpellTypeGroup.Incantation, "180", 0.39f),
			new QuestCastType(10, "TaskCastSumm", SpellTypeGroup.Summoning, "5", 0.26f),
			new QuestTime(11, "TaskTime", "7200", 0.10835f),
			new QuestTimeIdle(12, "TaskIdleTime", "3600", 0.10835f),
			new QuestTimePet(13, "TaskPetTime", "3600", 0.10835f),
			new QuestStat(14, "TaskTrials", GameManager.Instance.Trials.Completed, "1", 0.17174f),
			new QuestChallenge(15, "TaskChallenges", "1", 0.10835f),
			new QuestInvestCatas(16, "TaskInvestCatas", "4", 0.2167f),
			new QuestBats(17, "TaskBats", "5", 0.17174f),
			new QuestCorruption(18, "TaskCorruptions", "6", 0.01f),
			new QuestEarnCatas(19, "TaskGainCatas", "10", 0f),
			new QuestBuffTime(20, "TaskBuff", 14400.0, 0f)
		};
		questsExpedition = new List<Quest>
		{
			new QuestStat(100, "TaskExpeditionMonsters", ExpeditionManager.Instance.StatisticInfo.Monsters, "10", 0f),
			new QuestConsumable(101, "TaskExpeditionConsumables", "10", 0f),
			new QuestConsumableElixir(102, "TaskExpeditionPotions", "6", 0f)
		};
		questsCraft = new List<Quest>
		{
			new QuestStat(200, "TaskCraftingDust", Statistic.ResourcesCollected, "1000", 0.38845f)
		};
		questsEnchant = new List<Quest>
		{
			new QuestStat(300, "TaskEnchDust", Statistic.EnchantingDustExile, "500", 0.57666f),
			new QuestEnchanting(301, "TaskEnchItems", "2", 0.143239f),
			new QuestExperiment(302, "TaskExperiments", "2", 0.2167f)
		};
		questsT2 = new List<Quest>
		{
			new QuestCastAccum(400, "TaskAccums", "1000", 0.39f),
			new QuestCastPersistance(401, "TaskPersists", "400", 0.39f),
			new QuestCastCharge(402, "TaskChargingSpells", "1000", 0.36f)
		};
		questsGods = new List<Quest>
		{
			new QuestTimeGod(500, Gods.Life, "TaskPrayAnimatealia", "3600", 0.11568911f),
			new QuestTimeGod(501, Gods.Change, "TaskPrayAltermutus", "3600", 0.11568911f),
			new QuestTimeGod(502, Gods.Energy, "TaskPrayArdourium", "3600", 0.11568911f)
		};
		questsGods2 = new List<Quest>
		{
			new QuestTimeGod(503, Gods.Existence, "TaskPrayVeritallios", "3600", 0.11568911f),
			new QuestTimeGod(504, Gods.Creation, "TaskPrayProcreogenus", "3600", 0.11568911f),
			new QuestTimeGod(505, Gods.Time, "TaskPrayTempoaeverum", "3600", 0.11568911f)
		};
		questsGods3 = new List<Quest>
		{
			new QuestTimeGod(506, Gods.Chaos, "TaskPrayChaos", "3600", 0.11568911f)
		};
	}

	public List<Quest> GetAvailableList()
	{
		List<Quest> list = new List<Quest>(quests);
		if (GameManager.Instance.Paragon.ExpeditionIsAvailable)
		{
			list.AddRange(questsExpedition);
		}
		if (GameManager.Instance.Paragon.ItemsIsAvailable)
		{
			list.AddRange(questsCraft);
		}
		if (GameManager.Instance.Paragon.EnchantingIsAvailable)
		{
			list.AddRange(questsEnchant);
		}
		if (GameManager.Instance.Paragon.ClassT2IsAvailable)
		{
			list.AddRange(questsT2);
		}
		if (GameManager.Instance.Paragon.PantheonIsAvailable)
		{
			list.AddRange(questsGods);
		}
		if (GameManager.Instance.Paragon.Pantheon2IsAvailable)
		{
			list.AddRange(questsGods2);
		}
		if (GameManager.Instance.Paragon.Pantheon3IsAvailable)
		{
			list.AddRange(questsGods3);
		}
		return list;
	}

	public Quest GetQuest(int id)
	{
		switch (id / 100)
		{
		case 0:
			return quests.Find((Quest x) => x.ID == id);
		case 1:
			return questsExpedition.Find((Quest x) => x.ID == id);
		case 2:
			return questsCraft.Find((Quest x) => x.ID == id);
		case 3:
			return questsEnchant.Find((Quest x) => x.ID == id);
		case 4:
			return questsT2.Find((Quest x) => x.ID == id);
		case 5:
		{
			Quest quest = questsGods.Find((Quest x) => x.ID == id);
			if (quest == null)
			{
				quest = questsGods2.Find((Quest x) => x.ID == id);
			}
			if (quest == null)
			{
				quest = questsGods3.Find((Quest x) => x.ID == id);
			}
			return quest;
		}
		default:
			return null;
		}
	}
}
