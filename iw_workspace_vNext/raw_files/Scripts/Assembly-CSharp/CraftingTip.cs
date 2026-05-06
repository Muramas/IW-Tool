using System.Text;
using ModelShark;
using UnityEngine;

public class CraftingTip : MonoBehaviour, ITooltipBodyHandler, ITooltipHeaderHandler
{
	[SerializeField]
	private ModelShark.TooltipTrigger tooltip;

	private void Awake()
	{
		tooltip.bodyHandler = this;
		tooltip.headerHandler = this;
	}

	public string GetBody()
	{
		CraftManager craft = GameManager.Instance.Craft;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Level ");
		stringBuilder.AppendLine(craft.Crafting.level.ToString());
		stringBuilder.Append("Progress: ");
		stringBuilder.Append(craft.Crafting.exp.ToReadableString("F0"));
		stringBuilder.Append(" / ");
		stringBuilder.AppendLine(craft.Crafting.exp2lvl.ToReadableString("F0"));
		stringBuilder.AppendLine();
		stringBuilder.Append("Increases the amount of progress gained when investing resources into improvement and creation of a new item. Also increases Enchanting Dust gains from Experiments. The bonus is ");
		stringBuilder.Append(craft.Crafting.bonus * 100f);
		stringBuilder.Append("% per level. Total bonus: ");
		stringBuilder.Append(craft.Crafting.bonus * (float)craft.Crafting.level * 100f);
		stringBuilder.AppendLine("%");
		return stringBuilder.ToString();
	}

	public string GetHeader()
	{
		return "<b>Crafting</b>";
	}
}
