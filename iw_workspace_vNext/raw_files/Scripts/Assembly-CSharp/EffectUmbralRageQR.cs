using System;

public class EffectUmbralRageQR : IEffect
{
	private VariableLong parameter;

	private SimpleEffect effect;

	private ScrollPanel scrolls;

	private bool applyed;

	private BigNumber penalty = 1.0;

	public EffectUmbralRageQR(Effect _effect, Variable target, BigNumber a, BigNumber m, float diminishing = 1f)
	{
		parameter = new VariableLong(0uL);
		effect = new SimpleEffect();
		effect.effect = _effect;
		effect.target = target;
		effect.add = a;
		effect.mult = m;
		effect.parameter = parameter;
		effect.diminishing = diminishing;
		scrolls = GameManager.Instance.Scrolls;
	}

	public void Apply()
	{
		if (!applyed)
		{
			effect.Apply();
			ScrollPanel scrollPanel = scrolls;
			scrollPanel.OnCast = (Action<Spell>)Delegate.Combine(scrollPanel.OnCast, new Action<Spell>(OnCast));
			applyed = true;
		}
	}

	public void SetEfficiency(Variable eff = null)
	{
		effect.SetEfficiency(eff);
	}

	public void SetGilding(Variable eff = null)
	{
		effect.SetGilding(eff);
	}

	public void Delete()
	{
		if (applyed)
		{
			effect.Delete();
			ScrollPanel scrollPanel = scrolls;
			scrollPanel.OnCast = (Action<Spell>)Delegate.Remove(scrollPanel.OnCast, new Action<Spell>(OnCast));
			parameter.SetValue(1uL);
			scrolls.SpellChargingCostReduction.Change(0.0, 1.0 / penalty);
			scrolls.SpellShardsCostReduction.Change(0.0, 1.0 / penalty);
			penalty = 1.0;
			applyed = false;
		}
	}

	public void Update()
	{
		if (applyed)
		{
			effect.Delete();
			effect.Apply();
		}
	}

	public string Preview(string key = "")
	{
		string result = "";
		if (key == "")
		{
			result = effect.Preview("");
		}
		else if (key == "t")
		{
			result = ((penalty - 1.0) * 100.0).ToReadableString() + "%";
		}
		return result;
	}

	private void OnCast(Spell spell)
	{
		if (spell.Type == SpellTypeGroup.Evocation)
		{
			parameter.Change(1);
			penalty *= (BigNumber)1.25;
			scrolls.SpellChargingCostReduction.Change(0.0, 1.25);
			scrolls.SpellShardsCostReduction.Change(0.0, 1.25);
			Scroll scroll = scrolls.Scrolls.Find((Scroll x) => x.spell != null && x.spell.NameKey == Spells.UmbralRage2);
			if (scroll.TimeOfAction < 1f)
			{
				scroll.TimeOfAction = 0f;
			}
			else
			{
				scroll.TimeOfAction -= 1f;
			}
		}
	}
}
