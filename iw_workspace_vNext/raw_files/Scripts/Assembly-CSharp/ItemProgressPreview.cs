using UnityEngine;
using UnityEngine.EventSystems;

public class ItemProgressPreview : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	private ItemSelect item;

	public void OnPointerEnter(PointerEventData eventData)
	{
		item.ShowProgres();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		item.HidePreview();
	}
}
