using System;
using System.Globalization;
using UnityEngine;

public class EffectTemperTheSteelPeriodic : EffectInstant
{
	public float Frequency;

	public float timer;

	private Spell sp;

	private Spells key;

	public EffectTemperTheSteelPeriodic(float frequency, float t, float A, float M, Variable W, float diminish, Spells key)
	{
		Frequency = frequency;
		time = t;
		a = A;
		m = M;
		w = W;
		diminishing = diminish;
		pow_diminishing = 1f;
		this.key = key;
	}

	public override void Apply()
	{
		if (sp == null)
		{
			sp = GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey == Spells.FuriousStrike);
		}
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		scrolls.OnCast = (Action<Spell>)Delegate.Combine(scrolls.OnCast, new Action<Spell>(onCast));
	}

	public override void Delete()
	{
		ScrollPanel scrolls = GameManager.Instance.Scrolls;
		scrolls.OnCast = (Action<Spell>)Delegate.Remove(scrolls.OnCast, new Action<Spell>(onCast));
	}

	private void onCast(Spell sp)
	{
		if (sp.Type == SpellTypeGroup.Evocation && sp.NameKey != Spells.TemperTheSteel && sp.NameKey != Spells.ETTS)
		{
			GameManager.Instance.SpellBook.GetSpell(key).UseThisRun.Change(1.0);
		}
	}

	public override void Update()
	{
		if (Frequency == 0f)
		{
			return;
		}
		timer += Time.deltaTime;
		if (timer > 1f / Frequency)
		{
			int num = (int)(timer * Frequency);
			double dBuild = sp.FullBuild * (double)getProgress();
			for (int i = 0; i < num; i++)
			{
				sp.AddProgressBuild(dBuild, shards: false);
			}
			timer -= 1f / Frequency;
		}
	}

	private float getProgress()
	{
		BigNumber value = GetEfficiency().Value;
		return 0.01f + (a * w.Value).ToFloat() + 0.015f * value.ToFloat();
	}

	public override string Preview(string key)
	{
		return (getProgress() * 100f).ToString("F2", CultureInfo.InvariantCulture) + "%";
	}
}
