using System;
using System.Collections.Generic;
using UnityEngine;

public class FamiliarManager : MonoBehaviour
{
	public class SaveData
	{
		[Serializable]
		public class FoodAmountEntry
		{
			public int foodId;

			public ulong amount;
		}

		public List<Familiar.SaveData> familiars;

		public ReliquariesController.SaveData reliquaries;

		public int[] activeSlots;

		public BigNumber dust;

		public int totalOpened;

		public List<FoodAmountEntry> foodAmounts;
	}

	public VariableBignumber Dust;

	[SerializeField]
	private FamiliarController familiarController;

	[SerializeField]
	private ReliquariesController reliquariesController;

	[SerializeField]
	private List<FamiliarSpot> familiarSpots;

	[SerializeField]
	private DisableCanvas disableCanvas;

	[SerializeField]
	private FamiliarFoodDatabase foodDatabase;

	private Dictionary<int, ulong> foodAmounts = new Dictionary<int, ulong>();

	public static readonly int[] SlotRankRequirements = new int[3] { 0, 150, 300 };

	public static readonly int[] DustRequirements = new int[21]
	{
		0, 100, 120, 150, 200, 250, 300, 375, 500, 625,
		800, 1000, 1250, 1500, 1850, 2300, 2850, 3500, 4200, 5000,
		7500
	};

	public static readonly int FoodRequirements = 100;

	public static readonly float FeedBuffDurationSeconds = 86400f;

	private List<Familiar> familiars = new List<Familiar>();

	private Dictionary<FamiliarTags, VariableInt> rankTotalsByTag = new Dictionary<FamiliarTags, VariableInt>();

	public Action OnOpenReliquary;

	public VariableInt TotalRank { get; private set; }

	public VariableInt TotalLevel { get; private set; }

	public VariableInt TotalReliquariesOpened { get; private set; }

	public IReadOnlyList<Familiar> AllFamiliars => familiars;

	public IReadOnlyDictionary<FamiliarTags, VariableInt> RankTotalsByTag => rankTotalsByTag;

	public bool HasAnyReliquaries()
	{
		return reliquariesController.HasAnyReliquaries();
	}

	public void Init()
	{
		Dust = new VariableBignumber(0.0);
		TotalLevel = new VariableInt(0);
		TotalReliquariesOpened = new VariableInt(0);
		BuildRankTrackers();
		GameContext.ContextAddResource("Familiars.TotalOpened", TotalReliquariesOpened);
		GameContext.ContextAddResource("Familiars.TotalLevel", TotalLevel);
		GameContext.ContextAddResource("Familiars.TotalRank", TotalRank);
		familiars = new List<Familiar>();
		foreach (FamiliarFormat familiar in GlobalData.Familiars)
		{
			familiars.Add(new Familiar(familiar, familiarController.Atlas));
		}
		familiars.Sort((Familiar a, Familiar b) => a.Rarity.CompareTo(b.Rarity));
		familiarController.Init(familiars);
		reliquariesController.Init();
		GameManager instance = GameManager.Instance;
		instance.GameTickReal = (Action<float>)Delegate.Combine(instance.GameTickReal, new Action<float>(AddExp));
		Paragons paragon = GameManager.Instance.Paragon;
		paragon.onChangeLVL = (Action)Delegate.Combine(paragon.onChangeLVL, new Action(UpdateFamiliarSpots));
	}

	private void OnDestroy()
	{
		GameManager instance = GameManager.Instance;
		instance.GameTickReal = (Action<float>)Delegate.Remove(instance.GameTickReal, new Action<float>(AddExp));
		if (GameManager.Instance.Paragon != null)
		{
			Paragons paragon = GameManager.Instance.Paragon;
			paragon.onChangeLVL = (Action)Delegate.Remove(paragon.onChangeLVL, new Action(UpdateFamiliarSpots));
		}
	}

