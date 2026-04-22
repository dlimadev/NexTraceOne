namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração de leitura de dados de impacto de breaking changes para análise de impacto transitivo.
///
/// Fornece dados de breaking changes registados com os seus consumidores directos,
/// dependências de serviço e tier de cada consumidor.
/// Desacopla o handler de breaking change impact das implementações concretas de repositório.
///
/// Wave AE.2 — GetSchemaBreakingChangeImpactReport.
/// </summary>
public interface IBreakingChangeImpactReader
{
    /// <summary>
    /// Lista breaking changes de um tenant no período lookback com consumidores directos.
    /// </summary>
    Task<IReadOnlyList<BreakingChangeEntry>> ListBreakingChangesByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);
}

/// <summary>
/// Entrada de uma breaking change com consumidores directos registados.
/// Wave AE.2.
/// </summary>
public sealed record BreakingChangeEntry(
    /// <summary>Identificador único da breaking change (corresponde a ChangelogEntryId).</summary>
    string ChangelogEntryId,
    /// <summary>Identificador do ativo de API afectado.</summary>
    string ApiAssetId,
    /// <summary>Nome do serviço produtor afectado.</summary>
    string ProducerServiceName,
    /// <summary>Versão de origem da alteração.</summary>
    string? FromVersion,
    /// <summary>Versão de destino da alteração.</summary>
    string ToVersion,
    /// <summary>Data e hora da breaking change.</summary>
    DateTimeOffset ChangedAt,
    /// <summary>Resumo da breaking change.</summary>
    string Summary,
    /// <summary>Lista de consumidores directos registados para esta API.</summary>
    IReadOnlyList<ConsumerServiceInfo> DirectConsumers);

/// <summary>
/// Informação de um serviço consumidor directo de um contrato.
/// Wave AE.2.
/// </summary>
public sealed record ConsumerServiceInfo(
    /// <summary>Nome do serviço consumidor.</summary>
    string ServiceName,
    /// <summary>Tier do serviço consumidor: Critical, Standard ou Experimental.</summary>
    string ServiceTier,
    /// <summary>Ambiente de consumo: production, non-production, etc.</summary>
    string Environment,
    /// <summary>Lista de nomes de serviços que dependem deste consumidor (1 hop).</summary>
    IReadOnlyList<string> DependentServiceNames);
