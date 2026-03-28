using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.Integrations.Domain.Events;

/// <summary>
/// Evento publicado quando o payload de uma execução de ingestão é processado semanticamente com sucesso.
/// Consumido por módulos downstream (Change Intelligence, Operational Intelligence) para enriquecer os seus dados.
/// </summary>
public sealed record IngestionPayloadProcessedDomainEvent(
    Guid ExecutionId,
    string? ServiceName,
    string? Environment,
    string? Version,
    string? CommitSha,
    string? ChangeType,
    DateTimeOffset ProcessedAt) : DomainEventBase;
