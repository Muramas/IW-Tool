using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Upgrade : Buyable
{
	public class Factory : Upgrade
	{
		public static Upgrade Create(UpgradeFormat u, UpgradeManager manager)
		{
			Upgrade upgrade = new Upgrade();
			upgrade.Data = u;
			upgrade.manager = manager;
			upgrade.Name = u.Name;
			upgrade.ID = u.ID;
			upgrade.base_cost_string = u.Cost;
			upgrade.SpriteKey = u.Sprite;
			upgrade.Class = ((u.Class == null) ? "" : u.Class);
			upgrade.Description = u.Description;
			if (u.Effect == null || u.Effect == "")
			{
				u.Effect = EffectNames.Linear.ToString();
			}
			upgrade.effect = GameContext.GetEffect(u.Effect);
			if (u.Addendum == null || u.Addendum == "")
			{
				u.Addendum = 0.ToString();
			}
			upgrade.add = new BigNumber(u.Addendum);
			if (u.Multiplier == null || u.Multiplier == "")
			{
				u.Multiplier = 1.ToString();
			}
			upgrade.mult = new BigNumber(u.Multiplier);
			upgrade.effect = GameContext.GetEffect(u.Effect);
			if (u.CondtionAccess != null && u.CondtionAccess != "")
			{
				if (u.CondtionAccess == "Achieve")
				{
					upgrade.achieve = true;
				}
				upgrade.condition_access = GameContext.GetCondition(u.CondtionAccess);
				upgrade.access_parameter = u.CAParameter;
				if (u.CAArgumet != "")
				{
					upgrade.access_argumet = new BigNumber(u.CAArgumet);
				}
				else
				{
					upgrade.access_argumet = new BigNumber(0.0);
				}
			}
			upgrade.Building(u);
			upgrade.isSpell = !string.IsNullOrEmpty(u.Spell);
			upgrade.isBuilding = !string.IsNullOrEmpty(u.Building);
			upgrade.BuildingKey = ((!string.IsNullOrEmpty(u.Building)) ? int.Parse(u.Building) : 0);
			return upgrade;
		}
	}

	public string Name;

	public string ID;

	public string Description;

	public string SpriteKey;

	public string Class;

	public bool Available;

	public Effect effect;

	public Variable target;

	public BigNumber add;

	public BigNumber mult;

	public Variable parameter;

	public int BuildingKey;

	public EffectUpdateType effect_type;

	private UpgradeManager manager;

	public bool achieve;

	private Achievement achieveCondition;

	private Condition condition_access;

	public string access_parameter;

	private BigNumber access_argumet;

	private Variable prev_parameter;

	private bool subscribe_condition;

	private bool can_apply;

	private bool isSubToUpdate;

	public bool applied;

	public UpgradeFormat Data;

	public bool isSpell { get; private set; }

	public bool isBuilding { get; private set; }

	private Upgrade()
	{
	}

	public string Preview()
	{
		return effect.preview(add, mult, parameter);
	}

	public void Apply()
	{
		if (applied && !isSpell)
		{
			return;
		}
		if (target == null)
		{
			Debug.Log(Name);
		}
		effect.apply(target, add, mult, parameter);
		if (effect_type != EffectUpdateType.Permanent)
		{
			prev_parameter = new VariableComplex(parameter.Value);
			if (effect_type == EffectUpdateType.Parameterized)
			{
				Variable variable = parameter;
				variable.OnChange = (Action)Delegate.Combine(variable.OnChange, new Action(OneShotSub));
			}
			else if (effect_type == EffectUpdateType.Conditional)
			{
				UpgradeManager upgradeManager = manager;
				upgradeManager.update_conditional_upgrade = (Action)Delegate.Combine(upgradeManager.update_conditional_upgrade, new Action(UpdateConditional));
			}
		}
		applied = true;
	}

	private void OneShotSub()
	{
		if (!isSubToUpdate)
		{
			isSubToUpdate = true;
			GameManager instance = GameManager.Instance;
			instance.GameTickReal = (Action<float>)Delegate.Combine(instance.GameTickReal, new Action<float>(UpdateParametrized));
		}
	}

	public void UpdateParametrized(float dt)
	{
		if (!(prev_parameter.Value == parameter.Value))
		{
			effect.delete(target, add, mult, prev_parameter);
			effect.apply(target, add, mult, parameter);
			prev_parameter = new VariableComplex(parameter.Value);
			GameManager instance = GameManager.Instance;
			instance.GameTickReal = (Action<float>)Delegate.Remove(instance.GameTickReal, new Action<float>(UpdateParametrized));
			isSubToUpdate = false;
		}
	}

	public void UpdateConditional()
	{
		if (can_apply)
		{
			effect.delete(target, add, mult, prev_parameter);
		}
		can_apply = target != null;
		if (can_apply)
		{
			effect.apply(target, add, mult, parameter);
			prev_parameter = parameter;
		}
	}

	public void Delete()
	{
		if (!applied)
		{
			return;
		}
		effect.delete(target, add, mult, prev_parameter);
		if (effect_type == EffectUpdateType.Parameterized)
		{
			Variable variable = parameter;
			variable.OnChange = (Action)Delegate.Remove(variable.OnChange, new Action(OneShotSub));
			if (isSubToUpdate)
			{
				GameManager instance = GameManager.Instance;
				instance.GameTickReal = (Action<float>)Delegate.Remove(instance.GameTickReal, new Action<float>(UpdateParametrized));
				isSubToUpdate = false;
			}
		}
		else if (effect_type == EffectUpdateType.Conditional)
		{
			UpgradeManager upgradeManager = manager;
			upgradeManager.update_conditional_upgrade = (Action)Delegate.Remove(upgradeManager.update_conditional_upgrade, new Action(UpdateConditional));
		}
		applied = false;
	}

	public bool CheckClassAccess()
	{
		bool flag = Class == "";
		if (!flag)
		{
			List<int> upgradeFilter = GameManager.Instance.CurrentHero.Hero.UpgradeFilter;
			for (int i = 0; i < upgradeFilter.Count; i++)
			{
				int num = upgradeFilter[i];
				if (Class.Length > num)
				{
					flag = Class[num] == '1';
					if (flag)
					{
						break;
					}
				}
			}
		}
		return flag;
	}

	public bool CheckClassExactly()
	{
		bool flag = Class == "";
		if (!flag)
		{
			int nameKey = (int)GameManager.Instance.CurrentHero.Hero.NameKey;
			flag = Class.Length > nameKey && Class[nameKey] == '1';
		}
		return flag;
	}

	public void CheckAvailable()
	{
		if (applied && subscribe_condition)
		{
			if (!achieve)
			{
				Variable resource = GameContext.GetResource(access_parameter);
				resource.OnChange = (Action)Delegate.Remove(resource.OnChange, new Action(CheckAvailable));
			}
			else
			{
				VariableLong timeSession = Statistic.TimeSession;
				timeSession.OnChange = (Action)Delegate.Remove(timeSession.OnChange, new Action(CheckAvailable));
			}
			subscribe_condition = false;
		}
		else if (condition_access != null)
		{
			bool flag;
			if (achieve)
			{
				if (achieveCondition == null)
				{
					achieveCondition = GameManager.Instance.AchievManager.AllAchievs.Find((Achievement x) => x.Key.ToString() == access_parameter && x.Level == access_argumet.ToInt());
				}
				flag = achieveCondition.Unlocked;
			}
			else
			{
				flag = condition_access.condition(access_parameter, access_argumet, ref target);
			}
			if (!achieve)
			{
				if (!flag)
				{
					if (!subscribe_condition)
					{
						Variable resource2 = GameContext.GetResource(access_parameter);
						resource2.OnChange = (Action)Delegate.Combine(resource2.OnChange, new Action(CheckAvailable));
						subscribe_condition = true;
					}
					Available = false;
				}
				else
				{
					if (subscribe_condition)
					{
						Variable resource3 = GameContext.GetResource(access_parameter);
						resource3.OnChange = (Action)Delegate.Remove(resource3.OnChange, new Action(CheckAvailable));
						subscribe_condition = false;
					}
					Available = true;
				}
			}
			else if (!flag)
			{
				if (!subscribe_condition)
				{
					VariableLong timeSession2 = Statistic.TimeSession;
					timeSession2.OnChange = (Action)Delegate.Combine(timeSession2.OnChange, new Action(CheckAvailable));
					subscribe_condition = true;
				}
				Available = false;
			}
			else
			{
				if (subscribe_condition)
				{
					VariableLong timeSession3 = Statistic.TimeSession;
					timeSession3.OnChange = (Action)Delegate.Remove(timeSession3.OnChange, new Action(CheckAvailable));
					subscribe_condition = false;
				}
				Available = true;
			}
		}
		else
		{
			Available = true;
		}
	}

	private void Building(UpgradeFormat data)
	{
		if (data.V != null && data.V != "")
		{
			target = GameContext.GetResource(data.V);
			if (target == null)
			{
				Debug.LogError("not found V in " + Name + " " + data.V);
			}
		}
		if (data.W != null && data.W != "")
		{
			parameter = GameContext.GetResource(data.W);
			effect_type = EffectUpdateType.Parameterized;
			if (parameter == null)
			{
				Debug.LogError("not found W in " + Name);
			}
		}
		Data = data;
		if (Description == null)
		{
			Description = "";
		}
	}

	public void Buy(bool manual = true)
	{
		Buy(GameManager.Instance.ManaChange);
		GameManager.Instance.UpgradeManager.ActiveUpgradeList.Add(this);
		if (GameManager.Instance.UpgradeManager.on_buy_upgrade != null)
		{
			GameManager.Instance.UpgradeManager.on_buy_upgrade(this);
		}
		if (manual)
		{
			GameManager.Instance.UpgradeManager.AvailableUpgradeList.Remove(this);
			GameManager.Instance.UpgradeManager.UpdateScroll();
		}
	}

	protected override void OnBuy()
	{
		Statistic.BoughtUpgrades.Change(1);
		Apply();
	}

	public string GetDescription()
	{
		string text = TranslationManager.Instance.Process(Description);
		string[] array = effect.preview(add, mult, parameter, null, asnumber: true).Split('@');
		BigNumber bigNumber = new BigNumber(array[0]);
		BigNumber bigNumber2 = (new BigNumber(array[1]) - 1.0) * 100.0;
		if (Settings.ColoredTips && parameter != null)
		{
			text = text.Replace("#%", "<color=#e2b018>" + (bigNumber * 100.0).Abs().ToReadableString() + "%</color>");
			text = text.Replace("#a&", "<color=#e2b018>" + bigNumber.ToReadableString() + "%</color>");
			text = text.Replace("#a", "<color=#e2b018>" + bigNumber.Abs().ToReadableString() + "</color>");
			text = text.Replace("#m%", "<color=#e2b018>" + (100.0 - bigNumber2).ToReadableString() + "%</color>");
			text = text.Replace("#m", "<color=#e2b018>" + bigNumber2.ToReadableString() + "%</color>");
			if (parameter != null)
			{
				text = text.Replace("#p", "<color=#e2b018>" + parameter.Value.ToReadableString("F0") + "</color>");
			}
		}
		else
		{
			text = text.Replace("#%", (bigNumber * 100.0).Abs().ToReadableString() + "%");
			text = text.Replace("#a&", bigNumber.ToReadableString() + "%");
			text = text.Replace("#a", bigNumber.Abs().ToReadableString());
			text = text.Replace("#m%", (100.0 - bigNumber2).ToReadableString() + "%");
			text = text.Replace("#m", bigNumber2.ToReadableString() + "%");
			if (parameter != null)
			{
				text = text.Replace("#p", parameter.Value.ToReadableString("F0"));
			}
		}
		return text;
	}

	public void Load()
	{
		Apply();
		manager.UpgradesChecksList.Remove(this);
		manager.AvailableUpgradeList.Remove(this);
		manager.ActiveUpgradeList.Add(this);
	}
}
