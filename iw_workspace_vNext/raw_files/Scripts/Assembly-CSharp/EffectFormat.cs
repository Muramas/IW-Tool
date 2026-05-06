using System.Text.RegularExpressions;

public class EffectFormat
{
	public string Description;

	protected static string pattern = "\\+{0,1}#\\d{1}[amtpqkwc@%&]{0,1}";

	private bool showColor = true;

	public string GetDescription(bool showColor)
	{
		this.showColor = showColor;
		return GetDescription();
	}

	public virtual string GetDescription()
	{
		string input = TranslationManager.Instance.Process(Description);
		return new Regex(pattern).Replace(input, ReplaceMatch);
	}

	protected virtual string ReplaceMatch(Match m)
	{
		string key = "";
		bool flag = false;
		int num = 0;
		string text = m.ToString();
		if (text[0] == '+')
		{
			flag = true;
			if (text.Length > 3)
			{
				key = m.ToString()[3].ToString();
			}
			num = int.Parse(m.ToString()[2].ToString());
		}
		else
		{
			if (text.Length > 2)
			{
				key = m.ToString()[2].ToString();
			}
			num = int.Parse(m.ToString()[1].ToString());
		}
		string preview = GetPreview(num, key);
		if (Settings.ColoredTips && preview != "" && showColor)
		{
			return "<color=#e2b018>" + (flag ? "+" : string.Empty) + preview + "</color>";
		}
		return (flag ? "+" : string.Empty) + preview;
	}

	protected virtual string GetPreview(int id, string key)
	{
		return string.Empty;
	}
}
