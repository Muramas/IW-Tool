using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FamiliarResourcePreview : MonoBehaviour
{
	[SerializeField]
	private FamiliarTags Tag;

	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private TextMeshProUGUI countText;

	private void OnEnable()
	{
		Refresh();
	}

	public void Refresh()
	{
		FamiliarManager familiarManager = GameManager.Instance?.Familiars;
		if (familiarManager == null)
		{
			return;
		}
		FamiliarFood foodForTag = familiarManager.GetFoodForTag(Tag);
		if (foodForTag == null)
		{
			if (iconImage != null)
			{
				iconImage.sprite = null;
				iconImage.enabled = false;
			}
			if (countText != null)
			{
				countText.text = "0";
			}
		}
		else
		{
			iconImage.sprite = foodForTag.Icon;
			iconImage.enabled = true;
			ulong foodAmount = familiarManager.GetFoodAmount(foodForTag.Id);
			countText.text = new BigNumber(foodAmount).ToReadableString("F0");
		}
	}
}
