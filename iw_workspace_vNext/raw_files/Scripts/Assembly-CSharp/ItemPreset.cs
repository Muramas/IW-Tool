using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ItemPreset : MonoBehaviour
{
	public int id;

	public List<ItemPresetFrame> frames;

	public Sprite Empty;

	public Sprite Filled;

	[SerializeField]
	private TMP_InputField nameField;

	private IEnumerator change;

	private Action<ItemPreset> OnChangeSet;

	private Action<int> OnExport;

	public void Init(Action<ItemPreset> onChange, Action<int> onExport)
	{
		OnChangeSet = onChange;
		OnExport = onExport;
	}

	public void Clear()
	{
		for (int i = 0; i < frames.Count; i++)
		{
			frames[i].SetItem(null);
		}
		nameField.text = string.Empty;
	}

	public string GetName()
	{
		return nameField.text;
	}

	public void Set(Preset<PresetSlot> preset)
	{
		Clear();
		if (preset == null || preset.slots.Count == 0)
		{
			return;
		}
		List<Item> availableItems = GameManager.Instance.Craft.AvailableItems;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < preset.slots.Count; i++)
		{
			PresetSlot p = preset.slots[i];
			Item item = availableItems.Find((Item x) => x.ID == p.item);
			if (item == null)
			{
				continue;
			}
			SlotKey key = item.Slot;
			ItemPresetFrame itemPresetFrame;
			if (key == SlotKey.Ring && num < 2)
			{
				itemPresetFrame = frames.FindAll((ItemPresetFrame x) => x.slot == key)[num];
				num++;
			}
			else if (key == SlotKey.Misc && num2 < 2)
			{
				itemPresetFrame = frames.FindAll((ItemPresetFrame x) => x.slot == key)[num2];
				num2++;
			}
			else
			{
				itemPresetFrame = frames.Find((ItemPresetFrame x) => x.slot == key);
			}
			itemPresetFrame.SetItem(item);
		}
		if (!string.IsNullOrEmpty(preset.name))
		{
			nameField.text = preset.name;
		}
		else
		{
			nameField.text = string.Empty;
		}
	}

	public void OpenExport()
	{
		OnExport(id);
	}

	public void Save()
	{
		List<ItemSlot> slots = GameManager.Instance.Craft.window.doll.Slots;
		int num = 0;
		int num2 = 0;
		ItemPresetFrame frame;
		for (int i = 0; i < frames.Count; i++)
		{
			frame = frames[i];
			if (frame.slot == SlotKey.Ring)
			{
				frame.SetItem(slots.FindAll((ItemSlot x) => x.Key == frame.slot)[num].Item);
				num++;
			}
			else if (frame.slot == SlotKey.Misc)
			{
				frame.SetItem(slots.FindAll((ItemSlot x) => x.Key == frame.slot)[num2].Item);
				num2++;
			}
			else
			{
				frame.SetItem(slots.Find((ItemSlot x) => x.Key == frame.slot).Item);
			}
		}
		OnChangeSet?.Invoke(this);
		GameManager.Instance.Craft.window.presets.OnSaveSet?.Invoke();
	}

	public void OnFocusNameField()
	{
		Settings.BlockInput = true;
	}

	public void OnChangeName()
	{
		Settings.BlockInput = false;
		nameField.text = nameField.text.Replace("#", string.Empty).Replace("@", string.Empty).Replace(";", string.Empty);
		OnChangeSet?.Invoke(this);
	}

	public void Load()
	{
		if (GameManager.Instance.Craft.window.CheckAvailable() && change == null)
		{
			change = changeSet();
			GameManager.Instance.StartCoroutine(change);
			GameManager.Instance.CurrentHero.IconSetAnim.StartAnimation();
		}
	}

	private IEnumerator changeSet()
	{
		List<ItemSlot> slots = GameManager.Instance.Craft.window.doll.Slots;
		ItemSlot slot;
		for (int i = 0; i < slots.Count; i++)
		{
			slot = slots[i];
			if (slot.Key == SlotKey.Ring || slot.Key == SlotKey.Misc)
			{
				if (slot.Item != null)
				{
					slots[i].Clear();
				}
				continue;
			}
			if (slot.Item != null)
			{
				ItemPresetFrame itemPresetFrame = frames.Find((ItemPresetFrame x) => x.slot == slot.Key);
				if (itemPresetFrame != null && itemPresetFrame.item != null && slot.Item.ID == itemPresetFrame.item.ID)
				{
					continue;
				}
			}
			slots[i].Clear();
		}
		GameManager.Instance.Craft.OnChangeItems();
		GameManager.Instance.AddProfit(0f);
		yield return null;
		for (int num = 0; num < frames.Count; num++)
		{
			ItemPresetFrame itemPresetFrame = frames[num];
			if (itemPresetFrame.item != null && !itemPresetFrame.item.equiped)
			{
				itemPresetFrame.item.EquipItem();
			}
		}
		GameManager.Instance.AddProfit(0f);
		GameManager.Instance.Craft.OnChangeItems();
		yield return null;
		GameManager.Instance.Craft.window.UpdateEnchantLevel();
		GameManager.Instance.Craft.window.ChangeFilter();
		yield return null;
		change = null;
	}
}
