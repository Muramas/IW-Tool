using IdleWizard.BuildTool.Core.Effects;

namespace IdleWizard.BuildTool.Core.Evaluation;

public sealed class BuildEvaluator
{
    public EvaluationReport Evaluate(
        CalculatorContext context,
        IEnumerable<ICalcEffect> effects)
    {
        foreach (var effect in effects)
        {
            context.Report.Effects.Add(effect.Apply(context));
        }

        return context.Report;
    }
}
