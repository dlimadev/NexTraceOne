namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração de leitura de dados de equilíbrio produtor-consumidor de eventos.
///
/// Fornece dados agregados por contrato de evento cobrindo:
/// número de produtores e consumidores por evento,
/// identificando eventos órfãos, consumidores cegos e fan-out elevado.
/// Desacopla o handler de balance das implementações concretas de repositório.
///
/// Wave AH.2 — GetEventProducerConsumerBalanceReport.
/// </summary>
public interface IEventProducerConsumerReader
{
    /// <summary>
    /// Lista entradas de equilíbrio produtor-consumidor de um tenant.
    /// </summary>
    Task<IReadOnlyList<EventBalanceEntry>> ListByTenantAsync(
        string tenantId,
        CancellationToken ct);
}

/// <summary>
/// Entrada de equilíbrio produtor-consumidor de um contrato de evento.
/// Wave AH.2.
/// </summary>
public sealed record EventBalanceEntry(
    /// <summary>Identificador do contrato de evento.</summary>
    string ContractId,
    /// <summary>Nome do evento (tópico/canal).</summary>
    string EventName,
    /// <summary>Número de serviços registados como produtores deste evento.</summary>
    int ProducerCount,
    /// <summary>Número de serviços registados como consumidores deste evento.</summary>
    int ConsumerCount,
    /// <summary>Indica se o contrato está activo.</summary>
    bool IsActive);

/// <summary>
/// Implementação nula de <see cref="IEventProducerConsumerReader"/> que devolve lista vazia.
/// Utilizada em ambientes sem dados de eventos registados.
/// Wave AH.2.
/// </summary>
public sealed class NullEventProducerConsumerReader : IEventProducerConsumerReader
{
    public Task<IReadOnlyList<EventBalanceEntry>> ListByTenantAsync(
        string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<EventBalanceEntry>>(Array.Empty<EventBalanceEntry>());
}
