namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração de leitura de dados de conformidade de contratos de eventos AsyncAPI.
///
/// Fornece dados de conformidade de produtores de eventos com os respectivos contratos AsyncAPI,
/// incluindo taxas de conformidade de schema, violações de payload e campos não registados.
/// Desacopla o handler de compliance das implementações concretas de repositório.
///
/// Wave AH.3 — GetEventContractComplianceReport.
/// </summary>
public interface IEventComplianceReader
{
    /// <summary>
    /// Lista entradas de conformidade de contratos de eventos de um tenant no período lookback.
    /// </summary>
    Task<IReadOnlyList<EventComplianceEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);
}

/// <summary>
/// Entrada de conformidade de um contrato de evento com o schema AsyncAPI registado.
/// Wave AH.3.
/// </summary>
public sealed record EventComplianceEntry(
    /// <summary>Identificador do contrato de evento.</summary>
    string ContractId,
    /// <summary>Nome do evento (tópico/canal).</summary>
    string EventName,
    /// <summary>Nome do serviço produtor.</summary>
    string ProducerServiceName,
    /// <summary>
    /// Taxa de conformidade com o schema: % de eventos observados que passaram
    /// validação de schema (0.0–100.0).
    /// </summary>
    double SchemaComplianceRate,
    /// <summary>Número total de payloads que violaram o schema no período.</summary>
    int PayloadViolationCount,
    /// <summary>Campos presentes nos payloads mas não definidos no schema registado.</summary>
    IReadOnlyList<string> UnregisteredFields,
    /// <summary>Campos obrigatórios no schema mas ausentes em algum payload observado.</summary>
    IReadOnlyList<string> MissingRequiredFields,
    /// <summary>
    /// Série temporal de violações por dia no período lookback.
    /// Chave: data (yyyy-MM-dd), valor: número de violações nesse dia.
    /// </summary>
    IReadOnlyDictionary<string, int> ViolationTimeline);

/// <summary>
/// Implementação nula de <see cref="IEventComplianceReader"/> que devolve lista vazia.
/// Utilizada em ambientes sem dados de conformidade de eventos.
/// Wave AH.3.
/// </summary>
public sealed class NullEventComplianceReader : IEventComplianceReader
{
    public Task<IReadOnlyList<EventComplianceEntry>> ListByTenantAsync(
        string tenantId, int lookbackDays, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<EventComplianceEntry>>(Array.Empty<EventComplianceEntry>());
}
