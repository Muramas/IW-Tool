public class ConditionUnlockAchievement : ConditionUnlock
{
	private Variable param;

	private AchievementKey key;

	private int level;

	private Achievement achiev;

	public ConditionUnlockAchievement(AchievementKey par, int _level, string des = "", bool calculate_des = true)
	{
		parameter = par.ToString();
		key = par;
		level = _level;
		Description = des;
		if (GameManager.Instance.AchievManager.AchievList != null)
		{
			achiev = GameManager.Instance.AchievManager.AchievList.Find((AchievementCategory x) => x.Key == key).Row[level];
		}
	}

	public override bool Check()
	{
		if (achiev == null && GameManager.Instance.AchievManager.AchievList != null)
		{
			achiev = GameManager.Instance.AchievManager.AchievList.Find((AchievementCategory x) => x.Key == key).Row[level];
		}
		if (achiev == null)
		{
			return false;
		}
		return achiev.Unlocked;
	}

	public override string Preview(bool show_progress = true, bool show_complete = true)
	{
		if (achiev == null && GameManager.Instance.AchievManager.AchievList != null)
		{
			achiev = GameManager.Instance.AchievManager.AchievList.Find((AchievementCategory x) => x.Key == key).Row[level];
		}
		if (achiev == null)
		{
			return string.Empty;
		}
		string text = achiev.GetDescription();
		if (show_progress && achiev != null)
		{
			text = text + " " + achiev.Preview();
		}
		return text;
	}
}
