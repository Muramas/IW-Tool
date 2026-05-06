using UnityEngine;
using UnityEngine.UI;

public class ScrollAnimation : MonoBehaviour
{
	[SerializeField]
	private float closedScrollSize;

	[SerializeField]
	private float openedPosition;

	[SerializeField]
	private float closedPosition;

	[SerializeField]
	private RectTransform bottom;

	[SerializeField]
	private Image scroll;

	[SerializeField]
	private BoxCollider2D Collider;

	public bool IsOpened { get; private set; }

	public void Close()
	{
		IsOpened = false;
		scroll.fillAmount = closedScrollSize;
		Vector3 localPosition = bottom.localPosition;
		localPosition.y = closedPosition;
		bottom.localPosition = localPosition;
		Collider.enabled = false;
	}

	public void Open()
	{
		IsOpened = true;
		scroll.fillAmount = 1f;
		Vector3 localPosition = bottom.localPosition;
		localPosition.y = openedPosition;
		bottom.localPosition = localPosition;
		Collider.enabled = true;
	}
}
