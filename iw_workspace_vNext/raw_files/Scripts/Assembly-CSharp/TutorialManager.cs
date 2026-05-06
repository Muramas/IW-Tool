using System;
using System.Collections;
using System.Collections.Generic;
using ModelShark;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
	public Animator buildings;

	public Animator top_panel;

	public Animator[] pages;

	public GameObject[] menu_pages;

	[SerializeField]
	private PopupTooltipRemotely hint;

	[SerializeField]
	private GameObject orb;

	[SerializeField]
	private GameObject building;

	[SerializeField]
	private GameObject upgrade;

	[SerializeField]
	private GameObject voidEntity;

	[SerializeField]
	private GameObject voidMana;

	[SerializeField]
	private GameObject pet;

	[SerializeField]
	private GameObject help;

	private Dictionary<int, Action> stages;

	private int stage;

	public bool isActive;

	private void Init()
	{
		stages = new Dictionary<int, Action>
		{
			{ 1, stage_click },
			{ 2, stage_mana_source },
			{ 3, stage_upgrades },
			{ 4, stage_events }
		};
		InitBaseState();
	}

	public void ShowOnLoad()
	{
		if (!isActive)
		{
			if (Statistic.TimeTotal.ValueInt - Statistic.TimeOfflineTotal.ValueInt > 600 || Reborn.Souls.Value >= 1.0)
			{
				DisableAll();
			}
			else
			{
				showTutorial();
			}
		}
	}

	private void showTutorial()
	{
		isActive = true;
		base.gameObject.SetActive(value: true);
		Init();
		StartCoroutine(delay(2f, delegate
		{
			stages[1]();
		}));
		base.enabled = true;
	}

	public void Show()
	{
		if (isActive)
		{
			DisableAll();
		}
		else
		{
			showTutorial();
		}
	}

	public void DisableAll()
	{
		skipAll();
		off();
	}

	private void Update()
	{
		if (stage == 9)
		{
			stage_help();
		}
	}

	private void InitBaseState()
	{
		buildings.SetTrigger("off");
		top_panel.SetTrigger("off");
		GameObject[] array = menu_pages;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		Animator[] array2 = pages;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].SetTrigger("off");
		}
	}

	private void stage_click()
	{
		if ((float)Statistic.TotalBuildings.ValueInt == 0f)
		{
			GameManager.Instance.BuyPack = 1;
			GameManager.Instance.BuyPackPanel.SetState(GameManager.Instance.BuyPack);
		}
		stage = 1;
		if (orb.activeInHierarchy)
		{
			hint.PopupTooltip(orb, "TutorClickOrb".Translate());
			Orb obj = GameManager.Instance.Orb;
			obj.OnClick = (Action<float>)Delegate.Combine(obj.OnClick, new Action<float>(close_stage_click));
		}
	}

	private void close_stage_click(float x)
	{
		Orb obj = GameManager.Instance.Orb;
		obj.OnClick = (Action<float>)Delegate.Remove(obj.OnClick, new Action<float>(close_stage_click));
		stages[2]();
	}

	private void stage_mana_source()
	{
		buildings.SetTrigger("on");
		top_panel.SetTrigger("on");
		stage = 2;
		StartCoroutine(delay(1f, delegate
		{
			if (building.activeInHierarchy)
			{
				hint.PopupTooltip(building, "TutorManaSource".Translate(), TipPosition.BottomLeftCorner);
				VariableInt totalBuildings = Statistic.TotalBuildings;
				totalBuildings.OnChange = (Action)Delegate.Combine(totalBuildings.OnChange, new Action(close_stage_manasources));
			}
		}));
	}

	private void close_stage_manasources()
	{
		VariableInt totalBuildings = Statistic.TotalBuildings;
		totalBuildings.OnChange = (Action)Delegate.Remove(totalBuildings.OnChange, new Action(close_stage_manasources));
		stages[3]();
	}

	private void stage_upgrades()
	{
		GameObject[] array = menu_pages;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		Animator[] array2 = pages;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].SetTrigger("on");
		}
		stage = 3;
		StartCoroutine(delay(1f, delegate
		{
			if (upgrade.activeInHierarchy)
			{
				hint.PopupTooltip(upgrade, "TutorUpgrade".Translate());
				VariableInt boughtUpgrades = Statistic.BoughtUpgrades;
				boughtUpgrades.OnChange = (Action)Delegate.Combine(boughtUpgrades.OnChange, new Action(close_stage_upgrades));
			}
		}));
	}

	private void close_stage_upgrades()
	{
		VariableInt boughtUpgrades = Statistic.BoughtUpgrades;
		boughtUpgrades.OnChange = (Action)Delegate.Remove(boughtUpgrades.OnChange, new Action(close_stage_upgrades));
		stages[4]();
		hint.Close();
		GameManager.Instance.Tasks.Activate(showMsg: true);
	}

	private void stage_events()
	{
		VariableLong timeSession = Statistic.TimeSession;
		timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(on_idle_mode));
		VariableInt scrollCount = GameManager.Instance.Scrolls.ScrollCount;
		scrollCount.OnChange = (Action)Delegate.Combine(scrollCount.OnChange, new Action(on_spell_unlock));
		VoidMana voidManaManager = GameManager.Instance.VoidManaManager;
		voidManaManager.OnSpawn = (Action)Delegate.Combine(voidManaManager.OnSpawn, new Action(on_voidmana_spawn));
		VariableBignumber variableBignumber = GameManager.Instance.VoidMana;
		variableBignumber.OnChange = (Action)Delegate.Combine(variableBignumber.OnChange, new Action(on_voidmana_earn));
		VariableInt level = GameManager.Instance.CurrentHero.Level;
		level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(on_pet));
	}

	private void on_idle_mode()
	{
		if (GameManager.Instance.Mana.Value < 250.0)
		{
			return;
		}
		VariableLong timeSession = Statistic.TimeSession;
		timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, new Action(on_idle_mode));
		GameObject gameObject = GameManager.Instance.Idle.defaultBar.gameObject;
		if (gameObject.activeInHierarchy)
		{
			hint.PopupTooltip(gameObject, "TutorIdle".Translate(), TipPosition.BottomLeftCorner);
			Idle idle = GameManager.Instance.Idle;
			idle.OnIdle = (Action)Delegate.Combine(idle.OnIdle, new Action(close_stage_idle));
			if (GameManager.Instance.Idle.IdleIsActive)
			{
				StartCoroutine(delay(4f, close_stage_idle));
			}
		}
	}

	private void close_stage_idle()
	{
		Idle idle = GameManager.Instance.Idle;
		idle.OnIdle = (Action)Delegate.Remove(idle.OnIdle, new Action(close_stage_idle));
		stage++;
		hint.Close();
	}

	private void on_spell_unlock()
	{
		VariableInt scrollCount = GameManager.Instance.Scrolls.ScrollCount;
		scrollCount.OnChange = (Action)Delegate.Remove(scrollCount.OnChange, new Action(on_spell_unlock));
		GameObject gameObject = GameManager.Instance.Scrolls.Scrolls[0].Icon.gameObject;
		if (gameObject.activeInHierarchy)
		{
			hint.PopupTooltip(gameObject, "TutorSpellbook".Translate());
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnOpen = (Action)Delegate.Combine(scrolls.OnOpen, new Action(close_stage_spells));
		}
	}

	private void close_stage_spells()
	{
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		scrolls.OnOpen = (Action)Delegate.Remove(scrolls.OnOpen, new Action(close_stage_spells));
		stage++;
		hint.Close();
	}

	private void on_voidmana_spawn()
	{
		VoidMana voidManaManager = GameManager.Instance.VoidManaManager;
		voidManaManager.OnSpawn = (Action)Delegate.Remove(voidManaManager.OnSpawn, new Action(on_voidmana_spawn));
		if (voidEntity.activeInHierarchy)
		{
			hint.PopupTooltip(voidEntity, "TutorVoidEntity".Translate(), TipPosition.BottomRightCorner);
			VariableLong clickableCollect = Statistic.ClickableCollect;
			clickableCollect.OnChange = (Action)Delegate.Combine(clickableCollect.OnChange, new Action(close_stage_void_entity));
		}
	}

	private void re_stage_void_entity()
	{
		VariableLong clickableCollect = Statistic.ClickableCollect;
		clickableCollect.OnChange = (Action)Delegate.Remove(clickableCollect.OnChange, new Action(close_stage_void_entity));
		BonusSpawner bonusSpawner = GameManager.Instance.BonusSpawner;
		bonusSpawner.OnLifeTimeEnd = (Action)Delegate.Remove(bonusSpawner.OnLifeTimeEnd, new Action(re_stage_void_entity));
		hint.Close();
		VoidMana voidManaManager = GameManager.Instance.VoidManaManager;
		voidManaManager.OnSpawn = (Action)Delegate.Combine(voidManaManager.OnSpawn, new Action(on_voidmana_spawn));
	}

	private void close_stage_void_entity()
	{
		VariableLong clickableCollect = Statistic.ClickableCollect;
		clickableCollect.OnChange = (Action)Delegate.Remove(clickableCollect.OnChange, new Action(close_stage_void_entity));
		stage++;
		hint.Close();
	}

	private void on_voidmana_earn()
	{
		VariableBignumber variableBignumber = GameManager.Instance.VoidMana;
		variableBignumber.OnChange = (Action)Delegate.Remove(variableBignumber.OnChange, new Action(on_voidmana_earn));
		StartCoroutine(delay(1f, delegate
		{
			if (voidMana.activeInHierarchy)
			{
				hint.PopupTooltip(voidMana, "TutorVoidMana".Translate(), TipPosition.BottomRightCorner);
				delay(4f, close_stage_void_earn);
			}
		}));
	}

	private void close_stage_void_earn()
	{
		stage++;
		hint.Close();
	}

	private void on_pet()
	{
		if (GameManager.Instance.CurrentHero.Level.ValueInt >= 3)
		{
			VariableInt level = GameManager.Instance.CurrentHero.Level;
			level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(on_pet));
			if (pet.activeInHierarchy)
			{
				hint.PopupTooltip(pet, "TutorPet".Translate(), TipPosition.BottomLeftCorner);
				PetChoosePanel petPanel = GameManager.Instance.CurrentPet.PetPanel;
				petPanel.OnOpen = (Action)Delegate.Combine(petPanel.OnOpen, new Action(close_stage_pet));
			}
		}
	}

	private void close_stage_pet()
	{
		PetChoosePanel petPanel = GameManager.Instance.CurrentPet.PetPanel;
		petPanel.OnOpen = (Action)Delegate.Remove(petPanel.OnOpen, new Action(close_stage_pet));
		stage++;
		hint.Close();
	}

	private void stage_help()
	{
		stage++;
		if (help.activeInHierarchy)
		{
			hint.PopupTooltip(help, "TutorHelp".Translate(), TipPosition.TopLeftCorner);
			delay(5f, close_stage_help);
		}
	}

	private void close_stage_help()
	{
		PetChoosePanel petPanel = GameManager.Instance.CurrentPet.PetPanel;
		petPanel.OnOpen = (Action)Delegate.Remove(petPanel.OnOpen, new Action(close_stage_pet));
		hint.Close();
		off();
	}

	private void skipAll()
	{
		buildings.SetTrigger("on");
		top_panel.SetTrigger("on");
		GameObject[] array = menu_pages;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		Animator[] array2 = pages;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].SetTrigger("on");
		}
		Orb obj = GameManager.Instance.Orb;
		obj.OnClick = (Action<float>)Delegate.Remove(obj.OnClick, new Action<float>(close_stage_click));
		PetChoosePanel petPanel = GameManager.Instance.CurrentPet.PetPanel;
		petPanel.OnOpen = (Action)Delegate.Remove(petPanel.OnOpen, new Action(close_stage_pet));
		VariableLong clickableCollect = Statistic.ClickableCollect;
		clickableCollect.OnChange = (Action)Delegate.Remove(clickableCollect.OnChange, new Action(close_stage_void_entity));
		BonusSpawner bonusSpawner = GameManager.Instance.BonusSpawner;
		bonusSpawner.OnLifeTimeEnd = (Action)Delegate.Remove(bonusSpawner.OnLifeTimeEnd, new Action(re_stage_void_entity));
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		scrolls.OnOpen = (Action)Delegate.Remove(scrolls.OnOpen, new Action(close_stage_spells));
		Idle idle = GameManager.Instance.Idle;
		idle.OnIdle = (Action)Delegate.Remove(idle.OnIdle, new Action(close_stage_idle));
		VariableInt boughtUpgrades = Statistic.BoughtUpgrades;
		boughtUpgrades.OnChange = (Action)Delegate.Remove(boughtUpgrades.OnChange, new Action(close_stage_upgrades));
		VariableInt totalBuildings = Statistic.TotalBuildings;
		totalBuildings.OnChange = (Action)Delegate.Remove(totalBuildings.OnChange, new Action(close_stage_manasources));
		VariableInt scrollCount = GameManager.Instance.Scrolls.ScrollCount;
		scrollCount.OnChange = (Action)Delegate.Remove(scrollCount.OnChange, new Action(on_spell_unlock));
		VoidMana voidManaManager = GameManager.Instance.VoidManaManager;
		voidManaManager.OnSpawn = (Action)Delegate.Remove(voidManaManager.OnSpawn, new Action(on_voidmana_spawn));
		VariableBignumber variableBignumber = GameManager.Instance.VoidMana;
		variableBignumber.OnChange = (Action)Delegate.Remove(variableBignumber.OnChange, new Action(on_voidmana_earn));
		VariableInt level = GameManager.Instance.CurrentHero.Level;
		level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(on_pet));
		hint.Close();
	}

	private void off()
	{
		bool num = isActive;
		isActive = false;
		StopAllCoroutines();
		base.gameObject.SetActive(value: false);
		base.enabled = false;
		if (num)
		{
			GameManager.Instance.Tasks.Activate();
		}
	}

	private IEnumerator delay(float dt, Action action)
	{
		yield return new WaitForSecondsRealtime(dt);
		action();
	}
}
