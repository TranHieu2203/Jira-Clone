namespace Workflow.Application.Engine;

public interface ITransitionStepRegistry
{
    ITransitionRule? FindRule(string typeKey);
    ITransitionValidator? FindValidator(string typeKey);
    ITransitionPostFunction? FindPostFunction(string typeKey);
}

public sealed class TransitionStepRegistry : ITransitionStepRegistry
{
    private readonly Dictionary<string, ITransitionRule> _rules;
    private readonly Dictionary<string, ITransitionValidator> _validators;
    private readonly Dictionary<string, ITransitionPostFunction> _postFunctions;

    public TransitionStepRegistry(
        IEnumerable<ITransitionRule> rules,
        IEnumerable<ITransitionValidator> validators,
        IEnumerable<ITransitionPostFunction> postFunctions)
    {
        _rules = rules.ToDictionary(r => r.TypeKey, StringComparer.OrdinalIgnoreCase);
        _validators = validators.ToDictionary(r => r.TypeKey, StringComparer.OrdinalIgnoreCase);
        _postFunctions = postFunctions.ToDictionary(r => r.TypeKey, StringComparer.OrdinalIgnoreCase);
    }

    public ITransitionRule? FindRule(string typeKey) =>
        _rules.GetValueOrDefault(typeKey);

    public ITransitionValidator? FindValidator(string typeKey) =>
        _validators.GetValueOrDefault(typeKey);

    public ITransitionPostFunction? FindPostFunction(string typeKey) =>
        _postFunctions.GetValueOrDefault(typeKey);
}
