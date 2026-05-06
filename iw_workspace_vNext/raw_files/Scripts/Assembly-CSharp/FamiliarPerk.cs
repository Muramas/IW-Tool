public class FamiliarPerk
{
	public int Level;

	private SimpleEffect effect;

	public string Description;

	private bool applied;

	private string argument;

	public FamiliarPerk(int level, SimpleEffect effect, string description, string argument = "")
	{
		Level = level;
		this.effect = effect;
		Description = description;
		this.argument = argument;
	}

	public void Apply()
	{
		if (!applied)
		{
			effect.Apply();
			applied = true;
		}
	}

	public void Remove()
	{
		if (applied)
		{
			effect.Delete();
			applied = false;
		}
	}

	public void Update()
	{
		if (applied)
		{
			effect.Update();
		}
	}

	public string GetDescription()
	{
		return TranslationManager.Instance.Process(Description) + " +" + effect.Preview(argument);
	}

	public int GetLevel()
	{
		return Level;
	}

	public bool IsApplied()
	{
		return applied;
	}
}
