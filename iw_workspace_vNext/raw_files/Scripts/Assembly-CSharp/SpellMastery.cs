using System;

public class SpellMastery
{
	public Skill Skill;

	public MasteryBonuses Bonuses;

	public VariableInt Level;

	public SpellMastery()
	{
		Skill = new Skill(0f, 600, 1.1f, 1f);
		Skill skill = Skill;
		skill.OnEarnExp = (Action)Delegate.Combine(skill.OnEarnExp, new Action(CheckLevel));
		Skill.name = "Mastery";
		Level = new VariableInt(0);
		GameContext.ContextAddResource("SpellGilding.MasteryLevel", Level);
		Bonuses = new MasteryBonuses(Level);
	}

	public void AddExp(BigNumber exp)
	{
		Skill.AddExp(exp);
	}

	private void CheckLevel()
	{
		if (Skill.level != Level.ValueInt)
		{
			Level.SetValue(Skill.level);
		}
	}
}
