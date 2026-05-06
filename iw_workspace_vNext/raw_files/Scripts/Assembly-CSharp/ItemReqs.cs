public class ItemReqs
{
	public Variable Parameter;

	public string Name;

	public int Value;

	public ItemReqs(Attributes par, string val)
	{
		Name = par.ToString();
		Parameter = GameManager.Instance.AttributeManager.FindAttribute(par);
		Value = int.Parse(val);
	}

	public bool Available()
	{
		return Parameter.Value.ToInt() >= Value;
	}
}
