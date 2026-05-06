using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ItemSelectIcon : MonoBehaviour
{
	public Item Item;

	public Image Icon;

	public Image Frame;

	public Image FavoriteMark;

	protected CraftManager craft;

	protected CraftWindow window;

	protected bool inited;

	private bool tipOpened;

	protected void Init()
	{
		craft = GameManager.Instance.Craft;
		window = craft.window;
	}

	public virtual void Init(Item item)
	{
		if (!inited)
		{
			Init();
		}
		Item = item;
		Icon.sprite = Item.Icon;
		UpdateLabels();
	}

	private void Update()
	{
		if (Item != null && FavoriteMark.enabled != Item.Favorite)
		{
			FavoriteMark.enabled = Item.Favorite;
		}
	}

	public void Equip()
	{
		Item.EquipItem();
	}

	public void EquipFromMenu()
	{
		Equip();
		if (tipOpened)
		{
			ShowTip();
		}
		GameManager.Instance.Craft.OnChangeItems();
	}

	public void ShowTip()
	{
		if (Item != null)
		{
			tipOpened = true;
			int num = 400;
			if (!string.IsNullOrEmpty(Item.LoreKey))
			{
				num = 580;
			}
			window.OpenTip(GetDescription(), Icon.transform, window.SelectTipOffset, num);
		}
	}

	public void HideTip()
	{
		window.CloseTip();
		tipOpened = false;
	}

	public string GetDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Item.Preview());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("ItemFavorite".Translate());
		stringBuilder.Append("ItemEquip".Translate());
		return stringBuilder.ToString();
	}

	public virtual void UpdateLabels()
	{
		FavoriteMark.enabled = Item.Favorite;
		Frame.sprite = window.Frames[Item.Tier];
	}
}
