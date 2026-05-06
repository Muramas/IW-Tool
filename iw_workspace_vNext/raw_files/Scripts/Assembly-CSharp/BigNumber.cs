using System;
using System.Globalization;
using UnityEngine;

[Serializable]
public struct BigNumber
{
	public double Mantissa;

	public int Exponent;

	private static string[] orderNames = new string[25]
	{
		"", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc",
		"No", "Dc", "UDc", "DDc", "TDc", "QaDc", "QiDc", "SxDc", "SpDc", "OcDc",
		"NoDc", "Vg", "UVg", "DVg", "TVg"
	};

	public BigNumber(double Mantissa, int Exponent)
	{
		this = default(BigNumber);
		if (double.IsNaN(Mantissa))
		{
			Mantissa = 1.0;
		}
		this.Mantissa = Mantissa;
		this.Exponent = Exponent;
		Check();
	}

	public BigNumber(double Value)
	{
		this = default(BigNumber);
		if (double.IsNaN(Mantissa))
		{
			Mantissa = 1.0;
		}
		Mantissa = Value;
		Check();
	}

	public BigNumber(string value)
	{
		this = default(BigNumber);
		if (string.IsNullOrEmpty(value))
		{
			return;
		}
		if (value.Contains("e"))
		{
			string[] array = value.Split(new string[1] { "e" }, StringSplitOptions.RemoveEmptyEntries);
			try
			{
				Mantissa = double.Parse(array[0], CultureInfo.InvariantCulture);
				if (double.IsNaN(Mantissa))
				{
					Mantissa = 1.0;
				}
			}
			catch
			{
				Debug.LogError("Parse Error " + value);
				Mantissa = 1.0;
			}
			if (array[1] != null)
			{
				Exponent = int.Parse(array[1]);
			}
			else
			{
				Exponent = 0;
			}
		}
		else
		{
			Mantissa = double.Parse(value, CultureInfo.InvariantCulture);
		}
		Check();
	}

	public static implicit operator BigNumber(double Number)
	{
		return new BigNumber(Number);
	}

	public static implicit operator BigNumber(string String)
	{
		return new BigNumber(String);
	}

	public void Check()
	{
		if (Mantissa == 0.0)
		{
			Exponent = 0;
			return;
		}
		while (Math.Abs(Mantissa) < 1.0)
		{
			Mantissa *= 10.0;
			Exponent--;
		}
		while (Math.Abs(Mantissa) >= 10.0)
		{
			Mantissa /= 10.0;
			Exponent++;
		}
	}

	public BigNumber CheckAndReturn()
	{
		Check();
		return this;
	}

	public override string ToString()
	{
		return Mantissa.ToString(CultureInfo.InvariantCulture) + "e" + Exponent;
	}

	public string ToScientific(string format = "F2", bool negativeExp = false)
	{
		if (Exponent < 0 && !negativeExp && Exponent > -3)
		{
			int i = Exponent;
			double num = Mantissa;
			for (; i < 0; i++)
			{
				num /= 10.0;
			}
			return num.ToString("F2", CultureInfo.InvariantCulture);
		}
		if ((double)Exponent < 3.0 && Exponent >= 0)
		{
			return (Mantissa * Math.Pow(10.0, Exponent)).ToString(format, CultureInfo.InvariantCulture);
		}
		bool flag = false;
		flag = ((!negativeExp) ? (Exponent >= 1) : ((double)Math.Abs(Exponent) >= 1.0));
		if (!negativeExp && Exponent < 0)
		{
			Mantissa *= Math.Pow(10.0, Exponent);
			return Mantissa.ToString(format, CultureInfo.InvariantCulture) + (flag ? ("e" + Exponent) : "");
		}
		return Mantissa.ToString("F2", CultureInfo.InvariantCulture) + (flag ? ("e" + Exponent) : "");
	}

	public string ToFullScientific(string format = "F2")
	{
		if ((double)Math.Abs(Exponent) < 3.0 && Math.Abs(Exponent) >= 0)
		{
			return (Mantissa * Math.Pow(10.0, Exponent)).ToString(format, CultureInfo.InvariantCulture);
		}
		return Mantissa.ToString("F2", CultureInfo.InvariantCulture) + "e" + Exponent;
	}

	public string ToShortReadable(string format = "F2")
	{
		int i = Exponent;
		double num = Mantissa;
		if (Exponent < 0)
		{
			for (; i < 0; i++)
			{
				num /= 10.0;
			}
			return num.ToString("F2", CultureInfo.InvariantCulture);
		}
		int num2 = (int)Mathf.Floor(i / 3);
		if (orderNames.Length > num2)
		{
			for (i -= num2 * 3; i > 0; i--)
			{
				num *= 10.0;
			}
			return num.ToString(format, CultureInfo.InvariantCulture) + orderNames[num2];
		}
		return num.ToString("F2", CultureInfo.InvariantCulture) + "e" + Exponent;
	}

	public string ToReadableStringNoExp()
	{
		int num = Exponent;
		double num2 = Mantissa;
		while (num > 0)
		{
			num2 *= 10.0;
			num--;
		}
		return num2.ToString("F2", CultureInfo.InvariantCulture);
	}

