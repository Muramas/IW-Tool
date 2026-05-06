using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public SlotKey Key;

	public int Number;

	public Item Item;

	public Image Back;

	public Image Frame;

	public Image Icon;

	public Image Favorite;

	public Image Unavailable;

	[SerializeField]
	private GameObject elvl;

	[SerializeField]
	private TextMeshProUGUI elvlLabel;

	public Action<Item> OnEquip;

	public void Clear()
	{
		if (Item != null)
		{
			Item.Unequip();
			Item = null;
			if (OnEquip != null)
			{
				OnEquip(null);
			}
		}
		UpdateVisual();
	}

	public virtual void ShowItem(Item item)
	{
		Item = item;
		UpdateVisual();
	}

	public virtual void SelectItem(Item item)
	{
		if (Item != null)
		{
			Item.Unequip();
		}
		Item = item;
		UpdateEfficiency();
		Item.Equip();
		if (OnEquip != null)
		{
			OnEquip(Item);
		}
		UpdateVisual();
	}

	public void UpdateEfficiency()
	{
		Item.SetEfficiency(GameManager.Instance.Craft.GetEfficiency(Key, Number));
	}

	public void Update()
	{
		CheckUnavailable();
	}

	public void UpdateEnchantLevel()
	{
		if (!(elvl == null) && Item != null)
		{
			if (Item.Enchant != null && Item.Enchant.GetFullLevel() > 0)
			{
				elvl.SetActive(value: true);
				elvlLabel.text = Item.Enchant.GetFullLevel().ToString();
			}
			else
			{
				elvl.SetActive(value: false);
			}
		}
	}

	protected virtual void CheckUnavailable()
	{
		if (Item != null)
		{
			if (Unavailable.enabled != !Item.active)
			{
				Unavailable.enabled = !Item.active;
			}
			if (Favorite != null && Favorite.enabled != Item.Favorite)
			{
				Favorite.enabled = Item.Favorite;
			}
		}
		else if (Unavailable.enabled)
		{
			Unavailable.enabled = false;
		}
	}

	public virtual void ClearVisual()
	{
		Icon.enabled = false;
		Frame.enabled = false;
		Unavailable.enabled = false;
		Back.sprite = GameManager.Instance.Craft.window.ItemSlots[0];
		if (elvl != null)
		{
			elvl.SetActive(value: false);
		}
	}

	public void UpdateVisual()
	{
		int num = 0;
		if (Item != null)
		{
			num = Item.Tier;
			Icon.sprite = Item.Icon;
			Icon.enabled = true;
			Frame.sprite = GameManager.Instance.Craft.window.Frames[Item.Tier];
			Frame.enabled = true;
			if (Favorite != null)
			{
				Favorite.enabled = Item.Favorite;
			}
			Unavailable.enabled = !Item.active;
		}
		else
		{
			Icon.enabled = false;
			Frame.enabled = false;
			Unavailable.enabled = false;
			if (Favorite != null)
			{
				Favorite.enabled = false;
			}
			if (elvl != null)
			{
				elvl.SetActive(value: false);
			}
		}
		Back.sprite = GameManager.Instance.Craft.window.ItemSlots[num];
	}

	public void ShowTip()
	{
		if (Item == null)
		{
			GameManager.Instance.Craft.window.OpenTip(Key.ToString().Translate(), base.transform, GameManager.Instance.Craft.window.SlotTipOffset, 160f);
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Item.Preview());
		stringBuilder.Append("ItemUnequip".Translate());
		int num = 400;
		if (!string.IsNullOrEmpty(Item.LoreKey))
		{
			num = 580;
		}
		GameManager.Instance.Craft.window.OpenTip(stringBuilder.ToString(), base.transform, GameManager.Instance.Craft.window.SlotTipOffset, num);
	}

	public void HideTip()
	{
		GameManager.Instance.Craft.window.CloseTip();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			Clear();
			GameManager.Instance.Craft.OnChangeItems();
		}
		else if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				Item.Favorite = !Item.Favorite;
				Favorite.enabled = Item.Favorite;
			}
			else
			{
				GameManager.Instance.Craft.window.ChangeFilter(Key);
			}
		}
	}
}
