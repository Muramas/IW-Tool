using System.Text;

public class ConditionUnlockMoreThan : ConditionUnlock
{
	private Variable param;

	private Variable target;

	private bool ext_des;

	public ConditionUnlockMoreThan(Variable par, Variable tar, string arg, string des, bool calculate_des = true)
	{
		argument = arg;
		target = tar;
		Description = des;
		ext_des = calculate_des;
		param = par;
	}

	public override bool Check()
	{
		if (param == null)
		{
			return false;
		}
		if (param.Value == 0.0 || target.Value == 0.0)
		{
			return false;
		}
		return param.Value >= argument * target.Value;
	}

	public override string Preview(bool show_progress = true, bool show_complete = true)
	{
		string text = Description.Translate() + " ";
		if (show_progress)
		{
			if (param != null)
			{
				if (param.Value >= argument * target.Value)
				{
					if (ext_des)
					{
						text += argument.ToReadableString("F0");
					}
					text += "completed".Translate();
				}
				else
				{
					StringBuilder stringBuilder = new StringBuilder();
					if (ext_des)
					{
						stringBuilder.Append(param.Value.ToReadableString("F0"));
						stringBuilder.Append(" / ");
						stringBuilder.Append((argument * target.Value).ToReadableString("F0"));
					}
					stringBuilder.Append(" (");
					stringBuilder.Append((param.Value / (argument * target.Value) * 100.0).ToReadableString());
					stringBuilder.Append("%)");
					text += stringBuilder.ToString();
				}
			}
		}
		else
		{
			text += target.Value.ToReadableString("F0");
		}
		return text;
	}
}
