using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeVisual : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
{
	public Button button;

	public Upgrade upgrade;

	public Image image;

	public UpgradeManager manager;

	public Image[] stars;

	private Timer timer;

	private void OnDisable()
	{
		HideTip();
	}

	public void SetUpgrade(Upgrade upgrade)
	{
		if (this.upgrade == upgrade)
		{
			return;
		}
		this.upgrade = upgrade;
		if (upgrade.SpriteKey == null || upgrade.SpriteKey == string.Empty)
		{
			image.sprite = null;
			return;
		}
		string[] array = upgrade.SpriteKey.Split('/');
		int rank = 0;
		if (array.Length > 1)
		{
			rank = int.Parse(array[1]);
		}
		image.sprite = GameManager.Instance.UpgradeManager.Atlas.Get(array[0]);
		(Sprite rankSprite, int starCount) tuple = manager.RankDefiner.DefineRank(rank, stars.Length);
		Sprite item = tuple.rankSprite;
		int item2 = tuple.starCount;
		for (int i = 0; i < stars.Length; i++)
		{
			if (item != null)
			{
				stars[i].sprite = item;
			}
			bool active = item != null && i < item2;
			stars[i].gameObject.SetActive(active);
		}
	}

	private void LateUpdate()
	{
		if (!upgrade.applied)
		{
			if (GameManager.Instance.Mana.Value >= upgrade.Cost != button.interactable)
			{
				button.interactable = GameManager.Instance.Mana.Value >= upgrade.Cost;
			}
		}
		else if (!button.interactable)
		{
			button.interactable = true;
		}
		if (button.targetGraphic.enabled == upgrade.applied)
		{
			button.targetGraphic.enabled = !upgrade.applied;
		}
	}

	public void Buy()
	{
		if (!upgrade.applied && upgrade.Cost <= GameManager.Instance.Mana.Value)
		{
			upgrade.Buy();
			HideTip();
		}
	}

	public void ShowTip()
	{
		UpdateTip();
		timer = GameManager.Instance.Timers.GetPeriodical(10f, 1f, UpdateTip, HideTip, ignore_scale: true);
		manager.tips.transform.parent.gameObject.SetActive(value: true);
	}

	public void HideTip()
	{
		if (!(manager.tips == null))
		{
			manager.tips.text = "";
			manager.tips.transform.parent.gameObject.SetActive(value: false);
			if (timer != null)
			{
				timer.Stop();
			}
		}
	}

	private void UpdateTip()
	{
		TextMeshProUGUI tips = manager.tips;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<b>");
		stringBuilder.Append(TranslationManager.Instance.Process(upgrade.Name));
		stringBuilder.AppendLine("</b>");
		stringBuilder.AppendLine();
		stringBuilder.Append("Cost".Translate());
		stringBuilder.Append(": ");
		if (!button.interactable)
		{
			if (Settings.RedCost)
			{
				stringBuilder.Append("<color=#ff3333ff>");
			}
			stringBuilder.Append(upgrade.Cost.ToReadableString("F0"));
			if (Settings.RedCost)
			{
				stringBuilder.Append("</color>");
			}
			stringBuilder.AppendLine();
		}
		else
		{
			stringBuilder.AppendLine(upgrade.Cost.ToReadableString("F0"));
		}
		stringBuilder.AppendLine();
		stringBuilder.Append(upgrade.GetDescription());
		tips.text = stringBuilder.ToString();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		ShowTip();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		HideTip();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			Remove();
		}
	}

	public void Remove()
	{
		if (upgrade.applied && GameManager.Instance.ChallengeManager.ActiveChallenge == null)
		{
			manager.Remove(upgrade);
		}
	}
}
