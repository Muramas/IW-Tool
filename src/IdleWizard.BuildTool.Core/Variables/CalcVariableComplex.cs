using IdleWizard.BuildTool.Core.Numbers;

namespace IdleWizard.BuildTool.Core.Variables;

public sealed class CalcVariableComplex : ICalcVariable
{
    public string Key { get; }

    public CalcBigNumber InternalValue { get; private set; }

    public CalcBigNumber Add { get; private set; } = CalcBigNumber.Zero;

    public CalcBigNumber Mult { get; private set; } = CalcBigNumber.One;

    public CalcBigNumber Value => (InternalValue + Add) * Mult;

    public CalcVariableComplex(string key, CalcBigNumber initial)
    {
        Key = key;
        InternalValue = initial;
    }

    public void Change(CalcBigNumber addendum, CalcBigNumber multiplier)
    {
        Add += addendum;
        Mult *= multiplier;
    }

    public void SetValue(CalcBigNumber value)
    {
        InternalValue = value;
    }

    public void ResetModifiers()
    {
        Add = CalcBigNumber.Zero;
        Mult = CalcBigNumber.One;
    }
}
