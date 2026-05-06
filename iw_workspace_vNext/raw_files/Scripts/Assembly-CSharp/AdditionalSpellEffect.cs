public class AdditionalSpellEffect
{
	public SimpleEffect effect;

	public float value;

	public string description;

	public BigNumber GetBonus(int rank)
	{
		return new BigNumber(1.5).Pow(rank);
	}

	public void UpdateEffect(int rank)
	{
		effect.mult = GetBonus(rank);
	}

	public string GetDescription(int rankCurrent, int rankNext)
	{
		string str = "SummMemeticEffect";
		if (rankNext == rankCurrent)
		{
			return str.Translate().Replace("#", effect.Preview(""));
		}
		BigNumber mult = effect.mult;
		string text = effect.Preview("");
		UpdateEffect(rankNext);
		string result = str.Translate().Replace("#", text + " -> " + effect.Preview(""));
		effect.mult = mult;
		return result;
	}

	public string GetDescription()
	{
		return description.Translate().Replace("#", effect.Preview(""));
	}
}
