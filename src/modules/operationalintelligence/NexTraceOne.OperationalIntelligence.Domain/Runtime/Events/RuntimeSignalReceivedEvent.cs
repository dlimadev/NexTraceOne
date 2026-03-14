using NexTraceOne.BuildingBlocks.Core;

namespace NexTraceOne.RuntimeIntelligence.Domain.Events;

/// <summary>
/// Evento emitido quando um sinal de runtime é recebido e processado.
/// Consumidores típicos: ChangeIntelligence (correlação), Audit.
/// </summary>
public sealed record RuntimeSignalReceivedEvent(
    Guid SignalId,
    string SourceSystem,
    string SignalType,
    DateTimeOffset ReceivedAt) : IntegrationEventBase("OperationalIntelligence");
