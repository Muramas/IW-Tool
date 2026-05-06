public class ConditionChallenge : ConditionUnlock
{
	private int id;

	private Challenge challenge;

	public ConditionChallenge(int par)
	{
		id = par;
	}

	private void Init()
	{
		challenge = GameManager.Instance.ChallengeManager.Challenges.Find((Challenge x) => x.ID == id);
		Description = "Challenge".Translate() + " " + TranslationManager.Instance.Process(challenge.Name);
	}

	public override bool Check()
	{
		if (challenge == null)
		{
			Init();
		}
		return challenge.Completed;
	}

	public override string Preview(bool show_progress = true, bool show_complete = true)
	{
		if (challenge == null)
		{
			Init();
		}
		return Description + (challenge.Completed ? (" " + "completed".Translate()) : string.Empty);
	}
}
