public class ConditionAttribute : ConditionUnlockVariable
{
	public ConditionAttribute(AttributeBar attr, string arg, bool calculate_des = true, bool show_progress = true)
		: base(attr.parameter.Level, arg, attr.parameter.Key.ToString(), calculate_des, show_progress)
	{
	}
}
