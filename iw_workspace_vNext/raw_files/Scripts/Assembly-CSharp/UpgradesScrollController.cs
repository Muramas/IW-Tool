using System.Collections.Generic;
using frame8.ScrollRectItemsAdapter.Util.GridView;

public class UpgradesScrollController : GridAdapter<MyGridParams, UpgradeCellViewsHolder>
{
	public bool inited;

	public List<Upgrade> Data;

	protected override void Start()
	{
		if (Data == null)
		{
			Data = new List<Upgrade>();
		}
		base.Start();
		Refresh(contentPanelEndEdgeStationary: false, keepVelocity: true);
		inited = true;
	}

	public void SetData(List<Upgrade> data)
	{
		Data = data;
		if (inited)
		{
			Refresh(contentPanelEndEdgeStationary: false, keepVelocity: true);
		}
	}

	protected override void UpdateCellViewsHolder(UpgradeCellViewsHolder viewsHolder)
	{
		Upgrade upgrade = Data[viewsHolder.ItemIndex];
		viewsHolder.UpdateViews(upgrade);
	}

	public override void Refresh(bool contentPanelEndEdgeStationary, bool keepVelocity = false)
	{
		_CellsCount = Data.Count;
		base.Refresh(contentPanelEndEdgeStationary, keepVelocity);
	}
}
