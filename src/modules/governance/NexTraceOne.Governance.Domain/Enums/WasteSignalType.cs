namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Tipos de sinal de desperdício operacional identificado pelo FinOps contextual.
/// </summary>
public enum WasteSignalType
{
    ExcessiveRetries = 0,
    RepeatedFailures = 1,
    IdleCostlyResource = 2,
    RepeatedReprocessing = 3,
    UnstableConsumer = 4,
    NoisyService = 5,
    DegradedCostAmplification = 6,
    QueueBacklogInefficiency = 7,
    OverProvisioned = 8,
    ChangeRelatedInstability = 9
}
