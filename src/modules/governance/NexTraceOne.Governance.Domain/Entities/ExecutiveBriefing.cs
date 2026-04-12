using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Domain.Errors;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade ExecutiveBriefing.
/// Garante que nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record ExecutiveBriefingId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Briefing executivo gerado por IA com secções estruturadas sobre o estado da plataforma.
/// Secções: PlatformStatus, TopIncidents, TeamPerformance, HighRiskChanges,
/// ComplianceStatus, CostTrends, ActiveRisks — cada uma armazenada como JSONB.
///
/// Ciclo de vida: Draft → Published → Archived.
/// Entidade central do módulo Governance para comunicação executiva.
/// </summary>
public sealed class ExecutiveBriefing : Entity<ExecutiveBriefingId>
{
    /// <summary>Título do briefing (máx. 300 caracteres).</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Frequência de geração do briefing.</summary>
    public BriefingFrequency Frequency { get; private init; }

    /// <summary>Estado actual do briefing no ciclo de vida.</summary>
    public BriefingStatus Status { get; private set; }

    /// <summary>Início do período coberto pelo briefing.</summary>
    public DateTimeOffset PeriodStart { get; private init; }

    /// <summary>Fim do período coberto pelo briefing.</summary>
    public DateTimeOffset PeriodEnd { get; private init; }

    /// <summary>Sumário executivo geral (texto livre, opcional).</summary>
    public string? ExecutiveSummary { get; private set; }

    // ── Secções JSONB ──

    /// <summary>Secção: Estado geral da plataforma (JSON).</summary>
    public string? PlatformStatusSection { get; private set; }

    /// <summary>Secção: Principais incidentes no período (JSON).</summary>
    public string? TopIncidentsSection { get; private set; }

    /// <summary>Secção: Performance das equipas (JSON).</summary>
    public string? TeamPerformanceSection { get; private set; }

    /// <summary>Secção: Mudanças de alto risco (JSON).</summary>
    public string? HighRiskChangesSection { get; private set; }

    /// <summary>Secção: Estado de compliance (JSON).</summary>
    public string? ComplianceStatusSection { get; private set; }

    /// <summary>Secção: Tendências de custo (JSON).</summary>
    public string? CostTrendsSection { get; private set; }

    /// <summary>Secção: Riscos activos (JSON).</summary>
    public string? ActiveRisksSection { get; private set; }

    /// <summary>Data/hora UTC em que o briefing foi gerado.</summary>
    public DateTimeOffset GeneratedAt { get; private init; }

    /// <summary>Identificador do agente de IA que gerou o briefing (máx. 200 caracteres).</summary>
    public string GeneratedByAgent { get; private init; } = string.Empty;

    /// <summary>Data/hora UTC em que o briefing foi publicado (null se Draft).</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>Data/hora UTC em que o briefing foi arquivado (null se não arquivado).</summary>
    public DateTimeOffset? ArchivedAt { get; private set; }

    /// <summary>Identificador do tenant proprietário (nullable para multi-tenant).</summary>
    public string? TenantId { get; private init; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private ExecutiveBriefing() { }

    /// <summary>
    /// Gera um novo executive briefing em estado Draft.
    /// O briefing é criado com todas as secções populadas pelo agente de IA.
    /// </summary>
    /// <param name="title">Título do briefing (máx. 300 caracteres).</param>
    /// <param name="frequency">Frequência de geração.</param>
    /// <param name="periodStart">Início do período coberto.</param>
    /// <param name="periodEnd">Fim do período coberto.</param>
    /// <param name="executiveSummary">Sumário executivo geral (opcional).</param>
    /// <param name="platformStatusSection">Secção Platform Status (JSON).</param>
    /// <param name="topIncidentsSection">Secção Top Incidents (JSON).</param>
    /// <param name="teamPerformanceSection">Secção Team Performance (JSON).</param>
    /// <param name="highRiskChangesSection">Secção High Risk Changes (JSON).</param>
    /// <param name="complianceStatusSection">Secção Compliance Status (JSON).</param>
    /// <param name="costTrendsSection">Secção Cost Trends (JSON).</param>
    /// <param name="activeRisksSection">Secção Active Risks (JSON).</param>
    /// <param name="generatedByAgent">Identificador do agente IA gerador.</param>
    /// <param name="tenantId">Identificador do tenant (opcional).</param>
    /// <param name="now">Data/hora UTC de geração.</param>
    /// <returns>Nova instância válida de ExecutiveBriefing em estado Draft.</returns>
    public static ExecutiveBriefing Generate(
        string title,
        BriefingFrequency frequency,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string? executiveSummary,
        string? platformStatusSection,
        string? topIncidentsSection,
        string? teamPerformanceSection,
        string? highRiskChangesSection,
        string? complianceStatusSection,
        string? costTrendsSection,
        string? activeRisksSection,
        string generatedByAgent,
        string? tenantId,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.StringTooLong(title, 300, nameof(title));
        Guard.Against.NullOrWhiteSpace(generatedByAgent, nameof(generatedByAgent));
        Guard.Against.StringTooLong(generatedByAgent, 200, nameof(generatedByAgent));

        return new ExecutiveBriefing
        {
            Id = new ExecutiveBriefingId(Guid.NewGuid()),
            Title = title.Trim(),
            Frequency = frequency,
            Status = BriefingStatus.Draft,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            ExecutiveSummary = executiveSummary?.Trim(),
            PlatformStatusSection = platformStatusSection,
            TopIncidentsSection = topIncidentsSection,
            TeamPerformanceSection = teamPerformanceSection,
            HighRiskChangesSection = highRiskChangesSection,
            ComplianceStatusSection = complianceStatusSection,
            CostTrendsSection = costTrendsSection,
            ActiveRisksSection = activeRisksSection,
            GeneratedAt = now,
            GeneratedByAgent = generatedByAgent.Trim(),
            TenantId = tenantId?.Trim(),
            PublishedAt = null,
            ArchivedAt = null
        };
    }

    /// <summary>
    /// Publica o briefing, tornando-o disponível para consulta executiva.
    /// Transição válida apenas de Draft para Published.
    /// </summary>
    /// <param name="now">Data/hora UTC da publicação.</param>
    /// <returns>Result indicando sucesso ou erro de transição inválida.</returns>
    public Result<Unit> Publish(DateTimeOffset now)
    {
        if (Status != BriefingStatus.Draft)
            return GovernanceBriefingErrors.InvalidTransition(Id.Value.ToString(), Status.ToString(), BriefingStatus.Published.ToString());

        Status = BriefingStatus.Published;
        PublishedAt = now;

        return Result<Unit>.Success(Unit.Value);
    }

    /// <summary>
    /// Arquiva o briefing, retirando-o da vista ativa mas preservando histórico.
    /// Transição válida apenas de Published para Archived.
    /// </summary>
    /// <param name="now">Data/hora UTC do arquivamento.</param>
    /// <returns>Result indicando sucesso ou erro de transição inválida.</returns>
    public Result<Unit> Archive(DateTimeOffset now)
    {
        if (Status != BriefingStatus.Published)
            return GovernanceBriefingErrors.InvalidTransition(Id.Value.ToString(), Status.ToString(), BriefingStatus.Archived.ToString());

        Status = BriefingStatus.Archived;
        ArchivedAt = now;

        return Result<Unit>.Success(Unit.Value);
    }
}