	public static int Sign(BigNumber n)
	{
		return Math.Sign(n.Mantissa);
	}

	public static BigNumber AmountOfElementsGeometryProgression(BigNumber sum, float q, BigNumber b)
	{
		return (1.0 - sum * (1f - q) / b).Log_a(q);
	}

	public static BigNumber AmountOfElementsAriphmeticProgression(BigNumber sum, BigNumber d, BigNumber a)
	{
		BigNumber bigNumber = a - d / 2.0;
		return ((2.0 * d * sum + bigNumber * bigNumber).Sqrt() - bigNumber) / d;
	}

	public static float AmountOfElementsGeometryProgression(float sum, float q, float first)
	{
		return Mathf.Log(1f - sum * (1f - q) / first, q);
	}

	public static BigNumber Lerp(BigNumber start, BigNumber end, float step)
	{
		return start + (end - start) * step;
	}

	public static BigNumber operator *(BigNumber num1, BigNumber num2)
	{
		return new BigNumber(num1.Mantissa * num2.Mantissa, num1.Exponent + num2.Exponent);
	}

	public static BigNumber operator /(BigNumber num1, BigNumber num2)
	{
		return new BigNumber(num1.Mantissa / num2.Mantissa, num1.Exponent - num2.Exponent);
	}

	public static bool operator >(BigNumber num1, BigNumber num2)
	{
		if (num1.Mantissa == 0.0)
		{
			return 0.0 > num2.Mantissa;
		}
		if (num2.Mantissa == 0.0)
		{
			return num1.Mantissa > 0.0;
		}
		if (num1.Mantissa >= 0.0 && num2.Mantissa < 0.0)
		{
			return true;
		}
		if (num1.Mantissa <= 0.0 && num2.Mantissa > 0.0)
		{
			return false;
		}
		if (num1.Exponent > num2.Exponent)
		{
			return true;
		}
		if (num1.Exponent < num2.Exponent)
		{
			return false;
		}
		if (num1.Mantissa > num2.Mantissa)
		{
			return true;
		}
		return false;
	}

	public static bool operator <(BigNumber num1, BigNumber num2)
	{
		if (num1.Mantissa == 0.0)
		{
			return 0.0 < num2.Mantissa;
		}
		if (num2.Mantissa == 0.0)
		{
			return num1.Mantissa < 0.0;
		}
		if (num1.Mantissa >= 0.0 && num2.Mantissa < 0.0)
		{
			return false;
		}
		if (num1.Mantissa <= 0.0 && num2.Mantissa > 0.0)
		{
			return true;
		}
		if (num1.Exponent < num2.Exponent)
		{
			return true;
		}
		if (num1.Exponent > num2.Exponent)
		{
			return false;
		}
		if (num1.Mantissa < num2.Mantissa)
		{
			return true;
		}
		return false;
	}

	public static bool operator ==(BigNumber num1, BigNumber num2)
	{
		if (Math.Abs(num1.Mantissa - num2.Mantissa) < 1E-14 && num1.Exponent == num2.Exponent)
		{
			return true;
		}
		return false;
	}

	public static bool operator !=(BigNumber num1, BigNumber num2)
	{
		return !(num1 == num2);
	}

	public static bool operator >=(BigNumber num1, BigNumber num2)
	{
		if (num1 > num2 || num1 == num2)
		{
			return true;
		}
		return false;
	}

	public static bool operator <=(BigNumber num1, BigNumber num2)
	{
		if (num1 < num2 || num1 == num2)
		{
			return true;
		}
		return false;
	}

	public static BigNumber operator -(BigNumber num1)
	{
		return new BigNumber(0.0 - num1.Mantissa, num1.Exponent);
	}

	public static BigNumber operator +(BigNumber num1, BigNumber num2)
	{
		if (num1.Exponent == num2.Exponent && (Math.Sign(num1.Mantissa) < 0 || Math.Sign(num2.Mantissa) < 0) && Math.Abs(num1.Mantissa + num2.Mantissa) < 1E-13)
		{
			return 0.0;
		}
		int value = Math.Abs(num1.Exponent - num2.Exponent);
		if (Math.Abs(value) > 14)
		{
			if (num1.Exponent > num2.Exponent)
			{
				return num1;
			}
			return num2;
		}
		double num3;
		double mantissa;
		int exponent;
		if (num1.Exponent > num2.Exponent)
		{
			num3 = num2.Mantissa;
			value = num1.Exponent - num2.Exponent;
			mantissa = num1.Mantissa;
			exponent = num1.Exponent;
			while (value > 0)
			{
				num3 /= 10.0;
				value--;
			}
		}
		else if (num2.Exponent > num1.Exponent)
		{
			num3 = num1.Mantissa;
			value = num2.Exponent - num1.Exponent;
			mantissa = num2.Mantissa;
			exponent = num2.Exponent;
			while (value > 0)
			{
				num3 /= 10.0;
				value--;
			}
		}
		else
		{
			num3 = num1.Mantissa;
			exponent = num1.Exponent;
			mantissa = num2.Mantissa;
		}
		return new BigNumber(num3 + mantissa, exponent);
	}

