using System;
using System.Text;
using UnityEngine;

public class ConditionalAchievemet : Achievement
{
	public string condition_key;

	public string parameter;

	public BigNumber argument;

	protected Variable param;

	protected bool param_sub;

	private Condition cond;

	public override void Init()
	{
		if (!Unlocked)
		{
			cond = GameContext.GetCondition(condition_key);
			cond.condition(parameter, argument, ref param);
			Subscribe();
		}
	}

	protected override bool condition()
	{
		if (Unlocked || !CheckUnlockable())
		{
			return Unlocked;
		}
		return cond.condition(parameter, argument, ref param);
	}

	public override bool CheckUnlock()
	{
		if (Unlocked || !CheckUnlockable())
		{
			return Unlocked;
		}
		Subscribe();
		return condition();
	}

	protected override void OnChangeUnlock()
	{
		if (Unlocked)
		{
			UnSubscribe();
		}
		else
		{
			Subscribe();
		}
	}

	public override string Preview()
	{
		string result = "";
		if (Unlocked || cond == null || param == null)
		{
			return argument.ToReadableString("F0");
		}
		if (cond != null)
		{
			bool flag = condition();
			if (Unlocked)
			{
				result = argument.ToReadableString("F0");
			}
			else
			{
				BigNumber bigNumber = param.Value;
				StringBuilder stringBuilder = new StringBuilder();
				if (flag)
				{
					stringBuilder.Append(param.Value.ToReadableString("F0"));
				}
				else
				{
					if (condition_key == ConditionNames.PetLevel.ToString())
					{
						bigNumber = new BigNumber(0.0);
						if (GameManager.Instance.CurrentPet.Pet != null && GameManager.Instance.CurrentPet.Pet.NameKey.ToString() == parameter)
						{
							bigNumber = param.Value;
						}
					}
					else if (condition_key == ConditionNames.HeroLevel.ToString())
					{
						bigNumber = ((!(GameManager.Instance.CurrentHero.Hero.NameKey.ToString() == parameter)) ? new BigNumber(0.0) : param.Value);
					}
					else if (condition_key == ConditionNames.AscensionLevel.ToString())
					{
						bigNumber = ((!GameManager.Instance.Ascension.IsActive || !(GameManager.Instance.Ascension.GetCurrent().NameKey.ToString() == parameter)) ? new BigNumber(0.0) : param.Value);
					}
					stringBuilder.Append(bigNumber.ToReadableString("F0"));
				}
				stringBuilder.Append(" / ");
				stringBuilder.Append(argument.ToReadableString("F0"));
				stringBuilder.Append(" (");
				stringBuilder.Append((bigNumber / argument * 100.0).ToReadableString());
				stringBuilder.Append("%)");
				result = stringBuilder.ToString();
			}
		}
		return result;
	}

	public override string GetTarget()
	{
		return argument.ToReadableString("F0");
	}

	protected override void Subscribe()
	{
		if (param_sub)
		{
			return;
		}
		if (cond == null)
		{
			cond = GameContext.GetCondition(condition_key);
		}
		if (cond == null)
		{
			Debug.LogError("achiev condition not found in context " + condition_key + " " + parameter);
		}
		if (param == null)
		{
			cond.condition(parameter, argument, ref param);
			if (param == null)
			{
				Debug.Log("parameter " + parameter);
			}
		}
		Variable variable = param;
		variable.OnChange = (Action)Delegate.Combine(variable.OnChange, new Action(base.Check));
		param_sub = true;
	}

	protected override void UnSubscribe()
	{
		if (param_sub)
		{
			Variable variable = param;
			variable.OnChange = (Action)Delegate.Remove(variable.OnChange, new Action(base.Check));
			param_sub = false;
		}
	}

	public override BigNumber GetArgument()
	{
		return argument;
	}
}
