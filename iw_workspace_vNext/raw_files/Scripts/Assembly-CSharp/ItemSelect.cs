using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSelect : ItemSelectIcon
{
	public TextMeshProUGUI NameLabel;

	public GameObject rustyBar;

	public Image rustyProgressBar;

	public TextMeshProUGUI rustyLabel;

	public GameObject progressBar;

	public Image progress;

	public Image progressPreview;

	public TextMeshProUGUI progressPreviewText;

	public GameObject costs;

	public TextMeshProUGUI red;

	public TextMeshProUGUI green;

	public TextMeshProUGUI blue;

	public TextMeshProUGUI yellow;

	public TextMeshProUGUI perfect;

	public Button button;

	public GameObject enchantBar;

	public TextMeshProUGUI levelLabel;

	public TextMeshProUGUI enchantingDust;

	public Button buttonChant;

	public Button buttonDChant;

	private int enchLvl = 1;

	public override void Init(Item item)
	{
		base.Init(item);
		UpdateLabels();
	}

	private void OnEnable()
	{
		if (Item != null)
		{
			UpdateLabels();
		}
	}

	private void OnDisable()
	{
		HidePreview();
	}

	private void Update()
	{
		if (Item == null)
		{
			return;
		}
		CheckButton();
		Icon.color = Color.white;
		if (!enchantBar.activeSelf)
		{
			return;
		}
		int num = enchLvl;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			enchLvl = 5;
			if (Item.Enchant.Level + enchLvl >= GameManager.Instance.Craft.MaxEnchantLevel.ValueInt)
			{
				enchLvl = GameManager.Instance.Craft.MaxEnchantLevel.ValueInt - Item.Enchant.Level;
			}
		}
		else
		{
			enchLvl = 1;
		}
		if (num != enchLvl)
		{
			UpdateLabels();
		}
	}

	public void Upgrade()
	{
		if (!CheckCost())
		{
			return;
		}
		Dictionary<CraftResource, int> cost = Item.GetCost();
		BigNumber cost2 = 0.0;
		foreach (KeyValuePair<CraftResource, int> item in cost)
		{
			if (item.Value >= 1)
			{
				GameManager.Instance.Craft.map[item.Key].Change(-item.Value);
				cost2 += (BigNumber)item.Value;
			}
		}
		GameManager.Instance.Craft.window.craftingMenu.rune.AddProgress(cost2);
		float num = GetProgress();
		if (Settings.SoundOn)
		{
			GameManager.Instance.Craft.PlaySuccessSFX();
		}
		Item.Invest(num);
		craft.Crafting.AddExp(num / (craft.BaseProgressToUpgradeItem * 1.65f));
		GameManager.Instance.Craft.window.craftingMenu.UpdateLabels();
		UpdateLabels();
		ShowPreview();
		GameManager.Instance.Craft.OnChangeItems();
	}

	public void Enchant()
	{
		if (Item.Enchant != null && Item.Enchant.Level < GameManager.Instance.Craft.MaxEnchantLevel.ValueInt)
		{
			Item.Enchant.Upgrade(enchLvl);
			if (Item.active && !Item.Enchant.applied)
			{
				Item.Enchant.Apply();
			}
			if (Settings.SoundOn)
			{
				GameManager.Instance.Craft.PlayFailSFX();
			}
			UpdateEnchanting();
			UpdateDE();
			GameManager.Instance.Craft.OnChangeItems();
			GameManager.Instance.Craft.OnEnchantItem(enchLvl);
		}
	}

	public void Disenchant()
	{
		if (Item.Enchant != null && GameManager.Instance.Nullifier.ValueInt > 0)
		{
			GameManager.Instance.ConfirmWindow.Open(string.Format("DisenchantItemTooltip".Translate(), "1 <sprite=4>", Item.Enchant.GetFullCost().ToReadableString("F0")), ApplyDisEnchant);
		}
	}

	private void ApplyDisEnchant()
	{
		if (Item.Enchant != null)
		{
			Item.Enchant.Disenchant();
			GameManager.Instance.Craft.ApplyDisenchantment();
			UpdateEnchanting();
			UpdateDE();
		}
	}

	private void ApplyFreeDisEnchant()
	{
		if (Item.Enchant != null)
		{
			Item.Enchant.Disenchant();
			UpdateEnchanting();
			UpdateDE();
		}
	}

	public float GetProgressPreview()
	{
		float num = GetProgress();
		return (Item.ProgressInvested + num) / (float)craft.ProgressToUpgradeItemCosts;
	}

	private float GetProgress()
	{
		float num = craft.GetProgressInvestItem() * craft.BaseProgressToUpgradeItem;
		if ((Item.ProgressInvested + num) / (float)craft.ProgressToUpgradeItemCosts > 0.98f)
		{
			num = (float)craft.ProgressToUpgradeItemCosts - Item.ProgressInvested;
		}
		return num;
	}

	public void ShowPreview()
	{
		progressPreview.fillAmount = GetProgressPreview();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append((100f * Item.ProgressInvested / (float)craft.ProgressToUpgradeItemCosts).ToString("F2", CultureInfo.InvariantCulture));
		stringBuilder.Append("% -> ");
		stringBuilder.Append((progressPreview.fillAmount * 100f).ToString("F2", CultureInfo.InvariantCulture));
		stringBuilder.Append("%");
		progressPreviewText.text = stringBuilder.ToString();
		progressPreviewText.gameObject.SetActive(value: true);
		progressPreview.gameObject.SetActive(value: true);
	}

	public void ShowProgres()
	{
		progressPreviewText.text = (100f * Item.ProgressInvested / (float)craft.ProgressToUpgradeItemCosts).ToString("F2", CultureInfo.InvariantCulture) + "%";
		progressPreviewText.gameObject.SetActive(value: true);
	}

	public void HidePreview()
	{
		perfect.gameObject.SetActive(value: false);
		progressPreview.gameObject.SetActive(value: false);
		progressPreviewText.gameObject.SetActive(value: false);
	}

	public override void UpdateLabels()
	{
		base.UpdateLabels();
		NameLabel.text = Item.Name;
		if (Item.Tier < 4)
		{
			Dictionary<CraftResource, int> cost = Item.GetCost();
			red.text = cost[CraftResource.Red].ToString();
			green.text = cost[CraftResource.Green].ToString();
			blue.text = cost[CraftResource.Blue].ToString();
			yellow.text = cost[CraftResource.Yellow].ToString();
			rustyBar.SetActive(value: false);
			progressBar.SetActive(value: true);
			progress.fillAmount = Item.ProgressInvested / (float)craft.ProgressToUpgradeItemCosts;
			enchantBar.SetActive(value: false);
		}
		else
		{
			progressBar.SetActive(value: false);
			rustyBar.SetActive(value: false);
			enchantBar.SetActive(GameManager.Instance.Paragon.EnchantingIsAvailable && Item.Enchant != null);
			UpdateEnchanting();
			UpdateDE();
		}
		CheckButton();
	}

	public void UpdateDE()
	{
		buttonDChant.gameObject.SetActive(Item.Enchant != null && Item.Enchant.Level > 0);
		buttonDChant.interactable = GameManager.Instance.Nullifier.ValueInt > 0;
	}

	public void CheckButton()
	{
		if (Item.Tier < 4)
		{
			if (Item.GetCost().Any((KeyValuePair<CraftResource, int> x) => x.Value > 0))
			{
				button.gameObject.SetActive(value: true);
				costs.SetActive(value: true);
				button.interactable = CheckCost();
			}
			else
			{
				button.gameObject.SetActive(value: false);
				costs.SetActive(value: false);
			}
		}
		else if (GameManager.Instance.Paragon.EnchantingIsAvailable)
		{
			buttonChant.interactable = Item.Enchant != null && craft.EnchantingDust.Value >= Item.Enchant.GetCost(enchLvl) && Item.Enchant.Level < craft.MaxEnchantLevel.ValueInt;
		}
	}

	protected bool CheckCost()
	{
		Dictionary<CraftResource, int> cost = Item.GetCost();
		if (BigNumber.Sign(craft.Red.Value - cost[CraftResource.Red]) >= 0 && BigNumber.Sign(craft.Green.Value - cost[CraftResource.Green]) >= 0 && BigNumber.Sign(craft.Blue.Value - cost[CraftResource.Blue]) >= 0)
		{
			return BigNumber.Sign(craft.Yellow.Value - cost[CraftResource.Yellow]) >= 0;
		}
		return false;
	}

	private void UpdateEnchanting()
	{
		if (Item.Enchant != null)
		{
			levelLabel.text = Item.Enchant.Level.ToString();
			enchantingDust.text = Item.Enchant.GetCost(enchLvl).ToReadableString("F0");
		}
		else
		{
			levelLabel.text = string.Empty;
			enchantingDust.text = string.Empty;
		}
	}
}
