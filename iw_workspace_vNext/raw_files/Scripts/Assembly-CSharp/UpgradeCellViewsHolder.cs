using frame8.ScrollRectItemsAdapter.Util.GridView;

public class UpgradeCellViewsHolder : CellViewsHolder
{
	private UpgradeVisual visual;

	public void UpdateViews(Upgrade upgrade)
	{
		visual.SetUpgrade(upgrade);
	}

	public override void CollectViews()
	{
		base.CollectViews();
		visual = views.Find("Upgrade").GetComponent<UpgradeVisual>();
	}
}
