using BB.Common;

namespace Workflow.Domain;

/// <summary>
/// Base cho 3 loại step gắn vào transition: Rule, Validator, PostFunction.
/// Lưu cấu hình dạng JSON, runtime thực thi qua strategy registry ở Application layer.
/// </summary>
public abstract class TransitionStep : BaseEntity
{
    public Guid TransitionId { get; protected set; }
    public string TypeKey { get; protected set; } = string.Empty;   // ví dụ "PERMISSION_RULE"
    public string ConfigJson { get; protected set; } = "{}";
    public int Order { get; protected set; }

    protected TransitionStep() { }

    protected TransitionStep(Guid transitionId, string typeKey, string configJson, int order)
    {
        if (string.IsNullOrWhiteSpace(typeKey))
            throw new DomainException("TRANSITION_STEP_TYPE_REQUIRED", "workflow.transition_step.type_required");

        TransitionId = transitionId;
        TypeKey = typeKey.ToUpperInvariant();
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson;
        Order = order;
    }

    public void UpdateConfig(string configJson) =>
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson;

    public void SetOrder(int order) => Order = order;
}

public sealed class TransitionRule : TransitionStep
{
    private TransitionRule() { }
    public TransitionRule(Guid transitionId, string typeKey, string configJson, int order)
        : base(transitionId, typeKey, configJson, order) { }
}

public sealed class TransitionValidator : TransitionStep
{
    private TransitionValidator() { }
    public TransitionValidator(Guid transitionId, string typeKey, string configJson, int order)
        : base(transitionId, typeKey, configJson, order) { }
}

public sealed class TransitionPostFunction : TransitionStep
{
    private TransitionPostFunction() { }
    public TransitionPostFunction(Guid transitionId, string typeKey, string configJson, int order)
        : base(transitionId, typeKey, configJson, order) { }
}
