using IdleWizard.BuildTool.Core.Effects;

namespace IdleWizard.BuildTool.Core.Evaluation;

public sealed class EvaluationReport
{
    public List<EffectApplyResult> Effects { get; } = new();

    public IEnumerable<EffectApplyResult> Unresolved =>
        Effects.Where(
            effect =>
                effect.Status is EffectStatus.UnresolvedTarget
                    or EffectStatus.UnresolvedCondition
                    or EffectStatus.UnsupportedOperation
                    or EffectStatus.Error
        );

    public bool IsFullyVerified => !Unresolved.Any();
}
