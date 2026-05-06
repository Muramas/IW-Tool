using System;

public class CritRatingCalculations
{
	public VariableComplex crit_rating;

	public BigNumber prev_chance_bonus;

	public BigNumber prev_profit_bonus;

	private Orb orb;

	private bool changed;

	public CritRatingCalculations()
	{
		crit_rating = new VariableComplex(0.0);
		prev_chance_bonus = 0.0;
		prev_profit_bonus = 0.0;
		orb = GameManager.Instance.Orb;
		VariableComplex variableComplex = crit_rating;
		variableComplex.OnChange = (Action)Delegate.Combine(variableComplex.OnChange, new Action(MarkForChange));
		VariableComplex crit_chance = orb.crit_chance;
		crit_chance.OnChange = (Action)Delegate.Combine(crit_chance.OnChange, new Action(MarkForChange));
	}

	public void Recalculate()
	{
		if (prev_chance_bonus != 0.0)
		{
			orb.crit_chance.Change(-prev_chance_bonus, 1.0);
		}
		if (prev_profit_bonus != 0.0)
		{
			orb.crit_profit.Change(-prev_profit_bonus, 1.0);
		}
		if (crit_rating.Value <= 1.0)
		{
			prev_chance_bonus = (prev_profit_bonus = 0.0);
			crit_rating.SetValue(0.0);
			return;
		}
		prev_chance_bonus = (crit_rating.Value + 1.0).Log10();
		_ = (BigNumber)0.0;
		if (orb.crit_chance.Value + prev_chance_bonus > 100.0)
		{
			_ = orb.crit_chance.Value + prev_chance_bonus - 100.0;
			if (orb.crit_chance.Value >= 100.0)
			{
				prev_chance_bonus = 0.0;
			}
			else
			{
				prev_chance_bonus = 100.0 - orb.crit_chance.Value;
			}
		}
		prev_profit_bonus = crit_rating.Value / 100.0;
		orb.crit_chance.Change(prev_chance_bonus, 1.0);
		orb.crit_profit.Change(prev_profit_bonus, 1.0);
	}

	public void Update()
	{
		if (changed)
		{
			Recalculate();
			changed = false;
		}
	}

	private void MarkForChange()
	{
		changed = true;
	}

	public void Restart()
	{
		crit_rating.Reset(0.0);
		prev_chance_bonus = (prev_profit_bonus = 0.0);
	}
}
