using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FamiliarPreview : MonoBehaviour
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private Image frame;

	[SerializeField]
	private Image background;

	[SerializeField]
	private GameObject rankIconsContainer;

	[SerializeField]
	private List<Image> rankIcons;

	[SerializeField]
	private Sprite commonFrame;

	[SerializeField]
	private Sprite selectedFrame;

	[SerializeField]
	private Image lockIcon;

	[SerializeField]
	private TextMeshProUGUI levelText;

	private RankDefiner rankDefiner;

	private Action<Familiar> onSelect;

	public Familiar familiar { get; private set; }

	public void Init(Familiar familiar, RankDefiner rankDefiner, Action<Familiar> onSelect, Sprite backgroundSprite)
	{
		this.familiar = familiar;
		this.rankDefiner = rankDefiner;
		icon.sprite = familiar.Icon;
		this.onSelect = onSelect;
		background.sprite = backgroundSprite;
	}

	private void OnEnable()
	{
		UpdateVisual();
		if (familiar != null)
		{
			VariableInt level = familiar.Level;
			level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(UpdateVisual));
			VariableInt rank = familiar.Rank;
			rank.OnChange = (Action)Delegate.Combine(rank.OnChange, new Action(UpdateVisual));
		}
	}

	private void OnDisable()
	{
		if (familiar != null)
		{
			VariableInt level = familiar.Level;
			level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(UpdateVisual));
			VariableInt rank = familiar.Rank;
			rank.OnChange = (Action)Delegate.Remove(rank.OnChange, new Action(UpdateVisual));
		}
	}

	public void SetBackground(Sprite backgroundSprite)
	{
		background.sprite = backgroundSprite;
	}

	public void UpdateVisual()
	{
		lockIcon.gameObject.SetActive(familiar != null && familiar.Rank.ValueInt == 0);
		if (familiar == null)
		{
			icon.enabled = false;
			levelText.text = string.Empty;
			rankIconsContainer.SetActive(value: false);
			return;
		}
		icon.enabled = true;
		rankIconsContainer.SetActive(value: true);
		(Sprite rankSprite, int starCount) tuple = rankDefiner.DefineRank(familiar.Rank.ValueInt, rankIcons.Count);
		Sprite item = tuple.rankSprite;
		int item2 = tuple.starCount;
		for (int i = 0; i < rankIcons.Count; i++)
		{
			rankIcons[i].enabled = i < item2;
			if (rankIcons[i].enabled)
			{
				rankIcons[i].sprite = item;
			}
		}
		UpdateFrame();
		levelText.text = ((familiar.Level.ValueInt > 0) ? familiar.Level.ValueInt.ToString() : string.Empty);
	}

	public void Clear()
	{
		if (familiar != null)
		{
			VariableInt rank = familiar.Rank;
			rank.OnChange = (Action)Delegate.Remove(rank.OnChange, new Action(UpdateVisual));
		}
		familiar = null;
		UpdateVisual();
	}

	public void Select()
	{
		onSelect?.Invoke(familiar);
	}

	public void UpdateFrame()
	{
		if (familiar.IsSelected)
		{
			frame.sprite = selectedFrame;
		}
		else
		{
			frame.sprite = commonFrame;
		}
	}
}
