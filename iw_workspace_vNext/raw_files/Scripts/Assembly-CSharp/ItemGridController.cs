using System.Collections.Generic;
using frame8.ScrollRectItemsAdapter.Util.GridView;

public class ItemGridController : GridAdapter<MyGridParams, ItemIconCellViewsHolder>
{
	public bool inited;

	public List<Item> Data;

	private void Initialize()
	{
		Data = new List<Item>();
		base.Start();
		inited = true;
	}

	protected override void Start()
	{
		if (!inited)
		{
			Initialize();
		}
	}

	public void SetData(List<Item> data)
	{
		if (!inited)
		{
			Initialize();
		}
		bool contentPanelEndEdgeStationary = Data != null;
		Data = data;
		Refresh(contentPanelEndEdgeStationary);
		ScrollTo(0);
	}

	public void AddData(Item data)
	{
		if (!inited)
		{
			Initialize();
		}
		bool contentPanelEndEdgeStationary = Data != null;
		Data.Insert(0, data);
		Refresh(contentPanelEndEdgeStationary);
		ScrollTo(0);
	}

	protected override void UpdateCellViewsHolder(ItemIconCellViewsHolder viewsHolder)
	{
		Item item = Data[viewsHolder.ItemIndex];
		viewsHolder.UpdateViews(item);
	}

	public override void Refresh(bool contentPanelEndEdgeStationary, bool keepVelocity = false)
	{
		_CellsCount = Data.Count;
		base.Refresh(contentPanelEndEdgeStationary, keepVelocity);
	}
}
