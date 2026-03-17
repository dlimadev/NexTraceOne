namespace NexTraceOne.BuildingBlocks.Core.Events;

/// <summary>
/// Marcador para Integration Events — eventos publicados entre módulos distintos.
/// Ao contrário dos Domain Events (intra-módulo), os Integration Events
/// cruzam fronteiras de bounded context. São serializados como OutboxMessages
/// e consumidos de forma assíncrona por outros módulos via Outbox Processor.
/// REGRA: Módulos nunca acessam DbContext de outros módulos diretamente.
/// A comunicação é sempre via Integration Events ou contratos públicos.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Identificador único do evento para garantia de idempotência.</summary>
    Guid EventId { get; }

    /// <summary>Data/hora UTC de ocorrência.</summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>Nome do módulo de origem (ex: "ChangeIntelligence").</summary>
    string SourceModule { get; }
}
