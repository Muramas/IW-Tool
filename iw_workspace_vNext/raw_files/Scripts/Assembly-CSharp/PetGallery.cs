using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PetGallery : MonoBehaviour
{
	public class PortraitList
	{
		public string Active;

		public List<PetPortrait> List;

		public PortraitList()
		{
			Active = "0";
			List = new List<PetPortrait>();
		}

		public GameObject GetFrame()
		{
			return GetPortrait().Frame;
		}

		public PetPortrait GetPortrait()
		{
			string[] array = Active.Split('#');
			PetPortrait petPortrait;
			if (array.Length == 1)
			{
				int id = int.Parse(array[0]);
				petPortrait = List.Find((PetPortrait x) => x.ID == id);
			}
			else
			{
				PetNames key = (PetNames)int.Parse(array[0]);
				int id2 = int.Parse(array[1]);
				petPortrait = List.Find((PetPortrait x) => x.Key == key && x.ID == id2);
			}
			if (petPortrait == null)
			{
				petPortrait = List[0];
				int key2 = (int)petPortrait.Key;
				Active = key2 + "#" + petPortrait.ID;
			}
			return petPortrait;
		}
	}

	public SpriteAtlas Atlas;

	public Dictionary<PetNames, PortraitList> Pets;

	[SerializeField]
	private PetGalleryBlock blockPrefab;

	public List<PetGalleryBlock> Blocks;

	public GalleryFrame Frame;

	public TextMeshProUGUI Label;

	public TextMeshProUGUI Coins;

	public GalleryPreview Preview;

	public VariableInt Unlocked;

	public PetNames OpenedKey;

	public void Init()
	{
		Unlocked = new VariableInt(0);
		GameContext.ContextAddResource("Gallery.Pet", Unlocked);
		List<PortraitFormat> petSkins = GlobalData.PetSkins;
		Pets = new Dictionary<PetNames, PortraitList>();
		PortraitCreator portraitCreator = new PortraitCreator();
		List<PetPortrait> list = new List<PetPortrait>();
		for (int i = 0; i < petSkins.Count; i++)
		{
			PetPortrait item = portraitCreator.CreatePetPortait(petSkins[i]);
			list.Add(item);
		}
		foreach (PetPortrait item2 in list)
		{
			foreach (PetNames pet2 in item2.GetPets())
			{
				if (!Pets.ContainsKey(pet2))
				{
					Pets.Add(pet2, new PortraitList());
				}
				Pets[pet2].List.Add(item2);
			}
		}
		foreach (KeyValuePair<PetNames, PortraitList> pet in Pets)
		{
			pet.Value.List.Sort(CompareItemByCost);
			if (Blocks.Find((PetGalleryBlock x) => x.Key == pet.Key) == null)
			{
				PetGalleryBlock petGalleryBlock = Object.Instantiate(blockPrefab, base.transform);
				petGalleryBlock.Key = pet.Key;
				Blocks.Add(petGalleryBlock);
			}
		}
		for (int num = 0; num < Blocks.Count; num++)
		{
			PetNames key = Blocks[num].Key;
			for (int num2 = 0; num2 < Pets[key].List.Count; num2++)
			{
				Blocks[num].Add(Pets[key].List[num2], Frame);
			}
			Activate(key, Pets[key].Active);
		}
		Preview.SetChoose(Activate, Unlock);
	}

	public void Update()
	{
		Coins.text = GameManager.Instance.AlterationSand.ValueInt.ToString();
	}

	public void Open(PetNames key)
	{
		if (!GameManager.Instance.CurrentPet.PetPanel.PetMap.ContainsKey(key))
		{
			key = PetNames.Pixie;
		}
		OpenedKey = key;
		if (key != PetNames.None)
		{
			Label.text = GameManager.Instance.CurrentPet.PetPanel.PetMap[key].Name;
		}
		else
		{
			Label.text = string.Empty;
		}
		for (int i = 0; i < Blocks.Count; i++)
		{
			Blocks[i].Open(Blocks[i].Key == key);
		}
		base.gameObject.SetActive(value: true);
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}

	public void Unlock(int key, int id)
	{
		PetNames k = (PetNames)key;
		Pets[k].List.Find((PetPortrait x) => x.ID == id && x.Key == k).Unlock();
		Unlocked.Change(1);
	}

	private string GetActiveID(PetNames key)
	{
		return Pets[key].Active;
	}

	private void SetActiveID(PetNames key, string id)
	{
		Pets[key].Active = id;
		GalleryBlock galleryBlock = Blocks.Find((PetGalleryBlock x) => x.Key == key);
		PetPortrait portrait = Pets[key].GetPortrait();
		for (int num = 0; num < galleryBlock.Frames.Count; num++)
		{
			GalleryFrame galleryFrame = galleryBlock.Frames[num];
			galleryFrame.UpdateColor(galleryFrame.Portrait.GetKey() == (int)portrait.Key && galleryFrame.Portrait.ID == portrait.ID);
		}
	}

	public void Set(PetNames key, string id)
	{
		string activeID = GetActiveID(key);
		Activate(key, id);
		SetActiveID(key, activeID);
	}

	public void SetExactly(PetNames key, int id)
	{
		GameObject frame = GameManager.Instance.PetGallery.Pets[key].List.Find((PetPortrait x) => x.ID == id).Frame;
		GameManager.Instance.CurrentPet.Portrait.UpdateFrame(frame);
	}

	public void RefreshAllFromActive()
	{
		foreach (KeyValuePair<PetNames, PortraitList> pet in Pets)
		{
			Activate(pet.Key, pet.Value.Active);
		}
	}

	public void RefreshFrame()
	{
		GameManager.Instance.CurrentPet.Portrait.UpdateFrame();
		if (GameManager.Instance.CurrentPet.PetPanel.SecondIsActive)
		{
			GameManager.Instance.CurrentPet.PetPanel.secondSlot.Portrait.UpdateFrame();
		}
	}

	public void Activate(int key, string id)
	{
		Activate((PetNames)key, id);
	}

	public void Activate(PetNames key, string id)
	{
		SetActiveID(key, id);
		if (GameManager.Instance.CurrentPet.PetPanel.GetKey(isFirst: true) == key || GameManager.Instance.CurrentPet.PetPanel.GetKey(isFirst: false) == key)
		{
			RefreshFrame();
		}
	}

	public GalleryToSave Save()
	{
		GalleryToSave galleryToSave = new GalleryToSave();
		foreach (KeyValuePair<PetNames, PortraitList> pet in Pets)
		{
			List<PetPortrait> list = pet.Value.List;
			List<int> list2 = new List<int>();
			for (int i = 0; i < list.Count; i++)
			{
				if (!list[i].IsFree() && list[i].Unlocked && list[i].Key == pet.Key)
				{
					list2.Add(list[i].ID);
				}
			}
			if (pet.Value.Active != "0" || list2.Count != 0)
			{
				galleryToSave.AddActive((int)pet.Key, pet.Value.Active);
				galleryToSave.AddUnlocked((int)pet.Key, list2);
			}
		}
		return galleryToSave;
	}

	public void Load(GalleryToSave save)
	{
		if (save == null)
		{
			save = new GalleryToSave();
		}
		int num = 0;
		foreach (KeyValuePair<PetNames, PortraitList> pet in Pets)
		{
			List<PetPortrait> list = pet.Value.List;
			List<int> unlocked = save.GetUnlocked((int)pet.Key);
			for (int i = 0; i < list.Count; i++)
			{
				PetPortrait petPortrait = list[i];
				if (petPortrait.Key == pet.Key && unlocked.Contains(petPortrait.ID))
				{
					petPortrait.Unlock();
					num++;
				}
				else if (petPortrait.Key == pet.Key)
				{
					petPortrait.Lock();
				}
			}
			Activate(pet.Key, save.GetActive((int)pet.Key));
		}
		Unlocked.SetValue(num);
	}

	private int CompareItemByCost(Portrait x, Portrait y)
	{
		if (x.Cost == y.Cost)
		{
			return Mathf.Clamp(x.ID - y.ID, -1, 1);
		}
		return Mathf.Clamp(x.Cost - y.Cost, -1, 1);
	}
}
