using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
	[Serializable]
	public class EventCurrencySave
	{
		public string currencyName;

		public int amount;

		public int year;

		public int rewardStep;

		public string eventStartDate;

		public EventCurrencySave(string name, int amt, string startDate)
		{
			currencyName = name;
			amount = amt;
			eventStartDate = startDate;
			year = DateTime.Now.Year;
			rewardStep = 0;
		}
	}

	public class EventSave
	{
		public int eggs;

		public int embers;

		public int harvest;

		public int tokens;

		public int seed;

		public int snow;

		public DateTime date;

		public float timer;

		public float spawn;

		public Dictionary<string, float> solstice;

		public February.SaveData february;

		public string name;

		public int spawned;

		public EventCurrencySave currentCurrencyStats;
	}

	public VariableInt Eggs;

	public VariableInt Embers;

	public VariableInt HarvestToken;

	public VariableInt SkinTokens;

	public VariableInt Snowflakes;

	public VariableInt XP;

	public int randomSeed;

	public DateTime Date;

	public GameObject Window;

	public float Timer;

	public float Spawn;

	private EventController controller;

	public Dictionary<string, float> solsticeBonuses;

	public February.SaveData february;

	public string Event;

	public int Spawned;

	public bool isActive;

	public EventCurrencySave currentCurrencyStats;

	private string currentEventStartDate;

	public void Init()
	{
		Eggs = new VariableInt(0);
		GameContext.ContextAddResource("Event.Eggs", Eggs);
		Embers = new VariableInt(0);
		GameContext.ContextAddResource("Event.Embers", Embers);
		SkinTokens = new VariableInt(0);
		GameContext.ContextAddResource("Event.Seeds", SkinTokens);
		HarvestToken = new VariableInt(0);
		GameContext.ContextAddResource("Event.Harvest", HarvestToken);
		Snowflakes = new VariableInt(0);
		GameContext.ContextAddResource("Event.Christmas", Snowflakes);
		XP = new VariableInt(0);
		GameContext.ContextAddResource("Event.XP", XP);
	}

	public void Apply()
	{
		controller?.Apply();
	}

	public void Remove()
	{
		controller?.Remove();
	}

	public void Solstice()
	{
		CreateEvent("Solstice");
	}

	public void Harvest()
	{
		CreateEvent("Harvest");
	}

	public void Halloween()
	{
		CreateEvent("Halloween");
	}

	public void Anniversary()
	{
		CreateEvent("Anniversary");
	}

	public void WHAT()
	{
		CreateEvent("WHAT");
	}

	public void Christmas()
	{
		CreateEvent("Christmas");
	}

	public void Easter()
	{
		CreateEvent("Easter");
	}

	public void Fool()
	{
		CreateEvent("Fool");
	}

	private void CreateEvent(string name)
	{
		if (isActive)
		{
			if (controller.name == name)
			{
				return;
			}
			UnityEngine.Object.Destroy(controller.gameObject);
		}
		Debug.Log("create " + name);
		controller = UnityEngine.Object.Instantiate(Resources.Load<EventController>("Event/" + name));
		controller.name = name;
		isActive = true;
		ResetCurrency();
	}

	public void SetEventStartDate(string startDate)
	{
		currentEventStartDate = startDate;
	}

	public void ResetCurrency()
	{
		if (currentCurrencyStats == null || controller == null)
		{
			return;
		}
		string text = controller.GetCurrencyName().Replace("Event.", "");
		if (currentCurrencyStats.currencyName != text)
		{
			currentCurrencyStats = null;
		}
		else if (!string.IsNullOrEmpty(currentEventStartDate))
		{
			if (!string.IsNullOrEmpty(currentCurrencyStats.eventStartDate))
			{
				if (currentCurrencyStats.eventStartDate != currentEventStartDate)
				{
					currentCurrencyStats = null;
				}
			}
			else
			{
				currentCurrencyStats.eventStartDate = currentEventStartDate;
			}
		}
		else if (currentCurrencyStats.year != DateTime.Now.Year)
		{
			currentCurrencyStats = null;
		}
	}

	public void RevertEventCosmetics()
	{
		GameManager.Instance.Gallery.RefreshAllFromActive();
		GameManager.Instance.PetGallery.RefreshAllFromActive();
		GameManager.Instance.Interior.Orbs.ActivateCurrent();
		GameManager.Instance.Interior.Backs.ActivateCurrent();
		if (GameManager.Instance.Interior.Hunter.isActive)
		{
			GameManager.Instance.Interior.Hunter.SetFake(GameManager.Instance.Interior.Hunter.selectedId);
		}
	}

	public void TurnOffAll()
	{
		if (controller != null)
		{
			RevertEventCosmetics();
			UnityEngine.Object.Destroy(controller.gameObject);
			isActive = false;
		}
	}

	public void Offline(float time)
	{
		if (controller == null)
		{
			return;
		}
		if (Spawn > 0f)
		{
			Timer += time;
			if (Timer >= 86400f)
			{
				Timer = 86400f;
			}
		}
		controller.Offline(time);
	}

	public EventSave Save()
	{
		return new EventSave
		{
			eggs = Eggs.ValueInt,
			embers = Embers.ValueInt,
			harvest = HarvestToken.ValueInt,
			tokens = SkinTokens.ValueInt,
			snow = Snowflakes.ValueInt,
			seed = randomSeed,
			date = Date,
			timer = Timer,
			spawn = Spawn,
			solstice = solsticeBonuses,
			february = february,
			name = ((controller != null) ? controller.name : string.Empty),
			spawned = ((controller != null) ? controller.GetSpawned() : 0),
			currentCurrencyStats = currentCurrencyStats
		};
	}

	public void PreLoad()
	{
		Remove();
		if (IsActiveEvent())
		{
			controller.PreLoad();
		}
	}

	public void Load(EventSave data)
	{
		if (data == null)
		{
			Eggs.SetValue(0);
			Embers.SetValue(0);
			HarvestToken.SetValue(0);
			SkinTokens.SetValue(0);
			Snowflakes.SetValue(0);
			randomSeed = 0;
			Date = DateTime.MinValue;
			Timer = 0f;
			Spawn = 0f;
			Event = string.Empty;
			currentCurrencyStats = null;
			return;
		}
		Eggs.SetValue(data.eggs);
		Embers.SetValue(data.embers);
		HarvestToken.SetValue(data.harvest);
		SkinTokens.SetValue(data.tokens);
		Snowflakes.SetValue(data.snow);
		randomSeed = data.seed;
		Date = data.date;
		Timer = data.timer;
		Spawn = data.spawn;
		solsticeBonuses = data.solstice;
		february = data.february;
		Event = data.name;
		Spawned = data.spawned;
		currentCurrencyStats = data.currentCurrencyStats;
		DateTime now = DateTime.Now;
		if (now.Year == 2026 && now.Month == 2 && now.Day >= 16 && now.Day <= 18 && data.name == "WHAT" && data.currentCurrencyStats != null && data.currentCurrencyStats.amount >= 3000 && data.currentCurrencyStats.year == 2026 && data.february != null && data.february.Exp >= 3000f)
		{
			february = new February.SaveData();
			currentCurrencyStats = null;
		}
	}

	public void PostLoad()
	{
		if (IsActiveEvent())
		{
			int spawned = 0;
			ResetCurrency();
			if (controller.name == Event)
			{
				spawned = Spawned;
			}
			controller.PostLoad(spawned);
		}
		Apply();
	}

	public void AddCollectedCurrency(string currencyName, int amount)
	{
		EventCurrencySave eventCurrencySave = currentCurrencyStats;
		if (eventCurrencySave != null && eventCurrencySave.currencyName == currencyName && IsSameEventInstance(eventCurrencySave))
		{
			eventCurrencySave.amount += amount;
		}
		else
		{
			currentCurrencyStats = new EventCurrencySave(currencyName, amount, currentEventStartDate);
		}
		if (!(controller != null))
		{
			return;
		}
		List<int> allThresholds = controller.GetAllThresholds();
		if (allThresholds != null)
		{
			for (int i = currentCurrencyStats.rewardStep; i < allThresholds.Count && currentCurrencyStats.amount >= allThresholds[i]; i++)
			{
				controller.OnReward(i);
				currentCurrencyStats.rewardStep = i + 1;
			}
		}
	}

	private bool IsSameEventInstance(EventCurrencySave stats)
	{
		if (!string.IsNullOrEmpty(currentEventStartDate) && !string.IsNullOrEmpty(stats.eventStartDate))
		{
			return stats.eventStartDate == currentEventStartDate;
		}
		return stats.year == DateTime.Now.Year;
	}

	private bool IsActiveEvent()
	{
		if (controller != null)
		{
			return controller.gameObject.activeSelf;
		}
		return false;
	}
}
