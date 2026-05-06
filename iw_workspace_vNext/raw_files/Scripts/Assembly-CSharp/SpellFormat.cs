using System;

[Serializable]
public class SpellFormat
{
	public string Name = "";

	public string Description = "";

	public string Key = "";

	public string Build = "";

	public string TypeBehavior = SpellTypeBehavior.Instant.ToString();

	public string SpellType = "";

	public string Duration = "";

	public string Requirements = "";

	public string ResetUses = "";

	public string SB = "";

	public string Limited = "";

	public string Curse = "";

	public string Shadow = "";

	public string Acc = "";

	public string Priority = "";

	public string AddCost = "";

	public string AddCostValue = "";

	public string AddCostName = "";
}
