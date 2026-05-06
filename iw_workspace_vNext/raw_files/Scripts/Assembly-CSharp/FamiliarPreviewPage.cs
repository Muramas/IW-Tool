using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FamiliarPreviewPage : MonoBehaviour
{
	[SerializeField]
	private GameObject previewPanel;

	[SerializeField]
	private Button previewButton;

	[SerializeField]
	private Image previewIcon;

	[SerializeField]
	private List<Image> ranks;

	[SerializeField]
	private Image previewXp;

	[SerializeField]
	private TextMeshProUGUI descriptionText;

	[SerializeField]
	private TextMeshProUGUI dustText;

	[SerializeField]
	private Button promoteButton;

	[SerializeField]
	private Button chooseButton;

	[SerializeField]
	private Image foodIcon;

	[SerializeField]
	private TextMeshProUGUI foodAmountText;

	[SerializeField]
	private Button feedButton;

	[SerializeField]
	private GameObject statisticsPanel;

	[SerializeField]
	private Button statisticsButton;

	[SerializeField]
	private List<FamiliarTagDescription> tagDescriptions;

	[SerializeField]
	private TextMeshProUGUI statisticsText;

	[SerializeField]
	private GameObject fedTimeContainer;

	[SerializeField]
	private TextMeshProUGUI fedTimeText;

	[SerializeField]
	private Image fedTimeIcon;

	[SerializeField]
	private RankDefiner rankDefiner;

	private Familiar selectedFamiliar;

	private float timer;

	public void Awake()
	{
		OpenStatistics();
	}

	public void OnEnable()
	{
		if (previewPanel.activeSelf)
		{
			UpdatePreview();
		}
		else
		{
			UpdateStatisticsText();
		}
	}

	public void Update()
	{
		timer += Time.unscaledDeltaTime;
		if (timer > 0.2f)
		{
			timer = 0f;
			if (selectedFamiliar != null)
			{
				previewXp.fillAmount = selectedFamiliar.GetXpProgress();
			}
			UpdateTagDescriptions();
			if (previewPanel.activeSelf)
			{
				UpdateFeedButton();
			}
			UpdateFedStatusUI();
		}
	}

	public void OpenStatistics()
	{
		UpdateTabState(showStatistics: true);
		UpdateStatisticsText();
		UpdateTagDescriptions();
	}

	public void OpenPreview()
	{
		UpdateTabState(showStatistics: false);
		UpdatePreview();
	}

	public void SelectFamiliar(Familiar familiar)
	{
		if (selectedFamiliar != null)
		{
			VariableInt level = selectedFamiliar.Level;
			level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(UpdatePreview));
			VariableInt rank = selectedFamiliar.Rank;
			rank.OnChange = (Action)Delegate.Remove(rank.OnChange, new Action(UpdatePreview));
		}
		selectedFamiliar = familiar;
		UpdatePreview();
		OpenPreview();
		if (selectedFamiliar != null)
		{
			VariableInt level2 = selectedFamiliar.Level;
			level2.OnChange = (Action)Delegate.Combine(level2.OnChange, new Action(UpdatePreview));
			VariableInt rank2 = selectedFamiliar.Rank;
			rank2.OnChange = (Action)Delegate.Combine(rank2.OnChange, new Action(UpdatePreview));
		}
	}

	public void UpdatePreview()
	{
		if (selectedFamiliar == null)
		{
			selectedFamiliar = GameManager.Instance.Familiars.AllFamiliars[0];
		}
		previewIcon.sprite = selectedFamiliar.Icon;
		descriptionText.text = selectedFamiliar.GetDescription();
		previewXp.transform.parent.gameObject.SetActive(selectedFamiliar.Rank.ValueInt > 0);
		previewXp.fillAmount = selectedFamiliar.GetXpProgress();
		UpdateRankIcons();
		RefreshButtons();
		UpdateFedStatusUI();
	}

	private void UpdateTabState(bool showStatistics)
	{
		statisticsButton.interactable = !showStatistics;
		previewButton.interactable = showStatistics;
		statisticsPanel.SetActive(showStatistics);
		previewPanel.SetActive(!showStatistics);
	}

	public void UpdateStatisticsText()
	{
		FamiliarManager familiars = GameManager.Instance.Familiars;
		IReadOnlyList<Familiar> allFamiliars = familiars.AllFamiliars;
		int value = allFamiliars.Count((Familiar f) => f.Rank.ValueInt > 0);
		int valueInt = familiars.TotalRank.ValueInt;
		int num = allFamiliars.Sum((Familiar f) => f.Level.ValueInt);
		int count = allFamiliars.Count;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Unlocked".Translate()).Append(" ");
		stringBuilder.Append(value).Append("/").AppendLine(count.ToString())
			.AppendLine();
		stringBuilder.Append("Summary Ranks".Translate()).Append(" ").AppendLine(valueInt.ToString());
		stringBuilder.Append("Summary Levels".Translate()).Append(" ").AppendLine(num.ToString());
		statisticsText.text = stringBuilder.ToString();
	}

	private void UpdateRankIcons()
	{
		(Sprite rankSprite, int starCount) tuple = rankDefiner.DefineRank(selectedFamiliar.Rank.ValueInt, ranks.Count);
		Sprite item = tuple.rankSprite;
		int item2 = tuple.starCount;
		for (int i = 0; i < ranks.Count; i++)
		{
			ranks[i].sprite = item;
			ranks[i].enabled = i < item2;
		}
	}

	private void RefreshButtons()
	{
		bool flag = selectedFamiliar.Rank.ValueInt > 0;
		promoteButton.gameObject.SetActive(flag);
		if (flag)
		{
			BigNumber dustRequirements = GameManager.Instance.Familiars.GetDustRequirements(selectedFamiliar.Rank.ValueInt, selectedFamiliar.Rarity);
			if (dustRequirements > 0.0)
			{
				dustText.text = dustRequirements.ToReadableString("F0");
			}
			else
			{
				dustText.text = "Max".Translate();
			}
			promoteButton.interactable = GameManager.Instance.Familiars.Dust.Value >= dustRequirements && selectedFamiliar.Rank.ValueInt < 20;
		}
		chooseButton.interactable = flag && !selectedFamiliar.IsSelected && GameManager.Instance.Familiars.HasEmptySlot();
		UpdateFeedButton();
	}

	private void UpdateFeedButton()
	{
		FamiliarManager familiars = GameManager.Instance.Familiars;
		bool flag = selectedFamiliar != null && selectedFamiliar.IsSelected && selectedFamiliar.Rank.ValueInt > 0 && selectedFamiliar.Tags != null && selectedFamiliar.Tags.Count > 0;
		feedButton.gameObject.SetActive(flag);
		if (flag)
		{
			FamiliarTags familiarTags = selectedFamiliar.Tags[0];
			FamiliarFood foodForTag = familiars.GetFoodForTag(familiarTags);
			if (foodForTag == null)
			{
				feedButton.interactable = false;
				foodIcon.sprite = null;
				TextMeshProUGUI textMeshProUGUI = foodAmountText;
				int foodRequirements = FamiliarManager.FoodRequirements;
				textMeshProUGUI.text = foodRequirements.ToString();
			}
			else
			{
				foodIcon.sprite = foodForTag.Icon;
				ulong foodAmount = familiars.GetFoodAmount(foodForTag.Id);
				int foodRequirements2 = FamiliarManager.FoodRequirements;
				foodAmountText.text = foodRequirements2.ToString();
				feedButton.interactable = foodAmount >= (ulong)foodRequirements2;
			}
		}
	}

	private void UpdateFedStatusUI()
	{
		if (selectedFamiliar == null)
		{
			fedTimeContainer.SetActive(value: false);
			return;
		}
		float fedStatus = selectedFamiliar.FedStatus;
		bool flag = fedStatus > 0f;
		fedTimeContainer.SetActive(flag);
		if (flag)
		{
			if (fedStatus < 86400f)
			{
				float fillAmount = Mathf.Clamp01((86400f - fedStatus) / 86400f);
				fedTimeIcon.fillAmount = fillAmount;
			}
			else
			{
				fedTimeIcon.fillAmount = 0f;
			}
			float num = fedStatus / 86400f;
			if (num >= 1f)
			{
				int num2 = Mathf.FloorToInt(num);
				int num3 = Mathf.FloorToInt((fedStatus - (float)num2 * 86400f) / 3600f);
				fedTimeText.text = num2 + "d".Translate() + num3 + "h".Translate();
			}
			else
			{
				int num4 = Mathf.FloorToInt(fedStatus / 3600f);
				int num5 = Mathf.FloorToInt((fedStatus - (float)num4 * 3600f) / 60f);
				fedTimeText.text = num4 + "h".Translate() + num5 + "m".Translate();
			}
		}
	}

	public void ChooseFamiliar()
	{
		if (selectedFamiliar != null && selectedFamiliar.Rank.ValueInt != 0 && !selectedFamiliar.IsSelected && GameManager.Instance.Familiars.HasEmptySlot())
		{
			GameManager.Instance.ConfirmWindow.Open("FamiliarChangeConfirmation".Translate(), delegate
			{
				GameManager.Instance.Familiars.ChooseFamiliar(selectedFamiliar);
				UpdatePreview();
			});
		}
	}

	public void DeselectFamiliar()
	{
		if (selectedFamiliar != null && selectedFamiliar.IsSelected)
		{
			GameManager.Instance.Familiars.DeselectFamiliar(selectedFamiliar);
		}
	}

	public void PromoteFamiliar()
	{
		if (selectedFamiliar != null && selectedFamiliar.Rank.ValueInt != 0)
		{
			int num = Mathf.FloorToInt(GameManager.Instance.Familiars.GetDustRequirements(selectedFamiliar.Rank.ValueInt, selectedFamiliar.Rarity).ToFloat());
			if (!(GameManager.Instance.Familiars.Dust.Value < num))
			{
				GameManager.Instance.Familiars.Dust.Change(-num);
				selectedFamiliar.SetRank(selectedFamiliar.Rank.ValueInt + 1);
				UpdatePreview();
			}
		}
	}

	public void FeedFamiliar()
	{
		if (selectedFamiliar == null || !selectedFamiliar.IsSelected)
		{
			return;
		}
		FamiliarManager familiars = GameManager.Instance.Familiars;
		if (selectedFamiliar.Tags == null || selectedFamiliar.Tags.Count == 0)
		{
			return;
		}
		FamiliarTags familiarTags = selectedFamiliar.Tags[0];
		FamiliarFood foodForTag = familiars.GetFoodForTag(familiarTags);
		if (foodForTag != null)
		{
			int foodRequirements = FamiliarManager.FoodRequirements;
			if (familiars.GetFoodAmount(foodForTag.Id) >= (ulong)foodRequirements)
			{
				familiars.ConsumeFood(foodForTag.Id, (ulong)foodRequirements);
				selectedFamiliar.AddFeedTime(FamiliarManager.FeedBuffDurationSeconds);
				UpdatePreview();
				UpdateFeedButton();
			}
		}
	}

	public FamiliarTags GetTagSelected()
	{
		if (selectedFamiliar == null || selectedFamiliar.Tags == null || selectedFamiliar.Tags.Count == 0)
		{
			return FamiliarTags.Construct;
		}
		return selectedFamiliar.Tags[0];
	}

	private void UpdateTagDescriptions()
	{
		foreach (FamiliarTagDescription tagDescription in tagDescriptions)
		{
			tagDescription.UpdateDescription();
		}
	}
}
