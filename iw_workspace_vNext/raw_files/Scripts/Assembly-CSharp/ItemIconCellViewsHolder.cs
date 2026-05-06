using frame8.ScrollRectItemsAdapter.Util.GridView;

public class ItemIconCellViewsHolder : CellViewsHolder
{
	private ItemSelectIcon visual;

	public void UpdateViews(Item item)
	{
		visual.Init(item);
	}

	public override void CollectViews()
	{
		base.CollectViews();
		visual = views.Find("Item").GetComponent<ItemSelectIcon>();
	}
}
