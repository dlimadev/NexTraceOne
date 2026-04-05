using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

/// <summary>
/// Snapshot de produtividade de uma equipa num período, usado como insumo para o DX Score.
/// </summary>
public sealed class ProductivitySnapshot : Entity<ProductivitySnapshotId>
{
    private ProductivitySnapshot() { }

    public string TeamId { get; private set; } = string.Empty;
    public string? ServiceId { get; private set; }
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public int DeploymentCount { get; private set; }
    public decimal AverageCycleTimeHours { get; private set; }
    public int IncidentCount { get; private set; }
    public int ManualStepsCount { get; private set; }
    public string? SnapshotSource { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>Cria um novo snapshot de produtividade para uma equipa.</summary>
    public static Result<ProductivitySnapshot> Create(
        string teamId,
        string? serviceId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        int deploymentCount,
        decimal averageCycleTimeHours,
        int incidentCount,
        int manualStepsCount,
        string? snapshotSource,
        DateTimeOffset recordedAt)
    {
        if (string.IsNullOrWhiteSpace(teamId) || teamId.Length > 200)
            return Error.Validation("INVALID_TEAM_ID", "TeamId is required.");
        if (periodEnd <= periodStart)
            return Error.Validation("INVALID_PERIOD", "PeriodEnd must be after PeriodStart.");
        if (deploymentCount < 0)
            return Error.Validation("INVALID_DEPLOYMENT_COUNT", "DeploymentCount must be >= 0.");
        if (averageCycleTimeHours < 0m)
            return Error.Validation("INVALID_CYCLE_TIME", "AverageCycleTimeHours must be >= 0.");
        if (incidentCount < 0)
            return Error.Validation("INVALID_INCIDENT_COUNT", "IncidentCount must be >= 0.");
        if (manualStepsCount < 0)
            return Error.Validation("INVALID_MANUAL_STEPS", "ManualStepsCount must be >= 0.");

        return Result<ProductivitySnapshot>.Success(new ProductivitySnapshot
        {
            Id = ProductivitySnapshotId.New(),
            TeamId = teamId,
            ServiceId = serviceId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            DeploymentCount = deploymentCount,
            AverageCycleTimeHours = averageCycleTimeHours,
            IncidentCount = incidentCount,
            ManualStepsCount = manualStepsCount,
            SnapshotSource = snapshotSource,
            RecordedAt = recordedAt
        });
    }
}

/// <summary>Identificador fortemente tipado de ProductivitySnapshot.</summary>
public sealed record ProductivitySnapshotId(Guid Value) : TypedIdBase(Value)
{
    public static ProductivitySnapshotId New() => new(Guid.NewGuid());
    public static ProductivitySnapshotId From(Guid id) => new(id);
}
