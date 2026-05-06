using ModelShark;
using TMPro;
using UnityEngine;

public class FamiliarTagDescription : MonoBehaviour, ITooltipBodyHandler
{
	public ModelShark.TooltipTrigger tooltip;

	public FamiliarTags Tag;

	public string Description;

	public string DescriptionTooltip;

	public TextMeshProUGUI DescriptionSelected;

	private void Awake()
	{
		tooltip.bodyHandler = this;
	}

	private void OnEnable()
	{
		UpdateDescription();
	}

	public void UpdateDescription()
	{
		DescriptionSelected.text = GetDescription() + " +" + (new BigNumber((float)GameManager.Instance.Familiars.RankTotalsByTag[Tag].ValueInt * 0.01f) * 100.0).ToReadableString() + "%";
	}

	public string GetDescription()
	{
		return string.Format(Description.Translate(), Tag.ToString().Translate());
	}

	public string GetTooltipDescription()
	{
		return string.Format(DescriptionTooltip.Translate(), Tag.ToString().Translate(), GameManager.Instance.Familiars.RankTotalsByTag[Tag].ValueInt);
	}

	public string GetBody()
	{
		return GetTooltipDescription();
	}
}
