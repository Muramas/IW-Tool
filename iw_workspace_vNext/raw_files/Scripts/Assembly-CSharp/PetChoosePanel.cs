using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetChoosePanel : MonoBehaviour
{
	public List<PetChoose> Pets;

	public Dictionary<PetNames, Pet> PetMap;

	public DisableCanvas canvas;

	public RectTransform panel_out_frame;

	public Image Icon;

	public TextMeshProUGUI Name;

	public TextMeshProUGUI Requirements;

	public TextMeshProUGUI Description;

	public GameObject Arrow;

	public GameObject Panel;

	public PetChoose SelectedPet;

	public Button ChooseButton;

	[SerializeField]
	private PetGallery gallery;

	public Action OnOpen;

	public bool isFirstSlot = true;

	public PetSlot mainSlot;

	public PetSecondSlot secondSlot;

	[SerializeField]
	private GameObject Body;

	[SerializeField]
	private Button DescrButton;

	[SerializeField]
	private Button SkinsButton;

	[SerializeField]
	private GameObject Descr;

	[SerializeField]
	private GameObject Skins;

	public bool SecondIsActive { get; private set; }

	public void Init()
	{
		PetMap = new Dictionary<PetNames, Pet>
		{
			{
				PetNames.Pixie,
				new Pixie()
			},
			{
				PetNames.ZombieWarrior,
				new ZombieWarrior()
			},
			{
				PetNames.Homunculus,
				new Homunculus()
			},
			{
				PetNames.Daemon,
				new Daemon()
			},
			{
				PetNames.Golem,
				new Golem()
			},
			{
				PetNames.Spellhound,
				new Spellhound()
			},
			{
				PetNames.Voidfiend,
				new Voidfiend()
			},
			{
				PetNames.Devourer,
				new Devourer()
			},
			{
				PetNames.HolySpirit,
				new HolySpirit()
			},
			{
				PetNames.Geode,
				new Geode()
			},
			{
				PetNames.ShadowStalker,
				new ShadowStalker()
			},
			{
				PetNames.Ent,
				new Ent()
			},
			{
				PetNames.RisenGiant,
				new RisenGiant()
			},
			{
				PetNames.Simulacrum,
				new Simulacrum()
			},
			{
				PetNames.PitLord,
				new PitLord()
			},
			{
				PetNames.AnimaConstruct,
				new AnimaConstruct()
			},
			{
				PetNames.Arcanaworg,
				new Arcanaworg()
			},
			{
				PetNames.Voidterror,
				new Voidterror()
			},
			{
				PetNames.Hungerer,
				new Hungerer()
			},
			{
				PetNames.Archivist,
				new Archivist()
			},
			{
				PetNames.LeyKeeper,
				new LeyKeeper()
			},
			{
				PetNames.Ebonsand,
				new Ebonsand()
			},
			{
				PetNames.HeraldOfRot,
				new HeraldOfRot()
			},
			{
				PetNames.LivingSin,
				new LivingSin()
			},
			{
				PetNames.MechanosApexis,
				new MechanosApexis()
			},
			{
				PetNames.VoidlightAmalgam,
				new VoidlightAmalgam()
			},
			{
				PetNames.NixInstability,
				new NixInstability()
			},
			{
				PetNames.GreaterChimaera,
				new GreaterChimaera()
			},
			{
				PetNames.Doppelganger,
				new Doppelganger()
			},
			{
				PetNames.Assistant,
				new Assistant()
			},
			{
				PetNames.PsychicCacophony,
				new PsychicCacophony()
			}
		};
		foreach (PetChoose pet in Pets)
		{
			pet.Init();
		}
		SecondIsActive = false;
	}

	public void InitSecond()
	{
		secondSlot.Init();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Close();
		}
	}

	public void Restart()
	{
		foreach (PetChoose pet in Pets)
		{
			pet.Restart();
		}
	}

	public void Open(bool firstSlot = true)
	{
		isFirstSlot = firstSlot;
		SelectedPet = null;
		Pet slot;
		if (firstSlot)
		{
			slot = mainSlot.Pet;
		}
		else
		{
			slot = secondSlot.Pet;
		}
		if (slot != null)
		{
			Pets.Find((PetChoose x) => x.Pet.NameKey == slot.NameKey).Select();
		}
		else
		{
			SelectPet(null);
		}
		Panel.SetActive(value: true);
		if (Descr.activeSelf)
		{
			OpenDescription();
		}
		else
		{
			OpenSkins();
		}
		canvas.On();
		foreach (PetChoose pet in Pets)
		{
			pet.CheckUnlock();
		}
		if (OnOpen != null)
		{
			OnOpen();
		}
	}

	public void OpenDescription()
	{
		SkinsButton.interactable = true;
		DescrButton.interactable = false;
		UpdateDescription();
		Skins.SetActive(value: false);
		Descr.SetActive(value: true);
	}

	public void OpenSkins()
	{
		SkinsButton.interactable = false;
		DescrButton.interactable = true;
		if (SelectedPet != null)
		{
			GameManager.Instance.PetGallery.Open(SelectedPet.Pet.NameKey);
		}
		Skins.SetActive(value: true);
		Descr.SetActive(value: false);
	}

	public void SSExpand()
	{
		(GameManager.Instance.CurrentPet.Pet as Assistant).OnExtendedTip();
	}

	public void Close()
	{
		Panel.SetActive(value: false);
		canvas.Off();
		SelectedPet = null;
		gallery.Preview.gameObject.SetActive(value: false);
	}

	public void SetArrow(Transform transf)
	{
		Arrow.transform.position = transf.position + new Vector3(0f, 0.4375f, 0f);
	}

	public void SelectPet(PetChoose petCh)
	{
		if (petCh != null)
		{
			panel_out_frame.sizeDelta = new Vector2(800f, 600f);
			SetArrow(petCh.transform);
			Arrow.SetActive(value: true);
			Name.text = petCh.Pet.Name;
			Icon.sprite = petCh.Portrait;
			Icon.gameObject.SetActive(value: true);
			Body.SetActive(value: true);
			GameManager.Instance.PetGallery.Open(petCh.Pet.NameKey);
		}
		else
		{
			panel_out_frame.sizeDelta = new Vector2(800f, 300f);
			Arrow.SetActive(value: false);
			Icon.gameObject.SetActive(value: false);
			Body.SetActive(value: false);
			GameManager.Instance.PetGallery.Close();
		}
		SelectedPet = petCh;
		UpdateReqs();
		UpdateDescription();
		PetSlot currentPet = GameManager.Instance.CurrentPet;
		if (!currentPet.IsAvailable() || SelectedPet == null || currentPet.PetPanel.GetKey(isFirst: true) == SelectedPet.PetName || currentPet.PetPanel.GetKey(isFirst: false) == SelectedPet.PetName || !SelectedPet.CanBeChoosen || !currentPet.AvailableToChange)
		{
			if (currentPet.IsAvailable() && SelectedPet != null && currentPet.Pet != null && SelectedPet.PetName == PetNames.PsychicCacophony && currentPet.Pet.NameKey == PetNames.Assistant && currentPet.Level.ValueInt >= 325 && !CheckFamily(SelectedPet.Pet))
			{
				ChooseButton.gameObject.SetActive(value: true);
				ChooseButton.interactable = GameManager.Instance.Paragon.PetT3IsAvailable;
			}
			else
			{
				ChooseButton.gameObject.SetActive(value: false);
			}
		}
		else
		{
			ChooseButton.gameObject.SetActive(!CheckFamily(SelectedPet.Pet));
			ChooseButton.interactable = SelectedPet.Check(isFirstSlot);
		}
	}

	private void UpdateDescription()
	{
		if (SelectedPet == null)
		{
			Description.text = string.Empty;
		}
		else
		{
			Description.text = SelectedPet.Pet.GetDescription();
		}
	}

	private void UpdateReqs()
	{
		PetChoose selectedPet = SelectedPet;
		if (selectedPet == null)
		{
			Requirements.text = string.Empty;
		}
		else if (selectedPet.CanBeChoosen)
		{
			Requirements.text = selectedPet.Pet.GetRequirements();
		}
		else if (selectedPet.Pet.NameKey == PetNames.Doppelganger)
		{
			Requirements.text = "DoppelRequirement".Translate();
		}
		else if (selectedPet.Pet.NameKey == PetNames.Assistant || selectedPet.Pet.NameKey == PetNames.PsychicCacophony)
		{
			Requirements.text = "SoulStealerRequirement".Translate();
		}
		else
		{
			Requirements.text = string.Empty;
		}
	}

	public bool CheckFamily(Pet pet)
	{
		return pet.Family == GetFamily(!isFirstSlot);
	}

	public void Choose()
	{
		SelectedPet.Choose();
	}

	public void ActivatePet(Pet pet, Sprite sprite)
	{
		PetNames petNames = PetNames.None;
		if (pet != null)
		{
			petNames = pet.NameKey;
		}
		if ((petNames == PetNames.Assistant && GameManager.Instance.CurrentHero.Hero.NameKey == HeroesNames.Demonologist) || (pet != null && (mainSlot.Pet == pet || secondSlot.Pet == pet) && CheckFamily(pet)))
		{
			return;
		}
		Pet pet2;
		if (isFirstSlot)
		{
			pet2 = mainSlot.Pet;
			if (pet2 != null)
			{
				if (pet2.Name == pet.Name)
				{
					return;
				}
				pet2.DisableAll();
				GameManager.Instance.AddProfit(0f);
			}
			else
			{
				mainSlot.Portrait.gameObject.SetActive(value: false);
			}
			GameManager.Instance.Idle.Restart();
			GameManager.Instance.AddProfit(0f);
			mainSlot.PlayedTime.SetValue(1uL);
			mainSlot.SkipedPlayedTime.SetValue(0.0);
			mainSlot.Level.SetValue(1);
			mainSlot.Pet = pet;
			mainSlot.Pet.Level = mainSlot.Level;
			pet2 = mainSlot.Pet;
			mainSlot.Pet.RecalculateLevel(0.0);
			mainSlot.Pet.ApplyEffects();
			mainSlot.Portrait.UpdateFrame();
			if (mainSlot.OnSelect != null)
			{
				mainSlot.OnSelect();
			}
			return;
		}
		pet2 = secondSlot.Pet;
		if (pet2 != null)
		{
			if (pet2.Name == pet.Name)
			{
				return;
			}
			pet2.DisableAll();
			GameManager.Instance.AddProfit(0f);
		}
		else
		{
			secondSlot.Portrait.gameObject.SetActive(value: false);
		}
		GameManager.Instance.Idle.Restart();
		GameManager.Instance.AddProfit(0f);
		secondSlot.Level.SetValue(1);
		secondSlot.Pet = pet;
		secondSlot.Pet.Level = secondSlot.Level;
		secondSlot.PlayedTime.SetValue(1uL);
		secondSlot.SkipedPlayedTime.SetValue(0.0);
		pet2 = secondSlot.Pet;
		secondSlot.Pet.RecalculateLevel(0.0);
		secondSlot.Pet.ApplyEffects();
		secondSlot.Portrait.UpdateFrame();
	}

	public PetChoose GetPet(PetNames key)
	{
		PetChoose result = null;
		if (key != PetNames.None)
		{
			for (int i = 0; i < Pets.Count; i++)
			{
				if (Pets[i].Pet.NameKey == key)
				{
					result = Pets[i];
					break;
				}
			}
		}
		return result;
	}

	public Pet GetCurrentPet()
	{
		if (isFirstSlot)
		{
			return mainSlot.Pet;
		}
		return secondSlot.Pet;
	}

	public PetNames GetKey(bool isFirst)
	{
		if (isFirst)
		{
			if (mainSlot.Pet != null)
			{
				return mainSlot.Pet.NameKey;
			}
		}
		else if (secondSlot.Pet != null)
		{
			return secondSlot.Pet.NameKey;
		}
		return PetNames.None;
	}

	public PetFamily GetFamily(bool isFirst)
	{
		if (isFirst)
		{
			if (mainSlot.Pet != null)
			{
				return mainSlot.Pet.Family;
			}
		}
		else if (secondSlot.Pet != null)
		{
			return secondSlot.Pet.Family;
		}
		return PetFamily.None;
	}

	public void EnableSecond()
	{
		secondSlot.gameObject.SetActive(value: true);
		secondSlot.Restart();
		if (mainSlot.Pet != null)
		{
			mainSlot.Pet.UpdateEffect();
		}
		SecondIsActive = true;
	}

	public void DisableSecond()
	{
		secondSlot.gameObject.SetActive(value: false);
		secondSlot.Restart();
		SecondIsActive = false;
	}

	public Vector3 GetSlotPosition(Pet pet)
	{
		if (mainSlot.Pet == pet)
		{
			return mainSlot.transform.position;
		}
		return secondSlot.transform.position;
	}

	public void SetStateAbility(Pet pet, bool state)
	{
		if (mainSlot.Pet == pet)
		{
			mainSlot.Ability.SetActive(state);
			mainSlot.AbilityFiller.fillAmount = 0f;
		}
		else if (secondSlot.Pet == pet)
		{
			secondSlot.Ability.SetActive(state);
			secondSlot.AbilityFiller.fillAmount = 0f;
		}
	}

	public void SetAbilityProgress(Pet pet, float progress)
	{
		if (mainSlot.Pet == pet)
		{
			mainSlot.AbilityFiller.fillAmount = progress;
		}
		else if (secondSlot.Pet == pet)
		{
			secondSlot.AbilityFiller.fillAmount = progress;
		}
	}

	public void SetAbilitColor(Pet pet, Color color)
	{
		if (mainSlot.Pet == pet)
		{
			mainSlot.AbilityFiller.color = color;
		}
		else if (secondSlot.Pet == pet)
		{
			secondSlot.AbilityFiller.color = color;
		}
	}
}
