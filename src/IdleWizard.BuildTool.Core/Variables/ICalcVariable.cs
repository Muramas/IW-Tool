using IdleWizard.BuildTool.Core.Numbers;

namespace IdleWizard.BuildTool.Core.Variables;

public interface ICalcVariable
{
    string Key { get; }
    CalcBigNumber Value { get; }

    void Change(CalcBigNumber addendum, CalcBigNumber multiplier);
    void SetValue(CalcBigNumber value);
}
