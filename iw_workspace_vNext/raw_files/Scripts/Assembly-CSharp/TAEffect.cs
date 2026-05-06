using System.Collections.Generic;
using UnityEngine;

public class TAEffect : SimpleEffect
{
	private bool prevIsActive;

	public TAEffect()
	{
		effect = GameContext.GetEffect(EffectNames.Linear.ToString());
		target = (parameter = null);
		add = 0.0;
		mult = 1.0;
		diminishing = 1f;
	}

	public override void Apply()
	{
		if (applied)
		{
			return;
		}
		if (parameter != null)
		{
			if (prev_w == null)
			{
				prev_w = new VariableBignumber(parameter.Value);
			}
			else
			{
				prev_w.SetValue(parameter.Value);
			}
		}
		RecalculateEff();
		prevIsActive = isActiveInca();
		if (!prevIsActive)
		{
			applied_add = 0.0;
			applied_mult = 1.0;
			prev_w.SetValue(0.0);
		}
		else
		{
			applied_add = add;
			applied_mult = mult;
			if (effect == null)
			{
				Debug.Log("effect null");
			}
			effect.apply(target, applied_add, applied_mult, parameter, prev_e);
		}
		applied = true;
	}

	public override void Update()
	{
		if (applied)
		{
			Delete();
			Apply();
		}
	}

	private bool isActiveInca()
	{
		bool result = false;
		List<Scroll> scrolls = GameManager.Instance.Scrolls.Scrolls;
		Scroll scroll = null;
		for (int i = 0; i < scrolls.Count; i++)
		{
			scroll = scrolls[i];
			if (scroll.spell != null && scroll.active && scroll.spell.Type == SpellTypeGroup.Incantation)
			{
				result = true;
				break;
			}
		}
		return result;
	}
}
