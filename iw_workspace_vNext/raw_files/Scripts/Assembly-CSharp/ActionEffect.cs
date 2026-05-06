using System;
using UnityEngine;

public class ActionEffect : EffectDiminishing
{
	private Action onApply;

	private Action onDelete;

	private Action<BigNumber> onUpdate;

	private Func<string, string> onPreview;

	private float rate;

	private float timer;

	private bool applyed;

	private bool instant;

	public ActionEffect(Action apply, Action delete, Action<BigNumber> update, float period, bool isInstant = false, Func<string, string> preview = null)
	{
		onApply = apply;
		onDelete = delete;
		onUpdate = update;
		onPreview = preview;
		rate = period;
		instant = isInstant;
	}

	public override void Apply()
	{
		if (!instant)
		{
			if (applyed)
			{
				return;
			}
			applyed = true;
		}
		if (onApply != null)
		{
			onApply();
		}
	}

	public override void Update()
	{
		if (onUpdate != null && applyed && rate != 0f)
		{
			if (timer >= rate)
			{
				timer -= rate;
				onUpdate(GetEfficiency().Value);
			}
			timer += Time.deltaTime;
		}
	}

	public override void Delete()
	{
		if (applyed)
		{
			applyed = false;
			if (onDelete != null)
			{
				onDelete();
			}
		}
	}

	public override string Preview(string key = "")
	{
		if (onPreview != null)
		{
			return onPreview(key);
		}
		return string.Empty;
	}
}
