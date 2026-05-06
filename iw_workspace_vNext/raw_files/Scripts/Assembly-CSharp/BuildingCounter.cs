using System.Text;
using TMPro;
using UnityEngine;

public class BuildingCounter : MonoBehaviour
{
	public int tier;

	public TextMeshProUGUI level;

	public TMP_InputField input;

	public TMP_InputField input_step;

	private Building b;

	public void Calculate()
	{
		BigNumber bigNumber = input_step.text;
		BigNumber bigNumber2 = input.text;
		BigNumber bigNumber3 = (1.0 - bigNumber * (1.0 - bigNumber2) / 10.0).Log_a(bigNumber2.ToDouble()) - 75.0;
		BigNumber number = ((BigNumber)1.5).Pow(bigNumber3.ToInt() / 25) * 1.4 * 1.3 * 1.2 * 1.1;
		level.text = number.ToReadableString();
	}

	public void CalculateCost()
	{
		int num = int.Parse(input.text);
		b = GameManager.Instance.Buildings.Find((BuildingVisual x) => x.Tier == tier).building;
		BigNumber cost = getCost(num);
		level.text = cost.ToReadableString();
		StringBuilder stringBuilder = new StringBuilder();
		int num2 = 100;
		if (input_step.text != "")
		{
			num2 = int.Parse(input_step.text);
		}
		for (int num3 = 0; num3 < 10; num3++)
		{
			stringBuilder.AppendLine(getCost(num + num2 * num3).ToReadableString());
		}
	}

	private BigNumber getCost(int lvl)
	{
		return b.CostBase * b.cost_growth.Value.Pow(lvl) * 10.0;
	}
}
