using System;
using System.Collections.Generic;
using UnityEngine;

public class PantheonManager : MonoBehaviour
{
	public class SaveData
	{
		public Dictionary<Gods, BigNumber> exp;

		public Dictionary<Gods, int> maxLvls;

		public Dictionary<Gods, int> maxLvlsRealm;

		public List<Gods> chooses;

		public BigNumber prayingExp;

		public int majorMaxLvl;

		public int minorMaxLvl;

		public TempleSaveData temple;

		public SaveData()
		{
			exp = new Dictionary<Gods, BigNumber>();
			maxLvls = new Dictionary<Gods, int>();
			maxLvlsRealm = new Dictionary<Gods, int>();
			temple = new TempleSaveData();
		}

		public void Add(Gods key, BigNumber value)
		{
			exp.Add(key, value);
		}

		public void AddMaxLvl(Gods key, int value)
		{
			maxLvls.Add(key, value);
		}

		public void AddMaxLvlRealm(Gods key, int value)
		{
			maxLvlsRealm.Add(key, value);
		}

		public void AddChooses(List<Gods> chosen)
		{
			chooses = chosen;
		}

		public void SaveTemple(Temple data, Dictionary<Gods, God> gods)
		{
			temple.buff = data.Buff.Amount;
			temple.buffProgress = data.Buff.GetProgress();
			temple.xp = data.XP.Amount;
			temple.xpProgress = data.XP.GetProgress();
			temple.power = data.Power.Amount;
			temple.powerProgress = data.Power.GetProgress();
			if (data.Buff.Total != null)
			{
				temple.buffTotal = data.Buff.Total.ValueInt;
			}
			if (data.XP.Total != null)
			{
				temple.xpTotal = data.XP.Total.ValueInt;
			}
			if (data.Power.Total != null)
			{
				temple.powerTotal = data.Power.Total.ValueInt;
			}
			foreach (KeyValuePair<Gods, God> god in gods)
			{
				if (god.Value.OfferingBuff > 0f)
				{
					temple.buffs.Add(god.Key, god.Value.OfferingBuff);
				}
				if (god.Value.ScriptureXP > 0)
				{
					temple.xps.Add(god.Key, god.Value.ScriptureXP);
				}
				if (god.Value is MainGod)
				{
					MainGod mainGod = god.Value as MainGod;
					if (mainGod.MainTenet > 0)
					{
						temple.powers.Add(god.Key, mainGod.MainTenet);
					}
					if (mainGod.SecondaryTenet > 0)
					{
						temple.powersSec.Add(god.Key, mainGod.SecondaryTenet);
					}
				}
			}
		}

		public void LoadTemple(Temple data, Dictionary<Gods, God> gods)
		{
			data.Reset();
			data.Buff.Load(temple.buff, temple.buffProgress, temple.buffTotal);
			data.XP.Load(temple.xp, temple.xpProgress, temple.xpTotal);
			data.Power.Load(temple.power, temple.powerProgress, temple.powerTotal);
			foreach (KeyValuePair<Gods, float> buff in temple.buffs)
			{
				if (gods.ContainsKey(buff.Key))
				{
					gods[buff.Key].OfferingBuff = buff.Value;
				}
			}
			foreach (KeyValuePair<Gods, int> xp in temple.xps)
			{
				if (gods.ContainsKey(xp.Key))
				{
					gods[xp.Key].ScriptureXP = xp.Value;
				}
			}
			foreach (KeyValuePair<Gods, int> power in temple.powers)
			{
				if (gods.ContainsKey(power.Key) && gods[power.Key] is MainGod)
				{
					(gods[power.Key] as MainGod).SetMainTenet(power.Value);
				}
			}
			foreach (KeyValuePair<Gods, int> item in temple.powersSec)
			{
				if (gods.ContainsKey(item.Key) && gods[item.Key] is MainGod)
				{
					(gods[item.Key] as MainGod).SetSecondaryTenet(item.Value);
				}
			}
		}
	}

	public class TempleSaveData
	{
		public int buff;

		public float buffProgress;

		public int xp;

		public float xpProgress;

		public int power;

		public float powerProgress;

		public int buffTotal;

