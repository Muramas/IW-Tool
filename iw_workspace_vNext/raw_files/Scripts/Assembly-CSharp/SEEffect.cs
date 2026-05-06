using System;

public class SEEffect : EffectInstant
{
	private Spell spell;

	public SEEffect()
	{
		time = 1f;
		diminishing = 2f;
	}

	public override void Apply()
	{
		if (spell == null)
		{
			spell = GameManager.Instance.SpellBook.GetSpell(Spells.SyntheticEntity);
		}
		add_collect();
	}

	public int GetCollected()
	{
		return Convert.ToInt32(Math.Floor(ApplyW(time).ToDouble()));
	}

	private void add_collect()
	{
		int collected = GetCollected();
		int num = 0;
		num = ((!((BigNumber)((ulong)collected * (spell.chargeCount / 50 + 1)) > (BigNumber)"2e9")) ? ((int)(spell.chargeCount / 50)) : (2000000000 / collected - 1));
		collected *= 1 + num;
		GameManager.Instance.BonusSpawner.AddItems(collected);
		if (num > 0)
		{
			if (spell.chargeCount > (ulong)((long)num + 1L))
			{
				spell.chargeCount -= (ulong)num;
			}
			else
			{
				spell.chargeCount = 1uL;
			}
		}
	}

	public override string Preview(string key = "")
	{
		if (spell == null)
		{
			spell = GameManager.Instance.SpellBook.GetSpell(Spells.SyntheticEntity);
		}
		int collected = GetCollected();
		int num = 0;
		num = ((!((BigNumber)((ulong)collected * (spell.chargeCount / 50 + 1)) > (BigNumber)"2e9")) ? ((int)(spell.chargeCount / 50)) : (2000000000 / collected - 1));
		collected *= 1 + num;
		return new BigNumber(collected).ToReadableString("F0");
	}
}
