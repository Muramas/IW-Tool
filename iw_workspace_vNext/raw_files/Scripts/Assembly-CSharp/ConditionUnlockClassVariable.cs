using System.Text;

public class ConditionUnlockClassVariable : ConditionUnlock
{
	private Variable param;

	private bool ext_des;

	private bool display_des;

	private HeroesNames key;

	public ConditionUnlockClassVariable(HeroesNames key, Variable par, string arg, string des, bool calculate_des = true, bool show_progress = true)
	{
		this.key = key;
		argument = arg;
		Description = des;
		ext_des = calculate_des;
		display_des = show_progress;
		param = par;
	}

	public override bool Check()
	{
		if (param == null || GameManager.Instance.CurrentHero.Hero.NameKey != key)
		{
			return false;
		}
		return param.Value >= argument;
	}

	public override string Preview(bool show_progress = true, bool show_complete = true)
	{
		string text = Description.Translate() + " ";
		if (show_progress)
		{
			if (param != null && GameManager.Instance.CurrentHero.Hero.NameKey == key)
			{
				if (param.Value >= argument)
				{
					if (ext_des)
					{
						text += argument.ToReadableString("F0");
					}
					return text + "completed".Translate();
				}
				StringBuilder stringBuilder = new StringBuilder();
				if (ext_des)
				{
					stringBuilder.Append(param.Value.ToReadableString("F0"));
					stringBuilder.Append(" / ");
					stringBuilder.Append(argument.ToReadableString("F0"));
				}
				if (display_des)
				{
					stringBuilder.Append(" (");
					stringBuilder.Append((param.Value / argument * 100.0).ToReadableString());
					stringBuilder.Append("%)");
				}
				return text + stringBuilder.ToString();
			}
			return text + argument.ToReadableString();
		}
		return text + argument.ToReadableString();
	}
}
