using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftWindow : Window
{
	public CraftingResourcesPanel resources;

	public TextMeshProUGUI de_cores;

	public CraftingMenu craftingMenu;

	public ItemsDoll doll;

	public MythicController forge;

	public SlotUpgradeController slotUpgrade;

	public SkillVisual crafting;

	public SkillVisual gathering;

	public ItemScrollController itemsScroll;

	public ItemGridController itemsGrid;

	public Sprite[] Frames;

	public Sprite[] ItemSlots;

	public FlexTip Tip;

	public TMP_Dropdown sorting;

	public TMP_Dropdown filter;

	public Toggle grid;

	public Toggle available;

	public Toggle favorite;

	public Toggle equipped;

	public Toggle enchanted;

	public EnchantAll enchantAll;

	public string[] TierNames;

	public Vector2 SelectTipOffset = new Vector2(0f, 0f);

	public Vector2 SlotTipOffset = new Vector2(0f, -0.45f);

	public DisableCanvas canvas;

	public ItemPresetWindow presets;

	public Button DisAll;

	public Button OpenDoll;

	public Button OpenForge;

	public Button OpenCraft;

	public Button OpenProficiency;

	public Action OnChangeEquip;

	[SerializeField]
	private ItemsFilterByWords wordFilter;

	private Dictionary<int, SlotKey> filterMap;

	private float timer;

	private void Awake()
	{
		filterMap = new Dictionary<int, SlotKey>
		{
			{
				1,
				SlotKey.Head
			},
			{
				2,
				SlotKey.Chest
			},
			{
				3,
				SlotKey.Shoulder
			},
			{
				4,
				SlotKey.Hands
			},
			{
				5,
				SlotKey.Boots
			},
			{
				6,
				SlotKey.Neck
			},
			{
				7,
				SlotKey.Ring
			},
			{
				8,
				SlotKey.Waist
			},
			{
				9,
				SlotKey.Back
			},
			{
				10,
				SlotKey.Wrist
			},
			{
				11,
				SlotKey.Weapon
			},
			{
				12,
				SlotKey.Offhand
			},
			{
				13,
				SlotKey.Pants
			},
			{
				14,
				SlotKey.Research
			},
			{
				15,
				SlotKey.Misc
			},
			{
				16,
				SlotKey.Mount
			},
			{
				17,
				SlotKey.Accessory
			},
			{
				18,
				SlotKey.Phylactery
			}
		};
	}

	private void OnEnable()
	{
		OpenForge.gameObject.SetActive(GameManager.Instance.Paragon.ForgeIsAvailable);
		OpenProficiency.gameObject.SetActive(GameManager.Instance.Paragon.ProficiencyIsAvailable);
		if (!GameManager.Instance.Paragon.ForgeIsAvailable || !GameManager.Instance.Paragon.ProficiencyIsAvailable)
		{
			OpenItemDoll();
		}
	}

	public void Init(CraftInvestment invest)
	{
		craftingMenu.Init(invest);
		doll.Init();
		slotUpgrade.Init();
		forge.Init();
		foreach (ItemSlot slot in doll.Slots)
		{
			slot.OnEquip = (Action<Item>)Delegate.Combine(slot.OnEquip, new Action<Item>(OnSlotEquipChanged));
		}
	}

	private void OnSlotEquipChanged(Item item)
	{
		ChangeFilter();
	}

	public bool CheckAvailable()
	{
		GameManager instance = GameManager.Instance;
		if (instance.Paragon.ItemsIsAvailable)
		{
			if (instance.ChallengeManager.ActiveChallenge != null)
			{
				return instance.ChallengeManager.ActiveChallenge.Rules.Items;
			}
			return true;
		}
		return false;
	}

	public override void Open()
	{
		if (CheckAvailable())
		{
			GameManager.Instance.AttributeManager.Panel.Close();
			SkillLabels();
			GameManager.Instance.Craft.ResetDropList();
			craftingMenu.Refresh();
			doll.RefreshSlots();
			UpdateEnchantLevel();
			ChangeFilter();
			ChangeSorting();
			base.gameObject.SetActive(value: true);
			canvas.On();
		}
	}

	public void OpenItemDoll()
	{
		doll.gameObject.SetActive(value: true);
		forge.gameObject.SetActive(value: false);
		craftingMenu.gameObject.SetActive(value: false);
		slotUpgrade.gameObject.SetActive(value: false);
		ChangeFilter();
	}

	public void OpenItemForge()
	{
		doll.gameObject.SetActive(value: false);
		forge.gameObject.SetActive(value: true);
		craftingMenu.gameObject.SetActive(value: false);
		slotUpgrade.gameObject.SetActive(value: false);
		ChangeFilter();
	}

	public void OpenCraftingMenu()
	{
		doll.gameObject.SetActive(value: false);
		forge.gameObject.SetActive(value: false);
		craftingMenu.gameObject.SetActive(value: true);
		slotUpgrade.gameObject.SetActive(value: false);
		ChangeFilter();
	}

	public void OpenGildingMenu()
	{
		doll.gameObject.SetActive(value: false);
		forge.gameObject.SetActive(value: false);
		craftingMenu.gameObject.SetActive(value: false);
		slotUpgrade.gameObject.SetActive(value: true);
		ChangeFilter();
	}

	public override void Close()
	{
		Settings.BlockInput = false;
		canvas.Off();
		base.gameObject.SetActive(value: false);
		CloseTip();
	}

	public void ClearAll()
	{
		doll.ClearAll();
	}

	protected override void Update()
	{
		base.Update();
		OpenDoll.interactable = !doll.gameObject.activeSelf;
		OpenForge.interactable = !forge.gameObject.activeSelf;
		OpenCraft.interactable = !craftingMenu.gameObject.activeSelf;
		OpenProficiency.interactable = !slotUpgrade.gameObject.activeSelf;
		timer += Time.unscaledDeltaTime;
		if (timer > 1f)
		{
			timer = 0f;
			UpdateEnchantLevel();
		}
	}

	public void UpdateEnchantLevel()
	{
		foreach (ItemSlot slot in doll.Slots)
		{
			slot.UpdateEnchantLevel();
		}
	}

	public void DisenchantAll()
	{
		CraftManager craft = GameManager.Instance.Craft;
		if (GameManager.Instance.Nullifier.ValueInt >= 10)
		{
			GameManager.Instance.ConfirmWindow.Open(string.Format("DisenchantTooltip".Translate(), "10 <sprite=4>"), craft.ApplyDisenchantmentAll);
		}
	}

	public void UpdateList()
	{
		filter.value = 0;
		ChangeFilter();
	}

	public void OpenTip(string text, Transform target, Vector2 offset, float width = 400f)
	{
		Tip.SetText(text, target, offset, width);
	}

	public void CloseTip()
	{
		Tip.Close();
	}

	public void ChangeSorting()
	{
		List<Item> list = (grid.isOn ? itemsGrid.Data : itemsScroll.Data);
		if (list != null)
		{
			list = Sort(list);
			SetData(list);
		}
	}

	public List<Item> Sort(List<Item> list, int sort = -1)
	{
		if (sort == -1)
		{
			sort = sorting.value;
		}
		switch (sort)
		{
		case 0:
			list.Sort(CompareItemBySlot);
			break;
		case 1:
			list.Sort(CompareItemByQuality);
			break;
		case 2:
			list.Sort(CompareItemByQualityDescending);
			break;
		case 3:
			list.Sort(CompareItemBySet);
			break;
		case 4:
			list.Sort(CompareItemByEnchant);
			break;
		}
		return list;
	}

	public void UpdateDECores()
	{
		de_cores.transform.parent.gameObject.SetActive(GameManager.Instance.Paragon.EnchantingIsAvailable);
		UpdateDisenchantAll();
	}

	public void SetData(List<Item> data)
	{
		if (grid.isOn)
		{
			itemsGrid.SetData(data);
		}
		else
		{
			itemsScroll.SetData(data);
		}
	}

	public List<Item> GetData()
	{
		if (grid.isOn)
		{
			return itemsGrid.Data;
		}
		return itemsScroll.Data;
	}

	public void AddData(Item data)
	{
		if (filter.value != 0)
		{
			if (grid.isOn)
			{
				itemsGrid.AddData(data);
			}
			else
			{
				itemsScroll.AddData(data);
			}
		}
		else if (grid.isOn)
		{
			itemsGrid.SetData(GetAllToShow());
		}
		else
		{
			itemsScroll.SetData(GetAllToShow());
		}
	}

	public void SwitchGridScroll()
	{
		if (grid.isOn)
		{
			SetData(itemsScroll.Data);
			itemsScroll.gameObject.SetActive(value: false);
			itemsGrid.gameObject.SetActive(value: true);
		}
		else
		{
			SetData(itemsGrid.Data);
			itemsGrid.ScrollTo(0);
			itemsScroll.gameObject.SetActive(value: true);
			itemsGrid.gameObject.SetActive(value: false);
		}
	}

	public void ChangeFilter(SlotKey slot)
	{
		try
		{
			filter.value = filterMap.First((KeyValuePair<int, SlotKey> x) => x.Value == slot).Key;
		}
		catch
		{
			Debug.Log("WRONG KEY SLOT");
		}
	}

	public void ChangeFilter()
	{
		SlotKey slotKey = SlotKey.Head;
		List<Item> list = null;
		if (filter.value == 0)
		{
			list = ((!available.isOn) ? GetAllToShow() : GetAvailable());
		}
		else if (filterMap.ContainsKey(filter.value))
		{
			slotKey = filterMap[filter.value];
		}
		if (list == null)
		{
			list = new List<Item>();
			List<Item> allToShow = GetAllToShow();
			for (int i = 0; i < allToShow.Count; i++)
			{
				Item item = allToShow[i];
				if (item.Slot == slotKey && reqsFilter(item))
				{
					list.Add(item);
				}
			}
		}
		if (equipped.isOn)
		{
			list = ((!available.isOn) ? list.FindAll((Item x) => x.equiped) : list.FindAll((Item x) => x.active));
		}
		if (enchanted.isOn)
		{
			list = list.FindAll((Item x) => x.Enchant != null && x.Enchant.Level > 0);
		}
		if (favorite.isOn)
		{
			list = list.FindAll((Item x) => x.Favorite);
		}
		SetData(Sort(list));
		wordFilter?.ApplyWordFilter();
		enchantAll.Recalculate(GetData());
		if (!grid.isOn)
		{
			itemsScroll.ScrollTo(0);
		}
	}

	public bool ForgeIsOpen()
	{
		return forge.gameObject.activeSelf;
	}

	private List<Item> GetAvailable()
	{
		if (ForgeIsOpen())
		{
			return GameManager.Instance.Craft.AvailableItems.FindAll((Item x) => x.Tier == 6);
		}
		if (doll.gameObject.activeSelf || slotUpgrade.gameObject.activeSelf)
		{
			return GameManager.Instance.Craft.AvailableItems.FindAll((Item x) => doll.SlotIsAvailable(x.Slot) && x.CheckReqs());
		}
		return GameManager.Instance.Craft.AvailableItems.FindAll((Item x) => doll.SlotIsAvailable(x.Slot));
	}

	private List<Item> GetAllToShow()
	{
		if (ForgeIsOpen())
		{
			return GameManager.Instance.Craft.AvailableItems.FindAll((Item x) => x.Tier == 6);
		}
		return GameManager.Instance.Craft.AvailableItems.FindAll((Item x) => doll.SlotIsAvailable(x.Slot));
	}

	public void UpdateDisenchantAll()
	{
		if (GameManager.Instance.Paragon.EnchantingIsAvailable)
		{
			DisAll.gameObject.SetActive(value: true);
			bool flag = GameManager.Instance.Nullifier.ValueInt >= 10;
			if (flag)
			{
				flag = GameManager.Instance.Craft.AvailableItems.FindAll((Item x) => x.Enchant != null && x.Enchant.Level > 0).Count >= 10;
			}
			DisAll.interactable = flag;
		}
		else
		{
			DisAll.gameObject.SetActive(value: false);
		}
	}

	public void SkillLabels()
	{
		crafting.UpdateLabel();
		gathering.UpdateLabel();
	}

	private int CompareItemBySlot(Item x, Item y)
	{
		if (x.Slot == y.Slot)
		{
			return CompareItemByQuality(x, y);
		}
		if (x.Slot < y.Slot)
		{
			return -1;
		}
		return 1;
	}

	private int CompareItemBySet(Item x, Item y)
	{
		if (x.Set == null && y.Set != null)
		{
			return 1;
		}
		if (x.Set != null && y.Set == null)
		{
			return -1;
		}
		if (x.Set == y.Set)
		{
			return CompareItemBySlot(x, y);
		}
		if (x.Set.Key < y.Set.Key)
		{
			return -1;
		}
		return 1;
	}

	private int CompareItemByEnchant(Item x, Item y)
	{
		int num = ((y.Enchant != null) ? y.Enchant.Level : 0);
		int num2 = ((x.Enchant != null) ? x.Enchant.Level : 0);
		if (num == num2)
		{
			if (num == 0)
			{
				if (x.Tier > y.Tier)
				{
					return -1;
				}
				if (x.Tier < y.Tier)
				{
					return 1;
				}
				if (x.ProgressInvested != y.ProgressInvested)
				{
					if (x.ProgressInvested > y.ProgressInvested)
					{
						return -1;
					}
					return 1;
				}
				return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
			}
			return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
		}
		return num - num2;
	}

	private int CompareItemByQuality(Item x, Item y)
	{
		if (x.Tier == y.Tier)
		{
			return CompareItemByEnchant(x, y);
		}
		if (x.Tier > y.Tier)
		{
			return -1;
		}
		if (x.Tier < y.Tier)
		{
			return 1;
		}
		return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
	}

	private int CompareItemByQualityDescending(Item x, Item y)
	{
		return -CompareItemByQuality(x, y);
	}

	private bool reqsFilter(Item item)
	{
		if (available.isOn)
		{
			return item.CheckReqs();
		}
		return true;
	}
}
