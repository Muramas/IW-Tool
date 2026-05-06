using System;
using UnityEngine;

public class EffectAutoClick : EffectDiminishing
{
	public bool CritConstant;

	public float profit_mod;

	public BigNumber bonusProfit;

	public bool PetClick;

	public float PetExp;

	public float ShardDrop;

	public float Frequency;

	public Action onClick;

	protected float critchance_mod;

	protected float critprofit_mod;

	protected float timer;

	protected bool fromSpell;

	protected BigNumber c_chance;

	protected BigNumber c_profit;

	public EffectAutoClick(float frequency, float profit = 1f, float chance = 1f, float crit_profit = 1f, bool crit_const = false, float diminishing = 1f, bool fromSpell = true, Action _onClick = null)
	{
		profit_mod = profit;
		critchance_mod = chance;
		critprofit_mod = crit_profit;
		CritConstant = crit_const;
		bonusProfit = 1.0;
		PetClick = false;
		PetExp = 1f;
		ShardDrop = 1f;
		Frequency = frequency;
		onClick = _onClick;
		base.diminishing = diminishing;
		this.fromSpell = fromSpell;
	}

	public override void Apply()
	{
	}

	public override void Update()
	{
		if (Frequency == 0f)
		{
			return;
		}
		timer += Time.deltaTime;
		float clicks = getClicks();
		if (timer > 1f / clicks)
		{
			float num = timer * clicks;
			if (num > 2.1474836E+09f)
			{
				click(num);
				timer = 0f;
			}
			else
			{
				int num2 = Mathf.FloorToInt(num);
				click(num2);
				timer -= (float)num2 / clicks;
			}
		}
	}

	protected void click()
	{
		Orb orb = GameManager.Instance.Orb;
		if (CritConstant)
		{
			c_chance = new BigNumber(critchance_mod);
			c_profit = new BigNumber(critprofit_mod);
		}
		else
		{
			c_chance = orb.GetCritChange * critchance_mod;
			c_profit = orb.crit_profit.Value * critprofit_mod;
		}
		BigNumber bigNumber = 1.0;
		if (efficiency != null)
		{
			bigNumber = GetEfficiency().Value;
		}
		GameManager.Instance.Orb.AutoClick(orb.click_profit.Value * profit_mod * bonusProfit * bigNumber, c_chance, c_profit, ShardDrop, PetClick, idle_break: false, PetExp);
		if (onClick != null)
		{
			onClick();
		}
	}

	protected void click(float k)
	{
		Orb orb = GameManager.Instance.Orb;
		if (CritConstant)
		{
			c_chance = new BigNumber(critchance_mod);
			c_profit = new BigNumber(critprofit_mod);
		}
		else
		{
			c_chance = orb.GetCritChange * critchance_mod;
			c_profit = orb.crit_profit.Value * critprofit_mod;
		}
		BigNumber bigNumber = 1.0;
		if (efficiency != null)
		{
			bigNumber = GetEfficiency().Value;
		}
		GameManager.Instance.Orb.AutoClick(orb.click_profit.Value * profit_mod * bonusProfit * bigNumber, c_chance, c_profit, ShardDrop, PetClick, idle_break: false, PetExp, k);
		if (onClick != null)
		{
			onClick();
		}
	}

	protected float getClicks()
	{
		float num = Frequency;
		if (fromSpell)
		{
			num *= GameManager.Instance.Orb.autoclicksFromSpell.ValueFloat;
			if (gilding != null && efficiency != null && efficiency == GameManager.Instance.Scrolls.SummoningEfficiency)
			{
				num *= gilding.Value.Pow(0.10000000149011612).ToFloat();
			}
		}
		return num;
	}

	public override string Preview(string key = "")
	{
		float clicks = getClicks();
		if (clicks < 1000000f)
		{
			return new BigNumber(getClicks()).ToReadableString("0.##");
		}
		return new BigNumber(clicks).ToReadableString();
	}
}
