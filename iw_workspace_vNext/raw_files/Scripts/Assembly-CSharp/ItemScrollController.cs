using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using frame8.ScrollRectItemsAdapter.Util.GridView;

public class ItemScrollController : GridAdapter<MyGridParams, ItemCellViewsHolder>
{
	public bool inited;

	public List<Item> Data;

	[SerializeField]
	private Scrollbar scroll;

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
		float value = scroll.value;
		Refresh(contentPanelEndEdgeStationary);
		scroll.value = value;
	}

	public void AddData(Item data)
	{
		if (!inited)
		{
			Initialize();
		}
		bool contentPanelEndEdgeStationary = Data != null;
		Data.Insert(0, data);
		float value = scroll.value;
		Refresh(contentPanelEndEdgeStationary);
		scroll.value = value;
	}

	protected override void UpdateCellViewsHolder(ItemCellViewsHolder viewsHolder)
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
