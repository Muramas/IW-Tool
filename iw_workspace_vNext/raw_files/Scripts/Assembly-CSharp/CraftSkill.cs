using UnityEngine;

public class CraftSkill
{
	public int level;

	public BigNumber totalExp;

	public BigNumber exp2lvl;

	public BigNumber exp;

	public float bonus;

	private int baseExp;

	private float gr;

	public CraftSkill(float bonus, int baseExp, float gr)
	{
		this.bonus = bonus;
		level = 0;
		totalExp = (exp = (exp2lvl = 0.0));
		this.baseExp = baseExp;
		this.gr = gr;
		Recalculate();
	}

	public void Recalculate()
	{
		int num = (level = (int)(1.0 - totalExp * (1f - gr) / baseExp).Log_a(gr));
		exp2lvl = (float)baseExp * Mathf.Pow(gr, num);
		exp = totalExp - (float)baseExp * (1f - Mathf.Pow(gr, num)) / (1f - gr);
	}
}
