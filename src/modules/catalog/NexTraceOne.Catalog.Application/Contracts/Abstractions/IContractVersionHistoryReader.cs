namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração de leitura do histórico de versões de contratos do catálogo.
///
/// Fornece dados de versionamento de contratos para análise de linhagem — autor, aprovador,
/// datas de promoção, breaking changes e consumidores activos na data de deprecação.
/// Desacopla o handler de linhagem das implementações concretas de repositório.
///
/// Wave AB.2 — GetContractLineageReport.
/// </summary>
public interface IContractVersionHistoryReader
{
    /// <summary>
    /// Lista versões de um contrato específico dentro de uma janela temporal.
    /// </summary>
    Task<IReadOnlyList<ContractVersionEntry>> ListByContractAsync(
        string tenantId,
        string contractId,
        int lookbackDays,
        CancellationToken ct);

    /// <summary>
    /// Lista os identificadores de todos os contratos de um tenant.
    /// </summary>
    Task<IReadOnlyList<string>> ListContractIdsAsync(string tenantId, CancellationToken ct);
}

/// <summary>
/// Entrada de versão de um contrato no histórico de linhagem.
/// Agrega metadados de ciclo de vida, autoria, aprovação e impacto na transição.
/// Wave AB.2.
/// </summary>
public sealed record ContractVersionEntry(
    /// <summary>Identificador único do contrato.</summary>
    string ContractId,
    /// <summary>Nome legível do contrato.</summary>
    string ContractName,
    /// <summary>Número ou etiqueta da versão (ex: "v1.2.0").</summary>
    string Version,
    /// <summary>Estado do ciclo de vida: Draft, Published, Deprecated, Sunset.</summary>
    string LifecycleState,
    /// <summary>Nome do autor desta versão, ou null se não disponível.</summary>
    string? AuthorName,
    /// <summary>Nome do aprovador desta versão, ou null se não disponível.</summary>
    string? ApproverName,
    /// <summary>Data de publicação desta versão.</summary>
    DateTimeOffset PublishedAt,
    /// <summary>Data de deprecação, ou null se a versão ainda estiver activa.</summary>
    DateTimeOffset? DeprecatedAt,
    /// <summary>Número de breaking changes introduzidos face à versão anterior.</summary>
    int BreakingChangesFromPreviousVersion,
    /// <summary>Número de consumidores activos no momento da deprecação (0 se não deprecada).</summary>
    int ActiveConsumersAtDeprecation,
    /// <summary>Protocolo do contrato: REST, SOAP, AsyncAPI, etc.</summary>
    string? Protocol);
