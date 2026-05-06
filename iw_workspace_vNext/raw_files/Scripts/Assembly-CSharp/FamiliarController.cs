using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FamiliarController : MonoBehaviour
{
	public List<FamiliarSlot> Slots;

	public SpriteAtlas Atlas;

	[SerializeField]
	private FamiliarPreview prefab;

	[SerializeField]
	private Transform content;

	[SerializeField]
	private RankDefiner rankDefiner;

	[SerializeField]
	private List<Sprite> backgroundSprites;

	private List<FamiliarPreview> previews;

	[SerializeField]
	private FamiliarPreviewPage previewPage;

	[SerializeField]
	private TextMeshProUGUI dustTex;

	private Queue<FamiliarPreview> previewPool = new Queue<FamiliarPreview>();

	private float timer;

	public void Awake()
	{
		VariableInt totalRank = GameManager.Instance.Familiars.TotalRank;
		totalRank.OnChange = (Action)Delegate.Combine(totalRank.OnChange, new Action(UpdateSlots));
	}

	public void OnDestroy()
	{
		VariableInt totalRank = GameManager.Instance.Familiars.TotalRank;
		totalRank.OnChange = (Action)Delegate.Remove(totalRank.OnChange, new Action(UpdateSlots));
	}

	public void OnEnable()
	{
		UpdateSlots();
		UpdateDustText();
	}

	private void Update()
	{
		timer += Time.unscaledDeltaTime;
		if (timer >= 0.2f)
		{
			timer = 0f;
			UpdateDustText();
		}
	}

	public void UpdateDustText()
	{
		dustTex.text = GameManager.Instance.Familiars.Dust.Value.ToReadableString("F0");
	}

	public void Init(List<Familiar> familiars)
	{
		BuildPreviews(familiars);
		BuildSlots();
	}

	private void BuildSlots()
	{
		int[] slotRankRequirements = FamiliarManager.SlotRankRequirements;
		for (int i = 0; i < slotRankRequirements.Length; i++)
		{
			Slots[i].Init(i, slotRankRequirements[i], rankDefiner, OnSelectFamiliar, backgroundSprites);
		}
	}

	private void BuildPreviews(List<Familiar> familiars)
	{
		if (familiars == null)
		{
			return;
		}
		if (previews != null)
		{
			foreach (FamiliarPreview preview in previews)
			{
				preview.gameObject.SetActive(value: false);
				previewPool.Enqueue(preview);
			}
			previews.Clear();
		}
		previews = new List<FamiliarPreview>(familiars.Count);
		foreach (Familiar familiar in familiars)
		{
			FamiliarPreview familiarPreview;
			if (previewPool.Count > 0)
			{
				familiarPreview = previewPool.Dequeue();
				familiarPreview.transform.SetParent(content, worldPositionStays: false);
				familiarPreview.gameObject.SetActive(value: true);
			}
			else
			{
				familiarPreview = UnityEngine.Object.Instantiate(prefab, content);
			}
			familiarPreview.Init(familiar, rankDefiner, OnSelectFamiliar, backgroundSprites[familiar.Rarity]);
			previews.Add(familiarPreview);
		}
	}

	public void ResetActiveSlots()
	{
		for (int i = 0; i < Slots.Count; i++)
		{
			Slots[i].Clear();
		}
	}

	public void OnSelectFamiliar(Familiar familiar)
	{
		previewPage.SelectFamiliar(familiar);
	}

	public void OnChooseFamiliar(Familiar familiar)
	{
		for (int i = 0; i < Slots.Count; i++)
		{
			if (Slots[i].AssignedFamiliar == null)
			{
				AssignFamiliarToSlot(i, familiar);
				break;
			}
		}
		previews.Find((FamiliarPreview x) => x.familiar == familiar).UpdateFrame();
	}

	public void UpdateSlots()
	{
		for (int i = 0; i < Slots.Count; i++)
		{
			Slots[i].SetIsLocked(!IsSlotUnlocked(i));
		}
	}

	public bool IsSlotUnlocked(int slotIndex)
	{
		if (!IsSlotIndexValid(slotIndex))
		{
			return false;
		}
		FamiliarSlot familiarSlot = Slots[slotIndex];
		return GameManager.Instance.Familiars.TotalLevel.ValueInt >= familiarSlot.RequiredTotalLevel;
	}

	public bool AssignFamiliarToSlot(int slotIndex, Familiar familiar)
	{
		if (!IsSlotUnlocked(slotIndex))
		{
			return false;
		}
		FamiliarSlot familiarSlot = Slots[slotIndex];
		if (familiarSlot.AssignedFamiliar == familiar)
		{
			return true;
		}
		Familiar familiar2 = familiarSlot.Clear();
		if (familiar2 != null)
		{
			familiar2.IsSelected = false;
		}
		UnassignFamiliar(familiar);
		familiarSlot.Assign(familiar);
		familiar.IsSelected = true;
		return true;
	}

	public void ClearAllSlots()
	{
		foreach (FamiliarSlot slot in Slots)
		{
			Familiar familiar = slot.Clear();
			if (familiar != null)
			{
				familiar.IsSelected = false;
			}
		}
	}

	public void UnassignFamiliar(Familiar familiar)
	{
		foreach (FamiliarSlot slot in Slots)
		{
			if (slot.AssignedFamiliar == familiar)
			{
				slot.Clear();
				familiar.IsSelected = false;
				break;
			}
		}
	}

	private bool IsSlotIndexValid(int slotIndex)
	{
		if (slotIndex >= 0)
		{
			return slotIndex < Slots.Count;
		}
		return false;
	}

	public void Open()
	{
		base.gameObject.SetActive(value: true);
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}
}
