using NexTraceOne.AIKnowledge.Domain.Orchestration.Context;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Application.Abstractions;

/// <summary>
/// Abstração para construção do contexto de análise de risco de promoção.
/// Prepara todos os dados e parâmetros necessários para que a IA realize
/// uma análise de readiness e risco antes de uma promoção entre ambientes.
///
/// Este builder encapsula:
/// - Resolução e validação dos ambientes de origem e destino
/// - Verificação de que ambos pertencem ao mesmo tenant
/// - Determinação da janela de observação adequada
/// - Construção do contexto de IA com escopos de análise de promoção habilitados
/// </summary>
public interface IPromotionRiskContextBuilder
{
    /// <summary>
    /// Constrói o contexto de análise de risco para promoção de um serviço
    /// de um ambiente para outro dentro do mesmo tenant.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="sourceEnvironmentId">Ambiente de origem (ex.: staging).</param>
    /// <param name="targetEnvironmentId">Ambiente de destino (ex.: production).</param>
    /// <param name="serviceName">Nome do serviço sendo promovido.</param>
    /// <param name="version">Versão sendo promovida.</param>
    /// <param name="releaseId">Identificador opcional da release associada.</param>
    /// <param name="observationWindowDays">Número de dias para análise histórica (default: 7).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<PromotionRiskAnalysisContext> BuildAsync(
        TenantId tenantId,
        EnvironmentId sourceEnvironmentId,
        EnvironmentId targetEnvironmentId,
        string serviceName,
        string version,
        Guid? releaseId = null,
        int observationWindowDays = 7,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Constrói o contexto de comparação entre dois ambientes do mesmo tenant.
    /// Usado para análise ad-hoc sem estar necessariamente associado a uma promoção específica.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="subjectEnvironmentId">Ambiente sendo avaliado.</param>
    /// <param name="referenceEnvironmentId">Ambiente de referência (baseline).</param>
    /// <param name="serviceFilter">Filtro opcional de serviços.</param>
    /// <param name="dimensions">Dimensões de comparação desejadas.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<EnvironmentComparisonContext> BuildComparisonAsync(
        TenantId tenantId,
        EnvironmentId subjectEnvironmentId,
        EnvironmentId referenceEnvironmentId,
        IEnumerable<string>? serviceFilter = null,
        IEnumerable<ComparisonDimension>? dimensions = null,
        CancellationToken cancellationToken = default);
}
