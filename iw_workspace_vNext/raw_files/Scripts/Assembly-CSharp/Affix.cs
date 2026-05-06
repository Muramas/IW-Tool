using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

public class Affix : EffectFormat
{
	public class SaveData
	{
		public string id;

		public float v;
	}

	private SimpleEffect effect;

	private string target;

	private float range;

	private string modelId;

	private bool isInt;

	public Affix(float range, AffixData model)
	{
		target = model.Target;
		this.range = range;
		float num = model.Min + range * (model.Max - model.Min);
		float num2 = 0f;
		float num3 = 1f;
		if (!string.IsNullOrEmpty(model.Int))
		{
			num2 = Mathf.RoundToInt(num);
		}
		else
		{
			num3 = num;
		}
		effect = new SimpleEffect(GameContext.GetResource(target), num2, num3, EffectNames.Linear, model.Diminish);
		Description = model.Description;
		modelId = model.Id;
		isInt = model.IsInt();
	}

	public Affix(MythicData data)
	{
		target = data.ImplicitTarget;
		BigNumber a = 0.0;
		BigNumber m = 1.0;
		if (!data.IsAdditive())
		{
			m = data.ImplicitValue;
		}
		else
		{
			a = data.ImplicitValue;
		}
		effect = new SimpleEffect(GameContext.GetResource(target), a, m, EffectNames.Linear, float.Parse(data.ImplicitDiminish, CultureInfo.InvariantCulture));
		Description = data.ImplicitDescr;
		isInt = data.IsAdditive();
	}

	public Affix(Affix original)
	{
		target = original.GetTarget();
		effect = new SimpleEffect(original.effect.target, original.effect.add, original.effect.mult, EffectNames.Linear, original.effect.pow_diminishing);
		Description = original.Description;
		range = original.range;
		modelId = original.modelId;
		isInt = original.isInt;
	}

	public string GetModelId()
	{
		return modelId;
	}

	public void Apply()
	{
		effect.Apply();
	}

	public void Delete()
	{
		effect.Delete();
	}

	public void Update()
	{
		effect.Update();
	}

	public void SetEfficiency(Variable eff)
	{
		effect.SetEfficiency(eff);
	}

	public void SetGilding(Variable eff)
	{
		effect.SetGilding(eff);
	}

	public Variable GetGilding()
	{
		return effect.gilding;
	}

	public Variable GetEfficiency()
	{
		return effect.efficiency;
	}

	public string GetTarget()
	{
		return target;
	}

	public float GetRange()
	{
		return range;
	}

	protected override string GetPreview(int id, string key)
	{
		if (string.IsNullOrEmpty(key) && isInt)
		{
			key = "int";
		}
		return effect.Preview(key);
	}

	protected override string ReplaceMatch(Match m)
	{
		string text = base.ReplaceMatch(m);
		if (effect.GetEfficiency().Value > 1.0)
		{
			text = text + " (+" + effect.Preview("e") + ")";
		}
		return text;
	}

	public SaveData Save()
	{
		return new SaveData
		{
			id = modelId,
			v = range
		};
	}
}
