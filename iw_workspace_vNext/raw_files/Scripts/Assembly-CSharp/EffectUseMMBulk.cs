using UnityEngine;

public class EffectUseMMBulk : EffectUseMM
{
	private Spell s;

	private float part;

	private bool passive;

	private Spells spell;

	public EffectUseMMBulk(float chargePart, Spells key, bool triggerPassive = false)
	{
		part = chargePart;
		spell = key;
		passive = triggerPassive;
	}

	public override void Apply()
	{
		recalculate_count();
		if (ep == null)
		{
			ep = target.effects[0] as EffectAddProduction;
		}
		if (s == null)
		{
			s = GameManager.Instance.SpellBook.GetSpell(spell);
		}
		int num = 1 + Mathf.FloorToInt((float)s.chargeCount * part);
		if ((ulong)num > s.chargeCount + 1)
		{
			num = (int)s.chargeCount + 1;
		}
		if (num > 1)
		{
			s.Spend(num - 1);
		}
		count *= num;
		ep.k *= count;
		target.Apply();
		ep.k /= count;
		float num2 = (float)(num - 1) * GameManager.Instance.Scrolls.PersistentGain.ValueFloat;
		s.IncreaseUses(num2 * GameManager.Instance.Scrolls.PersistentMult.ValueFloat);
		s.IncreaseUseThisRun(num2 * GameManager.Instance.Scrolls.PersistentActiveMult.ValueFloat);
		target.IncreaseUses(count);
		target.IncreaseUseThisRun(count);
		Statistic.CastSpell.Change(count + (double)num - 1.0);
		Statistic.Change(Statistic.CastSpellTotal, count + (double)num - 1.0);
		Statistic.Change(Statistic.CastSpellRealm, count + (double)num - 1.0);
		if (passive && s.PassiveEffect is EffectJMS)
		{
			(s.PassiveEffect as EffectJMS).UpdateTimes(num - 1);
		}
		if (GameManager.Instance.Scrolls.OnCastAmount != null)
		{
			GameManager.Instance.Scrolls.OnCastAmount(target, count);
		}
		if (GameManager.Instance.Scrolls.OnCastAmount != null)
		{
			GameManager.Instance.Scrolls.OnCastAmount(s, num - 1);
		}
	}
}
