using System.Collections.Generic;
using System.Text;

public static class Statistic
{
	private const float YEAR = 31536000f;

	private const float TEN_YEARS = 315360000f;

	public static VariableBignumber ManaAllTime = new VariableBignumber(0.0);

	public static VariableBignumber ManaRealm = new VariableBignumber(0.0);

	public static VariableBignumber ManaSession = new VariableBignumber(0.0);

	public static VariableBignumber VoidManaAllTime = new VariableBignumber(0.0);

	public static VariableBignumber VoidManaRealm = new VariableBignumber(0.0);

	public static VariableBignumber VoidManaSession = new VariableBignumber(0.0);

	public static VariableBignumber MaxVoidManaSession = new VariableBignumber(0.0);

	public static VariableBignumber ClicksTotal = new VariableBignumber(0.0);

	public static VariableBignumber ClicksRealm = new VariableBignumber(0.0);

	public static VariableBignumber Clicks = new VariableBignumber(0.0);

	public static VariableBignumber AutoClicksTotal = new VariableBignumber(0.0);

	public static VariableBignumber AutoClicksRealm = new VariableBignumber(0.0);

	public static VariableBignumber AutoClicks = new VariableBignumber(0.0);

	public static VariableBignumber CastSpellTotal = new VariableBignumber(0.0);

	public static VariableBignumber CastSpellRealm = new VariableBignumber(0.0);

	public static VariableBignumber CastSpell = new VariableBignumber(0.0);

	public static VariableBignumber ShardsTotal = new VariableBignumber(0.0);

	public static VariableBignumber ShardsRealm = new VariableBignumber(0.0);

	public static VariableBignumber ShardsSession = new VariableBignumber(0.0);

	public static VariableLong ClickableCollectTotal = new VariableLong(0uL);

	public static VariableLong ClickableCollectRealm = new VariableLong(0uL);

	public static VariableLong ClickableCollect = new VariableLong(0uL);

	public static VariableLong TimeTotal = new VariableLong(0uL);

	public static VariableLong TimeRealm = new VariableLong(0uL);

	public static VariableLong TimeSession = new VariableLong(0uL);

	public static VariableBignumber SkipedTimeTotal = new VariableBignumber(0.0);

	public static VariableBignumber SkipedTimeRealm = new VariableBignumber(0.0);

	public static VariableBignumber SkipedTimeSession = new VariableBignumber(0.0);

	public static VariableLong TimeIdleTotal = new VariableLong(0uL);

	public static VariableLong TimeIdleRealm = new VariableLong(0uL);

	public static VariableLong TimeIdleSession = new VariableLong(0uL);

	public static VariableLong TimeOfflineTotal = new VariableLong(0uL);

	public static VariableLong TimeOfflineRealm = new VariableLong(0uL);

	public static VariableLong TimeOfflineSession = new VariableLong(0uL);

	public static VariableInt PetMaxLevel = new VariableInt(0);

	public static VariableInt PetMaxLevelAllTime = new VariableInt(0);

	public static VariableInt HeroMaxLevelAllTime = new VariableInt(0);

	public static VariableInt ApprenticeMaxLevelRealm = new VariableInt(0);

	public static VariableInt BoughtUpgrades = new VariableInt(0);

	public static VariableInt TotalBuildings = new VariableInt(0);

	public static VariableInt Ascends = new VariableInt(0);

	public static VariableInt AscendsInRealm = new VariableInt(0);

	public static VariableBignumber CTTotal = new VariableBignumber(0.0);

	public static VariableBignumber HCTotal = new VariableBignumber(0.0);

	public static VariableBignumber LSTotal = new VariableBignumber(0.0);

	public static List<VariableLong> ClassTime = new List<VariableLong>();

	public static VariableBignumber ResourcesCollected = new VariableBignumber(0.0);

	public static VariableBignumber ResourcesCollectedRealm = new VariableBignumber(0.0);

	public static VariableBignumber ResourcesCollectedTotal = new VariableBignumber(0.0);

	public static VariableBignumber EnchantingDustExile = new VariableBignumber(0.0);

	public static List<VariableBignumber> ResourcesRealm = new List<VariableBignumber>();

	public static List<VariableBignumber> ResourcesTotal = new List<VariableBignumber>();

	public static VariableInt UnlockedItems = new VariableInt(0);

