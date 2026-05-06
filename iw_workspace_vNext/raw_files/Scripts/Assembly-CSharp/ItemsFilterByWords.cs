using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ItemsFilterByWords : MonoBehaviour
{
	[SerializeField]
	private CraftWindow window;

	[SerializeField]
	private TMP_InputField input;

	public void OnEditEnd()
	{
		window.ChangeFilter();
	}

	public void ApplyWordFilter()
	{
		if (input == null || string.IsNullOrEmpty(input.text))
		{
			return;
		}
		List<string> list = new List<string>(input.text.ToLower().Split(';'));
		List<Item> list2 = window.GetData();
		if (list2 == null)
		{
			return;
		}
		foreach (string key in list)
		{
			if (!string.IsNullOrEmpty(key))
			{
				list2 = list2.FindAll((Item x) => x.Name.ToLower().Contains(key) || x.GetDescr().ToLower().Contains(key) || (x.Enchant != null && x.Enchant.GetDescription().ToLower().Contains(key)) || (x.Set != null && x.Set.Name.ToLower().Contains(key)));
			}
		}
		window.SetData(list2);
	}

	public void OnSelect()
	{
		Settings.BlockInput = true;
	}

	public void OnDeselect()
	{
		Settings.BlockInput = false;
	}

	public void ClearFilter()
	{
		input.text = string.Empty;
		ApplyWordFilter();
	}
}
