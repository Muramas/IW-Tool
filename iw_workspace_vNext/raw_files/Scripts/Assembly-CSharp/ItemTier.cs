using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public class ItemTier : IItemTier
{
	public string Description;

	public Dictionary<CraftResource, int> UpgradeCost;

	public List<ItemReqs> Reqs;

	public List<IEffect> Effects;

	public bool applied;

	protected static string pattern = "\\+{0,1}#\\d{1}[amtpqkwc@%&]{0,1}";

	public virtual void Apply()
	{
		if (applied)
		{
			return;
		}
		applied = true;
		for (int i = 0; i < Effects.Count; i++)
		{
			IEffect effect = Effects[i];
			effect.Apply();
			if (effect is SimpleEffect)
			{
				SimpleEffect simpleEffect = effect as SimpleEffect;
				if (simpleEffect.parameter != null)
				{
					Variable parameter = simpleEffect.parameter;
					parameter.OnChange = (Action)Delegate.Combine(parameter.OnChange, new Action(simpleEffect.Update));
				}
			}
		}
	}

	public virtual void Update()
	{
		if (!applied)
		{
			return;
		}
		foreach (IEffect effect in Effects)
		{
			effect.Update();
		}
	}

	public virtual void Delete()
	{
		if (!applied)
		{
			return;
		}
		applied = false;
		for (int i = 0; i < Effects.Count; i++)
		{
			IEffect effect = Effects[i];
			if (effect is SimpleEffect)
			{
				SimpleEffect simpleEffect = effect as SimpleEffect;
				if (simpleEffect.parameter != null)
				{
					Variable parameter = simpleEffect.parameter;
					parameter.OnChange = (Action)Delegate.Remove(parameter.OnChange, new Action(simpleEffect.Update));
				}
			}
			effect.Delete();
		}
	}

	public void SetGilding(Variable variable)
	{
		foreach (IEffect effect in Effects)
		{
			effect.SetGilding(variable);
		}
	}

	public void SetEfficiency(Variable variable)
	{
		foreach (IEffect effect in Effects)
		{
			effect.SetEfficiency(variable);
		}
	}

	public virtual int GetReqs(Attributes key)
	{
		string attr = key.ToString();
		return Reqs.Find((ItemReqs x) => x.Name == attr)?.Value ?? 0;
	}

	public bool CheckReqs()
	{
		if (Reqs == null)
		{
			return true;
		}
		bool flag = true;
		for (int i = 0; i < Reqs.Count && flag; i++)
		{
			flag = flag && Reqs[i].Available();
		}
		return flag;
	}

	public string GetReqDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (Reqs != null && Reqs.Count > 0)
		{
			stringBuilder.Append("Requirements".Translate());
			stringBuilder.Append(": ");
			for (int i = 0; i < Reqs.Count; i++)
			{
				stringBuilder.Append(Reqs[i].Name.Translate());
				stringBuilder.Append(" ");
				stringBuilder.Append(Reqs[i].Value.ToString());
				if (i < Reqs.Count - 1)
				{
					stringBuilder.Append(", ");
				}
			}
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString();
	}

	public Dictionary<CraftResource, int> GetCost()
	{
		return UpgradeCost;
	}

	public List<ItemReqs> GetReqs()
	{
		return Reqs;
	}

	public string PreviewEfficiency()
	{
		return Effects[0].Preview("e");
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
		SimpleEffect simpleEffect = Effects[num - 1] as SimpleEffect;
		string text2 = simpleEffect.Preview(key);
		string text3 = (flag ? "+" : string.Empty) + text2;
		if (Settings.ColoredTips && simpleEffect.pow_diminishing != 0f)
		{
			text3 = "<color=#e2b018>" + text3 + "</color>";
		}
		if (simpleEffect.GetEfficiency().Value > 1.0)
		{
			text3 = text3 + " (+" + simpleEffect.Preview("e") + ")";
		}
		return text3;
	}
}
