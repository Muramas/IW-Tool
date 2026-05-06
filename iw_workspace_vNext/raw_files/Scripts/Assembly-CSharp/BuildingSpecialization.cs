using System;
using System.Globalization;
using UnityEngine;

public class BuildingSpecialization : BuildingSpecBase
{
	public SimpleEffect Effect;

	private float startMult;

	private float bonus;

	public BuildingSpecialization(BuildingSpecializationFormat format, Variable efficiency)
		: base(format, efficiency)
	{
		startMult = float.Parse(format.Step, CultureInfo.InvariantCulture);
		bonus = new BigNumber(format.Bonus).ToFloat();
		Effect = new SimpleEffect(GameContext.GetResource(format.Target), bonus, 1f / startMult, EffectNames.PowW);
	}

	public override void SelectBuilding(Building building, bool reset = true)
	{
		base.SelectBuilding(building, reset);
		Effect.parameter = building.TotalLevel;
		UpdateEffect();
		Variable parameter = Effect.parameter;
		parameter.OnChange = (Action)Delegate.Combine(parameter.OnChange, new Action(Effect.Update));
		VariableInt level = Level;
		level.OnChange = (Action)Delegate.Combine(level.OnChange, new Action(UpdateEffect));
		Variable variable = efficiency;
		variable.OnChange = (Action)Delegate.Combine(variable.OnChange, new Action(UpdateEffect));
	}

	public override void ResetBuilding()
	{
		if (base.building != null)
		{
			TurnOff();
			base.ResetBuilding();
		}
	}

	public override void TurnOff()
	{
		if (base.building != null)
		{
			if (Effect.parameter != null)
			{
				Variable parameter = Effect.parameter;
				parameter.OnChange = (Action)Delegate.Remove(parameter.OnChange, new Action(Effect.Update));
			}
			VariableInt level = Level;
			level.OnChange = (Action)Delegate.Remove(level.OnChange, new Action(UpdateEffect));
			Variable variable = efficiency;
			variable.OnChange = (Action)Delegate.Remove(variable.OnChange, new Action(UpdateEffect));
			Effect.Delete();
		}
	}

	public void UpdateEffect()
	{
		Effect.Delete();
		Effect.add = 1.0 + (bonus - 1f) * efficiency.Value;
		Effect.mult = 1f / GetStep();
		Effect.Apply();
	}

	private float GetStep()
	{
		float num = Mathf.Pow(Level.ValueInt, 0.8f);
		if (num < 1f)
		{
			num = 1f;
		}
		float num2 = startMult * Mathf.Pow(BonusPerLevel, num - 1f);
		if (num2 < 1f)
		{
			num2 = 1f;
		}
		return num2;
	}

	public override int GetNextGoal()
	{
		if (base.building == null)
		{
			return 0;
		}
		int valueInt = base.building.TotalLevel.ValueInt;
		float step = GetStep();
		return Mathf.CeilToInt(step * (float)(Mathf.FloorToInt((float)valueInt / step) + 1));
	}

	public override string GetDescription()
	{
		return TranslationManager.Instance.Process(Description).Replace("#a", getFormated(getValue(), isPercent: true)).Replace("#m", getFormated(GetStep()));
	}

	public override string GetFullDescription()
	{
		return GetDescription() + " " + "Total".Translate() + ": " + formate(Effect.Preview(""));
	}

	private BigNumber getValue()
	{
		return 1.0 + (bonus - 1f) * efficiency.Value;
	}
}
