using System.Collections.Generic;
using UnityEngine;

public class CraftInvestment
{
	public int costRed;

	public int costGreen;

	public int costBlue;

	public int costYellow;

	public CraftInvestment()
	{
		Recalculate();
	}

	public void Spend()
	{
		GameManager.Instance.Craft.Red.Change(-costRed);
		GameManager.Instance.Craft.Blue.Change(-costBlue);
		GameManager.Instance.Craft.Green.Change(-costGreen);
		GameManager.Instance.Craft.Yellow.Change(-costYellow);
		Recalculate();
	}

	public void Recalculate()
	{
		costRed = Random.Range(200, 401);
		costGreen = Random.Range(200, 401);
		costBlue = Random.Range(200, 401);
		costYellow = Random.Range(200, 401);
	}

	public List<SaveData.IntIntPair> GetCost()
	{
		return new List<SaveData.IntIntPair>
		{
			new SaveData.IntIntPair(1, costRed),
			new SaveData.IntIntPair(2, costGreen),
			new SaveData.IntIntPair(3, costBlue),
			new SaveData.IntIntPair(4, costYellow)
		};
	}

	public void LoadCost(List<SaveData.IntIntPair> list)
	{
		if (list == null || list.Count == 0 || list.Find((SaveData.IntIntPair x) => x.ID == 1).Value < 1)
		{
			Recalculate();
			return;
		}
		costRed = list.Find((SaveData.IntIntPair x) => x.ID == 1).Value;
		costGreen = list.Find((SaveData.IntIntPair x) => x.ID == 2).Value;
		costBlue = list.Find((SaveData.IntIntPair x) => x.ID == 3).Value;
		costYellow = list.Find((SaveData.IntIntPair x) => x.ID == 4).Value;
	}

	public bool CheckCost()
	{
		if (BigNumber.Sign(GameManager.Instance.Craft.Red.Value - costRed) >= 0 && BigNumber.Sign(GameManager.Instance.Craft.Green.Value - costGreen) >= 0 && BigNumber.Sign(GameManager.Instance.Craft.Blue.Value - costBlue) >= 0)
		{
			return BigNumber.Sign(GameManager.Instance.Craft.Yellow.Value - costYellow) >= 0;
		}
		return false;
	}
}