	public static List<VariableInt> UnlockedByTiers = new List<VariableInt>();

	public static VariableInt BatsExile = new VariableInt(0);

	public static VariableInt CollectablessExile = new VariableInt(0);

	public static VariableInt CollectablesRealm = new VariableInt(0);

	public static VariableInt Collectables = new VariableInt(0);

	public static void ExileReset()
	{
		ManaSession.SetValue(0.0);
		VoidManaSession.SetValue(0.0);
		MaxVoidManaSession.SetValue(0.0);
		Clicks.SetValue(0.0);
		AutoClicks.SetValue(0.0);
		CastSpell.SetValue(0.0);
		ShardsSession.SetValue(0.0);
		ClickableCollect.SetValue(0uL);
		BoughtUpgrades.SetValue(0);
		TotalBuildings.SetValue(0);
		TimeSession.SetValue(0uL);
		SkipedTimeSession.SetValue(0.0);
		TimeIdleSession.SetValue(0uL);
		TimeOfflineSession.SetValue(0uL);
		PetMaxLevel.SetValue(1);
		ResourcesCollected.SetValue(0.0);
		CollectablessExile.SetValue(0);
		BatsExile.SetValue(0);
		EnchantingDustExile.SetValue(0.0);
	}

	public static void RealmReset()
	{
		ExileReset();
		AscendsInRealm.SetValue(0);
		ManaRealm.SetValue(0.0);
		VoidManaRealm.SetValue(0.0);
		ClicksRealm.SetValue(0.0);
		AutoClicksRealm.SetValue(0.0);
		CastSpellRealm.SetValue(0.0);
		ShardsRealm.SetValue(0.0);
		ClickableCollectRealm.SetValue(0uL);
		ApprenticeMaxLevelRealm.SetValue(1);
		TimeRealm.SetValue(0uL);
		SkipedTimeRealm.SetValue(0.0);
		TimeIdleRealm.SetValue(0uL);
		TimeOfflineRealm.SetValue(0uL);
		ResourcesCollectedRealm.SetValue(0.0);
		for (int i = 0; i < ResourcesRealm.Count; i++)
		{
			ResourcesRealm[i].SetValue(0.0);
		}
		CollectablesRealm.SetValue(0);
		CTTotal.SetValue(0.0);
		HCTotal.SetValue(0.0);
		LSTotal.SetValue(0.0);
	}

	public static void Reset()
	{
		RealmReset();
		ManaAllTime.SetValue(0.0);
		VoidManaAllTime.SetValue(0.0);
		Ascends.SetValue(0);
		AscendsInRealm.SetValue(0);
		ClicksTotal.SetValue(0.0);
		AutoClicksTotal.SetValue(0.0);
		CastSpellTotal.SetValue(0.0);
		ShardsTotal.SetValue(0.0);
		ClickableCollectTotal.SetValue(0uL);
		TimeTotal.SetValue(1uL);
		SkipedTimeTotal.SetValue(0.0);
		TimeIdleTotal.SetValue(1uL);
		TimeOfflineTotal.SetValue(1uL);
		PetMaxLevelAllTime.SetValue(1);
		HeroMaxLevelAllTime.SetValue(1);
		ResourcesCollected.SetValue(0.0);
		ResourcesCollectedRealm.SetValue(0.0);
		ResourcesCollectedTotal.SetValue(0.0);
		for (int i = 0; i < ResourcesTotal.Count; i++)
		{
			ResourcesTotal[i].SetValue(0.0);
		}
		UnlockedItems.SetValue(0);
		for (int j = 0; j < UnlockedByTiers.Count; j++)
		{
			UnlockedByTiers[j].SetValue(0);
		}
		for (int k = 0; k < ClassTime.Count; k++)
		{
			ClassTime[k].SetValue(0uL);
		}
		Collectables.SetValue(0);
		CollectablessExile.SetValue(0);
		BatsExile.SetValue(0);
		LSTotal.SetValue(0.0);
		HCTotal.SetValue(0.0);
		CTTotal.SetValue(0.0);
	}

	public static void Change(VariableInt variable, int change)
	{
		if (GameManager.Instance.ChallengeManager.StatsIsOn())
		{
			variable.Change(change);
		}
	}

	public static void Change(VariableLong variable, int change)
	{
		if (GameManager.Instance.ChallengeManager.StatsIsOn())
		{
			variable.Change(change);
		}
	}

