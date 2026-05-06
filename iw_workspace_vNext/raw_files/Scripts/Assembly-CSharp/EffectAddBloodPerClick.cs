using UnityEngine;

public class EffectAddBloodPerClick : EffectDiminishing
{
	public BigNumber amount;

	public float period = 1f;

	protected float timer;

	private Variable income;

	public EffectAddBloodPerClick(float t, float rate)
	{
		amount = t;
		period = 1f / rate;
		income = GameContext.GetResource("Nosferatu.BloodIncome");
	}

	public override void Apply()
	{
		timer = Time.deltaTime;
		if (income == null)
		{
			income = GameContext.GetResource("Nosferatu.BloodIncome");
		}
	}

	protected virtual void Tick(int k)
	{
		if (income != null)
		{
			GameManager.Instance.CurrentHero.ClassBonusStacks.Change(GetAmount() * k);
		}
	}

	public BigNumber GetAmount()
	{
		if (income == null)
		{
			return 0.0;
		}
		return amount * income.Value;
	}

	public override void Update()
	{
		float num = 1f / period;
		num *= GameManager.Instance.Orb.autoclicksFromSpell.ValueFloat;
		if (gilding != null)
		{
			num *= gilding.Value.Pow(0.10000000149011612).ToFloat();
		}
		timer += Time.deltaTime;
		if (timer > 1f / num)
		{
			int num2 = (int)(timer * num);
			Tick(num2);
			timer -= (float)num2 / num;
		}
	}

	public override string Preview(string key)
	{
		return GetAmount().ToReadableString("F0");
	}
}
