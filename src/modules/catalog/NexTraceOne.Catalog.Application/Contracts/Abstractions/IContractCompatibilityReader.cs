namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração de leitura de dados de histórico de changelogs para análise de compatibilidade retroativa.
///
/// Fornece dados agregados por contrato cobrindo:
/// histórico de changelogs com flag de breaking change, contagem de versões major,
/// e dados de adopção dos consumidores (lag de migração).
/// Desacopla o handler de backward compatibility das implementações concretas de repositório.
///
/// Wave AE.3 — GetApiBackwardCompatibilityReport.
/// </summary>
public interface IContractCompatibilityReader
{
    /// <summary>
    /// Lista entradas de compatibilidade de contratos de um tenant no período lookback.
    /// </summary>
    Task<IReadOnlyList<ContractCompatibilityEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);
}

/// <summary>
/// Entrada de compatibilidade retroativa de um contrato.
/// Agrega histórico de changelogs e dados de adopção dos consumidores.
/// Wave AE.3.
/// </summary>
public sealed record ContractCompatibilityEntry(
    /// <summary>Identificador do ativo de API.</summary>
    string ApiAssetId,
    /// <summary>Nome do serviço produtor.</summary>
    string ServiceName,
    /// <summary>Versão mais recente do contrato.</summary>
    string LatestVersion,
    /// <summary>Total de changelogs no período.</summary>
    int TotalChangelogs,
    /// <summary>Número de changelogs marcados como breaking change.</summary>
    int BreakingChangelogCount,
    /// <summary>Número de versões major lançadas no período.</summary>
    int MajorVersionCount,
    /// <summary>
    /// Média de dias que os consumidores demoram a migrar para a versão mais recente.
    /// 0 quando não há consumidores ou todos já estão na versão mais recente.
    /// </summary>
    double ConsumerAdoptionLagDays,
    /// <summary>Data do último changelog registado para este contrato.</summary>
    DateTimeOffset LastChangelogAt);
