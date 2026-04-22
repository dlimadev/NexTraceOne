namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração de leitura de dados de evolução de schemas de contratos AsyncAPI/Kafka.
///
/// Fornece dados agregados por contrato cobrindo:
/// histórico de changelogs AsyncAPI com flag de breaking change, versão corrente,
/// e dados de adopção dos consumidores (schema lag).
/// Desacopla o handler de schema evolution das implementações concretas de repositório.
///
/// Wave AH.1 — GetEventSchemaEvolutionReport.
/// </summary>
public interface IEventSchemaEvolutionReader
{
    /// <summary>
    /// Lista entradas de evolução de schemas de eventos de um tenant no período lookback.
    /// </summary>
    Task<IReadOnlyList<EventSchemaEvolutionEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);
}

/// <summary>
/// Entrada de evolução de schema de um contrato AsyncAPI/Kafka.
/// Agrega histórico de changelogs e dados de adopção dos consumidores.
/// Wave AH.1.
/// </summary>
public sealed record EventSchemaEvolutionEntry(
    /// <summary>Identificador do ativo de contrato de evento.</summary>
    string ContractId,
    /// <summary>Nome do evento (tópico Kafka ou canal AsyncAPI).</summary>
    string EventName,
    /// <summary>Nome do serviço produtor.</summary>
    string ProducerServiceName,
    /// <summary>Versão corrente do schema.</summary>
    string CurrentSchemaVersion,
    /// <summary>Total de changelogs registados no período.</summary>
    int TotalSchemaChanges,
    /// <summary>Número de changelogs marcados como breaking change.</summary>
    int BreakingSchemaChanges,
    /// <summary>Número de consumidores ainda em versão anterior à corrente.</summary>
    int ActiveConsumersOnOldVersion,
    /// <summary>
    /// Diferença em dias entre publicação da versão corrente e última actualização
    /// do consumidor mais atrasado. 0 quando não há lag.
    /// </summary>
    double SchemaLagDays,
    /// <summary>Data de publicação da versão corrente do schema.</summary>
    DateTimeOffset CurrentVersionPublishedAt);

/// <summary>
/// Implementação nula de <see cref="IEventSchemaEvolutionReader"/> que devolve lista vazia.
/// Utilizada em ambientes sem dados de changelogs de eventos AsyncAPI.
/// Wave AH.1.
/// </summary>
public sealed class NullEventSchemaEvolutionReader : IEventSchemaEvolutionReader
{
    public Task<IReadOnlyList<EventSchemaEvolutionEntry>> ListByTenantAsync(
        string tenantId, int lookbackDays, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<EventSchemaEvolutionEntry>>(Array.Empty<EventSchemaEvolutionEntry>());
}
