using System.Collections.Generic;

public class SkillManager
{
	public enum SkillNames
	{
		Gathering = 0,
		Crafting = 1,
		Praying = 2,
		Mentalizing = 3,
		Other = 100
	}

	public VariableComplex ExpBonus;

	private Dictionary<SkillNames, Skill> skills;

	private int BaseSkillExp = 1000;

	private float SkillGR = 1.2f;

	private float baseExpCraftPerAction = 100f;

	private float baseExpGatherPerAction = 50f;

	private float baseExpPrayPerAction = 50f;

	private float baseExpMemeticPerAction = 84f;

	public SkillManager()
	{
		ExpBonus = new VariableComplex(1.0);
		skills = new Dictionary<SkillNames, Skill>();
		Skill value = new Skill(0.03f, BaseSkillExp, SkillGR, baseExpGatherPerAction)
		{
			name = "Gathering",
			description = "GatheringDescr"
		};
		skills.Add(SkillNames.Gathering, value);
		value = new Skill(0.05f, BaseSkillExp, SkillGR, baseExpCraftPerAction)
		{
			name = "Crafting",
			description = "CraftingDescr"
		};
		skills.Add(SkillNames.Crafting, value);
		value = new Skill(0.03f, BaseSkillExp, SkillGR, baseExpPrayPerAction)
		{
			name = "Praying",
			description = "PrayingDescr"
		};
		skills.Add(SkillNames.Praying, value);
		value = new Skill(0.03f, BaseSkillExp, SkillGR, baseExpMemeticPerAction)
		{
			name = "Mentalizing",
			description = "MentalizingDescr"
		};
		skills.Add(SkillNames.Mentalizing, value);
	}

	public Skill Get(SkillNames key)
	{
		if (!skills.ContainsKey(key))
		{
			return null;
		}
		return skills[key];
	}
}
