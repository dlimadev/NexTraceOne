namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção cross-module que fornece os scores de dimensões de maturidade (0–100) de um tenant
/// para o relatório de benchmarking multi-tenant.
///
/// Por omissão é satisfeita por <c>NullCrossTenantMaturityReader</c> que retorna scores neutros (50)
/// (honest-null pattern) — nenhuma dimensão é calculada sem bridge real configurado.
///
/// Wave AJ.1 — GetCrossTenantMaturityReport (ChangeGovernance Compliance).
/// </summary>
public interface ICrossTenantMaturityReader
{
    /// <summary>
    /// Retorna os scores de dimensões de maturidade (0–100) de um tenant.
    /// Cada dimensão representa a percentagem de adopção de uma capacidade da plataforma.
    /// </summary>
    Task<TenantMaturityDimensions> GetDimensionsAsync(
        string tenantId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista os tenants com consentimento de benchmark, retornando as suas dimensões de maturidade
    /// para efeito de cálculo de percentis no ecossistema.
    /// Nunca retorna dados identificáveis — apenas scores agrupados.
    /// </summary>
    Task<IReadOnlyList<TenantMaturityDimensions>> ListConsentedTenantDimensionsAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dimensões de maturidade calculadas para um tenant.
    /// Cada score está entre 0 e 100 (percentagem de adopção da capacidade).
    /// </summary>
    public sealed record TenantMaturityDimensions(
        string TenantId,
        /// <summary>% de serviços com contratos registados e aprovados.</summary>
        decimal ContractGoverned,
        /// <summary>% de releases com ConfidenceScore registado.</summary>
        decimal ChangeConfidenceEnabled,
        /// <summary>% de serviços com SloObservation no último mês.</summary>
        decimal SloTracked,
        /// <summary>% de incidentes com runbook associado pós-evento.</summary>
        decimal RunbookCovered,
        /// <summary>% de serviços com ProfilingSession no último mês.</summary>
        decimal ProfilingActive,
        /// <summary>% de serviços com pelo menos 1 compliance report no trimestre.</summary>
        decimal ComplianceEvaluated,
        /// <summary>% de utilizadores activos que interagiram com AI assistant no mês.</summary>
        decimal AiAssistantUsed);
}
