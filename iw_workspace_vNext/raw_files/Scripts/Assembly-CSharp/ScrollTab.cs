using ModelShark;
using UnityEngine;
using UnityEngine.UI;

public class ScrollTab : MonoBehaviour
{
	[SerializeField]
	private GameObject opened;

	[SerializeField]
	private Button closed;

	[SerializeField]
	private ModelShark.TooltipTrigger tooltip;

	[SerializeField]
	private GameObject content;

	[SerializeField]
	private bool isOpened;

	public void Open()
	{
		if (!isOpened)
		{
			closed.gameObject.SetActive(value: false);
			opened.SetActive(value: true);
			isOpened = true;
			tooltip.StopHover();
			content.SetActive(value: true);
		}
	}

	public void Close()
	{
		if (isOpened)
		{
			closed.gameObject.SetActive(value: true);
			opened.SetActive(value: false);
			isOpened = false;
			content.SetActive(value: false);
		}
	}

	public void SetInteractable(bool isLocked)
	{
		closed.interactable = isLocked;
	}
}
