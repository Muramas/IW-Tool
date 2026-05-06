using UnityEngine;
using UnityEngine.UI;

public class ItemPresetFrame : MonoBehaviour
{
	public SlotKey slot;

	public Item item;

	public Image frame;

	public Image icon;

	public ItemPreset preset;

	public void SetItem(Item item)
	{
		if (item == null || item.Slot == slot)
		{
			this.item = item;
			UpdateSlot();
		}
	}

	public void UpdateSlot()
	{
		if (item == null)
		{
			icon.gameObject.SetActive(value: false);
			frame.sprite = preset.Empty;
		}
		else
		{
			icon.sprite = item.Icon;
			frame.sprite = preset.Filled;
			icon.gameObject.SetActive(value: true);
		}
	}
}