	public static BigNumber operator -(BigNumber num1, BigNumber num2)
	{
		if (num1.Exponent == num2.Exponent && Math.Abs(num1.Mantissa - num2.Mantissa) < 1E-13)
		{
			return 0.0;
		}
		num2.Mantissa = 0.0 - num2.Mantissa;
		return num1 + num2;
	}

	public BigNumber Pow(int x)
	{
		BigNumber bigNumber = this;
		BigNumber result = new BigNumber(1.0);
		while (x > 0)
		{
			if (x % 2 == 0)
			{
				bigNumber *= bigNumber;
				x /= 2;
			}
			else
			{
				result *= bigNumber;
				x--;
			}
		}
		return result;
	}

	public BigNumber Pow(double x)
	{
		if (x < 0.0)
		{
			return 1.0 / Pow(0.0 - x);
		}
		BigNumber bigNumber = this;
		double num = x % 1.0;
		bigNumber = Pow((int)x);
		double num2 = (double)Exponent * num;
		double y = num2 % 1.0;
		bigNumber.Exponent += (int)num2;
		bigNumber.Mantissa *= Math.Pow(Mantissa, num);
		bigNumber.Mantissa *= Math.Pow(10.0, y);
		return bigNumber;
	}

	public double Log10()
	{
		if (Mantissa == 0.0)
		{
			Debug.LogError("Log 0");
			return 0.0;
		}
		return (double)Exponent + Math.Log10(Mantissa);
	}

	public double Ln()
	{
		return (double)Exponent * Math.Log(10.0) + Math.Log(Mantissa);
	}

	public double Log_a(double a)
	{
		return (double)Exponent * Math.Log(10.0, a) + Math.Log(Mantissa, a);
	}

	public BigNumber Sqrt()
	{
		int num = Exponent / 2;
		double mantissa = ((Exponent - num * 2 == 1) ? Math.Sqrt(Mantissa * 10.0) : ((Exponent - num * 2 != -1) ? Math.Sqrt(Mantissa) : Math.Sqrt(Mantissa / 10.0)));
		return new BigNumber(mantissa, num);
	}

	public BigNumber Abs()
	{
		return new BigNumber(Math.Abs(Mantissa), Exponent);
	}

	public double ToDouble()
	{
		if (this >= double.MaxValue)
		{
			return double.MaxValue;
		}
		return Mantissa * Math.Pow(10.0, Exponent);
	}

	public float ToFloat()
	{
		if (this >= 3.4028234663852886E+38)
		{
			return float.MaxValue;
		}
		return (float)ToDouble();
	}

	public int ToInt()
	{
		if (this > 2147483647.0)
		{
			return int.MaxValue;
		}
		return Convert.ToInt32(ToDouble());
	}

	public ulong ToUlong()
	{
		if (this > 1.8446744073709552E+19)
		{
			return ulong.MaxValue;
		}
		if (Sign(this) < 0)
		{
			return 0uL;
		}
		return Convert.ToUInt64(ToDouble());
	}

	public BigNumber Floor()
	{
		if (Exponent < 6 && Exponent > -1)
		{
			double num = Mantissa;
			for (int num2 = Exponent; num2 > 0; num2--)
			{
				num *= 10.0;
			}
			num = (int)Math.Floor(num);
			return new BigNumber(num, 0);
		}
		if (Exponent < 0)
		{
			return 0.0;
		}
		return this;
	}

	public BigNumber Clamp(BigNumber min, BigNumber max)
	{
		if (this < min)
		{
			Exponent = min.Exponent;
			Mantissa = min.Mantissa;
		}
		if (this > max)
		{
			Exponent = max.Exponent;
			Mantissa = max.Mantissa;
		}
		return this;
	}

	public override bool Equals(object obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public static bool TryParse(string value, out BigNumber result)
	{
		try
		{
			result = new BigNumber(value);
			return true;
		}
		catch
		{
			result = null;
			return false;
		}
	}

	public static BigNumber ArithmeticSumm(int amount, BigNumber first, BigNumber delta)
	{
		return (2.0 * first + delta * (amount - 1)) * amount / 2.0;
	}

	public static BigNumber GeometrySumm(int amount, BigNumber first, BigNumber delta)
	{
		return first * (1.0 - delta.Pow(amount)) / (1.0 - delta);
	}

	public static BigNumber AmountArithProgression(BigNumber sum, BigNumber first, BigNumber growth)
	{
		return QuadEquationBig(growth, 2.0 * first - growth, -2.0 * sum);
	}

	private static BigNumber QuadEquationBig(BigNumber a, BigNumber b, BigNumber c)
	{
		BigNumber n = b * b - 4.0 * a * c;
		if (n.Mantissa == 0.0)
		{
			return -b / (2.0 * a);
		}
		if (Sign(n) < 0)
		{
			return 0.0;
		}
		return (n.Sqrt() - b) / (2.0 * a).Abs();
	}
}