	public void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			CloseFirst();
		}
	}

	public void AddReliquary(FamiliarChests chest, int amount = 1)
	{
		reliquariesController.AddChest(chest, amount);
	}

	public void AddFood(int foodId, ulong amount)
	{
		if (amount != 0L)
		{
			if (foodAmounts.TryGetValue(foodId, out var value))
			{
				foodAmounts[foodId] = value + amount;
			}
			else
			{
				foodAmounts[foodId] = amount;
			}
		}
	}

	public ulong GetFoodAmount(int foodId)
	{
		if (!foodAmounts.TryGetValue(foodId, out var value))
		{
			return 0uL;
		}
		return value;
	}

	public void ConsumeFood(int foodId, ulong amount)
	{
		if (amount != 0L)
		{
			ulong foodAmount = GetFoodAmount(foodId);
			foodAmounts[foodId] = foodAmount - amount;
		}
	}

	public FamiliarFood GetFoodForTag(FamiliarTags tag)
	{
		if (!(foodDatabase != null))
		{
			return null;
		}
		return foodDatabase.GetFoodForTag(tag);
	}

	public FamiliarFood GetFoodById(int id)
	{
		if (!(foodDatabase != null))
		{
			return null;
		}
		return foodDatabase.GetFoodById(id);
	}

	public Sprite GetFoodIcon(int foodId)
	{
		return GetFoodById(foodId)?.Icon;
	}

	public void ToggleState()
	{
		if (base.gameObject.activeSelf)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData();
		saveData.dust = Dust.Value;
		saveData.familiars = new List<Familiar.SaveData>();
		foreach (Familiar familiar in familiars)
		{
			if (familiar.Rank.ValueInt > 0)
			{
				saveData.familiars.Add(familiar.Save());
			}
		}
		saveData.reliquaries = reliquariesController.Save();
		saveData.activeSlots = new int[familiarController.Slots.Count];
		for (int i = 0; i < familiarController.Slots.Count; i++)
		{
			saveData.activeSlots[i] = familiarController.Slots[i].AssignedFamiliar?.Id ?? (-1);
		}
		saveData.totalOpened = TotalReliquariesOpened.ValueInt;
		saveData.foodAmounts = new List<SaveData.FoodAmountEntry>();
		foreach (KeyValuePair<int, ulong> foodAmount in foodAmounts)
		{
			if (foodAmount.Value != 0)
			{
				saveData.foodAmounts.Add(new SaveData.FoodAmountEntry
				{
					foodId = foodAmount.Key,
					amount = foodAmount.Value
				});
			}
		}
		return saveData;
	}

	public void Load(SaveData data)
	{
		TotalLevel.SetValue(0);
		foreach (Familiar familiar in familiars)
		{
			familiar.Load(null);
		}
		familiarController.ClearAllSlots();
		reliquariesController.Load(data?.reliquaries);
		if (data?.familiars != null)
		{
			foreach (Familiar.SaveData saved in data.familiars)
			{
				familiars.Find((Familiar x) => x.Id == saved.Id)?.Load(saved);
			}
		}
		UpdateRankTotals();
		if (data?.activeSlots != null)
		{
			for (int num = 0; num < familiarController.Slots.Count && num < data.activeSlots.Length; num++)
			{
				int savedId = data.activeSlots[num];
				if (savedId >= 0)
				{
					familiarController.AssignFamiliarToSlot(num, familiars.Find((Familiar x) => x.Id == savedId));
				}
			}
		}
		Dust.SetValue(data?.dust ?? ((BigNumber)0.0));
		TotalReliquariesOpened.SetValue(data?.totalOpened ?? 0);
		foodAmounts.Clear();
		if (data?.foodAmounts != null)
		{
			foreach (SaveData.FoodAmountEntry foodAmount in data.foodAmounts)
			{
				foodAmounts[foodAmount.foodId] = foodAmount.amount;
			}
		}
		UpdateFamiliarSpots();
	}

	public void PreLoad()
	{
		foreach (Familiar familiar in familiars)
		{
			familiar.DeactivateAll();
		}
	}

	public void PostLoad()
	{
		foreach (Familiar familiar in familiars)
		{
			familiar.CheckActivation();
		}
	}

	public BigNumber GetDustRequirements(int rank, int rarity)
	{
		if (rank >= DustRequirements.Length)
		{
			return 0.0;
		}
		return new BigNumber((float)DustRequirements[rank] * Mathf.Pow(2f, rarity - 1));
	}

	public bool HasEmptySlot()
	{
		int valueInt = TotalLevel.ValueInt;
		foreach (FamiliarSlot slot in familiarController.Slots)
		{
			if (slot.AssignedFamiliar == null && valueInt >= slot.RequiredTotalLevel)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasAnyActive()
	{
		foreach (FamiliarSlot slot in familiarController.Slots)
		{
			if (slot.AssignedFamiliar != null)
			{
				return true;
			}
		}
		return false;
	}

	public void ChooseFamiliar(Familiar familiar)
	{
		if (familiar.Rank.ValueInt != 0 && HasEmptySlot())
		{
			familiarController.OnChooseFamiliar(familiar);
			UpdateFamiliarSpots();
		}
	}

	public void DeselectFamiliar(Familiar familiar)
	{
		if (familiar != null && familiar.IsSelected)
		{
			familiarController.UnassignFamiliar(familiar);
			UpdateFamiliarSpots();
		}
	}

	public void UpdateFamiliarSpots()
	{
		bool familiarsIsAvailable = GameManager.Instance.Paragon.FamiliarsIsAvailable;
		for (int i = 0; i < familiarSpots.Count; i++)
		{
			familiarSpots[i].gameObject.SetActive(familiarsIsAvailable);
			if (familiarsIsAvailable)
			{
				familiarSpots[i].SetFamiliar(familiarController.Slots[i].AssignedFamiliar);
			}
			else
			{
				familiarSpots[i].SetFamiliar(null);
			}
		}
	}

	private void BuildRankTrackers()
	{
		TotalRank = new VariableInt(0);
		rankTotalsByTag = new Dictionary<FamiliarTags, VariableInt>();
		foreach (FamiliarTags value in Enum.GetValues(typeof(FamiliarTags)))
		{
			rankTotalsByTag[value] = new VariableInt(0);
		}
		BindRankListeners();
	}

	public void UpdateRank(Familiar familiar, int add)
	{
		TotalRank.Change(add);
		foreach (FamiliarTags tag in familiar.Tags)
		{
			rankTotalsByTag[tag].Change(add);
		}
	}

	private void BindRankListeners()
	{
		foreach (Familiar familiar in familiars)
		{
			VariableInt rank = familiar.Rank;
			rank.OnChange = (Action)Delegate.Remove(rank.OnChange, new Action(UpdateRankTotals));
			VariableInt rank2 = familiar.Rank;
			rank2.OnChange = (Action)Delegate.Combine(rank2.OnChange, new Action(UpdateRankTotals));
		}
		UpdateRankTotals();
	}

	private void UpdateRankTotals()
	{
		int num = 0;
		Dictionary<FamiliarTags, int> dictionary = new Dictionary<FamiliarTags, int>();
		foreach (FamiliarTags key in rankTotalsByTag.Keys)
		{
			dictionary[key] = 0;
		}
		foreach (Familiar familiar in familiars)
		{
			int valueInt = familiar.Rank.ValueInt;
			num += valueInt;
			foreach (FamiliarTags tag in familiar.Tags)
			{
				dictionary[tag] += valueInt;
			}
		}
		TotalRank.SetValue(num);
		foreach (KeyValuePair<FamiliarTags, int> item in dictionary)
		{
			rankTotalsByTag[item.Key].SetValue(item.Value);
		}
	}

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		disableCanvas.On();
		OpenFamiliars();
	}

	public void Close()
	{
		disableCanvas.Off();
		base.gameObject.SetActive(value: false);
	}

	public void OpenFamiliars()
	{
		familiarController.Open();
		reliquariesController.Close();
	}

	public void OpenReliquaries()
	{
		familiarController.Close();
		reliquariesController.Open();
	}

	public void AddExp(float dt)
	{
		if (!GameManager.Instance.Paragon.FamiliarsIsAvailable)
		{
			return;
		}
		foreach (FamiliarSlot slot in familiarController.Slots)
		{
			slot.AssignedFamiliar?.AddXP(dt);
		}
	}

	public void ResetActiveSlots()
	{
		familiarController.ResetActiveSlots();
		UpdateFamiliarSpots();
	}

	public void CloseFirst()
	{
		if (!reliquariesController.IsRolling)
		{
			if (reliquariesController.gameObject.activeSelf)
			{
				OpenFamiliars();
			}
			else
			{
				Close();
			}
		}
	}
}
