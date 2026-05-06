using IdleWizard.BuildTool.Core.Variables;

namespace IdleWizard.BuildTool.Core.Resources;

public sealed class ResourceRegistry
{
    private readonly Dictionary<string, ICalcVariable> _variables =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(ICalcVariable variable)
    {
        _variables[variable.Key] = variable;
    }

    public bool TryGet(string key, out ICalcVariable? variable)
    {
        return _variables.TryGetValue(key, out variable);
    }

    public IReadOnlyDictionary<string, ICalcVariable> All => _variables;
}
