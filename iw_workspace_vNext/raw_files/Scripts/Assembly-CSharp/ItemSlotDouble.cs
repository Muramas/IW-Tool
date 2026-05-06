using UnityEngine.UI;

public class ItemSlotDouble : ItemSlot
{
	public Image Icon2;

	public Image Frame2;

	public Image Unavailable2;

	public Image Back2;

	public Item Item2;

	public override void ShowItem(Item item)
	{
		if (!Icon.enabled)
		{
			base.ShowItem(item);
			return;
		}
		if (item != null)
		{
			Item2 = item;
			Icon2.sprite = item.Icon;
		}
		UpdateSecond();
	}

	public override void ClearVisual()
	{
		base.ClearVisual();
		Icon2.enabled = false;
		Frame2.enabled = false;
		Unavailable2.enabled = false;
		Back2.sprite = GameManager.Instance.Craft.window.ItemSlots[0];
	}

	protected override void CheckUnavailable()
	{
		base.CheckUnavailable();
		if (Item2 != null)
		{
			if (Unavailable2.enabled != !Item2.active)
			{
				Unavailable2.enabled = !Item2.active;
			}
		}
		else if (Unavailable2.enabled)
		{
			Unavailable2.enabled = false;
		}
	}

	private void UpdateSecond()
	{
		int num = 0;
		if (Item2 != null)
		{
			num = Item2.Tier;
			Icon2.sprite = Item2.Icon;
			Icon2.enabled = true;
			Frame2.sprite = GameManager.Instance.Craft.window.Frames[Item2.Tier];
			Frame2.enabled = true;
			Unavailable2.enabled = !Item.active;
		}
		else
		{
			Icon2.enabled = false;
			Frame2.enabled = false;
			Unavailable2.enabled = false;
		}
		Back2.sprite = GameManager.Instance.Craft.window.ItemSlots[num];
	}
}
