using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;

/// <summary>
/// Evento publicado após ingestão bem-sucedida de evento batch legacy.
/// Consumido pelo módulo OperationalIntelligence para correlação e criação automática de incidentes.
/// </summary>
public sealed record LegacyBatchEventIngestedEvent(
    string IngestionEventId,
    string? JobName,
    string? JobId,
    string? ProgramName,
    string? ReturnCode,
    string? Status,
    string? SystemName,
    string? LparName,
    string Severity,
    string? Message,
    DateTimeOffset Timestamp) : DomainEventBase;