		public int xpTotal;

		public int powerTotal;

		public Dictionary<Gods, float> buffs;

		public Dictionary<Gods, int> xps;

		public Dictionary<Gods, int> powers;

		public Dictionary<Gods, int> powersSec;

		public TempleSaveData()
		{
			buffs = new Dictionary<Gods, float>();
			xps = new Dictionary<Gods, int>();
			powers = new Dictionary<Gods, int>();
			powersSec = new Dictionary<Gods, int>();
		}
	}

	public VariableIntVector MaxChooses;

	public VariableInt MaxLevel;

	public VariableInt MaxLevelRealm;

	public VariableInt MinorMaxLevel;

	public VariableComplex GodExp;

	public VariableComplex ComebackSpeed;

	public VariableComplex MainAP;

	public VariableComplex SecAP;

	public VariableComplex MinorAP;

	public VariableComplex MinorExp;

	public PantheonWindow window;

	public BottomPanelButton sign;

	public Temple temple;

	[SerializeField]
	private float baseExpIncome = 0.01f;

	private List<bool> unlocks;

	private bool skillSecond;

	private List<Gods> chosen;

	private Dictionary<Gods, God> gods;

	private float timer;

	private Skill praying;

	private int available;

	private bool isLoading;

	public Action OnGetExp;

	public Action<int> OnLevelUp;

	private SimpleEffect maxChooseEffect;

	private List<int> maxChooseSteps = new List<int> { 10, 25, 50, 75, 100 };

	public SpecLinker SpecBinding;

	public void PreInit()
	{
		MaxChooses = new VariableIntVector(1);
		MaxLevel = new VariableInt(0);
		MaxLevelRealm = new VariableInt(0);
		MinorMaxLevel = new VariableInt(0);
		GodExp = new VariableComplex(1.0);
		ComebackSpeed = new VariableComplex(1.0);
		MainAP = new VariableComplex(1.0);
		SecAP = new VariableComplex(1.0);
		MinorAP = new VariableComplex(1.0);
		MinorExp = new VariableComplex(1.0);
		GameContext.ContextAddResource("Pantheon.ExpBonus", GodExp);
		GameContext.ContextAddResource("Pantheon.ComebackSpeed", ComebackSpeed);
		GameContext.ContextAddResource("Pantheon.MaxChoose", MaxChooses);
		GameContext.ContextAddResource("Pantheon.MaxLevel", MaxLevel);
		GameContext.ContextAddResource("Pantheon.MainAP", MainAP);
		GameContext.ContextAddResource("Pantheon.SecAP", SecAP);
		GameContext.ContextAddResource("Pantheon.MinorAP", MinorAP);
		GameContext.ContextAddResource("Pantheon.MinorExp", MinorExp);
		GameContext.ContextAddResource("Pantheon.MinorMaxLevel", MinorMaxLevel);
	}

