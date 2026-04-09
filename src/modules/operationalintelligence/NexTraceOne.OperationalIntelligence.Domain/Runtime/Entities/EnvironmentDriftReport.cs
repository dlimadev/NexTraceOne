using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

/// <summary>
/// Relatório de drift entre dois ambientes, com análise multi-dimensional.
/// Cobre: versões de serviço, configurações, contratos, dependências e políticas.
/// Cada relatório captura um snapshot comparativo num ponto no tempo.
///
/// Ciclo de vida: Generated → (Reviewed | Stale).
/// Fica Stale quando uma nova análise é gerada para os mesmos ambientes.
///
/// Invariantes:
/// - SourceEnvironment ≠ TargetEnvironment.
/// - Pelo menos uma dimensão analisada.
/// - TotalDriftItems >= CriticalDriftItems.
/// </summary>
public sealed class EnvironmentDriftReport : AuditableEntity<EnvironmentDriftReportId>
{
    private EnvironmentDriftReport() { }

    /// <summary>Nome do ambiente de referência (ex: "production").</summary>
    public string SourceEnvironment { get; private set; } = string.Empty;

    /// <summary>Nome do ambiente comparado (ex: "staging").</summary>
    public string TargetEnvironment { get; private set; } = string.Empty;

    /// <summary>Dimensões analisadas (JSONB) — versões, configurações, contratos, dependências, políticas.</summary>
    public string AnalyzedDimensions { get; private set; } = string.Empty;

    /// <summary>Secção de drifts de versão de serviço encontrados (JSONB).</summary>
    public string? ServiceVersionDrifts { get; private set; }

    /// <summary>Secção de drifts de configuração encontrados (JSONB).</summary>
    public string? ConfigurationDrifts { get; private set; }

    /// <summary>Secção de drifts de versão de contrato encontrados (JSONB).</summary>
    public string? ContractVersionDrifts { get; private set; }

    /// <summary>Secção de drifts de dependências encontrados (JSONB).</summary>
    public string? DependencyDrifts { get; private set; }

    /// <summary>Secção de drifts de políticas encontrados (JSONB).</summary>
    public string? PolicyDrifts { get; private set; }

    /// <summary>Recomendações de correção geradas pela análise (JSONB).</summary>
    public string? Recommendations { get; private set; }

    /// <summary>Total de itens com drift detectado em todas as dimensões.</summary>
    public int TotalDriftItems { get; private set; }

    /// <summary>Total de itens com drift crítico (requerem ação imediata).</summary>
    public int CriticalDriftItems { get; private set; }

    /// <summary>Severidade geral do relatório, derivada dos drifts encontrados.</summary>
    public DriftSeverity OverallSeverity { get; private set; } = DriftSeverity.Low;

    /// <summary>Estado do relatório no ciclo de vida.</summary>
    public DriftReportStatus Status { get; private set; } = DriftReportStatus.Generated;

    /// <summary>Data/hora UTC em que o relatório foi gerado.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Data/hora UTC da última revisão (se aplicável).</summary>
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>Comentário de revisão do utilizador.</summary>
    public string? ReviewComment { get; private set; }

    /// <summary>Tenant ao qual pertence o relatório.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// Cria um novo relatório de drift entre ambientes.
    /// </summary>
    public static EnvironmentDriftReport Generate(
        string sourceEnvironment,
        string targetEnvironment,
        string analyzedDimensions,
        string? serviceVersionDrifts,
        string? configurationDrifts,
        string? contractVersionDrifts,
        string? dependencyDrifts,
        string? policyDrifts,
        string? recommendations,
        int totalDriftItems,
        int criticalDriftItems,
        DriftSeverity overallSeverity,
        Guid? tenantId,
        DateTimeOffset generatedAt)
    {
        Guard.Against.NullOrWhiteSpace(sourceEnvironment);
        Guard.Against.NullOrWhiteSpace(targetEnvironment);
        Guard.Against.NullOrWhiteSpace(analyzedDimensions);

        if (string.Equals(sourceEnvironment, targetEnvironment, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Source and target environments must be different.");

        if (totalDriftItems < 0)
            throw new ArgumentException("Total drift items cannot be negative.");

        if (criticalDriftItems < 0 || criticalDriftItems > totalDriftItems)
            throw new ArgumentException("Critical drift items must be between 0 and total drift items.");

        return new EnvironmentDriftReport
        {
            Id = EnvironmentDriftReportId.New(),
            SourceEnvironment = sourceEnvironment,
            TargetEnvironment = targetEnvironment,
            AnalyzedDimensions = analyzedDimensions,
            ServiceVersionDrifts = serviceVersionDrifts,
            ConfigurationDrifts = configurationDrifts,
            ContractVersionDrifts = contractVersionDrifts,
            DependencyDrifts = dependencyDrifts,
            PolicyDrifts = policyDrifts,
            Recommendations = recommendations,
            TotalDriftItems = totalDriftItems,
            CriticalDriftItems = criticalDriftItems,
            OverallSeverity = overallSeverity,
            Status = DriftReportStatus.Generated,
            TenantId = tenantId,
            GeneratedAt = generatedAt
        };
    }

    /// <summary>
    /// Marca o relatório como revisado pelo utilizador.
    /// </summary>
    public Result<Unit> Review(string comment, DateTimeOffset reviewedAt)
    {
        Guard.Against.NullOrWhiteSpace(comment);

        if (Status == DriftReportStatus.Reviewed)
            return RuntimeIntelligenceErrors.DriftReportAlreadyReviewed(Id.Value.ToString());

        Status = DriftReportStatus.Reviewed;
        ReviewComment = comment;
        ReviewedAt = reviewedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Marca o relatório como stale (substituído por análise mais recente).
    /// </summary>
    public Result<Unit> MarkAsStale()
    {
        if (Status == DriftReportStatus.Stale)
            return RuntimeIntelligenceErrors.DriftReportAlreadyStale(Id.Value.ToString());

        Status = DriftReportStatus.Stale;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de EnvironmentDriftReport.</summary>
public sealed record EnvironmentDriftReportId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static EnvironmentDriftReportId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static EnvironmentDriftReportId From(Guid id) => new(id);
}
