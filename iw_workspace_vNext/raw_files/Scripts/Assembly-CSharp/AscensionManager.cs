using System;
using System.Collections.Generic;
using UnityEngine;

public class AscensionManager : MonoBehaviour
{
	public class SaveData
	{
		public int Key;

		public int MaxLvl;

		public List<DemonForm.SaveData> Forms;

		public PetFormData Pet;

		public float Energy;
	}

	public class PetFormData
	{
		public int key;

		public BigNumber exp;

		public ulong played;

		public BigNumber skipped;
	}

	public Dictionary<HeroesNames, DemonForm> Forms;

	public VariableInt MaxLevel;

	public VariableComplex ExpBonus;

	public VariableComplex AbilityPower;

	public AscensionChoosePanel panel;

	public Action OnAscension;

	public Action onChangeLVL;

	[SerializeField]
	private AscensionController controller;

	public bool IsActive { get; private set; }

	public HeroesNames Choosen { get; private set; }

	public bool IsAvailable => GameManager.Instance.Paragon.AscensionIsAvailable;

	public void Init()
	{
		MaxLevel = new VariableInt(0);
		ExpBonus = new VariableComplex(1.0);
		AbilityPower = new VariableComplex(1.0);
		Choosen = HeroesNames.None;
		Forms = new Dictionary<HeroesNames, DemonForm>();
		Forms.Add(HeroesNames.Tempest, new SpellForm());
		Forms.Add(HeroesNames.Demiurge, new SourceFrorm());
		Forms.Add(HeroesNames.Dread, new PetForm());
		Forms.Add(HeroesNames.Defiance, new AbilityForm());
		Paragons paragon = GameManager.Instance.Paragon;
		paragon.onChangeLVL = (Action)Delegate.Combine(paragon.onChangeLVL, new Action(CheckUnlock));
		GameManager instance = GameManager.Instance;
		instance.OnPostLoad = (Action)Delegate.Combine(instance.OnPostLoad, new Action(CheckUnlock));
		GameContext.ContextAddResource("Ascension.MaxLvl", MaxLevel);
		GameContext.ContextAddResource("Ascension.ExpBonus", ExpBonus);
		GameContext.ContextAddResource("Ascension.AbilityPower", AbilityPower);
		panel.Init();
	}

	public VariableInt GetAcitveLevel()
	{
		if (IsActive)
		{
			DemonForm current = GetCurrent();
			if (current != null)
			{
				return current.Level;
			}
		}
		return null;
	}

	public void CheckUnlock()
	{
		base.enabled = IsAvailable;
	}

	public void SetChoosen(HeroesNames key)
	{
		if (IsReadyToAscend(key))
		{
			Choosen = key;
			Activate();
		}
	}

	public void AddExp(BigNumber exp)
	{
		if (IsActive)
		{
			GetCurrent()?.AddFlatExp(exp);
		}
	}

	public DemonForm GetCurrent()
	{
		if (Forms.ContainsKey(Choosen))
		{
			return Forms[Choosen];
		}
		return null;
	}

	private void Update()
	{
		if (IsActive)
		{
			GetCurrent().update();
		}
	}

	public bool IsReadyToAscend(HeroesNames key)
	{
		if (Choosen == HeroesNames.None && Forms.ContainsKey(key))
		{
			return Forms[key].CheckClass();
		}
		return false;
	}

	public void Activate()
	{
		IsActive = true;
		GetCurrent().ApplyEffects();
		GameManager.Instance.CurrentHero.OnChange?.Invoke();
		OnAscension?.Invoke();
		onChangeLVL?.Invoke();
	}

	public void Deactivate()
	{
		GetCurrent()?.DisableAll();
		IsActive = false;
		Choosen = HeroesNames.None;
		OnAscension?.Invoke();
		onChangeLVL?.Invoke();
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData();
		saveData.Key = (int)Choosen;
		saveData.Forms = new List<DemonForm.SaveData>();
		saveData.MaxLvl = MaxLevel.ValueInt;
		DemonForm current = GetCurrent();
		if (current != null)
		{
			saveData.Forms.Add(current.Save());
		}
		if (current != null && current.NameKey == HeroesNames.Dread)
		{
			Pet pet = GameManager.Instance.CurrentPet.PetPanel.secondSlot.Pet;
			if (pet != null)
			{
				saveData.Pet = new PetFormData();
				saveData.Pet.key = (int)pet.NameKey;
				saveData.Pet.exp = pet.TotalExp.Value;
				saveData.Pet.played = pet.PlayedTime.ValueInt;
				saveData.Pet.skipped = pet.SkipedPlayedTime.Value;
			}
			else
			{
				saveData.Pet = null;
			}
		}
		else
		{
			saveData.Pet = null;
		}
		if (current != null && current.NameKey == HeroesNames.Defiance)
		{
			saveData.Energy = (current as AbilityForm).panel.currentEnergy;
		}
		else
		{
			saveData.Energy = 0f;
		}
		return saveData;
	}

	public void ResetAll()
	{
		Deactivate();
		foreach (KeyValuePair<HeroesNames, DemonForm> form in Forms)
		{
			form.Value.Reset();
		}
	}

	public void Load(SaveData data)
	{
		if (data == null)
		{
			Choosen = HeroesNames.None;
			MaxLevel.SetValue(0);
			{
				foreach (KeyValuePair<HeroesNames, DemonForm> form in Forms)
				{
					form.Value.Reset();
				}
				return;
			}
		}
		MaxLevel.SetValue(data.MaxLvl);
		foreach (DemonForm.SaveData form2 in data.Forms)
		{
			if (Forms.ContainsKey((HeroesNames)form2.key))
			{
				Forms[(HeroesNames)form2.key].Load(form2);
			}
		}
		SetChoosen((HeroesNames)data.Key);
		DemonForm current2 = GetCurrent();
		if (current2 == null)
		{
			return;
		}
		if (MaxLevel.ValueInt == 0)
		{
			MaxLevel.SetValue(current2.Level.ValueInt);
		}
		if (current2.NameKey == HeroesNames.Dread)
		{
			if (data.Pet == null)
			{
				GameManager.Instance.CurrentPet.PetPanel.secondSlot.SetPet(PetNames.None);
				return;
			}
			PetSecondSlot secondSlot = GameManager.Instance.CurrentPet.PetPanel.secondSlot;
			secondSlot.SetPet((PetNames)data.Pet.key);
			secondSlot.Pet.RecalculateLevel(data.Pet.exp);
			secondSlot.PlayedTime.SetValue(data.Pet.played);
			secondSlot.SkipedPlayedTime.SetValue(data.Pet.skipped);
		}
		else if (current2.NameKey == HeroesNames.Defiance)
		{
			(current2 as AbilityForm).panel.currentEnergy = data.Energy;
		}
	}

	public void PostLoad()
	{
		MaxLevel.SetValue(MaxLevel.ValueInt);
		onChangeLVL?.Invoke();
	}
}
