using Jobuler.Domain.Common;

namespace Jobuler.Domain.Constraints;

public enum ConstraintScopeType { Person, Role, Group, TaskType, Space }
public enum ConstraintSeverity   { Hard, Soft }

/// <summary>
/// Flexible constraint rule supporting all scope levels and severities.
/// rule_payload_json holds the constraint-specific parameters.
///
/// Known rule_type values:
///   min_rest_hours          — payload: { "hours": 8 }
///   no_overlap              — payload: {}
///   max_kitchen_per_week    — payload: { "max": 2 }
///   no_consecutive_burden   — payload: { "burden_level": "disliked" }
///   min_base_headcount      — payload: { "min": 3, "window_hours": 24 }
///   no_task_type_restriction — payload: { "task_type_id": "..." }
/// </summary>
public class ConstraintRule : AuditableEntity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public ConstraintScopeType ScopeType { get; private set; }
    public Guid? ScopeId { get; private set; }   // null when ScopeType = Space
    public ConstraintSeverity Severity { get; private set; }
    public string RuleType { get; private set; } = default!;
    public string RulePayloadJson { get; private set; } = "{}";
    public bool IsActive { get; private set; } = true;
    public DateOnly? EffectiveFrom { get; private set; }
    public DateOnly? EffectiveUntil { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    private ConstraintRule() { }

    public static ConstraintRule Create(
        Guid spaceId, ConstraintScopeType scopeType, Guid? scopeId,
        ConstraintSeverity severity, string ruleType, string rulePayloadJson,
        Guid createdByUserId, DateOnly? effectiveFrom = null, DateOnly? effectiveUntil = null) =>
        new()
        {
            SpaceId = spaceId,
            ScopeType = scopeType,
            ScopeId = scopeId,
            Severity = severity,
            RuleType = ruleType.Trim(),
            RulePayloadJson = rulePayloadJson,
            EffectiveFrom = effectiveFrom,
            EffectiveUntil = effectiveUntil,
            CreatedByUserId = createdByUserId
        };

    public void Update(string rulePayloadJson, DateOnly? effectiveUntil, Guid updatedByUserId)
    {
        RulePayloadJson = rulePayloadJson;
        EffectiveUntil = effectiveUntil;
        UpdatedByUserId = updatedByUserId;
        Touch();
    }

    public void Deactivate(Guid updatedByUserId) { IsActive = false; UpdatedByUserId = updatedByUserId; Touch(); }
}
