using System;
using System.Collections.Generic;

public class EffectAddTemporaryBuildings : EffectInstant
{
	public BuildingVisual target;

	public int b;

	public BigNumber k;

	private bool active;

	public int levels;

	public EffectAddTemporaryBuildings(int building, int _base)
	{
		target = new List<BuildingVisual>(GameManager.Instance.Buildings).Find((BuildingVisual x) => x.building.Tier == building);
		b = _base;
		a = 0.0;
		m = 1.0;
		k = 1.0;
		w = null;
		diminishing = 1.6f;
	}

	public EffectAddTemporaryBuildings(int building, int _base, BigNumber _a, BigNumber _m, BigNumber _k, Variable _w = null)
	{
		target = new List<BuildingVisual>(GameManager.Instance.Buildings).Find((BuildingVisual x) => x.building.Tier == building);
		b = _base;
		a = _a;
		m = _m;
		k = _k;
		w = _w;
		diminishing = 1.6f;
	}

	public override void Apply()
	{
		recalculate();
		active = true;
		target.building.TemporalyLevel.Change(levels);
	}

	public override void Delete()
	{
		target.building.TemporalyLevel.Change(-levels);
		active = false;
	}

	private void recalculate()
	{
		levels = 0;
		if (w != null)
		{
			levels = Convert.ToInt32(((a + m * (w.Value + 1.0).Log10()) * k).ToDouble());
		}
		else
		{
			levels = Convert.ToInt32((a * m * k).ToDouble());
		}
		if (levels < b)
		{
			levels = b;
		}
		if (efficiency != null)
		{
			levels = Convert.ToInt32(ApplyEfficiency(levels).ToDouble());
		}
	}

	public override string Preview(string key = "")
	{
		if (!active)
		{
			recalculate();
		}
		if (key == "t")
		{
			return "amount of " + target.building.Name;
		}
		return levels.ToString();
	}
}