	public static void Change(VariableBignumber variable, BigNumber change)
	{
		if (GameManager.Instance.ChallengeManager.StatsIsOn())
		{
			variable.Change(change);
		}
	}

	public static void ChangeSet(VariableInt variable, int new_value)
	{
		if (GameManager.Instance.ChallengeManager.StatsIsOn())
		{
			variable.SetValue(new_value);
		}
	}

	public static void ChangeSet(VariableLong variable, ulong new_value)
	{
		if (GameManager.Instance.ChallengeManager.StatsIsOn())
		{
			variable.SetValue(new_value);
		}
	}

	public static VariableBignumber GetEdustRealm()
	{
		return ResourcesRealm[4];
	}

	public static VariableBignumber GetEdustTotal()
	{
		return ResourcesTotal[4];
	}

	public static void ChangeClassTime(HeroesNames key, int add)
	{
		int num = (int)key;
		if (num >= 100)
		{
			num -= 81;
		}
		if (ClassTime.Count > num)
		{
			ClassTime[num].Change(add);
		}
	}

	public static ulong GetClassPlayedTime(HeroesNames key)
	{
		int num = (int)key;
		if (num >= 100)
		{
			num -= 81;
		}
		if (ClassTime.Count > num)
		{
			return ClassTime[num].ValueInt;
		}
		return 0uL;
	}

	public static string time_to_string_description(BigNumber time)
	{
		if (time > 63072002048.0)
		{
			return (time / 31536000.0).ToReadableString("F0") + " " + "years".Translate() + " ";
		}
		return time_to_string(time.ToUlong(), full: true);
	}

	public static string time_to_string(BigNumber time, bool full = false, bool fillSeconds = false)
	{
		if (time > 3153600000.0)
		{
			return (time / 31536000.0).ToReadableString("F0") + " " + "years".Translate() + " ";
		}
		return time_to_string(time.ToUlong(), full, fillSeconds);
	}

	public static string time_to_string(int time, bool full = false, bool fillSeconds = false)
	{
		return time_to_string((ulong)time, full, fillSeconds);
	}

	public static string time_to_string(ulong time, bool full = false, bool fillSeconds = false)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (time == 0L)
		{
			stringBuilder.Append(0);
			return stringBuilder.ToString();
		}
		if (time < 10 && fillSeconds)
		{
			fillSeconds = false;
		}
		ulong num = time % 60;
		time /= 60;
		if (time != 0)
		{
			ulong num2 = time % 60;
			time /= 60;
			if (time != 0)
			{
				ulong num3 = time % 24;
				time /= 24;
				if (time != 0)
				{
					ulong num4 = time % 365;
					time /= 365;
					if (time != 0)
					{
						if (time > 2000)
						{
							stringBuilder.Append(new BigNumber(time).ToReadableString("F0"));
							stringBuilder.Append(" ").Append("years".Translate());
							return stringBuilder.ToString();
						}
						stringBuilder.Append(time);
						if (full)
						{
							stringBuilder.Append(" ").Append("years".Translate()).Append(" ");
						}
						else
						{
							stringBuilder.Append("y".Translate());
						}
					}
					if (num4 != 0)
					{
						stringBuilder.Append(num4);
						if (full)
						{
							stringBuilder.Append(" ").Append("days".Translate()).Append(" ");
						}
						else
						{
							stringBuilder.Append("d".Translate());
						}
					}
				}
				if (num3 != 0)
				{
					stringBuilder.Append(num3);
					if (full)
					{
						stringBuilder.Append(" ").Append("hours".Translate()).Append(" ");
					}
					else
					{
						stringBuilder.Append("h".Translate());
					}
				}
			}
			if (num2 != 0)
			{
				stringBuilder.Append(num2);
				if (full)
				{
					stringBuilder.Append(" ").Append("min".Translate()).Append(" ");
				}
				else
				{
					stringBuilder.Append("m".Translate());
				}
			}
		}
		if (fillSeconds && num < 10)
		{
			stringBuilder.Append("0");
		}
		if (num != 0 || fillSeconds)
		{
			stringBuilder.Append(num);
			if (full)
			{
				stringBuilder.Append(" ").Append("sec".Translate()).Append(" ");
			}
			else
			{
				stringBuilder.Append("s".Translate());
			}
		}
		return stringBuilder.ToString();
	}
}
