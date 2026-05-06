using UnityEngine;

public class ManaManager
{
	public VariableBignumber Mana;

	public VariableBignumber StartingMana;

	private BigNumber manaChange = 0.0;

	private float manaChangeTimer;

	private float manaChangePeriod = 0.2f;

	private BigNumber avChange = 0.0;

	private BigNumber avMSChange = 0.0;

	private float avChangeTimer;

	public IncomeCounter counterIncome;

	public IncomeCounter counterMSources;

	public ManaManager()
	{
		Mana = new VariableBignumber(0.0);
		StartingMana = new VariableBignumber(0.0);
		manaChange = (avChange = (avMSChange = 0.0));
		manaChangeTimer = (avChangeTimer = 0f);
		counterIncome = new IncomeCounter(10);
		counterMSources = new IncomeCounter(10);
	}

	public void Restart()
	{
		Mana.SetValue(StartingMana.Value);
		manaChange = (avChange = (avMSChange = 0.0));
		manaChangeTimer = (avChangeTimer = 0f);
		counterIncome.Reset();
		counterMSources.Reset();
	}

	public void Reset(BigNumber value)
	{
		Mana.SetValue(value);
		manaChange = (avChange = (avMSChange = 0.0));
		manaChangeTimer = (avChangeTimer = 0f);
		counterIncome.Reset();
		counterMSources.Reset();
	}

	public void Change(BigNumber add, bool msources = false)
	{
		if (BigNumber.Sign(add) == 1)
		{
			manaChange += add;
			avChange += add;
			if (msources)
			{
				avMSChange += add;
			}
		}
		else
		{
			Mana.Change(add);
		}
	}

	public void ManaChange()
	{
		if (manaChangeTimer > manaChangePeriod && manaChange != 0.0)
		{
			Mana.Change(manaChange);
			Statistic.Change(Statistic.ManaAllTime, manaChange);
			Statistic.Change(Statistic.ManaRealm, manaChange);
			Statistic.ManaSession.Change(manaChange);
			manaChange = 0.0;
			manaChangeTimer = 0f;
		}
		if (avChangeTimer >= 1f)
		{
			counterIncome.Add(avChange);
			counterMSources.Add(avMSChange);
			avChange = (avMSChange = 0.0);
			avChangeTimer -= 1f;
		}
		if (Time.timeScale != 0f)
		{
			manaChangeTimer += Time.deltaTime / Time.timeScale;
			avChangeTimer += Time.deltaTime / Time.timeScale;
		}
	}
}
