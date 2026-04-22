namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção cross-module que fornece o histórico de avaliações de políticas para o
/// relatório de conformidade de plataforma (GetPlatformPolicyComplianceReport).
///
/// Por omissão é satisfeita por <c>NullPolicyEvaluationHistoryReader</c> que retorna uma
/// lista vazia (honest-null pattern) — nenhuma avaliação é contabilizada sem bridge real.
///
/// Wave AJ.3 — GetPlatformPolicyComplianceReport (ChangeGovernance Compliance).
/// </summary>
public interface IPolicyEvaluationHistoryReader
{
    /// <summary>
    /// Lista os resultados de avaliação de políticas no período indicado para o tenant.
    /// Retorna uma entrada por avaliação realizada, independentemente do resultado.
    /// </summary>
    Task<IReadOnlyList<PolicyEvaluationRecord>> ListEvaluationsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>Registo individual de uma avaliação de política realizada.</summary>
    public sealed record PolicyEvaluationRecord(
        /// <summary>ID da PolicyDefinition avaliada.</summary>
        Guid PolicyDefinitionId,
        /// <summary>Nome da entidade avaliada (serviço ou equipa).</summary>
        string EntityName,
        /// <summary>Tipo de entidade avaliada ("service" ou "team").</summary>
        string EntityType,
        /// <summary>True se a avaliação passou (Passed = true).</summary>
        bool Passed,
        /// <summary>Data e hora da avaliação.</summary>
        DateTimeOffset EvaluatedAt);
}
