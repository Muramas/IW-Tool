using System;
using UnityEngine;

public class EffectVR : IEffect
{
	private Action action;

	private Variable efficiency;

	private Variable gilding;

	public void Apply()
	{
	}

	public void SetEfficiency(Variable eff = null)
	{
		efficiency = eff;
	}

	public void SetGilding(Variable eff = null)
	{
		gilding = eff;
	}

	public void Delete()
	{
	}

	public void Update()
	{
		int num = Mathf.Clamp(GameManager.Instance.BonusSpawner.Clickables.FindAll((BonusClickable x) => x.isSpawned).Count, 0, 4);
		GameManager.Instance.VoidManaChange(getRadianceProfit() * Time.deltaTime * num / 4.0);
	}

	public string Preview(string key = "")
	{
		int num = Mathf.Clamp(GameManager.Instance.BonusSpawner.Clickables.FindAll((BonusClickable x) => x.isSpawned).Count, 0, 4);
		return (getRadianceProfit() * num / 4.0).ToReadableString();
	}

	public BigNumber getRadianceProfit()
	{
		BigNumber bigNumber = 1.0;
		if (efficiency != null)
		{
			bigNumber = efficiency.Value;
		}
		if (gilding != null)
		{
			bigNumber *= gilding.Value;
		}
		return bigNumber.Pow(1.2000000476837158) * 4.0 * GameManager.Instance.VoidManaManager.VoidCore.AmountPerEntity.Value / 30.0;
	}
}
