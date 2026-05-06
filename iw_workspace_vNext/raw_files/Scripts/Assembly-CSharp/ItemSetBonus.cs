using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public class ItemSetBonus
{
	public class Bonus
	{
		public int Amount;

		public SimpleEffect Effect;

		public string Description;

		private bool applied;

		protected static string pattern = "\\+{0,1}#\\d{1}[amtpqkwc@%&]{0,1}";

		public bool IsActive => applied;

		public Bonus(int amount, string descr, SimpleEffect effect)
		{
			Amount = amount;
			Description = descr;
			Effect = effect;
		}

		public void Apply()
		{
			if (!applied)
			{
				applied = true;
				if (Effect != null)
				{
					Effect.Apply();
				}
			}
		}

		public void Remove()
		{
			if (applied)
			{
				applied = false;
				if (Effect != null)
				{
					Effect.Delete();
				}
			}
		}

		public string Preview()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (!applied)
			{
				stringBuilder.Append("<color=#808080ff>");
			}
			stringBuilder.Append(Amount);
			stringBuilder.Append(" ");
			stringBuilder.Append(GetDescription());
			if (!applied)
			{
				stringBuilder.Append("</color>");
			}
			return stringBuilder.ToString();
		}

		protected virtual string GetDescription()
		{
			string input = TranslationManager.Instance.Process(Description);
			return new Regex(pattern).Replace(input, ReplaceMatch);
		}

		protected virtual string ReplaceMatch(Match m)
		{
			string key = "";
			bool flag = false;
			string text = m.ToString();
			if (text[0] == '+')
			{
				flag = true;
				if (text.Length > 3)
				{
					key = m.ToString()[3].ToString();
				}
			}
			else if (text.Length > 2)
			{
				key = m.ToString()[2].ToString();
			}
			return (flag ? "+" : string.Empty) + Effect.Preview(key);
		}
	}

	public ItemSetKeys Key;

	public List<Bonus> Bonuses;

	public List<Item> Items;

	private int amount;

	private bool isActive = true;

	public string Name => (Key.ToString() + " set").Translate();

	public ItemSetBonus(ItemSetKeys key)
	{
		Key = key;
		Bonuses = new List<Bonus>();
		amount = 0;
		Items = new List<Item>();
		isActive = true;
	}

	public void UpdateSet()
	{
		if (!isActive)
		{
			return;
		}
		amount = 0;
		for (int i = 0; i < Items.Count; i++)
		{
			if (Items[i].active)
			{
				amount++;
			}
		}
		for (int j = 0; j < Bonuses.Count; j++)
		{
			if (Bonuses[j].Amount <= amount)
			{
				Bonuses[j].Apply();
			}
			else
			{
				Bonuses[j].Remove();
			}
		}
	}

	public void Deactivate()
	{
		isActive = false;
		for (int i = 0; i < Bonuses.Count; i++)
		{
			Bonuses[i].Remove();
		}
	}

	public void Activate()
	{
		isActive = true;
		UpdateSet();
	}

	public string Preview()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<b>");
		stringBuilder.Append(Name);
		stringBuilder.AppendLine("</b>");
		for (int i = 0; i < Items.Count; i++)
		{
			stringBuilder.Append("   ");
			if (!Items[i].equiped)
			{
				stringBuilder.Append("<color=#808080ff>");
			}
			stringBuilder.Append(Items[i].Name);
			stringBuilder.Append(" (");
			stringBuilder.Append(Items[i].Slot.ToString().Translate());
			stringBuilder.Append(")");
			if (!Items[i].equiped)
			{
				stringBuilder.Append("</color>");
			}
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine();
		for (int j = 0; j < Bonuses.Count; j++)
		{
			stringBuilder.AppendLine(Bonuses[j].Preview());
		}
		return stringBuilder.ToString();
	}
}
