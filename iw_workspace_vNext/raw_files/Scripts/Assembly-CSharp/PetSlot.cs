using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetSlot : MonoBehaviour
{
	public Pet Pet;

	public PetNames DefaultPet;

	public VariableComplex ExpBonus;

	public VariableComplex AbilityPower;

	public VariableComplex AbilityPowerT1;

	public VariableComplex AbilityPowerT2;

	public TextMeshProUGUI LevelLabel;

	public Image ExpBar;

	public TextMeshProUGUI Tip;

	public PetPortraitFrame Portrait;

	public VariableInt Level;

	public PetChoosePanel PetPanel;

	public VariableLong PlayedTime;

	public VariableBignumber SkipedPlayedTime;

	public VariableInt StartingLevel;

	public VariableFloat ChargeAbilityBoost;

	public GameObject Ability;

	public Image AbilityFiller;

	[SerializeField]
	private BottomPanelButton sign;

	public Action OnSelect;

	public Action<BigNumber> OnGetExp;

	public CounterAverageBigNumber ExpCounter;

	public GameObject PetFrame;

	public bool tips_active;

	private Timer timer;

	private float tip_update_time = 0.25f;

	public Action OnMirror;

	public bool AvailableToChange { get; private set; }

	public void Init()
	{
		ExpBonus = new VariableComplex(1.0);
		AbilityPower = new VariableComplex(1.0);
		AbilityPowerT1 = new VariableComplex(1.0);
		AbilityPowerT2 = new VariableComplex(1.0);
		Level = new VariableInt(1);
		PlayedTime = new VariableLong(0uL);
		SkipedPlayedTime = new VariableBignumber(0.0);
		StartingLevel = new VariableInt(0);
		ChargeAbilityBoost = new VariableFloat(1f);
		ExpCounter = new CounterAverageBigNumber();
		DefaultPet = PetNames.None;
		GameContext.ContextAddResource(ResourceType.Pet.ToString() + "." + ResourcePet.BonusExp, ExpBonus);
		GameContext.ContextAddResource(ResourceType.Pet.ToString() + "." + ResourcePet.AbilityPower, AbilityPower);
		GameContext.ContextAddResource(ResourceType.Pet.ToString() + "." + ResourcePet.AbilityPowerT1, AbilityPowerT1);
		GameContext.ContextAddResource(ResourceType.Pet.ToString() + "." + ResourcePet.AbilityPowerT2, AbilityPowerT2);
		GameContext.ContextAddResource(ResourceType.Pet.ToString() + "." + ResourcePet.Time, PlayedTime);
		GameContext.ContextAddResource(ResourceType.Pet.ToString() + "." + ResourcePet.StartingLvl, StartingLevel);
		GameContext.ContextAddResource(ResourceType.Pet.ToString() + "." + ResourcePet.Level, Level);
		GameContext.ContextAddResource(ResourceType.Pet.ToString() + "." + ResourcePet.ChargeAbility, ChargeAbilityBoost);
		VariableInt startingLevel = StartingLevel;
		startingLevel.OnChange = (Action)Delegate.Combine(startingLevel.OnChange, new Action(RecalcLevel));
		Unblock();
		PetPanel.InitSecond();
	}

	private void RecalcLevel()
	{
		if (Pet != null)
		{
			Pet.RecalculateLevel();
		}
	}

	public void PanelInit()
	{
		PetPanel.Init();
	}

	public bool SecondPetIsAcitve()
	{
		if (GameManager.Instance.Ascension.IsAvailable)
		{
			return PetPanel.SecondIsActive;
		}
		return false;
	}

	public void ActivatePet(Pet pet, Sprite sprite, PetNames nameKey)
	{
		if ((nameKey == PetNames.Assistant && GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Demonologist) || Pet == pet)
		{
			return;
		}
		if (Pet != null)
		{
			if (Pet.Name == pet.Name)
			{
				Debug.Log("2 pet");
			}
			Pet.DisableAll();
			GameManager.Instance.AddProfit(0f);
		}
		else
		{
			Portrait.gameObject.SetActive(value: false);
		}
		GameManager.Instance.Idle.Restart();
		GameManager.Instance.AddProfit(0f);
		PlayedTime.SetValue(1uL);
		SkipedPlayedTime.SetValue(0.0);
		Level.SetValue(1);
		Pet = pet;
		Pet.Level = Level;
		Pet.RecalculateLevel(0.0);
		Pet.NameKey = nameKey;
		Pet.ApplyEffects();
		Portrait.UpdateFrame();
		ExpCounter.Reset();
		if (OnSelect != null)
		{
			OnSelect();
		}
	}

	public void Restart()
	{
		if (Pet != null)
		{
			PlayedTime.SetValue(0uL);
			Pet.DisableAll();
			Pet = null;
			Portrait.gameObject.SetActive(value: false);
		}
		ExpBar.fillAmount = 0f;
		LevelLabel.text = "";
		Level.SetValue(1);
		PetPanel.Restart();
		if (DefaultPet != PetNames.None)
		{
			SetPet(DefaultPet);
		}
	}

	public void PostLoad()
	{
		foreach (PetChoose pet in PetPanel.Pets)
		{
			pet.CheckUnlock();
		}
	}

	private void Update()
	{
		if (Pet != null)
		{
			Pet.update();
			ExpBar.fillAmount = (float)(Pet.Experience.Value / Pet.Exp2LevelUp).ToDouble();
			LevelLabel.text = Pet.Level.ValueInt.ToString();
		}
	}

	public void ShowTip(bool tutor = false)
	{
		Tip.transform.parent.position = base.transform.position + new Vector3(-0.5f, -0.8f, 0f);
		Tip.transform.parent.gameObject.SetActive(value: true);
		timer = GameManager.Instance.Timers.GetPeriodical(180f, tip_update_time, update_tip, delegate
		{
			HideTip();
		}, ignore_scale: true);
		update_tip();
		tips_active = true;
	}

	public void HideTip(bool tutor = false)
	{
		if (timer != null)
		{
			timer.Stop();
		}
		Tip.transform.parent.gameObject.SetActive(value: false);
		if (Pet != null)
		{
			Pet.OnHideTip();
		}
		Tip.text = "";
		tips_active = false;
	}

	private void update_tip()
	{
		if (Pet != null)
		{
			Tip.text = Pet.Tips_text();
		}
		else
		{
			Tip.text = "PetSlotTooltip".Translate() + "\n";
		}
	}

	public void ShowProgressTip()
	{
		timer = GameManager.Instance.Timers.GetPeriodical(10f, tip_update_time, update_progress_tip, HideProgressTip, ignore_scale: true);
		update_progress_tip();
		Tip.transform.parent.gameObject.SetActive(value: true);
	}

	public void HideProgressTip()
	{
		if (timer != null)
		{
			timer.Stop();
		}
		Tip.text = "";
		Tip.transform.parent.gameObject.SetActive(value: false);
	}

	private void update_progress_tip()
	{
		if (Pet != null)
		{
			Tip.text = Pet.Progress_Text();
		}
	}

	public void SetPet(PetNames key)
	{
		PetChoose pet = PetPanel.GetPet(key);
		if (pet != null)
		{
			ActivatePet(pet.Pet, pet.Portrait, pet.PetName);
			return;
		}
		if (Pet != null)
		{
			Pet.DisableAll();
		}
		Pet = null;
		Portrait.gameObject.SetActive(value: false);
		LevelLabel.text = "";
		ExpBar.fillAmount = 0f;
	}

	public void Block()
	{
		AvailableToChange = false;
		PetPanel.Close();
	}

	public void Unblock()
	{
		AvailableToChange = true;
	}

	public bool IsAvailable()
	{
		if (GameManager.Instance.ChallengeManager.IsChangePet())
		{
			return GameManager.Instance.Realmcraft.IsChangePet();
		}
		return false;
	}

	public void PetSlotClick()
	{
		PetPanel.isFirstSlot = true;
		if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Pet != null)
		{
			if (!GameManager.Instance.CurrentPet.AvailableToChange)
			{
				if (GameManager.Instance.CurrentPet.Pet == null)
				{
					return;
				}
				if (GameManager.Instance.CurrentPet.Pet.NameKey == PetNames.Assistant)
				{
					Assistant assistant = GameManager.Instance.CurrentPet.Pet as Assistant;
					if (assistant.expandedTip)
					{
						assistant.Evolve();
						return;
					}
				}
				if (GameManager.Instance.CurrentPet.Pet.NameKey == PetNames.Assistant)
				{
					(GameManager.Instance.CurrentPet.Pet as Assistant).OnExtendedTip();
					return;
				}
				if (GameManager.Instance.CurrentPet.Pet.NameKey == PetNames.PsychicCacophony)
				{
					(GameManager.Instance.CurrentPet.Pet as PsychicCacophony).OnExtendedTip();
					return;
				}
			}
			PetChoose petChoose = PetPanel.Pets.Find((PetChoose x) => x.PetName == Pet.NameKey);
			if (petChoose.Evolution != null && petChoose.Evolution.Check(isFirstSlot: true))
			{
				petChoose.Evolution.Choose();
				return;
			}
		}
		PetPanel.Open();
	}

	public void Mirror()
	{
		Settings.MirrorPet = !Settings.MirrorPet;
		if (OnMirror != null)
		{
			OnMirror();
		}
	}
}
