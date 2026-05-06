using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AffixSelector : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI label;

	[SerializeField]
	private Image highlight;

	[SerializeField]
	private Button button;

	protected Affix affix;

	private Action<Affix> OnSelect;

	public virtual void Init(Affix affix, Action<Affix> OnSelect)
	{
		this.affix = affix;
		this.OnSelect = OnSelect;
		highlight.enabled = false;
		UpdateDescription();
		base.gameObject.SetActive(value: true);
		MasterworkController masterwork = GameManager.Instance.Craft.window.forge.masterwork;
		masterwork.OnUpgrade = (Action)Delegate.Combine(masterwork.OnUpgrade, new Action(UpdateDescription));
	}

	private void OnEnable()
	{
		UpdateDescription();
	}

	private void UpdateDescription()
	{
		if (affix != null)
		{
			label.text = affix.GetDescription(affix.GetEfficiency() != null || affix.GetGilding() != null);
		}
	}

	public void Select()
	{
		OnSelect?.Invoke(affix);
	}

	public void SetBlocking(bool isBlocked)
	{
		button.interactable = !isBlocked;
	}

	public bool CheckSelect(Affix selected)
	{
		bool result = selected == affix;
		highlight.enabled = result;
		return result;
	}
}
