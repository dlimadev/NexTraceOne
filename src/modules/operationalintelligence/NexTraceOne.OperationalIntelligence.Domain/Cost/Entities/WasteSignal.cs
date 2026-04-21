using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

/// <summary>
/// Sinal de desperdício operacional detetado para um serviço num dado período.
/// Representa uma observação de ineficiência que pode ser acionável para redução de custo.
/// </summary>
public sealed class WasteSignal : AuditableEntity<WasteSignalId>
{
    private WasteSignal() { }

    public string ServiceName { get; private set; } = string.Empty;
    public string Environment { get; private set; } = string.Empty;
    public WasteSignalType SignalType { get; private set; }
    public decimal EstimatedMonthlySavings { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string Description { get; private set; } = string.Empty;
    public string? TeamName { get; private set; }
    public bool IsAcknowledged { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public string? AcknowledgedBy { get; private set; }
    public DateTimeOffset DetectedAt { get; private set; }

    public static WasteSignal Create(
        string serviceName,
        string environment,
        WasteSignalType signalType,
        decimal estimatedMonthlySavings,
        string description,
        DateTimeOffset detectedAt,
        string? teamName = null,
        string currency = "USD")
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.NullOrWhiteSpace(description);
        if (estimatedMonthlySavings < 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedMonthlySavings), "Savings must be non-negative.");

        return new WasteSignal
        {
            Id = WasteSignalId.New(),
            ServiceName = serviceName,
            Environment = environment,
            SignalType = signalType,
            EstimatedMonthlySavings = estimatedMonthlySavings,
            Currency = currency,
            Description = description,
            TeamName = teamName,
            DetectedAt = detectedAt,
            IsAcknowledged = false
        };
    }

    public void Acknowledge(string acknowledgedBy, DateTimeOffset at)
    {
        Guard.Against.NullOrWhiteSpace(acknowledgedBy);
        IsAcknowledged = true;
        AcknowledgedAt = at;
        AcknowledgedBy = acknowledgedBy;
    }
}

/// <summary>Identificador fortemente tipado de WasteSignal.</summary>
public sealed record WasteSignalId(Guid Value) : TypedIdBase(Value)
{
    public static WasteSignalId New() => new(Guid.NewGuid());
    public static WasteSignalId From(Guid id) => new(id);
}
