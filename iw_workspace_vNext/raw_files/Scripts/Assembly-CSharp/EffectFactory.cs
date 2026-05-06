using UnityEngine;

public static class EffectFactory
{
	public delegate void effect_action(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null);

	public delegate string effect_preview(BigNumber a, BigNumber m, Variable w, Variable e = null, bool asnumber = false);

	public static Effect Create(effect_action apply, effect_action delete, effect_preview preview = null)
	{
		return new Effect
		{
			preview = preview,
			apply = apply,
			delete = delete
		};
	}

	public static void Linear(Variable v, BigNumber a, BigNumber m, Variable w = null, Variable e = null)
	{
		if (e != null && e.Value != 1.0)
		{
			a *= e.Value;
			if (m != 1.0 || m != 0.0)
			{
				if (w == null)
				{
					if (m >= 1.0)
					{
						m = 1.0 + (m - 1.0) * e.Value;
					}
					else
					{
						m /= e.Value;
					}
				}
				else
				{
					m *= e.Value;
				}
			}
		}
		if (w == null)
		{
			v.Change(a, m);
		}
		else
		{
			v.Change(a * w.Value, 1.0 + m * w.Value);
		}
	}

	public static void LinearDelete(Variable v, BigNumber a, BigNumber m, Variable w = null, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
			if (m != 1.0 || m != 0.0)
			{
				if (w == null)
				{
					if (m >= 1.0)
					{
						m = 1.0 + (m - 1.0) * e.Value;
					}
					else
					{
						m /= e.Value;
					}
				}
				else
				{
					m *= e.Value;
				}
			}
		}
		if (w == null)
		{
			if (m == 0.0)
			{
				Debug.Log("/0 v= " + v.ToString() + v.Value.ToReadableString());
			}
			else
			{
				v.Change(-a, 1.0 / m);
			}
		}
		else if (1.0 + m * w.Value == 0.0)
		{
			Debug.Log("ayayaayaya");
		}
		else
		{
			v.Change(-a * w.Value, 1.0 / (1.0 + m * w.Value));
		}
	}

	public static string LinearPreview(BigNumber a, BigNumber m, Variable w = null, Variable e = null, bool asnumber = false)
	{
		if (e != null)
		{
			a *= e.Value;
			if (m != 1.0 || m != 0.0)
			{
				if (w == null)
				{
					if (m >= 1.0)
					{
						m = 1.0 + (m - 1.0) * e.Value;
					}
					else
					{
						m /= e.Value;
					}
				}
				else
				{
					m *= e.Value;
				}
			}
		}
		string text = "";
		if (asnumber)
		{
			text = ((w != null) ? ((a * w.Value).ToString() + "@" + (1.0 + m * w.Value).ToString()) : (a.ToString() + "@" + m.ToString()));
		}
		else
		{
			if (a != 0.0)
			{
				text = ((w != null) ? (text + (a * w.Value).ToReadableString("0.##")) : (text + a.ToReadableString("0.##")));
			}
			if ((m != 1.0 && w == null) || (m != 0.0 && w != null))
			{
				if (w == null)
				{
					text += ((m - 1.0) * 100.0).ToReadableString();
					text += "%";
				}
				else
				{
					text += (m * w.Value * 100.0).ToReadableString();
					text += "%";
				}
			}
			if (a == 0.0 && m == 1.0 && w == null)
			{
				text = text + 0 + "%";
			}
		}
		return text;
	}

	public static void Power(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		BigNumber bigNumber = 1.0;
		if (e != null)
		{
			bigNumber *= e.Value;
		}
		v.Change(0.0, 1.0 + bigNumber * (a * (w.Value + 1.0)).Pow(m.ToDouble()));
	}

	public static void PowerDelete(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		BigNumber bigNumber = 1.0;
		if (e != null)
		{
			bigNumber *= e.Value;
		}
		v.Change(0.0, 1.0 / (1.0 + bigNumber * (a * (w.Value + 1.0)).Pow(m.ToDouble())));
	}

	public static string PowerPreview(BigNumber a, BigNumber m, Variable w, Variable e = null, bool asnumber = false)
	{
		BigNumber bigNumber = 1.0;
		if (e != null)
		{
			bigNumber *= e.Value;
		}
		if (!asnumber)
		{
			return (100.0 * bigNumber * (a * (w.Value + 1.0)).Pow(m.ToDouble())).ToReadableString() + "%";
		}
		return "0@" + (1.0 + bigNumber * (a * (w.Value + 1.0)).Pow(m.ToDouble())).ToString();
	}

	public static void PowerA(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		v.Change(0.0, 1.0 + a * (w.Value + 1.0).Pow(m.ToDouble()));
	}

	public static void PowerADelete(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		v.Change(0.0, 1.0 / (1.0 + a * (w.Value + 1.0).Pow(m.ToDouble())));
	}

	public static string PowerAPreview(BigNumber a, BigNumber m, Variable w, Variable e = null, bool asnumber = false)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		if (!asnumber)
		{
			return (100.0 * a * (w.Value + 1.0).Pow(m.ToDouble())).ToReadableString() + "%";
		}
		return "0@" + (1.0 + a * (w.Value + 1.0).Pow(m.ToDouble())).ToString();
	}

	public static void AdditivePower(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		v.Change(a * (w.Value + 1.0).Pow(m.ToDouble()), 1.0);
	}

	public static void AdditivePowerDelete(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		v.Change(-a * (w.Value + 1.0).Pow(m.ToDouble()), 1.0);
	}

	public static string AdditivePowerPreview(BigNumber a, BigNumber m, Variable w, Variable e = null, bool asnumber = false)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		if (!asnumber)
		{
			return (a * (w.Value + 1.0).Pow(m.ToDouble())).ToReadableString();
		}
		return (a * (w.Value + 1.0).Pow(m.ToDouble())).ToString() + "@1";
	}

	public static void Log10(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		v.Change(0.0, 1.0 + a * new BigNumber((w.Value + 1.0).Log10()).Pow(m.ToDouble()));
	}

	public static void Log10Delete(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		v.Change(0.0, 1.0 / (1.0 + a * new BigNumber((w.Value + 1.0).Log10()).Pow(m.ToDouble())));
	}

	public static string Log10Preview(BigNumber a, BigNumber m, Variable w, Variable e = null, bool asnumber = false)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		if (!asnumber)
		{
			return (100.0 * a * new BigNumber((w.Value + 1.0).Log10()).Pow(m.ToDouble())).Abs().ToReadableString() + "%";
		}
		return "0@" + (1.0 + a * new BigNumber((w.Value + 1.0).Log10()).Pow(m.ToDouble())).ToString();
	}

	public static void Ln(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		v.Change(0.0, 1.0 + a * new BigNumber((w.Value + 1.0).Ln()).Pow(m.ToDouble()));
	}

	public static void LnDelete(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		v.Change(0.0, 1.0 / (1.0 + a * new BigNumber((w.Value + 1.0).Ln()).Pow(m.ToDouble())));
	}

	public static string LnPreview(BigNumber a, BigNumber m, Variable w, Variable e = null, bool asnumber = false)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		if (!asnumber)
		{
			return (100.0 * a * new BigNumber((w.Value + 1.0).Ln()).Pow(m.ToDouble())).ToReadableString() + "%";
		}
		return "0@" + (1.0 + a * new BigNumber((w.Value + 1.0).Ln()).Pow(m.ToDouble())).ToString();
	}

	public static void LogA(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		v.Change(0.0, 1.0 + (a * w.Value + 1.0).Log_a(m.ToDouble()));
	}

	public static void LogADelete(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		v.Change(0.0, 1.0 / (1.0 + (a * w.Value + 1.0).Log_a(m.ToDouble())));
	}

	public static string LogAPreview(BigNumber a, BigNumber m, Variable w, Variable e = null, bool asnumber = false)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		if (!asnumber)
		{
			return new BigNumber(100.0 * (a * w.Value + 1.0).Log_a(m.ToDouble())).ToReadableString() + "%";
		}
		return "0@" + (1.0 + (a * w.Value + 1.0).Log_a(m.ToDouble()));
	}

	public static void PowerIntW(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		int num = 0;
		num = ((!(w is VariableInt)) ? w.Value.ToInt() : (w as VariableInt).ValueInt);
		v.Change(0.0, a * (m + 1.0).Pow(num));
	}

	public static void PowerIntWDelete(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		int num = 0;
		num = ((!(w is VariableInt)) ? w.Value.ToInt() : (w as VariableInt).ValueInt);
		v.Change(0.0, 1.0 / (a * (m + 1.0).Pow(num)));
	}

	public static string PowerIntWPreview(BigNumber a, BigNumber m, Variable w, Variable e = null, bool asnumber = false)
	{
		if (e != null)
		{
			a *= e.Value;
		}
		int num = 0;
		num = ((!(w is VariableInt)) ? w.Value.ToInt() : (w as VariableInt).ValueInt);
		if (!asnumber)
		{
			return (100.0 * (a * (m + 1.0).Pow(num) - 1.0)).ToReadableString() + "%";
		}
		return "0@" + (a * (m + 1.0).Pow(num)).ToString();
	}

	public static void PowerW(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		int x = Mathf.FloorToInt((w.Value * m).ToFloat());
		BigNumber multiplier = a.Pow(x);
		if (e != null)
		{
			multiplier *= e.Value;
		}
		v.Change(0.0, multiplier);
	}

	public static void PowerWDelete(Variable v, BigNumber a, BigNumber m, Variable w, Variable e = null)
	{
		int x = Mathf.FloorToInt((w.Value * m).ToFloat());
		BigNumber bigNumber = a.Pow(x);
		if (e != null)
		{
			bigNumber *= e.Value;
		}
		v.Change(0.0, 1.0 / bigNumber);
	}

	public static string PowerWPreview(BigNumber a, BigNumber m, Variable w, Variable e = null, bool asnumber = false)
	{
		int x = Mathf.FloorToInt((w.Value * m).ToFloat());
		BigNumber bigNumber = a.Pow(x);
		if (e != null)
		{
			bigNumber *= e.Value;
		}
		if (!asnumber)
		{
			return (100.0 * (bigNumber - 1.0)).ToReadableString() + "%";
		}
		return "0@" + bigNumber.ToReadableString();
	}
}
