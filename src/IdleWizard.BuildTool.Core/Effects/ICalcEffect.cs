using IdleWizard.BuildTool.Core.Evaluation;

namespace IdleWizard.BuildTool.Core.Effects;

public interface ICalcEffect
{
    string SourceId { get; }
    string TargetKey { get; }
    EffectOperation Operation { get; }

    EffectApplyResult Apply(CalculatorContext context);
}
