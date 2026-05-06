using System;
using UnityEngine;

public class EffectTimeHelix : IEffect
{
	protected SimpleEffect e;

	private VariableBignumber v;

	private Variable efficiency;

	private Variable gilding;

	public EffectTimeHelix(SimpleEffect effect)
	{
		e = effect;
	}

	protected virtual void OnCastEnd(Spell sp)
	{
		if (sp.NameKey == Spells.TimeHelix)
		{
			OnUse();
		}
	}

	public void OnSelect()
	{
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		scrolls.OnCastEnd = (Action<Spell>)Delegate.Combine(scrolls.OnCastEnd, new Action<Spell>(OnCastEnd));
	}

	public void OnDeselect()
	{
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		scrolls.OnCastEnd = (Action<Spell>)Delegate.Remove(scrolls.OnCastEnd, new Action<Spell>(OnCastEnd));
	}

	public virtual void OnUse()
	{
		Time.timeScale = 1f;
		Statistic.Clicks.SetValue(0.0);
		Statistic.AutoClicks.SetValue(0.0);
		Statistic.ClickableCollect.SetValue(0uL);
		if (GameManager.Instance.CurrentPet.Pet != null)
		{
			GameManager.Instance.CurrentPet.Pet.RecalculateLevel(0.0);
		}
		GameManager.Instance.Mana.SetValue(0.0);
		GameManager.Instance.VoidMana.SetValue(0.0);
		GameManager.Instance.CurrentHero.SkipedPlayedTime.SetValue(1.0);
		GameManager.Instance.CurrentHero.Hero.UpdateExp();
		GameManager.Instance.Scrolls.RecheckSpells();
	}

	public void Apply()
	{
		Delete();
		e.SetEfficiency(efficiency);
		e.SetGilding(gilding);
		e.Apply();
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
		e.Delete();
	}

	public void Update()
	{
	}

	public string Preview(string key = "")
	{
		e.SetEfficiency(efficiency);
		e.SetGilding(gilding);
		return e.Preview(key);
	}
}
