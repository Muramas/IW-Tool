using System;
using System.Collections.Generic;
using Planes;
using UnityEngine;

public class RealmcraftManager : MonoBehaviour
{
	public class SaveData
	{
		public string Active;

		public Dictionary<string, float> Completed;

		public Dictionary<int, int> Paramnesics;

		public string RealmData;

		public List<RealmHistoryData> History;
	}

	public class ParamnesicsFormat
	{
		public string ID;

		public string Name;

		public string Description;

		public string Sprite;

		public string Param;

		public string A;

		public string M;

		public string P;

		public string MaxLvl;

		public string Reset;
	}

	public Dictionary<string, Plane> Planes;

	public VariableFloat Delusions;

	public VariableFloat DelusionsTotal;

	public VariableInt Completed;

	public RealmcraftWindow Window;

	public ParamnesicWindow Upgrades;

	public List<Paramnesic> Paramnesics;

	private string realm_message = "RealmChangeMessage";

	private SimpleEffect myst;

	private VariableBignumber mystScale;

	private SimpleEffect pet;

	private VariableBignumber petScale;

	private SimpleEffect hero;

	private VariableBignumber heroScale;

	private VariableFloat startingMana;

	private SimpleEffect memory;

	private VariableBignumber memoryScale;

	private SimpleEffect ascensionEffect;

	private VariableBignumber ascensionScale;

	public Action OnChangeActive;

	public SpriteAtlas Atlas;

	public List<RealmHistoryData> History;

	private bool resetAll;

	private bool resetDelusion;

	public Plane Active { get; private set; }

	public bool IsActive => Active != null;

	public void Init()
	{
		Planes = new Dictionary<string, Plane>();
		AddPlane(new global::Planes.Druid());
		AddPlane(new Demon());
		AddPlane(new Necro());
		AddPlane(new Arcan());
		AddPlane(new Prod());
		AddPlane(new global::Planes.Void());
		AddPlane(new Exo());
		AddPlane(new Chrono());
		AddPlane(new Umbra());
		AddPlane(new Alch());
		AddPlane(new global::Planes.Ironsoul());
		AddPlane(new global::Planes.Abolisher());
		AddPlane(new AppSalad());
		AddPlane(new Myst());
		AddPlane(new ProdSalad());
		AddPlane(new Frost());
		AddPlane(new Vamprire());
		AddPlane(new ArtificerPlane());
		AddPlane(new ShapeshifterPlane());
		AddPlane(new ArcherPlane());
		Delusions = new VariableFloat(0f);
		DelusionsTotal = new VariableFloat(0f);
		Completed = new VariableInt(0);
		startingMana = new VariableFloat(1f);
		Prepare();
		mystScale = new VariableBignumber(1.0);
		myst = new SimpleEffect(Reborn.SoulPower);
		VariableBignumber variableBignumber = mystScale;
		variableBignumber.OnChange = (Action)Delegate.Combine(variableBignumber.OnChange, new Action(UpdateMyst));
		petScale = new VariableBignumber(1.0);
		pet = new SimpleEffect(GameManager.Instance.CurrentPet.AbilityPower);
		VariableBignumber variableBignumber2 = petScale;
		variableBignumber2.OnChange = (Action)Delegate.Combine(variableBignumber2.OnChange, new Action(UpdatePet));
		heroScale = new VariableBignumber(1.0);
		hero = new SimpleEffect(GameManager.Instance.CurrentHero.AbilityPower);
		VariableBignumber variableBignumber3 = heroScale;
		variableBignumber3.OnChange = (Action)Delegate.Combine(variableBignumber3.OnChange, new Action(UpdateHero));
		memoryScale = new VariableBignumber(1.0);
		memory = new SimpleEffect(GameManager.Instance.Profit);
		VariableBignumber variableBignumber4 = memoryScale;
		variableBignumber4.OnChange = (Action)Delegate.Combine(variableBignumber4.OnChange, new Action(UpdateMemory));
		VariableBignumber totalMemories = GameManager.Instance.Realm.TotalMemories;
		totalMemories.OnChange = (Action)Delegate.Combine(totalMemories.OnChange, new Action(UpdateMemory));
		ascensionScale = new VariableBignumber(1.0);
		ascensionEffect = new SimpleEffect(GameManager.Instance.VoidManaManager.Power);
		VariableBignumber variableBignumber5 = ascensionScale;
		variableBignumber5.OnChange = (Action)Delegate.Combine(variableBignumber5.OnChange, new Action(UpdateAscension));
		AscensionManager ascension = GameManager.Instance.Ascension;
		ascension.onChangeLVL = (Action)Delegate.Combine(ascension.onChangeLVL, new Action(UpdateAscension));
		VariableFloat delusionsTotal = DelusionsTotal;
		delusionsTotal.OnChange = (Action)Delegate.Combine(delusionsTotal.OnChange, new Action(UpdateMyst));
		VariableFloat delusionsTotal2 = DelusionsTotal;
		delusionsTotal2.OnChange = (Action)Delegate.Combine(delusionsTotal2.OnChange, new Action(UpdatePet));
		VariableFloat delusionsTotal3 = DelusionsTotal;
		delusionsTotal3.OnChange = (Action)Delegate.Combine(delusionsTotal3.OnChange, new Action(UpdateHero));
		GameContext.ContextAddResource("Realm.DelusionTotal", DelusionsTotal);
		GameContext.ContextAddResource("Realm.Completed", Completed);
		GameContext.ContextAddResource("Realm.MystDelusion", mystScale);
		GameContext.ContextAddResource("Realm.PetDelusion", petScale);
		GameContext.ContextAddResource("Realm.CharDelusion", heroScale);
		GameContext.ContextAddResource("Realm.MemoryScale", memoryScale);
		GameContext.ContextAddResource("Realm.StartingMana", startingMana);
		GameContext.ContextAddResource("Realm.AscensionScale", ascensionScale);
		Paramnesics = new List<Paramnesic>();
		foreach (ParamnesicsFormat paramnesic in GlobalData.Paramnesics)
		{
			if (paramnesic.Param != null && paramnesic.Param != string.Empty)
			{
				Paramnesics.Add(new Paramnesic(paramnesic));
			}
		}
		Upgrades.Init(Paramnesics);
		History = new List<RealmHistoryData>();
		VariableFloat variableFloat = startingMana;
		variableFloat.OnChange = (Action)Delegate.Combine(variableFloat.OnChange, new Action(OnChangeStartingMana));
	}

