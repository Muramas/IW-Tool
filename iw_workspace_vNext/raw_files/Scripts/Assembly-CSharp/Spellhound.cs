using System;
using System.Globalization;
using System.Text;
using UnityEngine;

public class Spellhound : Pet
{
	private EffectAddShardPeriodic effect;

	private float profit;

	private float period = 10f;

	private Vector3 text_position;

	private Color color;

	private BigNumber exp_pool;

	public override void Init()
	{
		base.Name = "Spellhound";
		base.Init();
		NameKey = PetNames.Spellhound;
		Family = PetFamily.GreaterChimaera;
		base.LevelReq = 25;
		Conditions2Unlock.Add(new ConditionUnlockMore("Spell.Shards", "30000", "Exile Spell Shards collected:"));
		effect = new EffectAddShardPeriodic(1f, 1f);
		effect.time = profit;
		effect.Frequency = 0.1f;
		color = new Color(0f, 0.7058824f, 1f);
		EffectAddShardPeriodic effectAddShardPeriodic = effect;
		effectAddShardPeriodic.OnApply = (Action)Delegate.Combine(effectAddShardPeriodic.OnApply, (Action)delegate
		{
			if (effect.time != 0f)
			{
				GameManager.Instance.AnimatedText.TextUp(new BigNumber(effect.time).ToReadableString(), text_position, color, -1.2f);
			}
		});
		exp_pool = new BigNumber(0.0);
	}

	public override void ApplyEffects()
	{
		GameManager.Instance.CurrentPet.PetPanel.SetStateAbility(this, state: true);
		text_position = GameManager.Instance.CurrentPet.PetPanel.GetSlotPosition(this) - new Vector3(-1.2f, 1f, 0f);
		text_position.z = 0f;
		update_effect();
		effect.Apply();
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, new Action<Spell>(update_exp));
	}

	public override void update()
	{
		if (BigNumber.Sign(exp_pool) == 1)
		{
			BigNumber bigNumber = (exp_pool * 0.1 + 1.0) * Time.deltaTime;
			AddExpConst(bigNumber);
			exp_pool -= bigNumber;
		}
		effect.Frequency = Mathf.Max(0.1f * GameManager.Instance.CurrentPet.ChargeAbilityBoost.ValueFloat, 0.02f);
		GameManager.Instance.CurrentPet.PetPanel.SetAbilityProgress(this, effect.Frequency * effect.timer);
		update_effect();
	}

	private void update_exp(Spell spell = null)
	{
		if (spell.ShardsBuilding)
		{
			exp_pool += base.ExpBonus.ApplyModOnVar(spell.FullBuild / 1.25);
		}
	}

	private void recalculate_bonus()
	{
		BigNumber bigNumber = 4f * Level.Value.Pow(1.149999976158142).ToFloat() * GetAbilityPower();
		BigNumber bigNumber2 = "1e7";
		if (bigNumber > bigNumber2)
		{
			profit = bigNumber2.ToFloat();
			profit += (bigNumber - bigNumber2).Pow(0.15000000596046448).ToFloat();
		}
		else
		{
			profit = bigNumber.ToFloat();
		}
		effect.time = profit;
	}

	private void update_effect()
	{
		recalculate_bonus();
		effect.Update();
	}

	public override void DisableAll()
	{
		exp_pool = 0.0;
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, new Action<Spell>(update_exp));
		GameManager.Instance.CurrentPet.PetPanel.SetStateAbility(this, state: false);
	}

	public override void OfflineWork(int sec, bool isReal)
	{
		if (isReal && GameManager.Instance.Scrolls.unfilled_scrolls.Count > 0)
		{
			BigNumber shards = (float)(sec / 10) * 2.5f * Mathf.Pow(Level.ValueInt, 1.25f);
			GameManager.Instance.Scrolls.ShardsPool.Add(shards);
		}
	}

	public override string Tips_text()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(base.Tips_text());
		stringBuilder.AppendLine(string.Format("Spellhound Ability".Translate(), GetBonusAdditive(profit), GetBonusAdditive(period / GameManager.Instance.CurrentPet.ChargeAbilityBoost.ValueFloat)));
		stringBuilder.Append(AdditionalDescription());
		return stringBuilder.ToString();
	}

	public override string Progress_Text()
	{
		return string.Format("Spellhound Ability Progress".Translate(), (float)(int)(period - effect.timer) / GameManager.Instance.CurrentPet.ChargeAbilityBoost.ValueFloat, profit.ToString("F2", CultureInfo.InvariantCulture));
	}
}
