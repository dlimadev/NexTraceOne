using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Domain.Orchestration.Context;

/// <summary>
/// Value Object que representa o contexto de execução de uma operação de IA.
/// Garante que toda operação da IA seja tenant-aware, environment-aware e user-aware.
///
/// A IA no NexTraceOne é uma capacidade transversal e única.
/// O que muda por ambiente é o CONTEXTO, não a IA em si.
/// Este VO é o portador de contexto que a IA recebe para operar corretamente.
///
/// Fluxo: usuário aciona IA → sistema resolve contexto → IA recebe AiExecutionContext
/// e opera dentro das permissões, fontes e escopo deste contexto.
/// </summary>
public sealed class AiExecutionContext : ValueObject
{
    /// <summary>Identificador do tenant para isolamento.</summary>
    public TenantId TenantId { get; }

    /// <summary>Identificador do ambiente no qual a IA está operando.</summary>
    public EnvironmentId EnvironmentId { get; }

    /// <summary>Perfil operacional do ambiente — determina profundidade e modo de análise.</summary>
    public EnvironmentProfile EnvironmentProfile { get; }

    /// <summary>Indica se o ambiente tem comportamento similar à produção.</summary>
    public bool IsProductionLikeEnvironment { get; }

    /// <summary>
    /// Contexto de usuário que acionou a operação de IA.
    /// Inclui identidade e persona para controle de acesso e personalização.
    /// </summary>
    public AiUserContext UserContext { get; }

    /// <summary>
    /// Escopos de dados que a IA está autorizada a consultar neste contexto.
    /// Controlado pelo backend — o frontend não pode expandir este escopo.
    /// </summary>
    public IReadOnlyList<string> AllowedDataScopes { get; }

    /// <summary>
    /// Módulo ou funcionalidade do produto que acionou a IA (ex.: "incidents", "change-governance", "contracts").
    /// Permite que a IA ajuste foco e contexto para o domínio relevante.
    /// </summary>
    public string ModuleContext { get; }

    /// <summary>
    /// Janela de tempo para análise histórica.
    /// Define até onde no passado a IA pode consultar dados para este contexto.
    /// </summary>
    public AiTimeWindow TimeWindow { get; }

    /// <summary>
    /// Contexto de release/versão quando a operação de IA é associada a um deployment.
    /// Null quando não há release associada.
    /// </summary>
    public AiReleaseContext? ReleaseContext { get; }

    private AiExecutionContext(
        TenantId tenantId,
        EnvironmentId environmentId,
        EnvironmentProfile environmentProfile,
        bool isProductionLikeEnvironment,
        AiUserContext userContext,
        IReadOnlyList<string> allowedDataScopes,
        string moduleContext,
        AiTimeWindow timeWindow,
        AiReleaseContext? releaseContext)
    {
        TenantId = tenantId;
        EnvironmentId = environmentId;
        EnvironmentProfile = environmentProfile;
        IsProductionLikeEnvironment = isProductionLikeEnvironment;
        UserContext = userContext;
        AllowedDataScopes = allowedDataScopes;
        ModuleContext = moduleContext;
        TimeWindow = timeWindow;
        ReleaseContext = releaseContext;
    }

    /// <summary>
    /// Cria um AiExecutionContext para análise operacional padrão.
    /// </summary>
    public static AiExecutionContext Create(
        TenantId tenantId,
        EnvironmentId environmentId,
        EnvironmentProfile environmentProfile,
        bool isProductionLike,
        AiUserContext userContext,
        string moduleContext,
        IEnumerable<string>? allowedDataScopes = null,
        AiTimeWindow? timeWindow = null,
        AiReleaseContext? releaseContext = null)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(environmentId);
        ArgumentNullException.ThrowIfNull(userContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleContext);

        return new AiExecutionContext(
            tenantId,
            environmentId,
            environmentProfile,
            isProductionLike,
            userContext,
            (allowedDataScopes ?? AiDataScope.DefaultScopes).ToList().AsReadOnly(),
            moduleContext,
            timeWindow ?? AiTimeWindow.Default,
            releaseContext);
    }

    /// <summary>
    /// Verifica se a IA pode usar fontes de comparação cross-environment neste contexto.
    /// Permitido apenas em ambientes não produtivos para análise de regressão.
    /// </summary>
    public bool CanUseCrossEnvironmentComparison()
        => !IsProductionLikeEnvironment && AllowedDataScopes.Contains(AiDataScope.CrossEnvironmentComparison);

    /// <summary>
    /// Verifica se a IA pode realizar análise de readiness para promoção neste contexto.
    /// </summary>
    public bool CanAnalyzePromotionReadiness()
        => AllowedDataScopes.Contains(AiDataScope.PromotionAnalysis);

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TenantId;
        yield return EnvironmentId;
        yield return UserContext.UserId;
    }
}

/// <summary>Contexto do usuário que acionou a operação de IA.</summary>
public sealed record AiUserContext(
    string UserId,
    string UserName,
    string Persona,
    IReadOnlyList<string> Roles);

/// <summary>Janela de tempo para análise histórica pela IA.</summary>
public sealed record AiTimeWindow(DateTimeOffset From, DateTimeOffset To)
{
    /// <summary>Janela padrão: últimas 24 horas até agora.</summary>
    public static AiTimeWindow Default => new(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);

    /// <summary>Cria uma janela dos últimos N dias.</summary>
    public static AiTimeWindow LastDays(int days) => new(DateTimeOffset.UtcNow.AddDays(-days), DateTimeOffset.UtcNow);
}

/// <summary>Contexto de release/deployment associado a uma operação de IA.</summary>
public sealed record AiReleaseContext(
    Guid ReleaseId,
    string ServiceName,
    string Version,
    string TargetEnvironmentSlug,
    DateTimeOffset DeployedAt);

/// <summary>
/// Escopos de dados que a IA pode acessar em uma operação.
/// Controlados pelo backend — não expansíveis pelo frontend.
/// </summary>
public static class AiDataScope
{
    /// <summary>Telemetria do ambiente (métricas, traces, logs).</summary>
    public const string Telemetry = "telemetry";

    /// <summary>Histórico de incidentes do ambiente.</summary>
    public const string Incidents = "incidents";

    /// <summary>Histórico de mudanças e deployments.</summary>
    public const string Changes = "changes";

    /// <summary>Contratos e versões de APIs/eventos.</summary>
    public const string Contracts = "contracts";

    /// <summary>Topologia e dependências de serviços.</summary>
    public const string Topology = "topology";

    /// <summary>Runbooks e conhecimento operacional.</summary>
    public const string Runbooks = "runbooks";

    /// <summary>Comparação cross-environment (apenas não-produção).</summary>
    public const string CrossEnvironmentComparison = "cross_environment_comparison";

    /// <summary>Análise de readiness para promoção entre ambientes.</summary>
    public const string PromotionAnalysis = "promotion_analysis";

    /// <summary>Escopos padrão para análise operacional básica.</summary>
    public static readonly IReadOnlyList<string> DefaultScopes =
        [Telemetry, Incidents, Changes, Contracts, Topology];

    /// <summary>Escopos completos para análise de promoção e comparação cross-environment.</summary>
    public static readonly IReadOnlyList<string> FullAnalysisScopes =
        [Telemetry, Incidents, Changes, Contracts, Topology, Runbooks, CrossEnvironmentComparison, PromotionAnalysis];
}