	public void Update()
	{
		if (Active != null)
		{
			Active.Update();
		}
	}

	public void Prepare()
	{
		foreach (KeyValuePair<string, Plane> plane in Planes)
		{
			plane.Value.Prepare();
		}
	}

	public void PreLoad()
	{
		if (Active != null)
		{
			Active.PreLoad();
			Active.RemoveRules();
		}
		SetActive(null);
		RemoveAll();
		Delusions.SetValue(0f);
		DelusionsTotal.SetValue(0f);
		Completed.SetValue(0);
	}

	public void PostLoad()
	{
		if (Active != null)
		{
			Active.ApplyRules();
			Active.PostLoad();
		}
		ApplyAll();
		Completed.OnChange?.Invoke();
		UnlockEnhancements();
	}

	public void PreRealmReset()
	{
		UnlockEnhancements();
		if (Active != null)
		{
			Active.PreLoad();
			Active.RemoveRules();
		}
		SetActive(Window.Selected);
		RemoveAll();
	}

	public void PostRealmReset()
	{
		if (!IsActive)
		{
			GameManager.Instance.AchievManager.RefreshTriumphs();
		}
		else
		{
			GameManager.Instance.AchievManager.FailAllTriumphs();
		}
		ApplyAll();
		if (Active != null)
		{
			Active.ApplyRules();
			Active.PostLoad();
		}
	}

	public BigNumber GetStartingMana()
	{
		return new BigNumber(1.0, (int)((float)Active.GetBestScore().Exponent * (startingMana.ValueFloat - 1f)));
	}

	private void OnChangeStartingMana()
	{
		if (Active != null)
		{
			BigNumber bigNumber = GetStartingMana();
			if (Statistic.ManaRealm.Value < bigNumber || bigNumber < 1.0)
			{
				Statistic.ManaRealm.SetValue(bigNumber);
				Reborn.Souls.Reset(Reborn.Instance.GetAllMysts());
				Reborn.Instance.OnSoulsChange();
			}
		}
	}

	public void ResetOnExile()
	{
		foreach (Paramnesic paramnesic in Paramnesics)
		{
			if (paramnesic.IsResets)
			{
				paramnesic.Remove();
			}
		}
		Active?.PreLoad();
	}

	public void PostExile()
	{
		ApplyAll();
		Active?.PostLoad();
	}

	public void PostOffline()
	{
		Active?.PostOffline();
	}

	private void ApplyAll()
	{
		foreach (Paramnesic paramnesic in Paramnesics)
		{
			paramnesic.Apply();
		}
		myst.Apply();
		pet.Apply();
		hero.Apply();
		memory.Apply();
		ascensionEffect.Apply();
	}

	private void RemoveAll()
	{
		myst.Delete();
		pet.Delete();
		hero.Delete();
		memory.Delete();
		ascensionEffect.Delete();
		foreach (Paramnesic paramnesic in Paramnesics)
		{
			paramnesic.Remove();
		}
	}

