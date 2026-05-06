using System.Text;

public class ConditionUnlockSpellUse : ConditionUnlock
{
	private Variable param;

	private Spells key;

	private bool ext_des;

	private Spell spell;

	public ConditionUnlockSpellUse(Spells par, string arg, string des = "", bool calculate_des = true)
	{
		parameter = par.ToString();
		key = par;
		argument = arg;
		Description = des;
		ext_des = calculate_des;
	}

	private void findSpell()
	{
		if (GameManager.Instance.SpellBook.SpellList != null)
		{
			spell = GameManager.Instance.SpellBook.GetActualSpell(key);
			if (spell != null)
			{
				param = getCasts();
			}
		}
	}

	public override bool Check()
	{
		findSpell();
		if (param == null)
		{
			return false;
		}
		return argument.ToDouble() - param.Value.ToDouble() < 0.5;
	}

	private Variable getCasts()
	{
		if (spell == null)
		{
			return null;
		}
		if (spell.IsPersistent)
		{
			return spell.Use;
		}
		return spell.UseThisRun;
	}

	public override string Preview(bool show_progress = true, bool show_complete = true)
	{
		if (spell == null)
		{
			findSpell();
		}
		string text = (string.IsNullOrEmpty(Description) ? "" : Description.Translate()) + string.Format("CastSpellX".Translate(), spell.Name.Translate()) + " ";
		if (show_progress)
		{
			if (Check())
			{
				if (ext_des)
				{
					text += argument.ToReadableString("F0");
				}
				if (show_complete)
				{
					text = text + " " + "completed".Translate();
				}
			}
			else
			{
				BigNumber value = param.Value;
				StringBuilder stringBuilder = new StringBuilder();
				if (ext_des)
				{
					stringBuilder.Append(value.ToReadableString("F0"));
					stringBuilder.Append(" / ");
					stringBuilder.Append(argument.ToReadableString("F0"));
				}
				stringBuilder.Append(" (");
				stringBuilder.Append((value / argument * 100.0).ToReadableString());
				stringBuilder.Append("%)");
				text += stringBuilder.ToString();
			}
		}
		else
		{
			text += argument.ToReadableString();
		}
		return text;
	}
}
