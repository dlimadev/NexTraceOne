using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Domain.Orchestration.Context;

/// <summary>
/// Value Object que encapsula o contexto necessário para análise de risco de promoção entre ambientes.
///
/// Este é um dos requisitos centrais do produto: a IA deve ser capaz de analisar
/// o comportamento de um ambiente não produtivo e determinar o risco de promover
/// uma mudança para o próximo ambiente (tipicamente produção).
///
/// A análise de risco considera:
/// - Comportamento do serviço no ambiente atual (source)
/// - Baseline esperado do ambiente de destino (target)
/// - Histórico de incidentes relacionados
/// - Qualidade dos contratos e compatibilidade de versão
/// - Sinais de regressão detectados
/// - Cobertura de testes e qualidade mínima
/// </summary>
public sealed class PromotionRiskAnalysisContext : ValueObject
{
    /// <summary>Contexto de execução da IA que solicitou a análise.</summary>
    public AiExecutionContext ExecutionContext { get; }

    /// <summary>Ambiente de origem da promoção (ex.: staging, UAT).</summary>
    public EnvironmentId SourceEnvironmentId { get; }

    /// <summary>Perfil do ambiente de origem.</summary>
    public EnvironmentProfile SourceProfile { get; }

    /// <summary>Ambiente de destino da promoção (tipicamente produção).</summary>
    public EnvironmentId TargetEnvironmentId { get; }

    /// <summary>Perfil do ambiente de destino.</summary>
    public EnvironmentProfile TargetProfile { get; }

    /// <summary>Serviço sendo promovido.</summary>
    public string ServiceName { get; }

    /// <summary>Versão sendo promovida.</summary>
    public string Version { get; }

    /// <summary>
    /// Identificador da release sendo analisada.
    /// Permite à IA correlacionar a análise com dados de deployment específicos.
    /// </summary>
    public Guid? ReleaseId { get; }

    /// <summary>
    /// Janela de tempo para análise de comportamento no ambiente de origem.
    /// A IA analisa este período para detectar regressões e anomalias.
    /// </summary>
    public AiTimeWindow ObservationWindow { get; }

    private PromotionRiskAnalysisContext(
        AiExecutionContext executionContext,
        EnvironmentId sourceEnvironmentId,
        EnvironmentProfile sourceProfile,
        EnvironmentId targetEnvironmentId,
        EnvironmentProfile targetProfile,
        string serviceName,
        string version,
        AiTimeWindow observationWindow,
        Guid? releaseId)
    {
        ExecutionContext = executionContext;
        SourceEnvironmentId = sourceEnvironmentId;
        SourceProfile = sourceProfile;
        TargetEnvironmentId = targetEnvironmentId;
        TargetProfile = targetProfile;
        ServiceName = serviceName;
        Version = version;
        ObservationWindow = observationWindow;
        ReleaseId = releaseId;
    }

    /// <summary>
    /// Cria o contexto de análise de risco de promoção.
    /// </summary>
    public static PromotionRiskAnalysisContext Create(
        AiExecutionContext executionContext,
        EnvironmentId sourceEnvironmentId,
        EnvironmentProfile sourceProfile,
        EnvironmentId targetEnvironmentId,
        EnvironmentProfile targetProfile,
        string serviceName,
        string version,
        AiTimeWindow? observationWindow = null,
        Guid? releaseId = null)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(sourceEnvironmentId);
        ArgumentNullException.ThrowIfNull(targetEnvironmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        if (sourceEnvironmentId.Value == targetEnvironmentId.Value)
            throw new InvalidOperationException("Source and target environments must be different for promotion analysis.");

        return new PromotionRiskAnalysisContext(
            executionContext,
            sourceEnvironmentId,
            sourceProfile,
            targetEnvironmentId,
            targetProfile,
            serviceName,
            version,
            observationWindow ?? AiTimeWindow.LastDays(7),
            releaseId);
    }

    /// <summary>
    /// Indica se esta análise é uma promoção para ambiente de produção ou similar.
    /// </summary>
    public bool IsPromotionToProduction()
        => TargetProfile is EnvironmentProfile.Production or EnvironmentProfile.DisasterRecovery;

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ExecutionContext.TenantId;
        yield return SourceEnvironmentId;
        yield return TargetEnvironmentId;
        yield return ServiceName;
        yield return Version;
    }
}
