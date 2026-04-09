using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

public sealed record ChangeCostImpactId(Guid Value) : TypedIdBase(Value);

public sealed class ChangeCostImpact : Entity<ChangeCostImpactId>
{
    public Guid ReleaseId { get; private init; }
    public string ServiceName { get; private init; } = string.Empty;
    public string Environment { get; private init; } = string.Empty;
    public string? ChangeDescription { get; private init; }
    public decimal BaselineCostPerDay { get; private init; }
    public decimal ActualCostPerDay { get; private init; }
    public decimal CostDelta { get; private init; }
    public decimal CostDeltaPercentage { get; private init; }
    public CostChangeDirection Direction { get; private init; }
    public string? CostProvider { get; private init; }
    public string? CostDetails { get; private init; }
    public DateTimeOffset MeasurementWindowStart { get; private init; }
    public DateTimeOffset MeasurementWindowEnd { get; private init; }
    public DateTimeOffset RecordedAt { get; private init; }
    public string? TenantId { get; private init; }
    public uint RowVersion { get; set; }

    private ChangeCostImpact() { }

    public static ChangeCostImpact Record(
        Guid releaseId,
        string serviceName,
        string environment,
        string? changeDescription,
        decimal baselineCostPerDay,
        decimal actualCostPerDay,
        string? costProvider,
        string? costDetails,
        DateTimeOffset measurementWindowStart,
        DateTimeOffset measurementWindowEnd,
        string? tenantId,
        DateTimeOffset now)
    {
        Guard.Against.Default(releaseId, nameof(releaseId));
        Guard.Against.NullOrWhiteSpace(serviceName, nameof(serviceName));
        Guard.Against.StringTooLong(serviceName, 200, nameof(serviceName));
        Guard.Against.NullOrWhiteSpace(environment, nameof(environment));
        Guard.Against.StringTooLong(environment, 100, nameof(environment));
        Guard.Against.Negative(baselineCostPerDay, nameof(baselineCostPerDay));
        Guard.Against.Negative(actualCostPerDay, nameof(actualCostPerDay));

        if (changeDescription is not null)
            Guard.Against.StringTooLong(changeDescription, 500, nameof(changeDescription));

        if (costProvider is not null)
            Guard.Against.StringTooLong(costProvider, 100, nameof(costProvider));

        if (measurementWindowEnd <= measurementWindowStart)
            throw new ArgumentException("Measurement window end must be after start.", nameof(measurementWindowEnd));

        var delta = actualCostPerDay - baselineCostPerDay;
        var deltaPercentage = baselineCostPerDay != 0m
            ? Math.Round(delta / baselineCostPerDay * 100m, 2)
            : actualCostPerDay > 0m ? 100m : 0m;

        var direction = delta > 0m
            ? CostChangeDirection.Increase
            : delta < 0m
                ? CostChangeDirection.Decrease
                : CostChangeDirection.Neutral;

        return new ChangeCostImpact
        {
            Id = new ChangeCostImpactId(Guid.NewGuid()),
            ReleaseId = releaseId,
            ServiceName = serviceName.Trim(),
            Environment = environment.Trim(),
            ChangeDescription = changeDescription?.Trim(),
            BaselineCostPerDay = baselineCostPerDay,
            ActualCostPerDay = actualCostPerDay,
            CostDelta = delta,
            CostDeltaPercentage = deltaPercentage,
            Direction = direction,
            CostProvider = costProvider?.Trim(),
            CostDetails = costDetails,
            MeasurementWindowStart = measurementWindowStart,
            MeasurementWindowEnd = measurementWindowEnd,
            RecordedAt = now,
            TenantId = tenantId?.Trim()
        };
    }
}
