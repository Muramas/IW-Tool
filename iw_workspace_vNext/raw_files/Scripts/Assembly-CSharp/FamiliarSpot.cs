using System;
using UnityEngine;

public class FamiliarSpot : MonoBehaviour
{
	[SerializeField]
	private FamiliarObj familiarObj;

	private Familiar currentFamiliar;

	private void OnEnable()
	{
		BackgroundManager backs = GameManager.Instance.Interior.Backs;
		backs.OnChange = (Action)Delegate.Combine(backs.OnChange, new Action(UpdateColor));
	}

	private void OnDisable()
	{
		BackgroundManager backs = GameManager.Instance.Interior.Backs;
		backs.OnChange = (Action)Delegate.Remove(backs.OnChange, new Action(UpdateColor));
	}

	private void UpdateColor()
	{
		Color color = GameManager.Instance.Interior.Backs.GetColor();
		familiarObj?.SetColor(color);
	}

	public void SetFamiliar(Familiar familiar)
	{
		if (currentFamiliar != familiar)
		{
			if (familiarObj != null)
			{
				UnityEngine.Object.Destroy(familiarObj.gameObject);
				familiarObj = null;
			}
			if (familiar != null)
			{
				familiarObj = UnityEngine.Object.Instantiate(Resources.Load<FamiliarObj>("Familiars/" + familiar.Prefab), base.transform);
			}
			currentFamiliar = familiar;
			UpdateColor();
		}
	}
}
