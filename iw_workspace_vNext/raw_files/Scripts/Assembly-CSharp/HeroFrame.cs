using UnityEngine;
using UnityEngine.EventSystems;

public class HeroFrame : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public CharacterPanel panel;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			panel.Open();
		}
		else if (eventData.button == PointerEventData.InputButton.Right)
		{
			panel.OpenChangeClass();
		}
	}
}
