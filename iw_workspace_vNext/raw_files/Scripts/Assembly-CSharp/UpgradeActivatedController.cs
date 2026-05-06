using System.Collections.Generic;
using System.Linq;
using TMPro;

public class UpgradeActivatedController : UpgradesScrollController
{
	public class CompareBignumber : IComparer<BigNumber>
	{
		public int Compare(BigNumber x, BigNumber y)
		{
			if (x > y)
			{
				return 1;
			}
			if (x < y)
			{
				return -1;
			}
			return 0;
		}
	}

	public class CompareVariable : IComparer<Variable>
	{
		public int Compare(Variable x, Variable y)
		{
			if (x.Equals(y))
			{
				return 0;
			}
			return 1;
		}
	}

	public UpgradeManager manager;

	public TMP_Dropdown filter;

	private CompareBignumber comparatorBignumber;

	protected override void Start()
	{
		Data = new List<Upgrade>();
		comparatorBignumber = new CompareBignumber();
		base.Start();
		inited = true;
		UpdateData();
	}

	protected void OnEnable()
	{
		UpdateData();
	}

	public void UpdateData()
	{
		if (!inited)
		{
			return;
		}
		List<Upgrade> list = manager.ActiveUpgradeList;
		switch (filter.value)
		{
		case 1:
			list = list.FindAll((Upgrade x) => x.Data.V.StartsWith("Base"));
			break;
		case 2:
			list = list.FindAll((Upgrade x) => x.Data.V.StartsWith("Building"));
			break;
		case 3:
			list = list.FindAll((Upgrade x) => x.Data.V.StartsWith("Hero") || x.Data.V.StartsWith("Exorcist") || x.Data.V.StartsWith("Chrono") || x.Data.V.StartsWith("Shadow"));
			break;
		case 4:
			list = list.FindAll((Upgrade x) => x.Data.V.StartsWith("Pet"));
			break;
		case 5:
			list = list.FindAll((Upgrade x) => x.Data.V.StartsWith("Spell"));
			break;
		case 6:
			list = list.FindAll((Upgrade x) => x.Data.V.StartsWith("Click"));
			break;
		case 7:
			list = list.FindAll((Upgrade x) => x.Data.V.StartsWith("VoidMana"));
			break;
		case 8:
			list = list.FindAll((Upgrade x) => !string.IsNullOrEmpty(x.Data.CondtionAccess) && x.Data.CondtionAccess.StartsWith("Achieve") && !x.Data.CAParameter.StartsWith("T_"));
			break;
		case 9:
			list = list.FindAll((Upgrade x) => !string.IsNullOrEmpty(x.Data.CondtionAccess) && x.Data.CondtionAccess.StartsWith("Achieve") && x.Data.CAParameter.StartsWith("T_"));
			break;
		}
		List<Upgrade> data = (from x in list
			orderby x.Data.V, x.Data.CAParameter
			select x).ThenBy((Upgrade x) => x.Cost, comparatorBignumber).ToList();
		SetData(data);
	}
}
