using System;
using System.Globalization;
using UnityEngine;

public static class NumberUtils
{
	public static string FloatToTruncatedString(float value, int decimals = 2)
	{
		int num = 1;
		for (int i = 0; i < decimals; i++)
		{
			num *= 10;
		}
		return ((float)Mathf.FloorToInt(value * (float)num) / (float)num).ToString("F" + decimals, CultureInfo.InvariantCulture);
	}

	public static string BigNumberToReadableStringTruncated(BigNumber value, int decimals = 2, string format = "F2")
	{
		int num = 1;
		for (int i = 0; i < decimals; i++)
		{
			num *= 10;
		}
		double mantissa = Math.Floor(value.Mantissa * (double)num) / (double)num;
		return new BigNumber(mantissa, value.Exponent).ToReadableString(format);
	}
}
