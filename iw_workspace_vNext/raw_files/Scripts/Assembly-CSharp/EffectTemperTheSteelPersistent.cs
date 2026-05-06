using System;
using System.Collections.Generic;
using System.Globalization;

public class EffectTemperTheSteelPersistent : IEffect
{
	private bool applyed;

	private Variable w;

	public EffectTemperTheSteelPersistent(Spells key)
	{
		w = GameManager.Instance.SpellBook.GetSpell(key).UseThisRun;
	}

	public void Apply()
	{
		if (!applyed)
		{
			applyed = true;
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, new Action<Spell>(OnCast));
		}
	}

	public void SetEfficiency(Variable eff = null)
	{
	}

	public void SetGilding(Variable eff = null)
	{
	}

	public void Update()
	{
	}

	public void Delete()
	{
		if (applyed)
		{
			applyed = false;
			ScrollPanel scrolls = GameManager.Instance.Scrolls;
			scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, new Action<Spell>(OnCast));
		}
	}

	private void OnCast(Spell s)
	{
		if (s.Type != SpellTypeGroup.Evocation || s.ShardsBuilding)
		{
			return;
		}
		List<Scroll> list = GameManager.Instance.Scrolls.Scrolls.FindAll((Scroll x) => x.spell != null && x.spell.Type == SpellTypeGroup.Evocation && !x.spell.ShardsBuilding);
		int count = list.Count;
		if (count > 0)
		{
			float progress = getProgress();
			progress /= (float)count;
			for (int num = 0; num < count; num++)
			{
				list[num].AddProgressBuild((double)progress * list[num].spell.FullBuild, text: false, shards: false);
			}
		}
	}

	private float getProgress()
	{
		return 0.002f * (w.Value.Pow(0.30000001192092896).ToFloat() + 50f);
	}

	public string Preview(string key = "")
	{
		return (getProgress() * 100f).ToString("F2", CultureInfo.InvariantCulture) + "%";
	}
}
