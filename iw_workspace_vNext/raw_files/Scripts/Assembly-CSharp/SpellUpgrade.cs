using System.Text;

public class SpellUpgrade
{
	public Spells Key;

	public VariableBignumber GildingBonus;

	public VariableFloat CostMod;

	private SpellTypeBonuses Bonuses;

	private const float costIncrease = 1.75f;

	private bool applied;

	public int Level { get; private set; }

	public int Current { get; private set; }

	public Spell Spell { get; private set; }

	public SpellUpgrade(Spells key, int lvl = 0, int cur = 0)
	{
		Key = key;
		Level = lvl;
		Current = cur;
		GildingBonus = new VariableBignumber(1.0);
		CostMod = new VariableFloat(1f);
		Remove();
		Spell = GameManager.Instance.SpellBook.GetSpell(Key);
		if (Spell.Type == SpellTypeGroup.Evocation)
		{
			Bonuses = new EvocationUpgrade();
		}
		else if (Spell.Type == SpellTypeGroup.Incantation)
		{
			Bonuses = new IncantaionUpgrade();
		}
		else if (Spell.Type == SpellTypeGroup.Summoning)
		{
			Bonuses = new SummoningUpgrade();
		}
		if (GameManager.Instance.Paragon.GildingSpellsIsAvailable)
		{
			Apply();
		}
	}

	public void Load(int lvl, int cur)
	{
		Level = lvl;
		Current = cur;
		UpdateBonus();
	}

	public void Reset()
	{
		int level = (Current = 0);
		Level = level;
		UpdateBonus();
	}

	public void Apply()
	{
		if (!applied)
		{
			Bonuses.Init(Spell);
			Spell.SetGilding(GildingBonus);
			Spell.SetCostGilding(CostMod);
			applied = true;
			UpdateBonus();
		}
	}

	public void Remove()
	{
		if (Spell != null)
		{
			Spell.SetCostGilding(null);
			Spell.SetGilding(null);
		}
		if (Bonuses != null)
		{
			Bonuses.Deinit();
		}
		applied = false;
	}

	public void Upgrade()
	{
		Level++;
		Current++;
		UpdateBonus();
	}

	public void Minus(int c)
	{
		if (Current != 0)
		{
			if (c < 0)
			{
				c = Current;
			}
			Current -= c;
			if (Current < 0)
			{
				Current = 0;
			}
			UpdateBonus();
		}
	}

	public void Plus(int c)
	{
		if (c < 0)
		{
			c = Level - Current;
		}
		Current += c;
		if (Current >= Level)
		{
			Current = Level;
		}
		UpdateBonus();
	}

	public BigNumber GetCostIncrease()
	{
		return new BigNumber(1.75).Pow(Bonuses.GetLevel(Current));
	}

	public void UpdateBonus()
	{
		if (!applied)
		{
			return;
		}
		GildingBonus.SetValue(Bonuses.GetBonus(Current));
		CostMod.SetValue(GetCostIncrease());
		if (Spell.active)
		{
			Scroll scroll = GameManager.Instance.Scrolls.Scrolls.Find((Scroll x) => x.spell == Spell);
			if (scroll != null)
			{
				scroll.StopAction();
			}
		}
		Spell.ResetShards();
		GameManager.Instance.Scrolls.CheckFilledList();
		Bonuses.Update(Current);
	}

	public int GetLevel()
	{
		return Bonuses.GetLevel(Current);
	}

	public string GetDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		BigNumber bigNumber = Spell.FullBuild;
		stringBuilder.Append("Level".Translate()).Append(" ");
		stringBuilder.AppendLine(Bonuses.GetLevel(Current).ToString());
		if (Spell.ShardsBuilding || Spell.IsShadow)
		{
			stringBuilder.Append("Cost".Translate() + ": ");
			stringBuilder.Append(bigNumber.ToReadableString("F0"));
			stringBuilder.Append(" -> ");
			stringBuilder.Append((bigNumber * 1.75).ToReadableString("F0"));
		}
		else
		{
			bigNumber = GetCostIncrease() * 100.0;
			stringBuilder.Append("Charge".Translate() + ": ");
			stringBuilder.Append(bigNumber.ToReadableString());
			stringBuilder.Append("% -> ");
			stringBuilder.Append((bigNumber * 1.75).ToReadableString());
			stringBuilder.Append("%");
		}
		stringBuilder.AppendLine();
		if (!GameManager.Instance.SpellBook.NonscaledSpells.Contains(Key))
		{
			stringBuilder.Append("Efficiency bonus".Translate()).Append(" ");
			stringBuilder.Append(((GildingBonus.Value - 1.0) * 100.0).ToReadableString());
			stringBuilder.Append("% -> ");
			stringBuilder.Append(((GildingBonus.Value * Bonuses.GetMainBonus() - 1.0) * 100.0).ToReadableString());
			stringBuilder.AppendLine("%");
		}
		stringBuilder.AppendLine(Bonuses.GetDescription(Bonuses.GetLevel(Current)));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(Spell.GetDescription());
		return stringBuilder.ToString();
	}

	public static string GetDescription(Spell sp)
	{
		StringBuilder stringBuilder = new StringBuilder();
		BigNumber bigNumber = sp.FullBuild;
		if (sp.ShardsBuilding || sp.IsShadow)
		{
			stringBuilder.Append("Cost".Translate() + ": ");
			stringBuilder.Append(bigNumber.ToReadableString("F0"));
			stringBuilder.Append(" -> ");
			stringBuilder.Append((bigNumber * 1.75).ToReadableString("F0"));
		}
		else
		{
			bigNumber = 100.0;
			stringBuilder.Append("Charge".Translate() + ": ");
			stringBuilder.Append(bigNumber.ToReadableString());
			stringBuilder.Append("% -> ");
			stringBuilder.Append((bigNumber * 1.75).ToReadableString());
			stringBuilder.Append("%");
		}
		stringBuilder.AppendLine();
		string value = string.Empty;
		float num = 0f;
		if (sp.Type == SpellTypeGroup.Evocation)
		{
			num = 0.5f;
			value = EvocationUpgrade.GetBaseDescription();
		}
		else if (sp.Type == SpellTypeGroup.Incantation)
		{
			num = 0.20000005f;
			value = IncantaionUpgrade.GetBaseDescription();
		}
		else if (sp.Type == SpellTypeGroup.Summoning)
		{
			num = 0.29999995f;
			value = SummoningUpgrade.GetBaseDescription();
		}
		if (!GameManager.Instance.SpellBook.NonscaledSpells.Contains(sp.NameKey))
		{
			stringBuilder.Append("Efficiency bonus".Translate()).Append(" 0% -> ");
			stringBuilder.Append((new BigNumber(num) * 100.0).ToReadableString());
			stringBuilder.AppendLine("%");
		}
		stringBuilder.AppendLine(value);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(sp.GetDescription());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("SpellMemeticUpgradeHotkeys".Translate());
		return stringBuilder.ToString();
	}
}