	public void Init()
	{
		SpecBinding = new SpecLinker(this);
		gods = new Dictionary<Gods, God>();
		unlocks = new List<bool>(5) { false, false, false, false, false };
		chosen = new List<Gods>();
		AddGod(new MainGod(Gods.Energy, 1, "IncreaseEvoPerLevel", new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency, 0.0, 0.5), "IncreaseClickPerLevel", new SimpleEffect(GameManager.Instance.Orb.click_profit, 0.0, 0.20000000298023224)));
		AddGod(new MainGod(Gods.Life, 1, "IncreaseSummoningPerLevel", new SimpleEffect(GameManager.Instance.Scrolls.SummoningEfficiency, 0.0, 0.07500000298023224), "IncreaseVpEPerLevel", new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, 0.0, 0.08500000089406967)));
		AddGod(new MainGod(Gods.Change, 1, "IncreaseIncaPerLevel", new SimpleEffect(GameManager.Instance.Scrolls.IncantationEfficiency, 0.0, 0.07500000298023224), "IncreasePetXPPerLevel", new SimpleEffect(GameManager.Instance.CurrentPet.ExpBonus, 0.0, 0.08500000089406967)));
		AddGod(new MainGod(Gods.Existence, 2, "IncreaseAutoClickPerLevel", new SimpleEffect(GameManager.Instance.Orb.autoclick_profit, 0.0, 0.5), "IncreaseShardsPerLevel", new SimpleEffect(GameManager.Instance.Scrolls.ShardsPassive, 0.0, 0.08500000089406967)));
		AddGod(new MainGod(Gods.Time, 2, "IncreaseIdlePerLevel", new SimpleEffect(GameManager.Instance.Idle.IdleBonus, 0.0, 0.5), "IncreaseOfflinePerLevel", new SimpleEffect(GameManager.Instance.OfflineProduction, 0.0, 0.08500000089406967)));
		AddGod(new MainGod(Gods.Creation, 2, "IncreasePAPPerLevel", new SimpleEffect(GameManager.Instance.CurrentPet.AbilityPower, 0.0, 0.5), "IncreaseCharXPPerLevel", new SimpleEffect(GameManager.Instance.CurrentHero.ExpManaSources, 0.0, 0.08500000089406967)));
		AddGod(new MainGod(Gods.Chaos, 3, "IncreaseVPPerLevel", new SimpleEffect(GameManager.Instance.VoidManaManager.Power, 0.0, 0.5), "IncreaseMystPerLevel", new SimpleEffect(Reborn.SoulPower, 0.0, 0.08500000089406967)));
		AddGod(new MainGod(Gods.Consciousness, 4, "IncreaseCAPPerLevel", new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower, 0.0, 0.5), "IncreaseGodXPPerLevel", new SimpleEffect(GodExp, 0.0, 0.08500000089406967)));
		AddGod(new MinorGod(Gods.minorSpells, 5, "IncreaseSpellEffPerLevel", new List<SimpleEffect>
		{
			new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency, 0.0, 0.009999999776482582),
			new SimpleEffect(GameManager.Instance.Scrolls.SummoningEfficiency, 0.0, 0.009999999776482582),
			new SimpleEffect(GameManager.Instance.Scrolls.IncantationEfficiency, 0.0, 0.009999999776482582)
		}, new List<Gods>
		{
			Gods.Energy,
			Gods.Life,
			Gods.Change
		}));
		AddGod(new MinorGod(Gods.minorExp, 5, "IncreaseAllXPPerLevel", new List<SimpleEffect>
		{
			new SimpleEffect(GameManager.Instance.CurrentPet.ExpBonus, 0.0, 0.006000000052154064),
			new SimpleEffect(GameManager.Instance.CurrentHero.ExpMult, 0.0, 0.006000000052154064),
			new SimpleEffect(GodExp, 0.0, 0.006000000052154064)
		}, new List<Gods>
		{
			Gods.Creation,
			Gods.Consciousness,
			Gods.Change
		}));
		AddGod(new MinorGod(Gods.minorVoid, 5, "IncreaseVpEVPPerLevel", new List<SimpleEffect>
		{
			new SimpleEffect(GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity, 0.0, 0.125),
			new SimpleEffect(GameManager.Instance.VoidManaManager.Power, 0.0, 0.125)
		}, new List<Gods>
		{
			Gods.Life,
			Gods.Chaos
		}));
		AddGod(new MinorGod(Gods.minorIdle, 5, "IncreaseIdleAutoclickPerLevel", new List<SimpleEffect>
		{
			new SimpleEffect(GameManager.Instance.Orb.autoclick_profit, 0.0, 0.125),
			new SimpleEffect(GameManager.Instance.Idle.IdleBonus, 0.0, 0.125)
		}, new List<Gods>
		{
			Gods.Time,
			Gods.Existence,
			Gods.Energy
		}));
		AddGod(new MinorGod(Gods.minorEdust, 5, "IncreaseMajorMainAPPerLevel", new List<SimpleEffect>
		{
			new SimpleEffect(MainAP, 0.0, 0.009999999776482582)
		}, new List<Gods>
		{
			Gods.Change,
			Gods.Creation,
			Gods.Energy,
			Gods.Life,
			Gods.Existence,
			Gods.Time
		}));
		AddGod(new MinorGod(Gods.minorSplinter, 5, "IncreaseBreakthroughsExhibitPerLevel", new List<SimpleEffect>
		{
			new SimpleEffect(GameManager.Instance.Craft.ResearchGilding.Amount, 0.0, 0.009999999776482582),
			new SimpleEffect(GameManager.Instance.Craft.TrophiesGilding.Amount, 0.0, 0.009999999776482582)
		}, new List<Gods>
		{
			Gods.Chaos,
			Gods.Consciousness
		}));
		praying = GameManager.Instance.SkillManager.Get(SkillManager.SkillNames.Praying);
		window.Init();
		temple = new Temple();
		maxChooseEffect = new SimpleEffect(MaxChooses, 0.0, 1.0);
		maxChooseEffect.Apply();
		VariableInt maxLevelRealm = MaxLevelRealm;
		maxLevelRealm.OnChange = (Action)Delegate.Combine(maxLevelRealm.OnChange, new Action(RecalculateMaxChoose));
		RecalculateMaxChoose();
	}

	public void Update()
	{
		if (!GameManager.Instance.Paragon.PantheonIsAvailable)
		{
			return;
		}
		timer += Time.unscaledDeltaTime;
		if (timer > 1f)
		{
			GiveExp(1f);
			temple.Tick(1f);
			timer -= 1f;
			if (temple.Power.Amount > 0)
			{
				sign.TurnOnRed();
			}
			OnGetExp?.Invoke();
		}
	}

	public void Offline(BigNumber seconds)
	{
		if (GameManager.Instance.Paragon.PantheonIsAvailable)
		{
			GiveExp(seconds.ToFloat());
			int num = ((seconds > 604800.0) ? 604800 : seconds.ToInt());
			temple.Tick(num);
			OnGetExp?.Invoke();
		}
	}

	public BigNumber GetExpIncome()
	{
		return baseExpIncome * GodExp.Value * praying.GetBonus();
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData();
		saveData.majorMaxLvl = MaxLevel.ValueInt;
		saveData.minorMaxLvl = MinorMaxLevel.ValueInt;
		saveData.prayingExp = praying.totalExp;
		foreach (KeyValuePair<Gods, God> god in gods)
		{
			if (god.Value.ExpTotal.Mantissa > 0.0)
			{
				saveData.Add(god.Key, god.Value.ExpTotal);
			}
			if (god.Value.MaxLevel > 0)
			{
				saveData.AddMaxLvl(god.Key, god.Value.MaxLevel);
			}
			if (god.Value.MaxLevelRealm > 0)
			{
				saveData.AddMaxLvlRealm(god.Key, god.Value.MaxLevelRealm);
			}
		}
		saveData.AddChooses(chosen);
		saveData.SaveTemple(temple, gods);
		return saveData;
	}

	public void Load(SaveData data)
	{
		isLoading = true;
		ResetGods();
		if (data == null)
		{
			praying.Load(0.0);
			isLoading = false;
			return;
		}
		MaxLevel.SetValue(data.majorMaxLvl);
		MinorMaxLevel.SetValue(data.minorMaxLvl);
		int num = 0;
		foreach (KeyValuePair<Gods, BigNumber> item in data.exp)
		{
			God god = GetGod(item.Key);
			god.Load(item.Value);
			if (god.Level.ValueInt > num)
			{
				num = god.Level.ValueInt;
			}
		}
		foreach (KeyValuePair<Gods, int> maxLvl in data.maxLvls)
		{
			GetGod(maxLvl.Key).MaxLevel = maxLvl.Value;
		}
		int num2 = 0;
		foreach (KeyValuePair<Gods, int> item2 in data.maxLvlsRealm)
		{
			GetGod(item2.Key).MaxLevelRealm = item2.Value;
			if (item2.Value > num2)
			{
				num2 = item2.Value;
			}
		}
		int value = Mathf.Max(num, num2);
		MaxLevelRealm.SetValue(value);
		if (MinorMaxLevel.ValueInt == 0)
		{
			int num3 = 0;
			foreach (KeyValuePair<Gods, God> god2 in gods)
			{
				if (god2.Value.Group == 5 && god2.Value.Level.ValueInt > num3)
				{
					num3 = god2.Value.Level.ValueInt;
				}
			}
			MinorMaxLevel.SetValue(num3);
		}
		chosen = data.chooses;
		praying.Load(data.prayingExp);
		data.LoadTemple(temple, gods);
	}

	private void ChangeChosenAmount()
	{
		temple.SetAcolytes(MaxChooses.ValueInt - GetChosenAmount());
	}

	public void ResetOnRealmChange()
	{
		foreach (KeyValuePair<Gods, God> god in gods)
		{
			if (!GameManager.Instance.Realmcraft.IsActive && god.Value.MaxLevel < god.Value.MaxLevelRealm)
			{
				god.Value.MaxLevel = god.Value.MaxLevelRealm;
			}
			god.Value.ResetFull();
		}
		MaxLevelRealm.SetValue(0);
		temple.Reset();
	}

	public void ResetGods()
	{
		foreach (KeyValuePair<Gods, God> god in gods)
		{
			god.Value.ResetFull();
			god.Value.MaxLevel = 0;
		}
		MaxLevel.SetValue(0);
		MaxLevelRealm.SetValue(0);
		MinorMaxLevel.SetValue(0);
	}

	public void PreLoad()
	{
		Paragons paragon = GameManager.Instance.Paragon;
		paragon.onChangeLVL = (Action)Delegate.Remove(paragon.onChangeLVL, new Action(OnParagonChange));
		DeactivateAll();
	}

	public void PostLoad()
	{
		Reactivate();
		Paragons paragon = GameManager.Instance.Paragon;
		paragon.onChangeLVL = (Action)Delegate.Combine(paragon.onChangeLVL, new Action(OnParagonChange));
		MaxLevel.OnChange?.Invoke();
		isLoading = false;
		OnChangeMaxChoose();
		window.PostLoad();
	}

	public void DeactivateAll()
	{
		foreach (KeyValuePair<Gods, God> god in gods)
		{
			god.Value.DeactivateAll();
		}
	}

	public void Reactivate()
	{
		int num = 1;
		if (GameManager.Instance.Paragon.Pantheon2IsAvailable)
		{
			num++;
		}
		if (GameManager.Instance.Paragon.Pantheon3IsAvailable)
		{
			num++;
		}
		if (GameManager.Instance.Paragon.Pantheon4IsAvailable)
		{
			num++;
		}
		if (GameManager.Instance.Paragon.PantheonMinorsIsAvailable)
		{
			num++;
		}
		if (GameManager.Instance.Paragon.Gods2SkillIsAvailable)
		{
			num++;
		}
		if (GameManager.Instance.Paragon.GildingPantheonIsAvailable)
		{
			num++;
		}
		unlocks[0] = GameManager.Instance.Paragon.PantheonIsAvailable;
		unlocks[1] = GameManager.Instance.Paragon.Pantheon2IsAvailable;
		unlocks[2] = GameManager.Instance.Paragon.Pantheon3IsAvailable;
		unlocks[3] = GameManager.Instance.Paragon.Pantheon4IsAvailable;
		unlocks[4] = GameManager.Instance.Paragon.PantheonMinorsIsAvailable;
		skillSecond = GameManager.Instance.Paragon.Gods2SkillIsAvailable;
		MaxChooses.SetValue(num);
		available = 0;
		foreach (KeyValuePair<Gods, God> god in gods)
		{
			God value = god.Value;
			if (unlocks[value.Group - 1])
			{
				value.ActivateAbility(skillSecond);
				available++;
			}
			else
			{
				value.DeactivateAll();
			}
		}
		List<Gods> list = new List<Gods>();
		foreach (Gods item in chosen)
		{
			God value = GetGod(item);
			if (unlocks[value.Group - 1])
			{
				list.Add(item);
			}
		}
		chosen = list;
		RecalculateMaxChoose();
		OnChangeMaxChoose();
		ChangeChosenAmount();
	}

	public God GetGod(Gods key)
	{
		return gods[key];
	}

	public Dictionary<Gods, God> GetAllGods()
	{
		return gods;
	}

	public bool IsChosen(Gods key)
	{
		return chosen.Contains(key);
	}

	public int GetChosenAmount()
	{
		int num = chosen.Count;
		if (num > MaxChooses.ValueInt)
		{
			num = MaxChooses.ValueInt;
		}
		return num;
	}

	public bool IsAvailable(Gods key)
	{
		return unlocks[gods[key].Group - 1];
	}

	public void ChooseGod(Gods key)
	{
		if (IsChosen(key))
		{
			chosen.Remove(key);
			ChangeChosenAmount();
			return;
		}
		if (chosen.Count >= MaxChooses.ValueInt)
		{
			chosen.RemoveAt(0);
		}
		if (IsAvailable(key))
		{
			chosen.Add(key);
		}
		ChangeChosenAmount();
	}

	public void WarpTemple()
	{
		int chosenAmount = GetChosenAmount();
		if (chosenAmount <= temple.Buff.Amount)
		{
			temple.SpendBuff(chosenAmount);
			GiveExp(Temple.WARP_TIME, ignoreBuff: true);
		}
	}

	private void GiveExp(float seconds, bool ignoreBuff = false)
	{
		BigNumber bigNumber = 0.0;
		bigNumber += addExpToGods(chosen.FindAll((Gods x) => x < Gods.minorSpells), seconds, ignoreBuff) / seconds;
		bigNumber += addExpToGods(chosen.FindAll((Gods x) => x >= Gods.minorSpells), seconds, ignoreBuff) / seconds;
		praying.AddExp(bigNumber.Pow(0.5) * seconds);
	}

	private BigNumber addExpToGods(List<Gods> gods, float seconds, bool ignoreBuff = false)
	{
		BigNumber expIncome = GetExpIncome();
		BigNumber result = 0.0;
		float num = 0f;
		foreach (Gods god2 in gods)
		{
			God god = GetGod(god2);
			num = seconds;
			BigNumber bigNumber;
			if (!ignoreBuff && god.OfferingBuff > 0f && god.OfferingBuff < seconds)
			{
				float offeringBuff = god.OfferingBuff;
				bigNumber = god.AddExp(expIncome * offeringBuff);
				result += bigNumber;
				god.TickBuff(offeringBuff);
				num -= offeringBuff;
			}
			bigNumber = god.AddExp(expIncome * num, ignoreBuff);
			result += bigNumber;
			if (!ignoreBuff)
			{
				god.TickBuff(num);
			}
		}
		return result;
	}

	private void AddGod(God god)
	{
		gods.Add(god.Key, god);
	}

	private void OnParagonChange()
	{
		if (unlocks[0] != GameManager.Instance.Paragon.PantheonIsAvailable || unlocks[1] != GameManager.Instance.Paragon.Pantheon2IsAvailable || unlocks[2] != GameManager.Instance.Paragon.Pantheon3IsAvailable || unlocks[3] != GameManager.Instance.Paragon.Pantheon4IsAvailable || unlocks[4] != GameManager.Instance.Paragon.PantheonMinorsIsAvailable || skillSecond != GameManager.Instance.Paragon.Gods2SkillIsAvailable || GameManager.Instance.Paragon.GildingPantheonIsAvailable)
		{
			Reactivate();
		}
	}

	private void OnChangeMaxChoose()
	{
		if (!isLoading && chosen.Count >= MaxChooses.ValueInt)
		{
			List<Gods> list = new List<Gods>();
			for (int i = 0; i < MaxChooses.ValueInt; i++)
			{
				list.Add(chosen[chosen.Count - i - 1]);
			}
			chosen = list;
		}
		ChangeChosenAmount();
	}

	public int GetNextAcolyteStep()
	{
		for (int i = 0; i < maxChooseSteps.Count; i++)
		{
			if (MaxLevelRealm.ValueInt < maxChooseSteps[i])
			{
				return maxChooseSteps[i];
			}
		}
		return 0;
	}

	private void RecalculateMaxChoose()
	{
		int num = 0;
		for (int i = 0; i < maxChooseSteps.Count; i++)
		{
			if (MaxLevelRealm.ValueInt >= maxChooseSteps[i])
			{
				num++;
			}
		}
		maxChooseEffect.add = num;
		maxChooseEffect.Update();
		OnChangeMaxChoose();
	}
}
