namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção cross-module que agrega dados de saúde de múltiplos módulos para o
/// scorecard de saúde global do tenant (GetTenantHealthScoreReport).
///
/// Por omissão é satisfeita por <c>NullTenantHealthDataReader</c> que retorna scores neutros (50)
/// (honest-null pattern) — nenhum pilar é calculado sem bridge real configurado.
///
/// Wave AJ.2 — GetTenantHealthScoreReport (ChangeGovernance Compliance).
/// </summary>
public interface ITenantHealthDataReader
{
    /// <summary>
    /// Retorna os scores de saúde por pilar (0–100) de um tenant no período indicado.
    /// Cada pilar representa uma dimensão de saúde operacional da plataforma.
    /// </summary>
    Task<TenantHealthPillarData> GetPillarDataAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dados de saúde por pilar de um tenant.
    /// Cada score está entre 0 e 100.
    /// </summary>
    public sealed record TenantHealthPillarData(
        string TenantId,
        /// <summary>% de serviços com ownership + tier + contratos definidos.</summary>
        decimal ServiceGovernanceScore,
        /// <summary>Média de ConfidenceScore das últimas N releases (0–100).</summary>
        decimal ChangeConfidenceScore,
        /// <summary>SLO compliance rate + MTTR DORA tier combinados (0–100).</summary>
        decimal OperationalReliabilityScore,
        /// <summary>% de contratos Approved sem breaking changes não comunicados.</summary>
        decimal ContractHealthScore,
        /// <summary>% de serviços avaliados em ≥ 2 standards de compliance.</summary>
        decimal ComplianceCoverageScore,
        /// <summary>Ausência de serviços WasteAlert e WasteTier tenant (0–100).</summary>
        decimal FinOpsEfficiencyScore);
}
