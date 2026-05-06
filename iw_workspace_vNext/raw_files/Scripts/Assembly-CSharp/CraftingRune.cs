using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftingRune : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private float progress;

	[SerializeField]
	private CraftingMenu menu;

	[SerializeField]
	private Image filler;

	[SerializeField]
	private TextMeshProUGUI label;

	private void OnEnable()
	{
		UpdateLabel();
	}

	public void AddProgress(BigNumber cost)
	{
		progress += (0.04500000178813934 * cost / 1200.0).ToFloat();
		if (progress >= 1f)
		{
			int num = Mathf.FloorToInt(progress);
			progress -= num;
			GameManager.Instance.Trials.Keys.Change(num);
			menu.log.Throw(menu.GetSprite(DustGambling.DropType.Rune), num);
		}
		UpdateLabel();
	}

	public float GetProgress()
	{
		return progress;
	}

	public void LoadProgress(float p)
	{
		progress = p;
		UpdateLabel();
	}

	private void UpdateLabel()
	{
		filler.fillAmount = progress;
		label.text = (progress * 100f).ToString("F2", CultureInfo.InvariantCulture) + "%";
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		label.gameObject.SetActive(value: true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		label.gameObject.SetActive(value: false);
	}
}
