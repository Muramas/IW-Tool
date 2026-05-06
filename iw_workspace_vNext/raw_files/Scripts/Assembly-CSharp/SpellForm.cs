using System;
using System.Collections.Generic;
using System.Text;

public class SpellForm : DemonForm
{
	private SimpleEffect effectEvo;

	private SimpleEffect effectSum;

	private SimpleEffect effectInca;

	private BigNumber bonusEvo;

	private BigNumber bonusSum;

	private BigNumber bonusInca;

	private List<int> counter;

	public override void Init()
	{
		base.Init();
		NameKey = HeroesNames.Tempest;
		base.Name = "The Tempest";
		RequedClasses.Add(HeroesNames.Archon);
		RequedClasses.Add(HeroesNames.Oni);
		RequedClasses.Add(HeroesNames.Desolator);
		Feature = "Tempest Feature";
		Description = "Tempest Lore";
		ExpDescription = "Tempest XP";
		effectEvo = new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency);
		effectSum = new SimpleEffect(GameManager.Instance.Scrolls.SummoningEfficiency);
		effectInca = new SimpleEffect(GameManager.Instance.Scrolls.IncantationEfficiency);
		Skills.Add(effectEvo);
		Skills.Add(effectSum);
		Skills.Add(effectInca);
		counter = new List<int>();
		counter.Add(0);
		counter.Add(0);
		counter.Add(0);
		spells = new List<Spells>();
		spells.Add(Spells.FireWithFire);
		spells.Add(Spells.AbyssalStrike);
		spells.Add(Spells.ArtificialMuse);
		spells.Add(Spells.MaterializeCosmicConduit);
	}

	public override void ApplyEffects()
	{
		base.ApplyEffects();
		VariableLong timeSession = Statistic.TimeSession;
		timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(updateCounter));
		GameManager.Instance.Scrolls.Activate7th();
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		scrolls.OnRealCastAmount = (Action<Spell, double>)Delegate.Combine(scrolls.OnRealCastAmount, new Action<Spell, double>(OnCastEvo));
	}

	public override void DisableAll()
	{
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		scrolls.OnRealCastAmount = (Action<Spell, double>)Delegate.Remove(scrolls.OnRealCastAmount, new Action<Spell, double>(OnCastEvo));
		GameManager.Instance.Scrolls.Deactivate7th();
		VariableLong timeSession = Statistic.TimeSession;
		timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, new Action(updateCounter));
		base.DisableAll();
	}

	private void updateCounter()
	{
		for (int i = 0; i < counter.Count; i++)
		{
			counter[i] = 0;
		}
		foreach (Scroll scroll in GameManager.Instance.Scrolls.Scrolls)
		{
			if (scroll.spell == null)
			{
				continue;
			}
			counter[(int)scroll.spell.Type]++;
			if (scroll.spell.active)
			{
				if (scroll.spell.Type == SpellTypeGroup.Incantation)
				{
					AddExp(8.0);
				}
				else if (scroll.spell.Type == SpellTypeGroup.Summoning)
				{
					AddExp(8.0);
				}
			}
		}
		update_effect();
	}

	public override BigNumber GetFormPower()
	{
		return Level.Value * GameManager.Instance.Ascension.AbilityPower.Value;
	}

	public override void update_effect()
	{
		BigNumber formPower = GetFormPower();
		bonusEvo = 1.0 + 0.5f * (float)counter[0] * (float)counter[0] * formPower;
		effectEvo.mult = bonusEvo;
		effectEvo.Update();
		bonusSum = 1.0 + 0.008f * (float)counter[1] * (float)counter[1] * formPower;
		effectSum.mult = bonusSum;
		effectSum.Update();
		bonusInca = 1.0 + 0.0025f * (float)counter[2] * (float)counter[2] * formPower;
		effectInca.mult = bonusInca;
		effectInca.Update();
	}

	private void OnCastEvo(Spell sp, double amount)
	{
		if (sp.Type == SpellTypeGroup.Evocation)
		{
			AddExp(0.12999999523162842 * amount);
		}
	}

	public override string TipText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(base.TipText());
		stringBuilder.AppendLine(string.Format("Tempest Ability".Translate(), GetBonusMult(bonusEvo), GetBonusMult(bonusSum), GetBonusMult(bonusInca)));
		stringBuilder.Append("AscensionAP".Translate());
		stringBuilder.Append(" ");
		stringBuilder.Append(GetBonusMult(GameManager.Instance.Ascension.AbilityPower.Value + 1.0));
		return stringBuilder.ToString();
	}
}
