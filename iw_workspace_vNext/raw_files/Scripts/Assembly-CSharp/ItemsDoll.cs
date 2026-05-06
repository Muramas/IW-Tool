using System.Collections.Generic;
using UnityEngine;

public class ItemsDoll : MonoBehaviour
{
	public List<ItemSlot> Slots;

	public void Init()
	{
	}

	public void UnEquipMythic()
	{
		foreach (ItemSlot slot in Slots)
		{
			if (slot.Item != null && slot.Item.Tier == 6)
			{
				slot.Clear();
			}
		}
	}

	public void RefreshSlots()
	{
		foreach (ItemSlot slot in Slots)
		{
			slot.gameObject.SetActive(SlotIsAvailable(slot.Key));
		}
	}

	public void ClearAll()
	{
		foreach (ItemSlot slot in Slots)
		{
			slot.Clear();
		}
	}

	public void UpdateEff()
	{
		foreach (ItemSlot slot in Slots)
		{
			if (slot.Item != null)
			{
				slot.UpdateEfficiency();
			}
		}
	}

	public ItemSlot GetSlot(SlotKey key)
	{
		if (key != SlotKey.Ring && key != SlotKey.Misc)
		{
			return Slots.Find((ItemSlot x) => x.Key == key);
		}
		List<ItemSlot> list = Slots.FindAll((ItemSlot x) => x.Key == key);
		ItemSlot itemSlot = list.Find((ItemSlot x) => x.Item == null);
		if (itemSlot == null)
		{
			itemSlot = list[0];
		}
		return itemSlot;
	}

	public ItemSlot GetSlot(SlotKey key, int number = 0)
	{
		return Slots.FindAll((ItemSlot x) => x.Key == key)[number];
	}

	public bool SlotIsAvailable(SlotKey key)
	{
		bool flag = false;
		switch (key)
		{
		case SlotKey.Shoulder:
			return GameManager.Instance.Paragon.ShoulderIsAvailable;
		case SlotKey.Waist:
			return GameManager.Instance.Paragon.WaistIsAvailable;
		case SlotKey.Ring:
			return GameManager.Instance.Paragon.FingerIsAvailable;
		case SlotKey.Neck:
			return GameManager.Instance.Paragon.NeckIsAvailable;
		case SlotKey.Back:
			return GameManager.Instance.Paragon.BackIsAvailable;
		case SlotKey.Wrist:
			return GameManager.Instance.Paragon.WristIsAvailable;
		case SlotKey.Weapon:
			return GameManager.Instance.Paragon.WeaponIsAvailable;
		case SlotKey.Offhand:
			return GameManager.Instance.Paragon.OffhandIsAvailable;
		case SlotKey.Pants:
			return GameManager.Instance.Paragon.LegsIsAvailable;
		case SlotKey.Research:
		case SlotKey.Misc:
			return GameManager.Instance.Craft.AvailableItems.Find((Item x) => x.Slot == key) != null;
		case SlotKey.Mount:
			return GameManager.Instance.Paragon.MountIsAvailable;
		case SlotKey.Phylactery:
			return GameManager.Instance.Paragon.PhylacteryIsAvailable;
		case SlotKey.Accessory:
			return GameManager.Instance.Paragon.AccessoryIsAvailable;
		default:
			return true;
		}
	}
}
