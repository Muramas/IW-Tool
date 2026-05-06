using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSelectTrigger : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField]
	private ItemSelectIcon item;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				item.Item.Favorite = !item.Item.Favorite;
				item.UpdateLabels();
			}
			else if (GameManager.Instance.Craft.window.ForgeIsOpen())
			{
				GameManager.Instance.Craft.window.forge.Select(item.Item);
			}
			else
			{
				item.EquipFromMenu();
			}
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		item.ShowTip();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		item.HideTip();
	}
}
