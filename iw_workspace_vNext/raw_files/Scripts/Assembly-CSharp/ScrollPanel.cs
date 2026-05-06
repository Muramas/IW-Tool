using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollPanel : MonoBehaviour
{
	public Color CarelessAutocastColor;

	public Color CarefulAutocastColor;

	public List<Scroll> Scrolls;

	[SerializeField]
	private Scroll Seventh;

	public SpellChoosePanel ChoosePanel;

	public SpellShards ShardsPool;

	public float Period;

	private float timer;

	private float drop_chance = 0.2f;

	public VariableInt ScrollCount;

	public VariableInt MaxCharge;

	public VariableComplex ShardsPassive;

	public VariableComplex ShardsPerClick;

	public VariableComplex EvocationEfficiency;

	public VariableComplex EvocationDurationReduction;

	public VariableComplex IncantationEfficiency;

	public VariableComplex SummoningEfficiency;

	public VariableComplex SummoningDurationReduction;

	public VariableComplex IncantationDuration;

	public VariableComplex IncantationDurationReduction;

	public VariableComplex SpellShardsCostReduction;

	public VariableComplex SpellChargingCostReduction;

	public VariableComplex SpellSubcostReduction;

	public VariableComplex SpellChargingSpeed;

	public VariableBignumber AccumCastCount;

	public VariableBignumber EvoCastCount;

	public VariableComplex PersistentGain;

	public VariableComplex PersistentMult;

	public VariableComplex PersistentActiveMult;

	public VariableComplex AccumMult;

	public VariableComplex AugmentMult;

	public VariableComplex CastRate;

	public VariableFloat CastRateMult;

	public VariableComplex AccumulatedCasts;

	public VariableFloat PersistentSave;

	public List<Scroll> unfilled_scrolls;

	public VariableInt AutoCastCount;

	public List<Scroll> auto_cast_scrolls;

	public ShardsPoolVisual PoolVisual;

	public AutocastModes AutocastModes;

	private float shards_pool_timer;

	private float shards_pool_tick = 0.1f;

	private BigNumber prevAccumValue = 0.0;

	public Counter castsInSec;

	public Action OnOpen;

	public Action<Spell> OnPreCast;

	public Action<Spell> OnCast;

	public Action<Spell, double> OnCastAmount;

	public Action<Spell, double> OnRealCastAmount;

	public Action<Spell, double> OnCastOffline;

	public Action<Spell> OnCastEnd;

	public Action<Spell> OnPostCast;

	public Action ChangeSpells;

	public BigNumber shardsInSec = 0.0;

	public double evoInSec;

	[SerializeField]
	private HorizontalLayoutGroup layout;

	[SerializeField]
	private Transform setTextPosition;

	public Func<string, string> OnShowName;

	public Action OnLoadLoadout;

	public void Init()
	{
		ShardsPool = new SpellShards();
		ScrollCount = new VariableInt(0);
		VariableInt scrollCount = ScrollCount;
		scrollCount.OnChange = (Action)Delegate.Combine(scrollCount.OnChange, new Action(change_scroll_count));
		ShardsPassive = new VariableComplex(3.0);
		ShardsPerClick = new VariableComplex(0.5f / drop_chance);
		PersistentMult = new VariableComplex(1.0);
		PersistentGain = new VariableComplex(1.0);
		PersistentActiveMult = new VariableComplex(1.0);
		AccumMult = new VariableComplex(1.0);
		PersistentSave = new VariableFloat(1f);
		AugmentMult = new VariableComplex(1.0);
		CastRate = new VariableComplex(60.0);
		CastRateMult = new VariableFloat(1f);
		MaxCharge = new VariableInt(1);
		VariableInt maxCharge = MaxCharge;
		maxCharge.OnChange = (Action)Delegate.Combine(maxCharge.OnChange, new Action(on_change_max_charge));
		EvocationEfficiency = new VariableComplex(1.0);
		EvocationDurationReduction = new VariableComplex(1.0);
		IncantationEfficiency = new VariableComplex(1.0);
		IncantationDuration = new VariableComplex(1.0);
		IncantationDurationReduction = new VariableComplex(1.0);
		SummoningEfficiency = new VariableComplex(1.0);
		SummoningDurationReduction = new VariableComplex(1.0);
		AccumCastCount = new VariableBignumber(0.0);
		EvoCastCount = new VariableBignumber(0.0);
		SpellShardsCostReduction = new VariableComplex(1.0);
		VariableComplex spellShardsCostReduction = SpellShardsCostReduction;
		spellShardsCostReduction.OnChange = (Action)Delegate.Combine(spellShardsCostReduction.OnChange, new Action(CheckFilledList));
		SpellChargingCostReduction = new VariableComplex(1.0);
		SpellSubcostReduction = new VariableComplex(1.0);
		SpellChargingSpeed = new VariableComplex(1.0);
		AccumulatedCasts = new VariableComplex(0.0);
		VariableComplex accumulatedCasts = AccumulatedCasts;
		accumulatedCasts.OnChange = (Action)Delegate.Combine(accumulatedCasts.OnChange, new Action(OnChangeAccum));
		AutoCastCount = new VariableInt(0);
		castsInSec = new Counter();
		GameContext.AddSpellContext();
		auto_cast_scrolls = new List<Scroll>();
		unfilled_scrolls = new List<Scroll>();
		for (int i = 0; i < Scrolls.Count; i++)
		{
			if (Scrolls[i].spell != null)
			{
				Debug.Log(Scrolls[i].spell.NameKey);
				if (Scrolls[i].spell.ShardsBuilding)
				{
					unfilled_scrolls.Add(Scrolls[i]);
				}
			}
		}
		VariableInt scrollCount2 = ScrollCount;
		scrollCount2.OnChange = (Action)Delegate.Combine(scrollCount2.OnChange, new Action(pool_bar));
		VariableLong timeSession = Statistic.TimeSession;
		timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(ApplyChangeInSecond));
	}

	public void RecheckSpells()
	{
		for (int i = 0; i < Scrolls.Count; i++)
		{
			if (Scrolls[i].spell != null && !Scrolls[i].spell.AvailableToUse)
			{
				Scrolls[i].Clear();
			}
		}
	}

	private void ApplyChangeInSecond()
	{
		if (shardsInSec > 0.0)
		{
			Statistic.ShardsSession.Change(shardsInSec);
			Statistic.Change(Statistic.ShardsTotal, shardsInSec);
			Statistic.Change(Statistic.ShardsRealm, shardsInSec);
			shardsInSec = 0.0;
		}
	}

	private void OnChangeAccum()
	{
		BigNumber bigNumber = AccumulatedCasts.Value - prevAccumValue;
		foreach (Spell accumalatedSpell in GameManager.Instance.SpellBook.AccumalatedSpellList)
		{
			accumalatedSpell.Use.Change(bigNumber, 1.0);
		}
		prevAccumValue += bigNumber;
	}

	public void SpellsReset(bool full = false)
	{
		foreach (Spell spell in GameManager.Instance.SpellBook.SpellList)
		{
			spell.Restart(full);
		}
		foreach (Scroll scroll in Scrolls)
		{
			scroll.Restart();
		}
		ShardsPool.Average.Reset();
	}

	public void ChangeRealm()
	{
		foreach (Scroll scroll in Scrolls)
		{
			scroll.Restart();
		}
		foreach (Spell spell in GameManager.Instance.SpellBook.SpellList)
		{
			spell.ChangeRealm();
		}
	}

	public Dictionary<int, BigNumber> SaveRetain()
	{
		if (PersistentSave.ValueFloat <= 1f)
		{
			return null;
		}
		Dictionary<int, BigNumber> dictionary = new Dictionary<int, BigNumber>();
		List<Spell> list = GameManager.Instance.SpellBook.SpellList.FindAll((Spell x) => !x.ResetUses);
		BigNumber bigNumber = 0.0;
		foreach (Spell item in list)
		{
			bigNumber = item.Use.Value * (GameManager.Instance.Scrolls.PersistentSave.ValueFloat - 1f);
			if (bigNumber >= 1.0)
			{
				dictionary.Add((int)item.NameKey, bigNumber);
			}
		}
		if (dictionary.Count == 0)
		{
			return null;
		}
		return dictionary;
	}

	public void LoadRetain(Dictionary<int, BigNumber> casts)
	{
		if (casts == null)
		{
			return;
		}
		foreach (KeyValuePair<int, BigNumber> cast in casts)
		{
			GameManager.Instance.SpellBook.GetSpell((Spells)cast.Key).Use.SetValue(cast.Value);
		}
	}

	public void Restart()
	{
		ScrollCount.SetValue(0);
		AutoCastCount.SetValue(0);
		shards_pool_timer = 0f;
		EvocationEfficiency.Reset(1.0);
		EvocationDurationReduction.Reset(1.0);
		IncantationEfficiency.Reset(1.0);
		IncantationDuration.Reset(1.0);
		SummoningEfficiency.Reset(1.0);
		SummoningDurationReduction.Reset(1.0);
		AccumCastCount.Reset(0.0);
		EvoCastCount.SetValue(0.0);
		Seventh.Clear();
	}

	private void Update()
	{
		periodic_add_progress();
		if (AutoCastCount.ValueInt > 0)
		{
			if (!AutocastModes.gameObject.activeInHierarchy)
			{
				AutocastModes.gameObject.SetActive(value: true);
			}
		}
		else if (AutocastModes.gameObject.activeInHierarchy)
		{
			AutocastModes.gameObject.SetActive(value: false);
		}
	}

	private void LateUpdate()
	{
		CheckSetsHotkey();
		for (int i = 0; i < ScrollCount.ValueInt; i++)
		{
			Scrolls[i].UpdateScroll();
		}
		castsInSec.Update();
	}

	private void pool_bar()
	{
		if (ScrollCount.ValueInt > 0)
		{
			if (!PoolVisual.gameObject.activeInHierarchy)
			{
				PoolVisual.gameObject.SetActive(value: true);
			}
		}
		else if (PoolVisual.gameObject.activeInHierarchy)
		{
			PoolVisual.gameObject.SetActive(value: false);
			PoolVisual.HideTip();
		}
	}

	public void StopAll(bool countCast = false)
	{
		foreach (Scroll scroll in Scrolls)
		{
			if (countCast && scroll.spell != null && scroll.active)
			{
				scroll.IncreaseCasts();
			}
			scroll.StopAction();
		}
	}

	public void StopOnWarp()
	{
		foreach (Scroll scroll in Scrolls)
		{
			if (scroll.spell != null && scroll.spell.NameKey != Spells.GenerateParadox)
			{
				if (scroll.active)
				{
					scroll.IncreaseCasts();
				}
				scroll.StopAction();
			}
		}
	}

	public void AddToAutoCast(Scroll s)
	{
		if (AutoCastCount.ValueInt > 0)
		{
			if (AutoCastCount.ValueInt == auto_cast_scrolls.Count)
			{
				RemoveAutoCast(auto_cast_scrolls[0]);
			}
			auto_cast_scrolls.Add(s);
			s.AutoCast = true;
		}
	}

	public void RemoveAutoCast(Scroll s)
	{
		if (auto_cast_scrolls.Count > 0)
		{
			auto_cast_scrolls.Remove(s);
			s.AutoCast = false;
		}
		if (s.spell != null)
		{
			s.spell.ResetCounters();
		}
	}

	public void ClearAutocast()
	{
		foreach (Scroll scroll in Scrolls)
		{
			RemoveAutoCast(scroll);
		}
		auto_cast_scrolls = new List<Scroll>();
	}

	private void change_scroll_count()
	{
		if (ScrollCount.ValueInt < 0)
		{
			Debug.Log("< 0 scrolls");
			return;
		}
		if (ScrollCount.ValueInt > Scrolls.Count)
		{
			Debug.Log("error max scrolls");
			return;
		}
		for (int i = 0; i < ScrollCount.ValueInt; i++)
		{
			Scrolls[i].gameObject.SetActive(value: true);
		}
		for (int j = ScrollCount.ValueInt; j < Scrolls.Count; j++)
		{
			if (Scrolls[j].gameObject.activeInHierarchy)
			{
				Scrolls[j].Clear();
				Scrolls[j].gameObject.SetActive(value: false);
			}
		}
	}

	private void periodic_add_progress()
	{
		if (shards_pool_timer >= shards_pool_tick)
		{
			shards_pool_timer %= shards_pool_tick;
			FillScrolls(ShardsPool.GetShards());
		}
		shards_pool_timer += Time.unscaledDeltaTime;
		if (Period != 0f)
		{
			timer += Time.deltaTime;
			if (timer > Period)
			{
				int num = (int)(timer / Period);
				timer %= Period;
				ShardsPool.Add(ShardsPassive.Value * num);
			}
		}
	}

	public float GetProgressFromClick(float parameter = 1f)
	{
		float num = UnityEngine.Random.Range(0f, 1f);
		BigNumber bigNumber = 0.0;
		if (num < drop_chance)
		{
			bigNumber = calculate_click_progress() * parameter;
		}
		return (bigNumber * parameter).Clamp(0.0, "1e30").ToFloat();
	}

	public void FillScroll(Spells key, double dp, bool shards = false)
	{
		Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == key).AddProgressBuild(dp, text: false, shards: false);
	}

	public void FillScrolls(BigNumber dp)
	{
		if (BigNumber.Sign(dp) < 0)
		{
			return;
		}
		if (unfilled_scrolls.Count == 0)
		{
			ShardsPool.Insert(dp);
			return;
		}
		List<Scroll> list = unfilled_scrolls.FindAll((Scroll x) => x.spell.Priority == 1);
		if (list.Count == 0)
		{
			list = unfilled_scrolls.FindAll((Scroll x) => x.spell.Priority == 0);
		}
		if (list.Count == 0)
		{
			list = unfilled_scrolls.FindAll((Scroll x) => x.spell.Priority == -1);
		}
		if (list.Count == 0)
		{
			ShardsPool.Insert(dp);
			return;
		}
		int index = UnityEngine.Random.Range(0, list.Count);
		if (dp > 0.0)
		{
			double num = list[index].GetSpellShardCapacity();
			if ((num < 0.0 && !list[index].spell.LimitedCharges) || num > dp)
			{
				num = dp.ToDouble();
			}
			list[index].AddProgressBuild(num);
			FillScrolls(dp - num);
		}
	}

	public void AddShardsRandom(double shard, List<Spells> except = null)
	{
		List<Scroll> list = GameManager.Instance.Scrolls.unfilled_scrolls;
		if (list.Count == 0)
		{
			return;
		}
		double num = 0.0;
		double[] array = new double[list.Count];
		int num2 = 1;
		List<Scroll> list2;
		do
		{
			list2 = check_unfilled(list, array);
			List<Scroll> list3 = list2.FindAll((Scroll x) => x.spell.Priority == 1);
			if (list3.Count == 0)
			{
				num2 = 0;
				list3 = list2.FindAll((Scroll x) => x.spell.Priority > -1);
			}
			if (list3.Count == 0)
			{
				num2 = -1;
				list3 = list2.FindAll((Scroll x) => x.spell.Priority == -1);
			}
			list2 = new List<Scroll>();
			for (int num3 = 0; num3 < list3.Count; num3++)
			{
				if (list3[num3].GetSpellShardCapacity() > array[num3] && (except == null || !except.Contains(list3[num3].spell.NameKey)))
				{
					list2.Add(list3[num3]);
				}
			}
			if (list2.Count != 0)
			{
				num = shard / (double)list2.Count;
			}
			foreach (Scroll item in list2)
			{
				double spellShardCapacity = item.GetSpellShardCapacity();
				if (spellShardCapacity < num && spellShardCapacity > 0.0)
				{
					shard -= spellShardCapacity;
					array[list.IndexOf(item)] += spellShardCapacity;
				}
				else
				{
					shard -= num;
					array[list.IndexOf(item)] += num;
				}
			}
		}
		while (shard > 0.0 && list2.Count > 0 && num2 != -1 && num > 1.0);
		Scroll[] array2 = list.ToArray();
		for (int num4 = 0; num4 < array.Length; num4++)
		{
			if (array[num4] > 0.0)
			{
				array2[num4].AddProgressBuild(array[num4], except != null);
			}
		}
	}

	private BigNumber calculate_click_progress()
	{
		if (ShardsPerClick == null)
		{
			Debug.Log("Null Here!");
			return 0.0;
		}
		return ShardsPerClick.Value;
	}

	public void ClearSlot(Spell spell)
	{
		if (spell == null || !spell.choice)
		{
			return;
		}
		Scroll scroll = Scrolls.Find((Scroll x) => x.spell == spell);
		if (scroll != null)
		{
			if (unfilled_scrolls.Contains(scroll))
			{
				unfilled_scrolls.Remove(scroll);
			}
			scroll.Clear();
			if (ChangeSpells != null)
			{
				ChangeSpells();
			}
		}
		else
		{
			Debug.LogError("error spell.choise value");
		}
	}

	private void on_change_max_charge()
	{
		foreach (Scroll scroll in Scrolls)
		{
			if (scroll.spell != null && scroll.spell.ShardsBuilding && !scroll.spell.IsMax && !unfilled_scrolls.Contains(scroll))
			{
				unfilled_scrolls.Add(scroll);
			}
		}
	}

	private void CheckSetsHotkey()
	{
		if (Settings.BlockInput || (!Input.GetKey(KeyCode.Z) && !Input.GetKey(KeyCode.Y) && !Input.GetKey(KeyCode.Q)))
		{
			return;
		}
		int num = 0;
		if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
		{
			num = 1;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
		{
			num = 2;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
		{
			num = 3;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
		{
			num = 4;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
		{
			num = 5;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
		{
			num = 6;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
		{
			num = 7;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
		{
			num = 8;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
		{
			num = 9;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
		{
			num = 10;
		}
		if (num == 0)
		{
			return;
		}
		if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !Input.GetKey(KeyCode.Q))
		{
			GameManager.Instance.SpellBook.SetsPanel.SaveSet(num);
			return;
		}
		GameManager.Instance.SpellBook.SetsPanel.ActivateSet(num);
		Preset<SpellPresetSlot> preset = GameManager.Instance.SpellBook.SetsPanel.GetPreset().Get(num - 1);
		if (preset != null)
		{
			string text = preset.name;
			if (string.IsNullOrEmpty(text))
			{
				text = "Set".Translate() + " " + num;
			}
			if (OnShowName != null)
			{
				text = OnShowName(text);
			}
			if (Input.GetKey(KeyCode.Q))
			{
				OnLoadLoadout?.Invoke();
			}
			GameManager.Instance.AnimatedText.TextUp(text, setTextPosition.position, Color.white, 1f, 1f, ignoreLimits: true, ignoreTimeScale: true);
		}
	}

	public void CheckFilledList()
	{
		for (int i = 0; i < Scrolls.Count; i++)
		{
			Scrolls[i].CheckInFilled();
		}
	}

	private List<Scroll> check_unfilled(List<Scroll> all, double[] prep)
	{
		List<Scroll> list = new List<Scroll>();
		for (int i = 0; i < all.Count; i++)
		{
			if (all[i].GetSpellShardCapacity() > prep[i])
			{
				list.Add(all[i]);
			}
		}
		return list;
	}

	public void Activate7th()
	{
		Scrolls.Add(Seventh);
		ScrollCount.Change(1);
		AutoCastCount.Change(1);
		if (Seventh.spell != null)
		{
			if (Seventh.spell.choice)
			{
				Seventh.Clear();
			}
			else
			{
				Seventh.spell.choice = true;
			}
		}
		layout.spacing = -4f;
	}

	public void Deactivate7th()
	{
		if (!Scrolls.Contains(Seventh))
		{
			Debug.Log("don't contains");
			return;
		}
		Seventh.StopAction();
		if (Seventh.spell != null)
		{
			Seventh.spell.choice = false;
		}
		ScrollCount.Change(-1);
		AutoCastCount.Change(-1);
		Scrolls.Remove(Seventh);
		Seventh.Clear();
		layout.spacing = 0f;
	}

	public bool SpellIsActive(Spells key)
	{
		Scroll scroll = Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == key);
		if (scroll != null)
		{
			return scroll.active;
		}
		return false;
	}
}
