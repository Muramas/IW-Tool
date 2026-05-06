public class ConditionUnlock
{
	public string parameter;

	public BigNumber argument;

	public string Description;

	public virtual bool Check()
	{
		return false;
	}

	public virtual string Preview(bool show_progress = true, bool show_complete = true)
	{
		return "";
	}
}
