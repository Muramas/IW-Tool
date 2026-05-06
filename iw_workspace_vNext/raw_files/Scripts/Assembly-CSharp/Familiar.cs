using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Familiar
{
	public class SaveData
	{
		public int Id;

		public BigNumber XP;

		public int Rank;

		public float FedStatus;
	}

	public int Id;

	public string Name;

	public string Description;

	public int Rarity;

	public VariableInt Level;

	public VariableInt Rank;

	public SimpleEffect MainBonus;

	public List<FamiliarTags> Tags;

	public Sprite Icon;

	public string Prefab;

	public List<FamiliarPerk> Perks;

	private BigNumber baseBonus;

	private BigNumber XP;

	private BigNumber XP2Level;

	private BigNumber CurrentXP;

	private bool isSelected;

	private float fedStatus;

	private const float BASE_XP_REQ = 3600f;

	private const float XP_GROWTH_RATE = 1.5f;

	private const float XP_RANK_BONUS = 1.4f;

	public float FedStatus => fedStatus;

	public bool IsSelected
	{
		get
		{
			return isSelected;
		}
		set
		{
			if (isSelected == value)
			{
				return;
			}
			isSelected = value;
			if (isSelected)
			{
				UpdatePerks();
			}
			else
			{
				foreach (FamiliarPerk perk in Perks)
				{
					perk.Remove();
				}
			}
			UpdateMainBonus();
			MainBonus.Apply();
		}
	}

	public Familiar(FamiliarFormat format, SpriteAtlas atlas)
	{
		Id = format.Id;
		Name = format.Name;
		string[] array = format.MainBonus.Split("=");
		MainBonus = new SimpleEffect(GameContext.GetResource(array[0]), 0.0, 1.0);
		baseBonus = new BigNumber(array[1]);
		Description = array[2];
		Rarity = format.Rarity;
		Level = new VariableInt(0);
		Rank = new VariableInt(0);
		Icon = atlas.Get(format.Sprite);
		Prefab = format.Prefab;
		Perks = new List<FamiliarPerk>();
		CreatePerk(10, format.Perk10);
		CreatePerk(20, format.Perk20);
		Tags = new List<FamiliarTags>();
		string[] array2 = format.Tags.Split(" ");
		foreach (string value in array2)
		{
			Tags.Add((FamiliarTags)Enum.Parse(typeof(FamiliarTags), value));
		}
	}

	private void CreatePerk(int lvl, string perkString)
	{
		string[] array = perkString.Split("=");
		bool num = array.Length > 3;
		string argument = string.Empty;
		if (array.Length > 3)
		{
			argument = array[3].Replace("a", "");
		}
		SimpleEffect effect = ((!num) ? new SimpleEffect(GameContext.GetResource(array[0]), 0.0, array[1]) : new SimpleEffect(GameContext.GetResource(array[0]), array[1], 1.0));
		Perks.Add(new FamiliarPerk(lvl, effect, array[2], argument));
	}

	public void Load(SaveData data)
	{
		if (data == null)
		{
			XP = 0.0;
			Rank.SetValue(0);
			fedStatus = 0f;
		}
		else
		{
			XP = data.XP;
			Rank.SetValue(data.Rank);
			fedStatus = data.FedStatus;
		}
		RecalculateLevel();
	}

	public SaveData Save()
	{
		return new SaveData
		{
			Id = Id,
			XP = XP,
			Rank = Rank.ValueInt,
			FedStatus = fedStatus
		};
	}

	public void SetRank(int rank)
	{
		int valueInt = Rank.ValueInt;
		Rank.SetValue(rank);
		GameManager.Instance.Familiars.UpdateRank(this, rank - valueInt);
		UpdateMainBonus();
		RecalculateLevel();
		MainBonus.Apply();
		if (IsSelected)
		{
			UpdatePerks();
		}
	}

	private void RecalculateLevel()
	{
		if (Rank.ValueInt == 0)
		{
			Level.SetValue(0);
			return;
		}
		int num = 1 + BigNumber.AmountOfElementsGeometryProgression(XP, 1.5f, 3600.0).Floor().ToInt();
		XP2Level = 3600.0 * new BigNumber(1.5).Pow(num - 1);
		CurrentXP = XP - BigNumber.GeometrySumm(num - 1, 3600.0, 1.5);
		if (Level.ValueInt != num)
		{
			GameManager.Instance.Familiars.TotalLevel.Change(num - Level.ValueInt);
			Level.SetValue(num);
		}
	}

	public void AddFeedTime(float seconds)
	{
		fedStatus += seconds;
		UpdateMainBonus();
	}

	public void AddXP(float dt)
	{
		float num = Mathf.Min(dt, fedStatus);
		float num2 = dt - num;
		fedStatus = Mathf.Max(0f, fedStatus - dt);
		float num3 = Mathf.Pow(1.4f, Rank.ValueInt - 1);
		BigNumber bigNumber = (num * 2f + num2) * num3;
		XP += bigNumber;
		CurrentXP += bigNumber;
		if (CurrentXP >= XP2Level)
		{
			RecalculateLevel();
		}
	}

	private void FedStatusUpdate(float dt)
	{
		if (!(fedStatus <= 0f))
		{
			fedStatus = Mathf.Max(0f, fedStatus - dt);
			if (fedStatus <= 0f)
			{
				UpdateMainBonus();
			}
		}
	}

	public void UpdatePerks()
	{
		if (!IsSelected)
		{
			return;
		}
		int valueInt = Level.ValueInt;
		foreach (FamiliarPerk perk in Perks)
		{
			if (valueInt >= perk.Level)
			{
				perk.Apply();
			}
			else
			{
				perk.Remove();
			}
		}
	}

	public void DeactivateAll()
	{
		MainBonus.Delete();
		foreach (FamiliarPerk perk in Perks)
		{
			perk.Remove();
		}
		VariableInt level = Level;
		level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(UpdateMainBonus));
		VariableInt level2 = Level;
		level2.OnChange = (Action)Delegate.Remove(level2.OnChange, new Action(UpdatePerks));
		IReadOnlyDictionary<FamiliarTags, VariableInt> rankTotalsByTag = GameManager.Instance.Familiars.RankTotalsByTag;
		foreach (FamiliarTags tag in Tags)
		{
			VariableInt variableInt = rankTotalsByTag[tag];
			variableInt.OnChange = (Action)Delegate.Remove(variableInt.OnChange, new Action(UpdateMainBonus));
		}
	}

	public void CheckActivation()
	{
		if (!GameManager.Instance.Paragon.FamiliarsIsAvailable)
		{
			return;
		}
		UpdateMainBonus();
		MainBonus.Apply();
		UpdatePerks();
		if (Rank.ValueInt <= 0)
		{
			return;
		}
		VariableInt level = Level;
		level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(UpdateMainBonus));
		VariableInt level2 = Level;
		level2.OnChange = (Action)Delegate.Combine(level2.OnChange, new Action(UpdatePerks));
		IReadOnlyDictionary<FamiliarTags, VariableInt> rankTotalsByTag = GameManager.Instance.Familiars.RankTotalsByTag;
		foreach (FamiliarTags tag in Tags)
		{
			VariableInt variableInt = rankTotalsByTag[tag];
			variableInt.OnChange = (Action)Delegate.Combine(variableInt.OnChange, new Action(UpdateMainBonus));
		}
	}

	public void UpdateMainBonus()
	{
		BigNumber mainBonus = GetMainBonus();
		if (!IsSelected)
		{
			mainBonus *= (BigNumber)0.009999999776482582;
		}
		else if (fedStatus > 0f)
		{
			mainBonus *= (BigNumber)2.0;
		}
		MainBonus.mult = 1.0 + mainBonus;
		MainBonus.Update();
	}

	public BigNumber GetMainBonus()
	{
		return (baseBonus.Pow(Level.ValueInt) - 1.0) * (1f + (float)GetRaiting() * 0.01f);
	}

	public float GetXpProgress()
	{
		return (CurrentXP / XP2Level).ToFloat();
	}

	public int GetRaiting()
	{
		IReadOnlyDictionary<FamiliarTags, VariableInt> rankTotalsByTag = GameManager.Instance.Familiars.RankTotalsByTag;
		int num = 0;
		foreach (FamiliarTags tag in Tags)
		{
			num += rankTotalsByTag[tag].ValueInt;
		}
		return num;
	}

	public string GetDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<size=20><b>").Append(Name.Translate()).Append("</size></b>")
			.AppendLine()
			.AppendLine();
		stringBuilder.Append(string.Join(", ", Tags.Select((FamiliarTags tag) => tag.ToString().Translate()))).AppendLine();
		stringBuilder.Append("Rank".Translate()).Append(" ").Append(Rank.ValueInt)
			.AppendLine();
		stringBuilder.Append("Level".Translate()).Append(" ").Append(Level.ValueInt)
			.AppendLine();
		if (Rank.ValueInt > 0)
		{
			stringBuilder.Append("XP".Translate()).Append(" ").Append(CurrentXP.ToReadableString())
				.Append("/")
				.Append(XP2Level.ToReadableString())
				.AppendLine();
			stringBuilder.Append("+").Append(((new BigNumber(Mathf.Pow(1.4f, Rank.ValueInt - 1)) - 1.0) * 100.0).ToReadableString()).Append("% ");
			stringBuilder.Append("XPGainFromRank".Translate()).AppendLine();
		}
		if (fedStatus > 0f)
		{
			stringBuilder.Append(string.Format("FedStatus".Translate(), Statistic.time_to_string(fedStatus, full: false, fillSeconds: true))).AppendLine();
		}
		stringBuilder.AppendLine();
		stringBuilder.Append(TranslationManager.Instance.Process(Description));
		stringBuilder.Append(" +");
		stringBuilder.Append(getPreview()).AppendLine();
		foreach (FamiliarPerk perk in Perks)
		{
			stringBuilder.AppendLine(getPerkPreview(perk));
		}
		return stringBuilder.ToString();
	}

	public string PreviewShort()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Name.Translate()).AppendLine();
		stringBuilder.Append(string.Join(", ", Tags.Select((FamiliarTags tag) => tag.ToString().Translate()))).AppendLine();
		stringBuilder.Append("Rank".Translate()).Append(" ").Append(Rank.ValueInt)
			.AppendLine();
		stringBuilder.Append("Level".Translate()).Append(" ").Append(Level.ValueInt);
		return stringBuilder.ToString();
	}

	private string getPreview()
	{
		string text = FormatMainBonusPreview(isActive: true);
		string text2 = FormatMainBonusPreview(isActive: false);
		string text3 = "NotAcitve".Translate();
		if (Settings.ColoredTips)
		{
			text = "<color=#e2b018>" + text + "</color>";
		}
		if (IsSelected || Rank.ValueInt == 0)
		{
			return text;
		}
		return text + " (+" + text2 + " " + text3 + ")";
	}

	private string FormatMainBonusPreview(bool isActive)
	{
		BigNumber mainBonus = GetMainBonus();
		BigNumber bigNumber = (isActive ? mainBonus : (mainBonus * 0.009999999776482582));
		if (isActive && fedStatus > 0f)
		{
			bigNumber *= (BigNumber)2.0;
		}
		return (bigNumber * 100.0).ToReadableString() + "%";
	}

	private string getPerkPreview(FamiliarPerk perk)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool num = Level.ValueInt >= perk.GetLevel();
		if (!num)
		{
			stringBuilder.Append("<color=#808080ff>");
		}
		stringBuilder.Append("[");
		stringBuilder.Append(perk.GetLevel());
		stringBuilder.Append("] ");
		stringBuilder.Append(perk.GetDescription());
		if (!num)
		{
			stringBuilder.Append("</color>");
		}
		return stringBuilder.ToString();
	}
}
