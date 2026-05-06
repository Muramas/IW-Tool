using System.Text;

public class ConditionUnlockVariable : ConditionUnlock
{
	protected Variable param;

	private bool ext_des;

	private bool display_des;

	public ConditionUnlockVariable(Variable par, string arg, string des, bool calculate_des = true, bool show_progress = true)
	{
		argument = arg;
		Description = des;
		ext_des = calculate_des;
		display_des = show_progress;
		param = par;
	}

	public override bool Check()
	{
		if (param == null)
		{
			return false;
		}
		return param.Value >= argument - 0.0010000000474974513;
	}

	public Variable GetParameter()
	{
		return param;
	}

	public override string Preview(bool show_progress = true, bool show_complete = true)
	{
		string text = TranslationManager.Instance.Process(Description) + " ";
		if (show_progress)
		{
			if (param != null)
			{
				if (param.Value >= argument)
				{
					if (ext_des)
					{
						text = text + argument.ToReadableString("F0") + " ";
					}
					if (show_complete)
					{
						text += "completed".Translate();
					}
				}
				else
				{
					StringBuilder stringBuilder = new StringBuilder();
					if (ext_des)
					{
						stringBuilder.Append(param.Value.ToReadableString("F0"));
						stringBuilder.Append(" / ");
						stringBuilder.Append(argument.ToReadableString("F0"));
					}
					if (display_des)
					{
						BigNumber bigNumber = param.Value / argument;
						stringBuilder.Append(" (");
						stringBuilder.Append((bigNumber * 100.0).ToReadableString());
						stringBuilder.Append("%)");
					}
					text += stringBuilder.ToString();
				}
			}
		}
		else if (display_des)
		{
			text += argument.ToReadableString();
		}
		return text;
	}
}
