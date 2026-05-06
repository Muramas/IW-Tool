using UnityEngine;

public class AdditiveEffect : EffectDiminishing
{
	public Effect effect;

	public Variable target;

	public BigNumber add;

	public int level;

	protected BigNumber applied_add;

	protected bool applyed;

	public AdditiveEffect(Variable t, BigNumber a)
	{
		effect = GameContext.GetEffect(EffectNames.Linear.ToString());
		target = t;
		add = a;
		diminishing = 1f;
	}

	public void ChangeLevel(int lvl)
	{
		level = lvl;
		Update();
	}

	public override void Apply()
	{
		if (applyed || level == 0)
		{
			return;
		}
		if (target == null)
		{
			Debug.Log("target is null");
			Debug.Log(add);
			return;
		}
		applied_add = add * level;
		if (effect == null)
		{
			Debug.Log("effect null");
		}
		effect.apply(target, applied_add, 1.0, null);
		applyed = true;
	}

	public override void Delete()
	{
		if (applyed)
		{
			effect.delete(target, applied_add, 1.0, null);
			applyed = false;
		}
	}

	public override void Update()
	{
		if (applyed)
		{
			Delete();
			Apply();
		}
	}

	public override string Preview(string key = "t")
	{
		if (key == string.Empty)
		{
			return add.ToReadableString("F0");
		}
		return (add * level).ToReadableString();
	}
}
