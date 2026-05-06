public class SpellTypeBonuses
{
	protected Spell spell;

	public virtual void Init(Spell sp)
	{
		spell = sp;
	}

	public virtual void Deinit()
	{
	}

	public virtual void Update(int lvl)
	{
	}

	public virtual string GetDescription(int lvl)
	{
		return string.Empty;
	}

	public virtual float GetMainBonus()
	{
		return 1f;
	}

	public virtual BigNumber GetBonus(int lvl)
	{
		return new BigNumber(GetMainBonus()).Pow(GetLevel(lvl));
	}

	public virtual int GetLevel(int lvl)
	{
		return lvl;
	}
}
