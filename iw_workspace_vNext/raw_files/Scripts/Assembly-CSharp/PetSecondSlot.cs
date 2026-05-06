using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetSecondSlot : MonoBehaviour
{
	public Pet Pet;

	public TextMeshProUGUI LevelLabel;

	public Image ExpBar;

	public TextMeshProUGUI Tip;

	public PetPortraitFrame Portrait;

	public VariableInt Level;

	public PetChoosePanel PetPanel;

	public GameObject Ability;

	public Image AbilityFiller;

	public Action OnSelect;

	public VariableLong PlayedTime;

	public VariableBignumber SkipedPlayedTime;

	public bool tips_active;

	private Timer timer;

	private float tip_update_time = 0.25f;

	public Action OnMirror;

	public void Init()
	{
		Level = new VariableInt(1);
		PlayedTime = new VariableLong(1uL);
		SkipedPlayedTime = new VariableBignumber(0.0);
		VariableInt startingLevel = GameManager.Instance.CurrentPet.StartingLevel;
		startingLevel.OnChange = (Action)Delegate.Combine(startingLevel.OnChange, new Action(RecalcLevel));
	}

	private void RecalcLevel()
	{
		if (Pet != null)
		{
			Pet.RecalculateLevel();
		}
	}

	private void OnEnable()
	{
		if (Pet != null)
		{
			Portrait.UpdateFrame();
		}
		else
		{
			Portrait.gameObject.SetActive(value: false);
		}
	}

	private void OnDisable()
	{
		Portrait.gameObject.SetActive(value: false);
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
		Level.SetValue(1);
		Pet = pet;
		Pet.Level = Level;
		Pet.RecalculateLevel(0.0);
		Pet.NameKey = nameKey;
		Pet.ApplyEffects();
		Portrait.UpdateFrame();
		if (OnSelect != null)
		{
			OnSelect();
		}
	}

	public void Restart()
	{
		if (Pet != null)
		{
			Pet.DisableAll();
			Pet = null;
			Portrait.gameObject.SetActive(value: false);
		}
		ExpBar.fillAmount = 0f;
		LevelLabel.text = "";
		Level.SetValue(1);
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
		Tip.transform.parent.position = base.transform.position + new Vector3(0f, -1f, 0f);
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
		Tip.text = GetDescription();
	}

	private string GetDescription()
	{
		if (Pet == null)
		{
			return "Click to choose a pet.\n";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Pet.Tips_text());
		stringBuilder.AppendLine();
		stringBuilder.Append("Real played time: ");
		stringBuilder.AppendLine(Statistic.time_to_string(Pet.PlayedTime.Value));
		stringBuilder.Append("Game played time: ");
		stringBuilder.AppendLine(Statistic.time_to_string(Pet.PlayedTime.Value + Pet.SkipedPlayedTime.Value));
		return stringBuilder.ToString();
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

	public void PetSlotClick()
	{
		PetPanel.isFirstSlot = false;
		if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Pet != null)
		{
			PetChoose petChoose = PetPanel.Pets.Find((PetChoose x) => x.PetName == Pet.NameKey);
			if (petChoose.Evolution != null && petChoose.Evolution.Check(isFirstSlot: false))
			{
				petChoose.Evolution.Choose();
				return;
			}
		}
		PetPanel.Open(firstSlot: false);
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
