using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PetChoose : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public PetNames PetName;

	public Pet Pet;

	public Sprite Portrait;

	public Image Frame;

	public Image FrameIcon;

	public PetChoose Evolution;

	public Achievement UnlockAchiev;

	public bool CanBeChoosen = true;

	private bool activate;

	public void CheckUnlock()
	{
		if (!Pet.Unlocked)
		{
			if (UnlockAchiev == null)
			{
				AchievementCategory achievementCategory = GameManager.Instance.AchievManager.AchievList.Find((AchievementCategory x) => x.Key.ToString() == PetName.ToString());
				if (achievementCategory != null)
				{
					UnlockAchiev = achievementCategory.Row[0];
				}
			}
			if (UnlockAchiev != null && UnlockAchiev.Unlocked)
			{
				Pet.Unlocked = true;
			}
			else if (PetName == PetNames.Doppelganger || PetName == PetNames.Assistant || PetName == PetNames.PsychicCacophony)
			{
				Pet.Unlocked = true;
			}
		}
		UpdateFrameColor();
	}

	public bool Check(bool isFirstSlot)
	{
		if ((Pet.Unlocked || Pet.CheckConditions()) && (Pet.Tier != 2 || GameManager.Instance.Paragon.PetT2IsAvailable) && (Pet.Tier != 3 || GameManager.Instance.Paragon.PetT3IsAvailable) && Pet.CheckReqs() && !GameManager.Instance.CurrentPet.PetPanel.CheckFamily(Pet))
		{
			if (!isFirstSlot)
			{
				return Pet.Tier < 3;
			}
			return true;
		}
		return false;
	}

	public void Init()
	{
		Pet = GameManager.Instance.CurrentPet.PetPanel.PetMap[PetName];
		Frame.gameObject.SetActive(value: false);
		Pet.Init();
	}

	public void Select()
	{
		GameManager.Instance.CurrentPet.PetPanel.SelectPet(this);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			Select();
		}
		else if (eventData.button == PointerEventData.InputButton.Right)
		{
			Select();
			if (CanBeChoosen && IsCanBeChoosen() && Check(GameManager.Instance.CurrentPet.PetPanel.isFirstSlot))
			{
				Choose();
			}
		}
	}

	public void Update()
	{
		if (Pet == null)
		{
			return;
		}
		if (GameManager.Instance.CurrentPet.PetPanel.GetKey(isFirst: true) == Pet.NameKey || GameManager.Instance.CurrentPet.PetPanel.GetKey(isFirst: false) == Pet.NameKey)
		{
			if (!activate)
			{
				Frame.gameObject.SetActive(value: true);
				base.transform.localScale = Vector3.one * 1.2f;
				activate = true;
			}
		}
		else if (activate)
		{
			Frame.gameObject.SetActive(value: false);
			base.transform.localScale = Vector3.one;
			activate = false;
		}
	}

	public void Choose()
	{
		PetSlot currentPet = GameManager.Instance.CurrentPet;
		if (!currentPet.IsAvailable() || (!currentPet.AvailableToChange && (currentPet.Pet == null || currentPet.Pet.NameKey != PetNames.Assistant)))
		{
			return;
		}
		if (currentPet.Pet == null || !Settings.ConfirmMessageChoose)
		{
			choose();
			return;
		}
		GameManager.Instance.ConfirmWindow.Open("Changing the active pet will reset the level of current one. Are you sure?", delegate
		{
			choose();
		});
	}

	private void choose()
	{
		PetSlot currentPet = GameManager.Instance.CurrentPet;
		if ((CanBeChoosen || (PetName == PetNames.PsychicCacophony && Pet.CheckReqs())) && currentPet.IsAvailable())
		{
			if (!Pet.Unlocked)
			{
				Pet.Unlocked = true;
				UnlockAchiev.Unlock();
				UpdateFrameColor();
			}
			SetPet();
			if (Settings.AutoClose)
			{
				currentPet.PetPanel.Close();
			}
			else
			{
				currentPet.PetPanel.SetArrow(base.transform);
			}
		}
	}

	public void SetPet()
	{
		GameManager.Instance.CurrentPet.PetPanel.ActivatePet(Pet, Portrait);
	}

	public void Restart()
	{
		if (Pet != null)
		{
			if (PetName == PetNames.Doppelganger || PetName == PetNames.Assistant || PetName == PetNames.PsychicCacophony || PetName == PetNames.AntiDoppel)
			{
				Pet.Unlocked = true;
			}
			else
			{
				Pet.Unlocked = false;
			}
			Pet.Restart();
			CheckUnlock();
		}
	}

	public bool IsCanBeChoosen()
	{
		PetSlot currentPet = GameManager.Instance.CurrentPet;
		if (!currentPet.IsAvailable())
		{
			return false;
		}
		PetChoosePanel petPanel = currentPet.PetPanel;
		if (petPanel.GetKey(isFirst: true) == PetName || petPanel.GetKey(isFirst: false) == PetName || !CanBeChoosen || !currentPet.AvailableToChange)
		{
			if (currentPet.Pet != null && PetName == PetNames.PsychicCacophony && Pet.CheckReqs() && !petPanel.CheckFamily(Pet))
			{
				return true;
			}
			return false;
		}
		return !petPanel.CheckFamily(Pet);
	}

	private void UpdateFrameColor()
	{
		bool flag = Pet.Unlocked;
		if (!GameManager.Instance.CurrentPet.PetPanel.isFirstSlot)
		{
			flag = flag && !GameManager.Instance.CurrentPet.PetPanel.CheckFamily(Pet) && (GameManager.Instance.CurrentPet.PetPanel.isFirstSlot || Pet.Tier < 3) && CanBeChoosen;
		}
		FrameIcon.color = (flag ? Color.white : Color.grey);
	}
}
