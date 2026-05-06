using ModelShark;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpellUpgradeVisual : MonoBehaviour, ITooltipBodyHandler, ITooltipHeaderHandler
{
	public Spells Key;

	[SerializeField]
	private Image icon;

	[SerializeField]
	private TextMeshProUGUI lvl;

	[SerializeField]
	private Button invest;

	[SerializeField]
	private TextMeshProUGUI cost;

	[SerializeField]
	private Button plusButton;

	[SerializeField]
	private Button minusButton;

	[SerializeField]
	private ModelShark.TooltipTrigger tooltip;

	private SpellUpgrade upgrade;

	private SpellGilding manager;

	private int count = 1;

	public void Init(Spells key)
	{
		Key = key;
		icon.sprite = GameManager.Instance.SpellBook.atlas.Get(Key.ToString());
		manager = GameManager.Instance.Gilding.Spells;
		upgrade = manager.Get(key);
		UpdateText();
		tooltip.bodyHandler = this;
		tooltip.headerHandler = this;
		base.gameObject.SetActive(value: true);
	}

	private void Update()
	{
		if ((!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) || (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)))
		{
			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
			{
				SetCount(5);
			}
			else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				SetCount(10);
			}
			else
			{
				SetCount(1);
			}
		}
	}

	private void SetCount(int count)
	{
		if (this.count != count)
		{
			this.count = count;
			UpdateText();
		}
	}

	public void UpdateText()
	{
		BigNumber number = GetCost();
		if (upgrade == null)
		{
			lvl.text = "0/0";
		}
		else
		{
			lvl.text = upgrade.Current + "/" + upgrade.Level;
		}
		cost.text = number.ToReadableString("F0");
		UpdateButton();
		minusButton.interactable = upgrade != null && upgrade.Current > 0;
		plusButton.interactable = upgrade != null && upgrade.Current < upgrade.Level;
		tooltip.UpdateText();
	}

	public void UpdateButton()
	{
		invest.interactable = manager.ink.Value.Floor() >= GetCost();
	}

	public void Invest()
	{
		int num = count;
		if (num < 0)
		{
			num = 1;
		}
		for (int i = 0; i < num; i++)
		{
			Upgrade();
		}
		GameManager.Instance.Gilding.Spells.CheckMax(upgrade.Level);
		UpdateText();
	}

	private void Upgrade()
	{
		BigNumber bigNumber = GetCost(1);
		if (!(manager.ink.Value.Floor() < bigNumber))
		{
			manager.ink.Change(-bigNumber);
			if (upgrade == null)
			{
				upgrade = manager.Add(Key);
			}
			upgrade.Upgrade();
			manager.mastery.AddExp(bigNumber.Pow(1.0499999523162842));
		}
	}

	public void Minus()
	{
		if (upgrade != null)
		{
			upgrade.Minus(count);
			UpdateText();
		}
	}

	public void Plus()
	{
		if (upgrade != null)
		{
			upgrade.Plus(count);
			UpdateText();
		}
	}

	private BigNumber GetCost(int toBuy = 0)
	{
		BigNumber bigNumber = 0.0;
		if (toBuy == 0)
		{
			toBuy = count;
			if (toBuy < 0)
			{
				toBuy = 1;
			}
		}
		if (upgrade == null)
		{
			return manager.GetCost(0, toBuy);
		}
		return manager.GetCost(upgrade.Level, toBuy);
	}

	public string GetHeader()
	{
		if (upgrade == null)
		{
			return GameManager.Instance.SpellBook.GetSpell(Key).Name.Translate();
		}
		return upgrade.Spell.Name.Translate();
	}

	public string GetBody()
	{
		if (upgrade == null)
		{
			return SpellUpgrade.GetDescription(GameManager.Instance.SpellBook.GetSpell(Key));
		}
		return upgrade.GetDescription();
	}
}
