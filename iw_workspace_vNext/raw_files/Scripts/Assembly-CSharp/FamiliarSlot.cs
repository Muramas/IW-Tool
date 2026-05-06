using System;
using System.Collections.Generic;
using ModelShark;
using UnityEngine;

public class FamiliarSlot : MonoBehaviour, ITooltipBodyHandler
{
	[SerializeField]
	private FamiliarPreview preview;

	[SerializeField]
	private GameObject lockedIcon;

	private RankDefiner rankDefiner;

	private Action<Familiar> onSelect;

	[SerializeField]
	private ModelShark.TooltipTrigger tooltip;

	private List<Sprite> backgroundSprites;

	public int Index { get; private set; }

	public int RequiredTotalLevel { get; private set; }

	public Familiar AssignedFamiliar { get; private set; }

	private void Awake()
	{
		tooltip.bodyHandler = this;
	}

	private void OnEnable()
	{
		if (AssignedFamiliar == null)
		{
			preview.SetBackground(backgroundSprites[0]);
		}
	}

	public void Init(int index, int requiredTotalLevel, RankDefiner rankDefiner, Action<Familiar> onSelect, List<Sprite> backgroundSprites)
	{
		Index = index;
		RequiredTotalLevel = requiredTotalLevel;
		this.rankDefiner = rankDefiner;
		this.onSelect = onSelect;
		this.backgroundSprites = backgroundSprites;
	}

	public Familiar Clear()
	{
		Familiar assignedFamiliar = AssignedFamiliar;
		if (assignedFamiliar != null)
		{
			assignedFamiliar.IsSelected = false;
			AssignedFamiliar = null;
			preview.Clear();
			preview.SetBackground(backgroundSprites[0]);
		}
		return assignedFamiliar;
	}

	public void Assign(Familiar familiar)
	{
		AssignedFamiliar = familiar;
		preview.Init(familiar, rankDefiner, onSelect, backgroundSprites[familiar.Rarity]);
		UpdateVisual();
	}

	public void UpdateVisual()
	{
		preview.UpdateVisual();
	}

	public void SetIsLocked(bool isLocked)
	{
		lockedIcon.SetActive(isLocked);
		tooltip.enabled = isLocked;
	}

	public string GetBody()
	{
		if (lockedIcon.activeSelf)
		{
			return string.Format("FamiliarSlotLocked".Translate(), RequiredTotalLevel);
		}
		if (AssignedFamiliar != null)
		{
			return AssignedFamiliar.PreviewShort();
		}
		return "FamiliarSlotEmpty".Translate();
	}
}
