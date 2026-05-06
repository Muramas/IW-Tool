using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MarketLockCrafting : MonoBehaviour
{
	[SerializeField]
	private List<Button> buttons;

	private void OnEnable()
	{
		foreach (Button button in buttons)
		{
			button.interactable = GameManager.Instance.Paragon.ItemsIsAvailable;
		}
	}
}
