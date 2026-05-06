using frame8.ScrollRectItemsAdapter.Util.GridView;

public class ItemCellViewsHolder : CellViewsHolder
{
	private ItemSelect visual;

	public void UpdateViews(Item item)
	{
		visual.Init(item);
	}

	public override void CollectViews()
	{
		base.CollectViews();
		visual = views.Find("Item").GetComponent<ItemSelect>();
	}
}
