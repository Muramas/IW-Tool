using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FamiliarRewardPreview : MonoBehaviour
{
	[SerializeField]
	private GameObject container;

	[SerializeField]
	private Image icon;

	[SerializeField]
	private TextMeshProUGUI labelText;

	[SerializeField]
	private List<Image> rankIcons;

	[SerializeField]
	private RankDefiner rankDefiner;

	[SerializeField]
	private TextMeshProUGUI statusText;

	[SerializeField]
	private TextMeshProUGUI dustText;

	[SerializeField]
	private FamiliarRewardAnimator animator;

	private Familiar familiar;

	private int originalRank;

	public void ShowFamiliar(Familiar familiar)
	{
		this.familiar = familiar;
		originalRank = familiar.Rank.ValueInt;
		container.SetActive(value: true);
		icon.sprite = familiar.Icon;
		labelText.text = familiar.Name.Translate();
		statusText.text = string.Empty;
		dustText.gameObject.SetActive(value: false);
	}

	public void UpdateRank(int rank)
	{
		UpdateRankIcons(rank);
		statusText.text = GetStatusLabel(rank);
		animator.Play(container.transform, Mathf.Clamp01((float)rank / 10f));
	}

	public void ShowDust(int dust)
	{
		dustText.gameObject.SetActive(value: true);
		dustText.text = dust.ToString();
	}

	private void UpdateRankIcons(int rank)
	{
		(Sprite rankSprite, int starCount) tuple = rankDefiner.DefineRank(rank, rankIcons.Count);
		Sprite item = tuple.rankSprite;
		int item2 = tuple.starCount;
		for (int i = 0; i < rankIcons.Count; i++)
		{
			rankIcons[i].enabled = i < item2;
			rankIcons[i].sprite = item;
		}
	}

	private string GetStatusLabel(int rank)
	{
		if (originalRank == 0 && rank >= 1)
		{
			return "Unlocked".Translate() + "!";
		}
		if (rank > originalRank)
		{
			return "Upgraded".Translate() + "!";
		}
		return string.Empty;
	}
}
