using System.Globalization;

namespace IdleWizard.BuildTool.Core.Numbers;

public readonly record struct CalcBigNumber(double Mantissa, long Exponent)
{
    public static CalcBigNumber Zero => new(0, 0);
    public static CalcBigNumber One => new(1, 0);

    public static CalcBigNumber FromDouble(double value)
    {
        if (value == 0 || double.IsNaN(value))
            return Zero;

        var exponent = (long)Math.Floor(Math.Log10(Math.Abs(value)));
        var mantissa = value / Math.Pow(10, exponent);

        return Normalize(mantissa, exponent);
    }

    public static CalcBigNumber Parse(string value)
    {
        value = value.Trim();

        if (value.Contains('e', StringComparison.OrdinalIgnoreCase))
        {
            var parts = value.Split(new[] { 'e', 'E' });
            return Normalize(double.Parse(parts[0], CultureInfo.InvariantCulture), long.Parse(parts[1], CultureInfo.InvariantCulture));
        }

        return FromDouble(double.Parse(value, CultureInfo.InvariantCulture));
    }

    public static CalcBigNumber Normalize(double mantissa, long exponent)
    {
        if (mantissa == 0 || double.IsNaN(mantissa))
            return Zero;

        while (Math.Abs(mantissa) >= 10)
        {
            mantissa /= 10;
            exponent++;
        }

        while (Math.Abs(mantissa) < 1)
        {
            mantissa *= 10;
            exponent--;
        }

        return new CalcBigNumber(mantissa, exponent);
    }

    public double Log10()
    {
        if (Mantissa == 0)
            return double.NegativeInfinity;

        return Math.Log10(Math.Abs(Mantissa)) + Exponent;
    }


    public CalcBigNumber Pow(double power)
    {
        if (Mantissa == 0)
        {
            return Zero;
        }

        var log = Log10() * power;
        var exponent = (long)Math.Floor(log);
        var mantissa = Math.Pow(10, log - exponent);

        return Normalize(mantissa, exponent);
    }

    public static CalcBigNumber operator /(CalcBigNumber a, CalcBigNumber b)
    {
        return Normalize(a.Mantissa / b.Mantissa, a.Exponent - b.Exponent);
    }

    public static CalcBigNumber operator -(CalcBigNumber a, CalcBigNumber b)
    {
        return a + new CalcBigNumber(-b.Mantissa, b.Exponent);
    }
    public static CalcBigNumber operator *(CalcBigNumber a, CalcBigNumber b)
    {
        return Normalize(a.Mantissa * b.Mantissa, a.Exponent + b.Exponent);
    }

    public static CalcBigNumber operator +(CalcBigNumber a, CalcBigNumber b)
    {
        if (a.Mantissa == 0)
            return b;

        if (b.Mantissa == 0)
            return a;

        var diff = a.Exponent - b.Exponent;

        if (diff > 15)
            return a;

        if (diff < -15)
            return b;

        var exponent = Math.Max(a.Exponent, b.Exponent);

        var mantissa =
            a.Mantissa * Math.Pow(10, a.Exponent - exponent)
            + b.Mantissa * Math.Pow(10, b.Exponent - exponent);

        return Normalize(mantissa, exponent);
    }

    public override string ToString()
    {
        return Mantissa == 0
            ? "0"
            : $"{Mantissa:0.############}e{Exponent}";
    }
}

