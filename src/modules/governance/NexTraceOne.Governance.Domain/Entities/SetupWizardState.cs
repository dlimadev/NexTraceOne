using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para SetupWizardStep.</summary>
public sealed record SetupWizardStepId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Persiste o estado de um passo do SetupWizard por tenant.
/// Permite ao admin retomar o onboarding onde parou.
/// </summary>
public sealed class SetupWizardStep : Entity<SetupWizardStepId>
{
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Identificador do passo (ex: "database", "security", "organization").</summary>
    public string StepId { get; private init; } = string.Empty;

    /// <summary>Dados configurados neste passo (JSON key→value).</summary>
    public string DataJson { get; private set; } = "{}";

    /// <summary>Quando o passo foi marcado como concluído.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private SetupWizardStep() { }

    public static SetupWizardStep Create(
        string tenantId,
        string stepId,
        string dataJson,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(stepId, nameof(stepId));

        return new SetupWizardStep
        {
            Id = new SetupWizardStepId(Guid.NewGuid()),
            TenantId = tenantId,
            StepId = stepId.ToLowerInvariant(),
            DataJson = string.IsNullOrWhiteSpace(dataJson) ? "{}" : dataJson,
            CompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(string dataJson, DateTimeOffset now)
    {
        DataJson = string.IsNullOrWhiteSpace(dataJson) ? "{}" : dataJson;
        CompletedAt = now;
        UpdatedAt = now;
    }
}
