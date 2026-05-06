using System;
using System.Collections.Generic;
using CardGame;
using UnityEngine;

public class TaskManager
{
	public class SaveData
	{
		public List<int> id;

		public SaveData()
		{
			id = new List<int>();
		}
	}

	public List<Task> Tasks;

	private Sprite sprite;

	private TaskNarrator narrator;

	public TaskManager(Sprite sprite)
	{
		Tasks = new List<Task>();
		InitAllTasks();
		this.sprite = sprite;
	}

	private void InitAllTasks()
	{
		Func<bool> condition = () => true;
		Func<Spell, bool> condition2 = (Spell sp) => true;
		Task task = new Task(1, "ScrollQuest", "ScrollQuestDescr", "<sprite=0>", "Relic", 1, OnComplete);
		Func<bool> condition3 = () => GameManager.Instance.Scrolls.ScrollCount.ValueInt >= 1;
		TaskObjectiveEvent scrollObjective = task.AddObjective("ScrollObjective", condition3);
		scrollObjective.Init(delegate
		{
			VariableInt scrollCount = GameManager.Instance.Scrolls.ScrollCount;
			scrollCount.OnChange = (Action)Delegate.Combine(scrollCount.OnChange, new Action(scrollObjective.Check));
			scrollObjective.Check();
		}, delegate
		{
			VariableInt scrollCount = GameManager.Instance.Scrolls.ScrollCount;
			scrollCount.OnChange = (Action)Delegate.Remove(scrollCount.OnChange, new Action(scrollObjective.Check));
		});
		Tasks.Add(task);
		task = new Task(2, "SpellQuest", "SpellQuestDescr", "<sprite=0>", "Relic", 1, OnComplete);
		task.AddObjectiveSpell("SpellObjective", condition2);
		Tasks.Add(task);
		task = new Task(3, "FirstComboQuest", "FirstComboDescr", "<sprite=0>", "Relic", 1, OnComplete);
		Func<bool> condition4 = () => GameManager.Instance.Scrolls.ScrollCount.ValueInt >= 2;
		TaskObjectiveEvent scrollObjective2 = task.AddObjective("Scroll2Objective", condition4);
		scrollObjective2.Init(delegate
		{
			VariableInt scrollCount = GameManager.Instance.Scrolls.ScrollCount;
			scrollCount.OnChange = (Action)Delegate.Combine(scrollCount.OnChange, new Action(scrollObjective2.Check));
			scrollObjective2.Check();
		}, delegate
		{
			VariableInt scrollCount = GameManager.Instance.Scrolls.ScrollCount;
			scrollCount.OnChange = (Action)Delegate.Remove(scrollCount.OnChange, new Action(scrollObjective2.Check));
		});
		task.AddObjectiveSpell("FirstComboObjective", delegate(Spell spell)
		{
			if (spell.NameKey != Spells.MagicMissile)
			{
				return false;
			}
			HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			return GameManager.Instance.Idle.IdleIsActive && GameManager.Instance.VoidMana.Value > 1.0 && nameKey == HeroesNames.Apprentice && scrolls.SpellIsActive(Spells.GemResonance);
		});
		Tasks.Add(task);
		task = new Task(4, "CharacterQuest", "CharacterQuestDescr", "<sprite=0>", "Relic", 1, OnComplete);
		TaskObjectiveEvent charObjective = task.AddObjective("CharacterObjective", () => GameManager.Instance.CurrentHero.Hero.NameKey != HeroesNames.Apprentice);
		charObjective.Init(delegate
		{
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Combine(currentHero.OnChange, new Action(charObjective.Check));
			charObjective.Check();
		}, delegate
		{
			HeroSlot currentHero = GameManager.Instance.CurrentHero;
			currentHero.OnChange = (Action)Delegate.Remove(currentHero.OnChange, new Action(charObjective.Check));
		});
		Tasks.Add(task);
		task = new Task(5, "BurstQuest", "BurstQuestDescr", "<sprite=0>", "Relic", 1, OnComplete);
		TaskObjectiveEvent petObjective = task.AddObjective("BurstObjectivePet", delegate
		{
			HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
			Pet pet = GameManager.Instance.CurrentPet.Pet;
			return (nameKey == HeroesNames.Druid && pet != null && pet.NameKey == PetNames.Pixie) || (nameKey == HeroesNames.Demonologist && pet != null && pet.NameKey == PetNames.Daemon) || (nameKey == HeroesNames.Necromancer && pet != null && pet.NameKey == PetNames.ZombieWarrior);
		});
		petObjective.Init(delegate
		{
			PetSlot currentPet = GameManager.Instance.CurrentPet;
			currentPet.OnSelect = (Action)Delegate.Combine(currentPet.OnSelect, new Action(petObjective.Check));
			petObjective.Check();
		}, delegate
		{
			PetSlot currentPet = GameManager.Instance.CurrentPet;
			currentPet.OnSelect = (Action)Delegate.Remove(currentPet.OnSelect, new Action(petObjective.Check));
		});
		task.AddObjectiveSpell("BurstObjectiveCasts", delegate
		{
			HeroesNames nameKey = GameManager.Instance.CurrentHero.Hero.NameKey;
			Pet pet = GameManager.Instance.CurrentPet.Pet;
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			return (GameManager.Instance.Idle.IdleIsActive && GameManager.Instance.VoidMana.Value > 1.0 && nameKey == HeroesNames.Druid && pet != null && pet.NameKey == PetNames.Pixie && scrolls.SpellIsActive(Spells.SummonWoodlandCreatures) && scrolls.SpellIsActive(Spells.SummonEvergrowingForest)) || (nameKey == HeroesNames.Demonologist && pet != null && pet.NameKey == PetNames.Daemon && scrolls.SpellIsActive(Spells.Empower) && scrolls.SpellIsActive(Spells.Hellrage)) || (nameKey == HeroesNames.Necromancer && pet != null && pet.NameKey == PetNames.ZombieWarrior && scrolls.SpellIsActive(Spells.Empower) && scrolls.SpellIsActive(Spells.MagicalWeapon) && scrolls.SpellIsActive(Spells.AnimateSkeletalMinions));
		});
		Tasks.Add(task);
		task = new Task(11, "ExileQuest", "ExileQuestDescr", "<sprite=0>", "Relic", 1, OnComplete);
		TaskObjectiveEvent exileObjective = task.AddObjective("ExileQuestObjective", () => Reborn.Souls.Value >= 1000.0);
		exileObjective.Init(delegate
		{
			GameManager instance = GameManager.Instance;
			instance.OnPostExile = (Action)Delegate.Combine(instance.OnPostExile, new Action(exileObjective.Check));
			exileObjective.Check();
		}, delegate
		{
			GameManager instance = GameManager.Instance;
			instance.OnPostExile = (Action)Delegate.Remove(instance.OnPostExile, new Action(exileObjective.Check));
		});
		Tasks.Add(task);
		task = new Task(6, "TrialQuest", "TrialQuestDescr", "<sprite=2>", "TrialRune", 3, OnComplete);
		Func<bool> condition5 = () => GameManager.Instance.Paragon.Level >= 3;
		TaskObjectiveEvent paragon3 = task.AddObjective("Paragon3Objective", condition5);
		paragon3.Init(delegate
		{
			Paragons paragon18 = GameManager.Instance.Paragon;
			paragon18.onChangeLVL = (Action)Delegate.Combine(paragon18.onChangeLVL, new Action(paragon3.Check));
			paragon3.Check();
		}, delegate
		{
			Paragons paragon18 = GameManager.Instance.Paragon;
			paragon18.onChangeLVL = (Action)Delegate.Remove(paragon18.onChangeLVL, new Action(paragon3.Check));
		});
		TaskObjectiveEvent trialObjective = task.AddObjective("TrialObjective", condition);
		trialObjective.Init(delegate
		{
			TrialManager trials = GameManager.Instance.Trials;
			trials.OnStart = (Action)Delegate.Combine(trials.OnStart, new Action(trialObjective.Check));
		}, delegate
		{
			TrialManager trials = GameManager.Instance.Trials;
			trials.OnStart = (Action)Delegate.Remove(trials.OnStart, new Action(trialObjective.Check));
		});
		Tasks.Add(task);
		task = new Task(7, "ExpeditionQuest", "ExpeditionQuestDescr", "<sprite=2>", "TrialRune", 3, OnComplete);
		Func<bool> condition6 = () => GameManager.Instance.Paragon.Level >= 4;
		TaskObjectiveEvent paragon4 = task.AddObjective("Paragon4Objective", condition6);
		paragon4.Init(delegate
		{
			Paragons paragon18 = GameManager.Instance.Paragon;
			paragon18.onChangeLVL = (Action)Delegate.Combine(paragon18.onChangeLVL, new Action(paragon4.Check));
			paragon4.Check();
		}, delegate
		{
			Paragons paragon18 = GameManager.Instance.Paragon;
			paragon18.onChangeLVL = (Action)Delegate.Remove(paragon18.onChangeLVL, new Action(paragon4.Check));
		});
		TaskObjectiveEvent experiditionObjective = task.AddObjective("ExpeditionObjective", condition);
		experiditionObjective.Init(delegate
		{
			ExpeditionIdle expeditionIdle = ExpeditionManager.Instance.ExpeditionIdle;
			expeditionIdle.OnStart = (Action)Delegate.Combine(expeditionIdle.OnStart, new Action(experiditionObjective.Check));
		}, delegate
		{
			ExpeditionIdle expeditionIdle = ExpeditionManager.Instance.ExpeditionIdle;
			expeditionIdle.OnStart = (Action)Delegate.Remove(expeditionIdle.OnStart, new Action(experiditionObjective.Check));
		});
		Tasks.Add(task);
		task = new Task(8, "CraftQuest", "CraftQuestDescr", "<sprite=10>", "AllDust", 2000, OnComplete);
		Func<bool> condition7 = () => GameManager.Instance.Paragon.Level >= 9;
		TaskObjectiveEvent paragon9 = task.AddObjective("Paragon9Objective", condition7);
		paragon9.Init(delegate
		{
			Paragons paragon18 = GameManager.Instance.Paragon;
			paragon18.onChangeLVL = (Action)Delegate.Combine(paragon18.onChangeLVL, new Action(paragon9.Check));
			paragon9.Check();
		}, delegate
		{
			Paragons paragon18 = GameManager.Instance.Paragon;
			paragon18.onChangeLVL = (Action)Delegate.Remove(paragon18.onChangeLVL, new Action(paragon9.Check));
		});
		TaskObjectiveEvent craftObjectrive = task.AddObjective("CraftObjective", condition);
		craftObjectrive.Init(delegate
		{
			CraftManager craft = GameManager.Instance.Craft;
			craft.OnCraftItem = (Action)Delegate.Combine(craft.OnCraftItem, new Action(craftObjectrive.Check));
			craftObjectrive.Check();
		}, delegate
		{
			CraftManager craft = GameManager.Instance.Craft;
			craft.OnCraftItem = (Action)Delegate.Remove(craft.OnCraftItem, new Action(craftObjectrive.Check));
		});
		Tasks.Add(task);
		task = new Task(9, "SetQuest", "SetQuestDescr", "<sprite=0>", "Relic", 1, OnComplete);
		TaskObjectiveEvent setSpell = task.AddObjective("SetObjectiveSpell", condition);
		setSpell.Init(delegate
		{
			SetsPanel setsPanel = GameManager.Instance.SpellBook.SetsPanel;
			setsPanel.OnSaveSet = (Action)Delegate.Combine(setsPanel.OnSaveSet, new Action(setSpell.Check));
		}, delegate
		{
			SetsPanel setsPanel = GameManager.Instance.SpellBook.SetsPanel;
			setsPanel.OnSaveSet = (Action)Delegate.Remove(setsPanel.OnSaveSet, new Action(setSpell.Check));
		});
		TaskObjectiveEvent setItems = task.AddObjective("SetObjectiveItems", condition);
		setItems.Init(delegate
		{
			ItemPresetWindow presets = GameManager.Instance.Craft.window.presets;
			presets.OnSaveSet = (Action)Delegate.Combine(presets.OnSaveSet, new Action(setItems.Check));
		}, delegate
		{
			ItemPresetWindow presets = GameManager.Instance.Craft.window.presets;
			presets.OnSaveSet = (Action)Delegate.Remove(presets.OnSaveSet, new Action(setItems.Check));
		});
		TaskObjectiveEvent setLoad = task.AddObjective("SetObjectiveLoad", condition);
		setLoad.Init(delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnLoadLoadout = (Action)Delegate.Combine(scrolls.OnLoadLoadout, new Action(setLoad.Check));
		}, delegate
		{
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnLoadLoadout = (Action)Delegate.Remove(scrolls.OnLoadLoadout, new Action(setLoad.Check));
		});
		Tasks.Add(task);
		task = new Task(10, "EnchantQuest", "EnchantQuestDescr", "<sprite=11>", "EnchantingDust", 500, OnComplete);
		Func<bool> condition8 = () => GameManager.Instance.Paragon.Level >= 17;
		TaskObjectiveEvent paragon17 = task.AddObjective("Paragon17Objective", condition8);
		paragon17.Init(delegate
		{
			Paragons paragon18 = GameManager.Instance.Paragon;
			paragon18.onChangeLVL = (Action)Delegate.Combine(paragon18.onChangeLVL, new Action(paragon17.Check));
			paragon17.Check();
		}, delegate
		{
			Paragons paragon18 = GameManager.Instance.Paragon;
			paragon18.onChangeLVL = (Action)Delegate.Remove(paragon18.onChangeLVL, new Action(paragon17.Check));
		});
		TaskObjectiveEvent enchantObjective = task.AddObjective("EnchantObjective", condition);
		enchantObjective.Init(delegate
		{
			CraftManager craft = GameManager.Instance.Craft;
			craft.OnEnchantSingle = (Action)Delegate.Combine(craft.OnEnchantSingle, new Action(enchantObjective.Check));
		}, delegate
		{
			CraftManager craft = GameManager.Instance.Craft;
			craft.OnEnchantSingle = (Action)Delegate.Remove(craft.OnEnchantSingle, new Action(enchantObjective.Check));
		});
		Tasks.Add(task);
	}

	public void Activate(bool showMsg = false)
	{
		Task current = GetCurrent();
		if (current != null)
		{
			if (narrator == null)
			{
				narrator = UnityEngine.Object.Instantiate(Resources.Load<TaskNarrator>("QuestNarrator"), GameManager.Instance.Tutorial.top_panel.transform);
				narrator.transform.SetAsFirstSibling();
			}
			narrator.gameObject.SetActive(value: true);
			current?.Activate(showMsg);
		}
		else if (narrator != null)
		{
			narrator.gameObject.SetActive(value: false);
		}
	}

	public void PreLoad()
	{
		foreach (Task task in Tasks)
		{
			task.Reset();
		}
	}

	public void PostLoad()
	{
		if (!GameManager.Instance.Tutorial.isActive)
		{
			Activate();
		}
	}

	public Task GetCurrent()
	{
		return Tasks.Find((Task x) => !x.IsCompleted);
	}

	private void OnComplete(Task task)
	{
		GameManager.Instance.AchievManager.unlock_message.Unlock("\"" + task.GetName() + "\" " + "QuestCompleted".Translate(), sprite);
		Task current = GetCurrent();
		if (current != null)
		{
			current.Activate();
			return;
		}
		if (narrator != null)
		{
			narrator.gameObject.SetActive(value: false);
		}
		GameManager.Instance.LoreInfo.Show("FinalQuest".Translate(), "FinalQuestDescr".Translate());
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData();
		foreach (Task task in Tasks)
		{
			if (task.IsCompleted)
			{
				saveData.id.Add(task.ID);
			}
		}
		return saveData;
	}

	public SaveData SavePremiumOnly()
	{
		SaveData saveData = new SaveData();
		foreach (Task task in Tasks)
		{
			if (task.IsCompleted && task.RewardKey == "Relic")
			{
				saveData.id.Add(task.ID);
			}
		}
		return saveData;
	}

	public void Load(SaveData data)
	{
		foreach (Task task2 in Tasks)
		{
			task2.Reset();
		}
		if (data == null)
		{
			return;
		}
		foreach (int v in data.id)
		{
			Task task = Tasks.Find((Task x) => x.ID == v);
			if (task != null)
			{
				task.IsCompleted = true;
			}
		}
	}
}
