using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class AttributeManager : MonoBehaviour
{
	public class BonusToAttribute
	{
		public Attributes Key;

		public SimpleEffect effect;

		public BonusToAttribute(CharacterAttribute attribute)
		{
			Key = attribute.Key;
			effect = new SimpleEffect(attribute.Level);
		}
	}

	public bool IsEnabled = true;

	public VariableInt Total;

	public VariableInt Free;

	public VariableInt Searched;

	public VariableInt BonusPoints;

	public VariableFloat SearchRate;

	public VariableFloat AttributePower;

	public VariableComplex VersatilityPower;

	public VariableFloat SavePart;

	public int Resets;

	public AttributePanel Panel;

	public Button AddButton;

	private float based = 1.25f;

	public VariableInt MaxValue;

	public VariableBignumber BonusToAll;

	public VariableBignumber BonusToAllMem;

	private Vector3 text_position;

	private Color color;

	private List<BonusToAttribute> effectsToAll;

	private Dictionary<Attributes, BonusToAttribute> effectsToAllApplied;

	public Action OnInvest;

	private bool markToUpdateBonusToAll;

	public BigNumber need { get; private set; }

	public double progress { get; private set; }

	public int Int
	{
		get
		{
			return Panel.intelligence.parameter.TrueLevel.ValueInt;
		}
		set
		{
			Panel.intelligence.parameter.TrueLevel.SetValue(value);
			Panel.intelligence.parameter.Level.SetValue(value);
		}
	}

	public int Ins
	{
		get
		{
			return Panel.insight.parameter.TrueLevel.ValueInt;
		}
		set
		{
			Panel.insight.parameter.TrueLevel.SetValue(value);
			Panel.insight.parameter.Level.SetValue(value);
		}
	}

	public int Scr
	{
		get
		{
			return Panel.spellcraft.parameter.TrueLevel.ValueInt;
		}
		set
		{
			Panel.spellcraft.parameter.TrueLevel.SetValue(value);
			Panel.spellcraft.parameter.Level.SetValue(value);
		}
	}

	public int Wis
	{
		get
		{
			return Panel.wisdom.parameter.TrueLevel.ValueInt;
		}
		set
		{
			Panel.wisdom.parameter.TrueLevel.SetValue(value);
			Panel.wisdom.parameter.Level.SetValue(value);
		}
	}

	public int Dom
	{
		get
		{
			return Panel.dominance.parameter.TrueLevel.ValueInt;
		}
		set
		{
			Panel.dominance.parameter.TrueLevel.SetValue(value);
			Panel.dominance.parameter.Level.SetValue(value);
		}
	}

	public int Pat
	{
		get
		{
			return Panel.patience.parameter.TrueLevel.ValueInt;
		}
		set
		{
			Panel.patience.parameter.TrueLevel.SetValue(value);
			Panel.patience.parameter.Level.SetValue(value);
		}
	}

	public int Mas
	{
		get
		{
			return Panel.mastery.parameter.TrueLevel.ValueInt;
		}
		set
		{
			Panel.mastery.parameter.TrueLevel.SetValue(value);
			Panel.mastery.parameter.Level.SetValue(value);
		}
	}

	public int Emp
	{
		get
		{
			return Panel.empathy.parameter.TrueLevel.ValueInt;
		}
		set
		{
			Panel.empathy.parameter.TrueLevel.SetValue(value);
			Panel.empathy.parameter.Level.SetValue(value);
		}
	}

	public int Ver
	{
		get
		{
			return Panel.dump.parameter.TrueLevel.ValueInt;
		}
		set
		{
			Panel.dump.parameter.TrueLevel.SetValue(value);
			Panel.dump.parameter.Level.SetValue(value);
		}
	}

	public void Init()
	{
		Total = new VariableInt(0);
		Free = new VariableInt(0);
		Searched = new VariableInt(0);
		SearchRate = new VariableFloat(1f);
		BonusToAll = new VariableBignumber(0.0);
		BonusToAllMem = new VariableBignumber(0.0);
		BonusPoints = new VariableInt(0);
		MaxValue = new VariableInt(100);
		AttributePower = new VariableFloat(1f);
		VersatilityPower = new VariableComplex(0.0);
		SavePart = new VariableFloat(1f);
		Resets = 1;
		need = 100.0;
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + ResourceChar.Total, Total);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + ResourceChar.Free, Free);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + ResourceChar.Seached, Searched);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + ResourceChar.Rate, SearchRate);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + ResourceChar.ToAll, BonusToAll);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + ResourceChar.ToAllMem, BonusToAllMem);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + ResourceChar.Cap, MaxValue);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + ResourceChar.AttributePower, AttributePower);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + ResourceChar.VersatilityPower, VersatilityPower);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + ".SavePart", SavePart);
		CreateDump(Panel.dump);
		Create(Attributes.Intelligence, Panel.intelligence);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + Attributes.Intelligence, Panel.intelligence.parameter.Level);
		Create(Attributes.Insight, Panel.insight);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + Attributes.Insight, Panel.insight.parameter.Level);
		Create(Attributes.Spellcraft, Panel.spellcraft);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + Attributes.Spellcraft, Panel.spellcraft.parameter.Level);
		Create(Attributes.Wisdom, Panel.wisdom);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + Attributes.Wisdom, Panel.wisdom.parameter.Level);
		Create(Attributes.Dominance, Panel.dominance);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + Attributes.Dominance, Panel.dominance.parameter.Level);
		Create(Attributes.Patience, Panel.patience);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + Attributes.Patience, Panel.patience.parameter.Level);
		Create(Attributes.Mastery, Panel.mastery);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + Attributes.Mastery, Panel.mastery.parameter.Level);
		Create(Attributes.Empathy, Panel.empathy);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + Attributes.Empathy, Panel.empathy.parameter.Level);
		GameContext.ContextAddResource(ResourceType.Char.ToString() + "." + Attributes.Versatility, Panel.dump.parameter.Level);
		Searched.OnChange = null;
		Reborn.Souls.OnChange = null;
		GameManager.Instance.CurrentHero.Level.OnChange = null;
		InitBonusToAll();
		VariableInt searched = Searched;
		searched.OnChange = (Action)Delegate.Combine(searched.OnChange, new Action(RecalculateNeed));
		VariableBignumber souls = Reborn.Souls;
		souls.OnChange = (Action)Delegate.Combine(souls.OnChange, new Action(RecalculateNeed));
		VariableInt level = GameManager.Instance.CurrentHero.Level;
		level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(RecalculateNeed));
		PostSave();
		text_position = new Vector3(0f, Camera.main.ScreenToWorldPoint(GameManager.Instance.CurrentHero.transform.position).y - 1.4f, 0f);
		text_position.z = 0f;
		color = new Color(1f, 1f, 0.2f);
		GameManager instance = GameManager.Instance;
		instance.OnExile = (Action)Delegate.Combine(instance.OnExile, new Action(CheckButon));
		RealmManager realm = GameManager.Instance.Realm;
		realm.OnRealm = (Action)Delegate.Combine(realm.OnRealm, new Action(CheckButon));
		Panel.Init();
	}

	public void Restart()
	{
		Paragons paragon = GameManager.Instance.Paragon;
		paragon.onChangeLVL = (Action)Delegate.Remove(paragon.onChangeLVL, new Action(MarkToUpdateBonusToAll));
		VariableBignumber bonusToAllMem = BonusToAllMem;
		bonusToAllMem.OnChange = (Action)Delegate.Remove(bonusToAllMem.OnChange, new Action(MarkToUpdateBonusToAll));
		VariableBignumber bonusToAll = BonusToAll;
		bonusToAll.OnChange = (Action)Delegate.Remove(bonusToAll.OnChange, new Action(MarkToUpdateBonusToAll));
		VariableFloat attributePower = AttributePower;
		attributePower.OnChange = (Action)Delegate.Remove(attributePower.OnChange, new Action(UpdateAllAttributes));
		VariableComplex versatilityPower = VersatilityPower;
		versatilityPower.OnChange = (Action)Delegate.Remove(versatilityPower.OnChange, new Action(UpdateVersatility));
		RemoveBonusToAll();
		foreach (AttributeBar allAttribute in Panel.GetAllAttributes())
		{
			allAttribute.OffPerks();
		}
		Panel.dump.Off();
	}

	public void PostReset()
	{
		ApplyBonusToAll();
		foreach (AttributeBar allAttribute in Panel.GetAllAttributes())
		{
			allAttribute.OnPerk();
		}
		Panel.dump.On();
		Paragons paragon = GameManager.Instance.Paragon;
		paragon.onChangeLVL = (Action)Delegate.Combine(paragon.onChangeLVL, new Action(MarkToUpdateBonusToAll));
		VariableBignumber bonusToAllMem = BonusToAllMem;
		bonusToAllMem.OnChange = (Action)Delegate.Combine(bonusToAllMem.OnChange, new Action(MarkToUpdateBonusToAll));
		VariableBignumber bonusToAll = BonusToAll;
		bonusToAll.OnChange = (Action)Delegate.Combine(bonusToAll.OnChange, new Action(MarkToUpdateBonusToAll));
		VariableFloat attributePower = AttributePower;
		attributePower.OnChange = (Action)Delegate.Combine(attributePower.OnChange, new Action(UpdateAllAttributes));
		VariableComplex versatilityPower = VersatilityPower;
		versatilityPower.OnChange = (Action)Delegate.Combine(versatilityPower.OnChange, new Action(UpdateVersatility));
		RecalculateNeed();
		MarkToUpdateBonusToAll();
	}

	public void RealmReset()
	{
		PreSave();
		Restart();
		ResetAttributesAll();
		Free.SetValue(0);
		Searched.SetValue(0);
		progress = 0.0;
	}

	public void PostRealmReset()
	{
		PostSave();
		CheckButon();
		MarkToUpdateBonusToAll();
	}

	private void Update()
	{
		EarnCalculations();
	}

	private void LateUpdate()
	{
		if (markToUpdateBonusToAll && IsAvailabeToUpdate())
		{
			UpdateBonusToAll();
			markToUpdateBonusToAll = false;
		}
	}

	private void Create(Attributes key, AttributeBar bar)
	{
		List<AttributeFormat> list = key switch
		{
			Attributes.Intelligence => GlobalData.Intelligence, 
			Attributes.Insight => GlobalData.Insight, 
			Attributes.Spellcraft => GlobalData.Spellcraft, 
			Attributes.Wisdom => GlobalData.Wisdom, 
			Attributes.Dominance => GlobalData.Dominance, 
			Attributes.Patience => GlobalData.Patience, 
			Attributes.Mastery => GlobalData.Mastery, 
			Attributes.Empathy => GlobalData.Empathy, 
			_ => GlobalData.Intelligence, 
		};
		CharacterAttribute characterAttribute = new CharacterAttribute();
		characterAttribute.Key = key;
		characterAttribute.Level = new VariableInt(0);
		characterAttribute.TrueLevel = new VariableInt(0);
		characterAttribute.perks = new List<CharacterAttribute.Perk>();
		foreach (AttributeFormat item in list)
		{
			int num = int.Parse(item.Level);
			if (num == 0)
			{
				characterAttribute.Description = item.Description;
				characterAttribute.main_effect = new CharacterEffect(float.Parse(item.m, CultureInfo.InvariantCulture), GameContext.GetResource(item.Target), characterAttribute.Level);
			}
			else if (!string.IsNullOrEmpty(item.Target))
			{
				SimpleEffect simpleEffect = new SimpleEffect();
				if (!string.IsNullOrEmpty(item.e))
				{
					simpleEffect.effect = GameContext.GetEffect(item.e);
				}
				simpleEffect.target = GameContext.GetResource(item.Target);
				simpleEffect.add = (string.IsNullOrEmpty(item.a) ? 0f : float.Parse(item.a, CultureInfo.InvariantCulture));
				simpleEffect.mult = (string.IsNullOrEmpty(item.m) ? 1f : float.Parse(item.m, CultureInfo.InvariantCulture));
				if (!string.IsNullOrEmpty(item.w))
				{
					simpleEffect.parameter = GameContext.GetResource(item.w);
				}
				characterAttribute.perks.Add(new CharacterAttribute.Perk(num, simpleEffect, item.Description));
			}
		}
		bar.parameter = characterAttribute;
		bar.Init();
		bar.parameter.main_effect.Start();
		CheckButon();
	}

	private void CreateDump(AttributeBarDump dump)
	{
		CharacterAttribute characterAttribute = new CharacterAttribute();
		characterAttribute.Key = Attributes.Versatility;
		characterAttribute.Level = new VariableInt(0);
		characterAttribute.TrueLevel = new VariableInt(0);
		characterAttribute.Description = "[IncreaseProfits]{#1}";
		characterAttribute.main_effect = new CharacterEffect(1.018f, GameManager.Instance.Profit, characterAttribute.Level);
		dump.parameter = characterAttribute;
		dump.Init();
		dump.parameter.main_effect.Start();
	}

	private void EarnCalculations()
	{
		if (IsAvailable())
		{
			BigNumber bigNumber = need * progress + Time.unscaledDeltaTime;
			int num = 0;
			while (bigNumber >= need)
			{
				bigNumber -= (BigNumber)need.ToDouble();
				earn1();
				num++;
			}
			progress = (bigNumber / need).ToDouble();
			if (num > 0)
			{
				Panel.EnableButtons();
				GameManager.Instance.AnimatedText.TextUp("+" + num + "Attribute".Translate(), text_position, color, -1.2f);
			}
		}
	}

	public bool IsAvailabeToUpdate()
	{
		return GameManager.Instance.Paragon.AttributesIsAvailable;
	}

	public bool IsAvailable()
	{
		if (GameManager.Instance.Paragon.AttributesIsAvailable && GameManager.Instance.ChallengeManager.StatsIsOn() && IsEnabled && Time.deltaTime != 0f)
		{
			return Time.timeScale != 0f;
		}
		return false;
	}

	public void RecalculateNeed()
	{
		if (IsAvailable())
		{
			int valueInt = GameManager.Instance.CurrentHero.Level.ValueInt;
			if (valueInt > Statistic.HeroMaxLevelAllTime.ValueInt)
			{
				valueInt = Statistic.HeroMaxLevelAllTime.ValueInt;
			}
			float num = (float)Searched.ValueInt - (float)(Reborn.Souls.Value + 1.0).Log10() * 1.75f - (float)valueInt * 0.2f;
			if (num > 50f)
			{
				num = 50f;
			}
			need = new BigNumber(based).Pow(num) * 60.0;
			if (need < 10.0)
			{
				need = 10.0;
			}
			need /= (BigNumber)SearchRate.ValueFloat;
			if (need < 1.0)
			{
				need = 1.0;
			}
		}
	}

	private void earn1()
	{
		Searched.Change(1);
		Free.Change(1);
		CheckButon();
		int num = TotalInRealm();
		if (num > Total.ValueInt)
		{
			Total.SetValue(num);
		}
	}

	public int TotalInRealm()
	{
		return Searched.ValueInt + BonusPoints.ValueInt;
	}

	public void ResetAttributes()
	{
		ResetAttributesExile();
		Panel.CheckEnable();
	}

	public void ResetAttributesExile()
	{
		ResetAttributesAll();
		Panel.intelligence.UpdateButton();
		CheckButon();
	}

	public void HardReset()
	{
		ResetAttributesAll();
		Total.SetValue(0);
		Free.SetValue(0);
		Searched.SetValue(0);
		CheckButon();
	}

	public void ResetAttributesAll(bool update = false)
	{
		foreach (AttributeBar allAttribute in Panel.GetAllAttributes())
		{
			allAttribute.parameter.Reset();
		}
		Panel.dump.parameter.Reset();
		if (update)
		{
			UpdateBonusToAll();
		}
	}

	public void LoadProgress(double _progress)
	{
		if (double.IsNaN(_progress))
		{
			_progress = 0.0;
		}
		progress = _progress;
	}

	public int Offline(BigNumber sec)
	{
		if (!GameManager.Instance.Paragon.AttributesIsAvailable)
		{
			return 0;
		}
		return Offline(sec, progress);
	}

	public int Offline(BigNumber sec, double _progress)
	{
		if (!IsAvailable())
		{
			return 0;
		}
		if (double.IsNaN(_progress))
		{
			_progress = 0.0;
		}
		int free = GetFree();
		progress = _progress;
		if (GameManager.Instance.Paragon.AttributesIsAvailable && GameManager.Instance.ChallengeManager.StatsIsOn())
		{
			BigNumber bigNumber = sec + need * progress;
			while (bigNumber >= need)
			{
				bigNumber -= (BigNumber)need.ToDouble();
				earn1();
			}
			progress = (bigNumber / need).ToDouble();
		}
		CheckButon();
		return GetFree() - free;
	}

	public void PreSave()
	{
		VariableInt bonusPoints = BonusPoints;
		bonusPoints.OnChangeInt = (Action<int>)Delegate.Remove(bonusPoints.OnChangeInt, new Action<int>(ApplyBonusPointsChange));
	}

	public void PostSave()
	{
		ApplyBonusPointsChange(BonusPoints.ValueInt);
		VariableInt bonusPoints = BonusPoints;
		bonusPoints.OnChangeInt = (Action<int>)Delegate.Combine(bonusPoints.OnChangeInt, new Action<int>(ApplyBonusPointsChange));
	}

	public VariableInt FindAttribute(Attributes key)
	{
		VariableInt result = null;
		switch (key)
		{
		case Attributes.Intelligence:
			result = Panel.intelligence.parameter.Level;
			break;
		case Attributes.Insight:
			result = Panel.insight.parameter.Level;
			break;
		case Attributes.Spellcraft:
			result = Panel.spellcraft.parameter.Level;
			break;
		case Attributes.Wisdom:
			result = Panel.wisdom.parameter.Level;
			break;
		case Attributes.Dominance:
			result = Panel.dominance.parameter.Level;
			break;
		case Attributes.Patience:
			result = Panel.patience.parameter.Level;
			break;
		case Attributes.Mastery:
			result = Panel.mastery.parameter.Level;
			break;
		case Attributes.Empathy:
			result = Panel.empathy.parameter.Level;
			break;
		}
		return result;
	}

	public void CheckButon()
	{
		if (!GameManager.Instance.Paragon.AttributesIsAvailable)
		{
			AddButton.transform.parent.gameObject.SetActive(value: false);
			return;
		}
		AddButton.transform.parent.gameObject.SetActive(GetFree() > 0);
		AddButton.targetGraphic.color = Color.white;
	}

	private void UpdateVersatility()
	{
		Panel.dump.parameter.main_effect.Update();
	}

	private void UpdateAllAttributes()
	{
		foreach (AttributeBar allAttribute in Panel.GetAllAttributes())
		{
			allAttribute.parameter.main_effect.Update();
		}
	}

	private void InitBonusToAll()
	{
		effectsToAll = new List<BonusToAttribute>
		{
			new BonusToAttribute(Panel.intelligence.parameter),
			new BonusToAttribute(Panel.insight.parameter),
			new BonusToAttribute(Panel.spellcraft.parameter),
			new BonusToAttribute(Panel.wisdom.parameter),
			new BonusToAttribute(Panel.dominance.parameter),
			new BonusToAttribute(Panel.patience.parameter),
			new BonusToAttribute(Panel.mastery.parameter),
			new BonusToAttribute(Panel.empathy.parameter)
		};
	}

	public void ApplyBonusToAll()
	{
		int num = BonusToAll.Value.ToInt();
		if (num >= MaxValue.ValueInt)
		{
			num = MaxValue.ValueInt;
		}
		num += BonusToAllMem.Value.ToInt();
		effectsToAllApplied = new Dictionary<Attributes, BonusToAttribute>();
		foreach (BonusToAttribute item in effectsToAll)
		{
			if ((item.Key != Attributes.Mastery || GameManager.Instance.Paragon.MasteryIsAvailable) && (item.Key != Attributes.Empathy || GameManager.Instance.Paragon.EmpathyIsAvailable))
			{
				item.effect.add = num;
				item.effect.Apply();
				effectsToAllApplied.Add(item.Key, item);
			}
		}
	}

	private bool CheckAvailableAttribute(BonusToAttribute bonus, bool isAvailable)
	{
		Attributes key = bonus.Key;
		if (effectsToAllApplied.ContainsKey(key))
		{
			if (!isAvailable)
			{
				effectsToAllApplied[key].effect.Delete();
				effectsToAllApplied.Remove(key);
			}
		}
		else if (GameManager.Instance.Paragon.EmpathyIsAvailable)
		{
			effectsToAllApplied.Add(key, bonus);
		}
		return isAvailable;
	}

	private void MarkToUpdateBonusToAll()
	{
		markToUpdateBonusToAll = true;
	}

	public void UpdateBonusToAll()
	{
		int num = BonusToAll.Value.ToInt();
		if (num >= MaxValue.ValueInt)
		{
			num = MaxValue.ValueInt;
		}
		num += BonusToAllMem.Value.ToInt();
		foreach (BonusToAttribute item in effectsToAll)
		{
			if ((item.Key != Attributes.Mastery || CheckAvailableAttribute(item, GameManager.Instance.Paragon.MasteryIsAvailable)) && (item.Key != Attributes.Empathy || CheckAvailableAttribute(item, GameManager.Instance.Paragon.EmpathyIsAvailable)))
			{
				item.effect.add = num;
				if (item.effect.IsActive)
				{
					item.effect.Update();
				}
				else
				{
					item.effect.Apply();
				}
			}
		}
	}

	public int GetFree()
	{
		return Free.ValueInt + BonusPoints.ValueInt;
	}

	public int SaveRetain()
	{
		if (SavePart.ValueFloat <= 1f)
		{
			return 0;
		}
		return Mathf.FloorToInt((float)Searched.ValueInt * (SavePart.ValueFloat - 1f));
	}

	public void LoadRetain(int points)
	{
		Free.Change(points);
		Searched.Change(points);
	}

	private void RemoveBonusToAll()
	{
		if (effectsToAllApplied == null)
		{
			return;
		}
		foreach (KeyValuePair<Attributes, BonusToAttribute> item in effectsToAllApplied)
		{
			item.Value.effect.Delete();
		}
		effectsToAllApplied = new Dictionary<Attributes, BonusToAttribute>();
	}

	private void ApplyBonusPointsChange(int change)
	{
		int num = Searched.ValueInt + BonusPoints.ValueInt;
		if (num > Total.ValueInt)
		{
			Total.SetValue(num);
		}
	}
}