	private void AddPlane(Plane plane)
	{
		Planes.Add(plane.ID, plane);
	}

	public Plane Get(string id)
	{
		if (!Planes.ContainsKey(id))
		{
			return null;
		}
		return Planes[id];
	}

	public void Select(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			SetActive(null);
			return;
		}
		SetActive(Get(id));
		if (!IsAvailable(Active))
		{
			SetActive(null);
		}
	}

	private void SetActive(Plane active)
	{
		Active = active;
		OnChangeActive?.Invoke();
	}

	public bool IsChangeHero()
	{
		if (Active != null)
		{
			return Active.IsChangeHero;
		}
		return true;
	}

	public bool IsChangePet()
	{
		if (Active != null)
		{
			return Active.IsChangePet;
		}
		return true;
	}

	public bool IsAvailableMemGroup(RealmManager.MemoryGroups group)
	{
		if (!IsActive)
		{
			return true;
		}
		return Active.AvailableMemories.Contains(group);
	}

	public bool IsAvailable(Plane plane)
	{
		if (plane == null)
		{
			return true;
		}
		bool flag = plane.Memories <= GameManager.Instance.Realm.TotalMemories.Value;
		if (!flag)
		{
			return flag;
		}
		foreach (string previou in plane.Previous)
		{
			Plane plane2 = Get(previou);
			flag = flag && (plane2.IsCompleted || (plane2 == Active && GetDelusionProgress() >= 1f));
		}
		return flag;
	}

	public void Convert()
	{
		resetAll = false;
		GameManager.Instance.ConfirmWindow.Open(realm_message.Translate(), ChangeRealm, "RealmChangeEnableMemories".Translate(), delegate
		{
			resetAll = true;
		});
		if (GameManager.Instance.Paragon.GildingBuildingsIsAvailable)
		{
			GameManager.Instance.ConfirmWindow.AddOption2("RealmChangeEnableSources".Translate(), GameManager.Instance.Gilding.Buildings.ResetSoft);
		}
		if (GameManager.Instance.Paragon.GildingSpellsIsAvailable)
		{
			GameManager.Instance.ConfirmWindow.AddOption3("RealmChangeEnableSpells".Translate(), GameManager.Instance.Gilding.Spells.ResetSoft);
		}
		if (IsInvested())
		{
			GameManager.Instance.ConfirmWindow.AddOption4("RealmChangeEnableDelusion".Translate(), delegate
			{
				resetDelusion = true;
			});
		}
		if (GameManager.Instance.Paragon.GildingPantheonIsAvailable)
		{
			GameManager.Instance.ConfirmWindow.AddOption5("ExileResetPantheon".Translate(), GameManager.Instance.Gilding.Pantheon.ResetSoft);
		}
	}

	private void ChangeRealm()
	{
		GetDelusions();
		if (resetDelusion)
		{
			ResetSoft();
			resetDelusion = false;
		}
		GameManager.Instance.RealmChange(ResetAll);
		GameManager.Instance.Realm.OnRealm?.Invoke();
	}

	private void ResetAll()
	{
		GameManager.Instance.Realm.ResetAll(resetAll);
	}

	private void GetDelusions()
	{
		if (IsActive)
		{
			float delusionProgress = GetDelusionProgress();
			float ad = delusionProgress - Active.Progress;
			Delusions.Change(ad);
			DelusionsTotal.Change(ad);
			if (Active.Progress == 0f && delusionProgress >= 1f)
			{
				Completed.Change(1);
			}
			Active.Progress = delusionProgress;
		}
	}

	public float GetDelusionProgress()
	{
		if (!IsActive)
		{
			return 0f;
		}
		float num = Active.Progress;
		float num2 = GetCurrentScore().ToFloat();
		if (num2 > num)
		{
			num = num2;
		}
		return num;
	}

	public BigNumber GetCurrentScore()
	{
		if (!IsActive)
		{
			return new BigNumber(0.0);
		}
		float num = 0f;
		BigNumber value = Statistic.ManaRealm.Value;
		if (value >= Active.Mana)
		{
			num = 1f + (float)((value.Log10() - (double)Active.Mana.Exponent) / (double)Active.ManaStep.Exponent);
		}
		return num;
	}

	private void UpdateMyst()
	{
		myst.mult = 1.0 + (mystScale.Value - 1.0) * DelusionsTotal.Value;
		myst.Update();
	}

	private void UpdatePet()
	{
		pet.mult = 1.0 + (petScale.Value - 1.0) * DelusionsTotal.Value;
		pet.Update();
	}

	private void UpdateHero()
	{
		hero.mult = 1.0 + (heroScale.Value - 1.0) * DelusionsTotal.Value;
		hero.Update();
	}

	private void UpdateMemory()
	{
		if (memoryScale.Value >= 1.0)
		{
			memory.mult = 1.0 + GameManager.Instance.Realm.TotalMemories.Value.Pow(memoryScale.Value.ToFloat() - 1f);
		}
		else
		{
			memory.mult = 1.0;
		}
		memory.Update();
	}

	private void UpdateAscension()
	{
		VariableInt acitveLevel = GameManager.Instance.Ascension.GetAcitveLevel();
		if (acitveLevel == null || ascensionScale.Value <= 1.0)
		{
			ascensionEffect.mult = 1.0;
		}
		else
		{
			ascensionEffect.mult = 1.0 + acitveLevel.Value.Pow(ascensionScale.Value.ToFloat() - 1f);
		}
		ascensionEffect.Update();
	}

	public void SaveHistory()
	{
		if (History.Count >= 20)
		{
			History.RemoveAt(0);
		}
		string id = ((Active == null) ? string.Empty : Active.ID);
		BigNumber score = ((Active == null) ? GameManager.Instance.Realm.CalcualteMemories() : GetCurrentScore());
		History.Add(new RealmHistoryData(id, score, Statistic.GetEdustRealm().Value, GameManager.Instance.BuildingManager.CatalystAmount.Value, Statistic.TimeRealm.ValueInt));
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData();
		saveData.Active = ((Active == null) ? string.Empty : Active.ID);
		saveData.Completed = new Dictionary<string, float>();
		foreach (KeyValuePair<string, Plane> plane in Planes)
		{
			if (plane.Value.IsCompleted)
			{
				saveData.Completed.Add(plane.Key, plane.Value.Progress);
			}
		}
		saveData.Paramnesics = new Dictionary<int, int>();
		foreach (Paramnesic paramnesic in Paramnesics)
		{
			if (paramnesic.Level.ValueInt > 0)
			{
				saveData.Paramnesics.Add(paramnesic.Id, paramnesic.Level.ValueInt);
			}
		}
		if (Active == null)
		{
			saveData.RealmData = string.Empty;
		}
		else
		{
			saveData.RealmData = Active.Save();
		}
		saveData.History = History;
		return saveData;
	}

	public bool IsInvested()
	{
		if (DelusionsTotal.ValueFloat < 1f)
		{
			return false;
		}
		return DelusionsTotal.ValueFloat != Delusions.ValueFloat;
	}

	public void ResetSoft()
	{
		foreach (Paramnesic paramnesic in Paramnesics)
		{
			paramnesic.Remove();
			paramnesic.Level.SetValue(0);
		}
		Upgrades.Abort();
		Delusions.SetValue(DelusionsTotal.ValueFloat);
	}

	public void Load(SaveData data)
	{
		Upgrades.Abort();
		SetActive(null);
		foreach (KeyValuePair<string, Plane> plane in Planes)
		{
			plane.Value.Progress = 0f;
		}
		foreach (Paramnesic paramnesic2 in Paramnesics)
		{
			paramnesic2.Remove();
			paramnesic2.Level.SetValue(0);
		}
		if (data == null)
		{
			Delusions.SetValue(0f);
			DelusionsTotal.SetValue(0f);
			Completed.SetValue(0);
			return;
		}
		SetActive(Get(data.Active));
		BigNumber bigNumber = 0.0;
		if (data.Completed != null)
		{
			foreach (KeyValuePair<string, float> item in data.Completed)
			{
				Get(item.Key).Progress = item.Value;
				bigNumber += (BigNumber)item.Value;
			}
			Completed.SetValue(data.Completed.Count);
		}
		else
		{
			Completed.SetValue(0);
		}
		DelusionsTotal.SetValue(bigNumber);
		BigNumber bigNumber2 = 0.0;
		if (data.Paramnesics != null)
		{
			foreach (KeyValuePair<int, int> v in data.Paramnesics)
			{
				Paramnesic paramnesic = Paramnesics.Find((Paramnesic x) => x.Id == v.Key);
				if (paramnesic != null)
				{
					paramnesic.Level.SetValue(v.Value);
					bigNumber2 += (BigNumber)v.Value;
				}
			}
		}
		Delusions.SetValue(bigNumber - bigNumber2);
		Active?.Load(data.RealmData);
		if (data.History != null)
		{
			History = data.History;
		}
		else
		{
			History = new List<RealmHistoryData>();
		}
	}

	private void UnlockEnhancements()
	{
		EnhancementController enhancements = GameManager.Instance.SpellBook.Enhancements;
		foreach (KeyValuePair<string, Plane> plane in Planes)
		{
			if (plane.Value.Progress >= 5f && plane.Value.Enhancement != Spells.None)
			{
				enhancements.Unlock(plane.Value.Enhancement);
			}
		}
	}
}
