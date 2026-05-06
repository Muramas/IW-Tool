using UnityEngine;

public class MultiChargesEffect : EffectDiminishing
{
	public float time = 20f;

	public int k = 1;

	private Spells spellkey;

	private float part;

	private Spell s;

	public MultiChargesEffect(float t, Spells key, float percent, float powDimish = 1f)
	{
		time = t;
		spellkey = key;
		part = percent;
		pow_diminishing = powDimish;
	}

	public int GetChargesPerCast()
	{
		if (s == null)
		{
			s = GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == spellkey);
		}
		return 1 + Mathf.FloorToInt((float)s.chargeCount * part);
	}

	public override void Apply()
	{
		if (s == null)
		{
			s = GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == spellkey);
		}
		k = GetChargesPerCast();
		if ((ulong)k > s.chargeCount + 1)
		{
			k = (int)s.chargeCount + 1;
		}
		if (k > 1)
		{
			s.Spend(k - 1);
			Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == spellkey);
			if (scroll != null)
			{
				scroll.IncreaseFakeCasts(k - 1);
			}
		}
		BigNumber bigNumber = ApplyEfficiency(time * (float)k);
		GameManager.Instance.ManaChange(GameManager.Instance.PPS.Value * bigNumber);
	}

	public override string Preview(string key = "")
	{
		if (s == null)
		{
			s = GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == spellkey);
		}
		BigNumber bigNumber = ApplyEfficiency(time * 1f + (float)(int)((float)s.chargeCount * part));
		if (key == "t")
		{
			if (bigNumber > 1.8446744073709552E+19)
			{
				return bigNumber.ToReadableString("F0") + " " + "sec".Translate();
			}
			return Statistic.time_to_string(bigNumber);
		}
		return "+" + (GameManager.Instance.PPS.Value * bigNumber).ToReadableString("F0");
	}
}
