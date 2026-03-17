namespace NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

/// <summary>
/// Value object que representa uma entrada individual de mudança detectada em um diff semântico de contrato.
/// Armazenado como JSON dentro de ContractDiff.
/// </summary>
public sealed record ChangeEntry(
    string ChangeType,
    string Path,
    string? Method,
    string Description,
    bool IsBreaking);
