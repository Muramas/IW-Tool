using System.Globalization;
using ModelShark;
using UnityEngine;

public class ResourceTooltip : MonoBehaviour, ITooltipBodyHandler
{
	[SerializeField]
	private ModelShark.TooltipTrigger trigger;

	[SerializeField]
	private CraftResource key;

	private void Awake()
	{
		trigger.bodyHandler = this;
	}

	public string GetBody()
	{
		BigNumber value = GameManager.Instance.Resources.map[key].Value;
		float diminish = GameManager.Instance.Resources.GetDiminish(key);
		BigNumber value2 = GameManager.Instance.Resources.Cap.Value;
		return string.Format("CraftingDustTooltip".Translate(), (value + GameManager.Instance.Craft.map[key].Value).ToReadableString("F0"), value.ToReadableString("F0"), value2.ToReadableString(), (diminish * 100f).ToString("F2", CultureInfo.InvariantCulture));
	}
}
