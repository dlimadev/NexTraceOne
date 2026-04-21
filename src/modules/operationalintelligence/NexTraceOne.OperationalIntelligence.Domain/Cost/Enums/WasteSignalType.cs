namespace NexTraceOne.OperationalIntelligence.Domain.Cost.Enums;

/// <summary>Classifica o tipo de sinal de desperdício operacional detetado.</summary>
public enum WasteSignalType
{
    IdleResources,
    Overprovisioned,
    UnattachedStorage,
    UnusedLicenses,
    OrphanedResources,
    OverlappingServices
}
