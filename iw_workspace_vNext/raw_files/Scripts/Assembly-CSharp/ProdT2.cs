using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ProdT2 : Hero
{
	private SimpleEffect effect_profit;

	private BigNumber bonus_profit;

	private ChronoWarping chrono;

	public ChronoCounter counter;

	private SimpleEffect effect_evo;

	private BigNumber bonus_evo;

	public override void Init()
	{
		NameKey = HeroesNames.Temporalist;
		base.Name = NameKey.ToString();
		base.Init();
		RequedClasses.Add(HeroesNames.Apprentice);
		RequedClasses.Add(HeroesNames.Prodigy);
		RequedClasses.Add(HeroesNames.Chronomancer);
		base.LevelReq = 120;
		Tier = 2;
		SpellList = new List<Spells>
		{
			Spells.MagicMissile,
			Spells.GemResonance,
			Spells.RitualOfPower,
			Spells.ConjureLesserElemental,
			Spells.ConjureGreaterElemental,
			Spells.ConjurePrimalElemental,
			Spells.ConjureManabeast,
			Spells.VoidAutomaton,
			Spells.SyntheticEntity,
			Spells.VoidLure,
			Spells.VoidRadiance,
			Spells.AlterTheLaws,
			Spells.TrueSorcery,
			Spells.PrimalPower,
			Spells.KelphiorsBlackBeam,
			Spells.LeyOverdrive,
			Spells.QuasiIncantation,
			Spells.SingularityBeam,
			Spells.TemporalDistortion,
			Spells.GenerateParadox,
			Spells.ConvergeTimelines,
			Spells.Revert,
			Spells.StabilizeTheFlow,
			Spells.Superposition,
			Spells.CraftedWormhole
		};
		effect_profit = new SimpleEffect(GameManager.Instance.Profit);
		effect_evo = new SimpleEffect(GameManager.Instance.Scrolls.EvocationEfficiency);
		Chronomancer chronomancer = GameManager.Instance.CurrentHero.HeroPanel.HeroMap[HeroesNames.Chronomancer] as Chronomancer;
		chrono = chronomancer.chrono;
		counter = chronomancer.counter;
	}

	public override void PostInit()
	{
		Conditions2Unlock = new List<ConditionUnlock>();
		Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Prodigy, 0, "Unlock Prodigy:"));
		Conditions2Unlock.Add(new ConditionUnlockAchievement(AchievementKey.Chronomancer, 0, "Unlock Chronomancer:"));
		Conditions2Unlock.Add(new ConditionUnlockMore("Hero.Level", "160", "Reach Character level:"));
		Conditions2Unlock.Add(new ConditionUnlockSpellUse(Spells.SpellFocus, "10000"));
	}

	private void RecalculateChrono()
	{
		chrono.maxValue.SetValue((float)Level.ValueInt * 7.5f);
	}

	public override void update()
	{
		AddChrono();
	}

	private void AddChrono()
	{
		if (chrono.current.Value.ToFloat() != chrono.maxValue.ValueFloat && Time.timeScale != 0f)
		{
			float num = chrono.current.Value.ToFloat();
			float num2 = chrono.profitPerSec.ValueFloat * Time.deltaTime / Time.timeScale;
			num += num2;
			Statistic.Change(Statistic.CTTotal, num2);
			if (num >= chrono.maxValue.ValueFloat)
			{
				num = chrono.maxValue.ValueFloat;
			}
			chrono.current.SetValue(num);
			update_counter();
			counter.SetFiller(chrono.current.Value.ToFloat() / chrono.maxValue.ValueFloat);
		}
	}

	private void update_counter()
	{
		counter.SetCont(chrono.current.Value.ToFloat().ToString("F0") + "/" + chrono.maxValue.ValueFloat.ToString("F0"));
	}

	public override void ApplyEffects()
	{
		Spell spell = GameManager.Instance.SpellBook.GetSpell(Spells.TimeHelix);
		spell.Delete();
		spell.ResetUsesAll(full: true);
		chrono.current = GameManager.Instance.CurrentHero.ClassBonusStacks;
		update_effect();
		effect_profit.Apply();
		effect_evo.Apply();
		SubEffect();
		VariableLong timeSession = Statistic.TimeSession;
		timeSession.OnChange = (Action)Delegate.Combine(timeSession.OnChange, new Action(update_effect));
		VariableInt level = Level;
		level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(RecalculateChrono));
		RecalculateChrono();
		counter.gameObject.SetActive(value: true);
		update_counter();
		if (quotes == null)
		{
			quotes = new TemporalistQS();
		}
		quotes.Apply();
		GameManager.Instance.BuildingManager.BuildCreator.SetBuilding(8, "Ley Temporal Singleton");
	}

	public override void DisableAll()
	{
		UnsubEffect();
		VariableLong timeSession = Statistic.TimeSession;
		timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, new Action(update_effect));
		VariableInt level = Level;
		level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(RecalculateChrono));
		counter.gameObject.SetActive(value: false);
		chrono.current.SetValue(0.0);
		effect_profit.Delete();
		effect_evo.Delete();
		quotes.Disable();
		GameManager.Instance.BuildingManager.BuildCreator.SetBuilding(8, "The Nexus");
		Time.timeScale = 1f;
	}

	public override void update_effect()
	{
		BigNumber bigNumber = (float)GameManager.Instance.CurrentHero.PlayedTime.ValueInt / 3600f;
		if (bigNumber > 720.0)
		{
			bigNumber = 720.0 + (bigNumber - 719.0).Pow(0.8999999761581421);
		}
		BigNumber bigNumber2 = GameManager.Instance.CurrentHero.SkipedPlayedTime.Value * GameManager.Instance.CurrentHero.APSpeed.Value / 31536000.0;
		bigNumber2 /= (BigNumber)16000.0;
		bonus_profit = 1.0 + (1.0 + bigNumber.Pow(2.15) * bigNumber2.Pow(1.7799999713897705)) * (1.0 + experience.Value / "1e7") * GetBonusMult().Pow(0.5) / "1e10";
		effect_profit.mult = bonus_profit;
		effect_profit.Update();
		bonus_evo = 1.0 + (1.0 + bigNumber.Pow(0.5) * 0.02500000037252903 * bigNumber2.Pow(1.149999976158142)) * GetBonusMult().Pow(0.5) / 5000000.0;
		effect_evo.mult = bonus_evo;
		effect_evo.Update();
	}

	public override string Tips_text()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(base.Tips_text());
		stringBuilder.AppendLine(string.Format("Temporalist Ability".Translate(), GetBonusMult(bonus_profit), GetBonusMult(bonus_evo), Statistic.time_to_string(GameManager.Instance.CurrentHero.SkipedPlayedTime.Value)));
		stringBuilder.Append(AdditionalDescription());
		return stringBuilder.ToString();
	}
}
