using UnityEngine;

public class FamiliarButtonLocker : MonoBehaviour
{
	[SerializeField]
	private BottomPanelButton familiarMenu;

	[SerializeField]
	private FamiliarManager familiars;

	private float timer;

	private void Update()
	{
		timer += Time.unscaledDeltaTime;
		if (timer > 1f)
		{
			timer = 0f;
			if (familiars.HasAnyReliquaries())
			{
				familiarMenu.TurnOnRed();
			}
			else
			{
				familiarMenu.TurnOff();
			}
			if (familiars.gameObject.activeSelf)
			{
				familiarMenu.TurnOnBlue();
			}
		}
	}
}
