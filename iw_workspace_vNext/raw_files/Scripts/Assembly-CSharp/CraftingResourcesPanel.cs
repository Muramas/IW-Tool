using System;
using TMPro;
using UnityEngine;

public class CraftingResourcesPanel : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI red;

	[SerializeField]
	private TextMeshProUGUI green;

	[SerializeField]
	private TextMeshProUGUI blue;

	[SerializeField]
	private TextMeshProUGUI orange;

	[SerializeField]
	private TextMeshProUGUI enchanting;

	private CraftManager h_manager;

	private CraftManager manager
	{
		get
		{
			if (h_manager == null)
			{
				h_manager = GameManager.Instance.Craft;
			}
			return h_manager;
		}
	}

	private void OnEnable()
	{
		Sub();
		UpdateAll();
	}

	private void OnDisable()
	{
		Unsub();
	}

	private void Sub()
	{
		VariableBignumber variableBignumber = manager.Red;
		variableBignumber.OnChange = (Action)Delegate.Combine(variableBignumber.OnChange, new Action(UpdateRed));
		VariableBignumber variableBignumber2 = manager.Blue;
		variableBignumber2.OnChange = (Action)Delegate.Combine(variableBignumber2.OnChange, new Action(UpdateBlue));
		VariableBignumber variableBignumber3 = manager.Green;
		variableBignumber3.OnChange = (Action)Delegate.Combine(variableBignumber3.OnChange, new Action(UpdateGreen));
		VariableBignumber yellow = manager.Yellow;
		yellow.OnChange = (Action)Delegate.Combine(yellow.OnChange, new Action(UpdateYellow));
		VariableBignumber enchantingDust = manager.EnchantingDust;
		enchantingDust.OnChange = (Action)Delegate.Combine(enchantingDust.OnChange, new Action(UpdateEnchanting));
	}

	private void Unsub()
	{
		VariableBignumber variableBignumber = manager.Red;
		variableBignumber.OnChange = (Action)Delegate.Remove(variableBignumber.OnChange, new Action(UpdateRed));
		VariableBignumber variableBignumber2 = manager.Blue;
		variableBignumber2.OnChange = (Action)Delegate.Remove(variableBignumber2.OnChange, new Action(UpdateBlue));
		VariableBignumber variableBignumber3 = manager.Green;
		variableBignumber3.OnChange = (Action)Delegate.Remove(variableBignumber3.OnChange, new Action(UpdateGreen));
		VariableBignumber yellow = manager.Yellow;
		yellow.OnChange = (Action)Delegate.Remove(yellow.OnChange, new Action(UpdateYellow));
		VariableBignumber enchantingDust = manager.EnchantingDust;
		enchantingDust.OnChange = (Action)Delegate.Remove(enchantingDust.OnChange, new Action(UpdateEnchanting));
	}

	private void UpdateAll()
	{
		UpdateRed();
		UpdateGreen();
		UpdateBlue();
		UpdateYellow();
		UpdateEnchanting();
	}

	private void UpdateRed()
	{
		red.text = manager.Red.Value.ToReadableString("F0");
	}

	private void UpdateBlue()
	{
		blue.text = manager.Blue.Value.ToReadableString("F0");
	}

	private void UpdateGreen()
	{
		green.text = manager.Green.Value.ToReadableString("F0");
	}

	private void UpdateYellow()
	{
		orange.text = manager.Yellow.Value.ToReadableString("F0");
	}

	private void UpdateEnchanting()
	{
		if (GameManager.Instance.Paragon.EnchantingIsAvailable)
		{
			enchanting.text = manager.EnchantingDust.Value.ToReadableString("F0");
		}
		else
		{
			enchanting.text = 0.ToString();
		}
	}
}
