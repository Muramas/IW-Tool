using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
	public Transform content;

	public UpgradeVisual prefab;

	public TextMeshProUGUI tips;

	public SpriteAtlas Atlas;

	public Sprite[] tierMap;

	public List<Upgrade> UpgradeList;

	public List<Upgrade> AvailableUpgradeList;

	public List<Upgrade> ActiveUpgradeList;

	public Button available;

	public Button activated;

	public ScrollRect upgradesScroll;

	public LinkedList<Upgrade> UpgradesChecksList;

	private List<Upgrade> SpellUnlocks;

	private List<Upgrade> Buildings;

	public RankDefiner RankDefiner;

	public Action update_conditional_upgrade;

	public Action<Upgrade> on_buy_upgrade;

	public Action<Upgrade> on_remove_upgrade;

	private float timer;

	private float d_time = 1f;

	[SerializeField]
	private UpgradesScrollController viewControler;

	[SerializeField]
	private UpgradeActivatedController viewActivatedControler;

	public void Init()
	{
		UpgradeList = new List<Upgrade>();
		AvailableUpgradeList = new List<Upgrade>();
		ActiveUpgradeList = new List<Upgrade>();
		UpgradesChecksList = new LinkedList<Upgrade>();
		CreateAll();
		HeroSlot currentHero = GameManager.Instance.CurrentHero;
		currentHero.OnChange = (Action)Delegate.Combine(currentHero.OnChange, new Action(filterUpgrades));
	}

	public void RemoveUpgrade(Upgrade u)
	{
		u.Delete();
		u.CheckAvailable();
	}

	public void Restart(bool isLoading = false)
	{
		UpgradesChecksList.Clear();
		AvailableUpgradeList = new List<Upgrade>();
		for (int i = 0; i < ActiveUpgradeList.Count; i++)
		{
			ActiveUpgradeList[i].Delete();
			ActiveUpgradeList[i].Available = false;
		}
		ActiveUpgradeList = new List<Upgrade>();
		for (int j = 0; j < UpgradeList.Count; j++)
		{
			UpgradeList[j].CheckAvailable();
		}
		if (!isLoading)
		{
			filterUpgrades();
			if (viewControler.inited)
			{
				CheckList();
				UpdateScroll();
				UpdateActiveScroll();
			}
		}
	}

	public void PostLoad()
	{
		for (int i = 0; i < UpgradeList.Count; i++)
		{
			UpgradeList[i].CheckAvailable();
		}
		filterUpgrades();
		if (viewControler.inited)
		{
			CheckList();
			UpdateScroll();
			UpdateActiveScroll();
		}
	}

	private void filterUpgrades()
	{
		List<int> buildings = GameManager.Instance.BuildingManager.GetAllUpgradeId();
		UpgradesChecksList = new LinkedList<Upgrade>(UpgradeList.FindAll((Upgrade x) => x.CheckClassAccess() || (x.isBuilding && buildings.Contains(x.BuildingKey))));
		int num = 0;
		foreach (Upgrade activeUpgrade in ActiveUpgradeList)
		{
			num++;
			UpgradesChecksList.Remove(activeUpgrade);
		}
		foreach (Upgrade spellUnlock in SpellUnlocks)
		{
			if (spellUnlock.CheckClassExactly() && !spellUnlock.applied)
			{
				if (!AvailableUpgradeList.Contains(spellUnlock))
				{
					AddAvailable(spellUnlock);
				}
			}
			else if (AvailableUpgradeList.Contains(spellUnlock))
			{
				AvailableUpgradeList.Remove(spellUnlock);
			}
		}
		foreach (Upgrade availableUpgrade in AvailableUpgradeList)
		{
			num++;
			UpgradesChecksList.Remove(availableUpgrade);
		}
	}

	public void BuyAll()
	{
		int count = AvailableUpgradeList.Count;
		int num = 0;
		for (int i = 0; i < count && AvailableUpgradeList[i].Cost <= GameManager.Instance.Mana.Value; i++)
		{
			AvailableUpgradeList[i].Buy(manual: false);
			num++;
		}
		if (num > 0)
		{
			AvailableUpgradeList.RemoveRange(0, num);
			UpdateScroll();
		}
		tips.transform.parent.gameObject.SetActive(value: false);
	}

	public void Remove(Upgrade upgrade)
	{
		RemoveUpgrade(upgrade);
		ActiveUpgradeList.Remove(upgrade);
		UpgradesChecksList.AddFirst(upgrade);
		UpdateActiveScroll();
		Statistic.BoughtUpgrades.Change(-1);
		if (on_remove_upgrade != null)
		{
			on_remove_upgrade(upgrade);
		}
	}

	public bool CanBuy()
	{
		bool flag = AvailableUpgradeList.Count > 0;
		if (flag)
		{
			flag = AvailableUpgradeList[0].Cost <= GameManager.Instance.Mana.Value;
		}
		return flag;
	}

	private void Update()
	{
		if (timer >= d_time)
		{
			timer -= d_time;
			UpdateUpgrade();
			CheckList();
		}
		timer += Time.unscaledDeltaTime;
	}

	public void CreateAll()
	{
		foreach (UpgradeFormat upgrade in GlobalData.Upgrades)
		{
			string effect = EffectNames.Linear.ToString();
			if (upgrade.Effect == "")
			{
				upgrade.Effect = effect;
			}
			Upgrade item = Upgrade.Factory.Create(upgrade, this);
			UpgradeList.Add(item);
		}
		UpgradeList.Sort(CompareItemByCost);
		SpellUnlocks = UpgradeList.FindAll((Upgrade x) => x.isSpell);
		Buildings = UpgradeList.FindAll((Upgrade x) => x.isBuilding);
	}

	public void CheckList()
	{
		BigNumber bigNumber = Statistic.ManaSession.Value * 100.0;
		int num = 0;
		LinkedList<Upgrade>.Enumerator enumerator = UpgradesChecksList.GetEnumerator();
		List<Upgrade> list = new List<Upgrade>();
		while (enumerator.MoveNext())
		{
			if (enumerator.Current.Available)
			{
				if (!(enumerator.Current.Cost < bigNumber))
				{
					break;
				}
				num++;
				list.Add(enumerator.Current);
				AddAvailable(enumerator.Current);
			}
		}
		if (num == 0 && AvailableUpgradeList.Count == 0)
		{
			num++;
			Upgrade upgrade = UpgradesChecksList.FirstOrDefault((Upgrade x) => x.Available);
			if (upgrade != null)
			{
				list.Add(upgrade);
				AddAvailable(upgrade);
			}
		}
		if (num > 0)
		{
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				UpgradesChecksList.Remove(list[num2]);
			}
			UpdateScroll();
			if (!activated.interactable)
			{
				UpdateActiveScroll();
			}
		}
	}

	public void UpdateScroll()
	{
		viewControler.SetData(AvailableUpgradeList);
	}

	public void UpdateActiveScroll()
	{
		viewActivatedControler.UpdateData();
	}

	public void UpdateUpgrade()
	{
		if (update_conditional_upgrade != null)
		{
			update_conditional_upgrade();
		}
	}

	private int CompareItemByCost(Upgrade x, Upgrade y)
	{
		if (x.CostBase == y.CostBase)
		{
			return 0;
		}
		if (x.CostBase > y.CostBase)
		{
			return 1;
		}
		return -1;
	}

	private UpgradeVisual create(Upgrade up)
	{
		return UnityEngine.Object.Instantiate(prefab);
	}

	public void AddAvailable(Upgrade u)
	{
		int count = AvailableUpgradeList.Count;
		for (int i = 0; i < count; i++)
		{
			if (CompareItemByCost(u, AvailableUpgradeList[i]) <= 0)
			{
				AvailableUpgradeList.Insert(i, u);
				return;
			}
		}
		AvailableUpgradeList.Add(u);
	}

	public void OpenActivatedUpgrades()
	{
		viewControler.gameObject.SetActive(value: false);
		available.interactable = true;
		activated.interactable = false;
		viewActivatedControler.gameObject.SetActive(value: true);
	}

	public void CloseActivatedUpgrades()
	{
		viewActivatedControler.gameObject.SetActive(value: false);
		available.interactable = false;
		activated.interactable = true;
		viewControler.gameObject.SetActive(value: true);
	}
}
