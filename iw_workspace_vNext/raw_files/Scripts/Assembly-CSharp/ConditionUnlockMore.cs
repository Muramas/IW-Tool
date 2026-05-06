using System.Text;

public class ConditionUnlockMore : ConditionUnlock
{
	private Variable param;

	private BigNumber starts = 0.0;

	private bool ext_des;

	private bool less;

	private bool isTime;

	public ConditionUnlockMore(string par, string arg, string des, bool calculate_des = true, BigNumber start_from = default(BigNumber), bool less = false, bool isTime = false)
	{
		parameter = par;
		argument = arg;
		Description = des;
		ext_des = calculate_des;
		this.less = less;
		this.isTime = isTime;
		if (start_from != default(BigNumber))
		{
			starts = start_from;
		}
		param = GameContext.GetResource(parameter);
	}

	public override bool Check()
	{
		if (param == null)
		{
			param = GameContext.GetResource(parameter);
		}
		if (param == null)
		{
			return false;
		}
		return compair();
	}

	private bool compair()
	{
		if (!less)
		{
			return param.Value >= argument;
		}
		return param.Value <= argument;
	}

	public override string Preview(bool show_progress = true, bool show_complete = true)
	{
		string text = Description.Translate() + " ";
		if (show_progress)
		{
			if (param == null)
			{
				param = GameContext.GetResource(parameter);
			}
			if (param != null)
			{
				if (compair())
				{
					if (ext_des)
					{
						text += argument.ToReadableString("F0");
					}
					text = text + " " + "completed".Translate();
				}
				else if (less)
				{
					text = text + " " + "failed".Translate();
				}
				else
				{
					StringBuilder stringBuilder = new StringBuilder();
					if (ext_des)
					{
						if (isTime)
						{
							stringBuilder.Append(Statistic.time_to_string(param.Value));
						}
						else
						{
							stringBuilder.Append(param.Value.ToReadableString("F0"));
						}
						stringBuilder.Append(" / ");
						if (isTime)
						{
							stringBuilder.Append(Statistic.time_to_string(argument));
						}
						else
						{
							stringBuilder.Append(argument.ToReadableString("F0"));
						}
					}
					stringBuilder.Append(" (");
					stringBuilder.Append(((param.Value - starts) / (argument - starts) * 100.0).ToReadableString());
					stringBuilder.Append("%)");
					text += stringBuilder.ToString();
				}
			}
		}
		else
		{
			text += argument.ToReadableString();
		}
		return text;
	}
}
