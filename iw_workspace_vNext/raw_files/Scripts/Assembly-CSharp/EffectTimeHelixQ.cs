using UnityEngine;

public class EffectTimeHelixQ : EffectTimeHelix
{
	public EffectTimeHelixQ(SimpleEffect effect)
		: base(effect)
	{
		e = effect;
	}

	protected override void OnCastEnd(Spell sp)
	{
		if (sp.NameKey == Spells.TimeHelix2)
		{
			OnUse();
		}
	}

	public override void OnUse()
	{
		Time.timeScale = 1f;
		Statistic.Clicks.SetValue(0.0);
		Statistic.AutoClicks.SetValue(0.0);
		Statistic.ClickableCollect.SetValue(0uL);
		GameManager.Instance.Mana.SetValue(0.0);
		GameManager.Instance.VoidMana.SetValue(0.0);
		GameManager.Instance.CurrentHero.SkipedPlayedTime.SetValue(1.0);
		GameManager.Instance.CurrentHero.Hero.UpdateExp();
		if (GameManager.Instance.CurrentPet.Pet != null && GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.active && x.spell.NameKey == Spells.GenerateParadox) != null)
		{
			GameManager.Instance.CurrentPet.Pet.RecalculateLevel(GameManager.Instance.CurrentPet.Pet.TotalExp.Value + 1.0);
		}
		GameManager.Instance.Scrolls.RecheckSpells();
	}
}
