using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

/// <summary>
/// Relatório de resiliência gerado após um experimento de chaos engineering.
/// Compara o blast radius teórico vs impacto real, regista observações de
/// telemetria durante o experimento e produz um score de resiliência (0–100).
///
/// Ciclo de vida: Generated → Reviewed → Archived.
/// Também permite: Generated → Archived (sem revisão prévia).
///
/// Invariantes:
/// - ChaosExperimentId não pode ser vazio.
/// - ServiceName e Environment não podem ser nulos ou vazios.
/// - ExperimentType não pode ser nulo ou vazio.
/// - ResilienceScore deve estar entre 0 e 100.
/// - TenantId não pode ser nulo ou vazio.
/// </summary>
public sealed class ResilienceReport : AuditableEntity<ResilienceReportId>
{
    private const int MaxServiceNameLength = 200;
    private const int MaxEnvironmentLength = 100;
    private const int MaxExperimentTypeLength = 100;
    private const int MaxReviewCommentLength = 2000;
    private const int MaxUserIdLength = 200;

    private ResilienceReport() { }

    /// <summary>Identificador do experimento de chaos associado.</summary>
    public Guid ChaosExperimentId { get; private set; }

    /// <summary>Nome do serviço alvo do experimento.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente onde o experimento foi executado.</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Tipo de experimento de chaos (latency-injection, pod-kill, etc.).</summary>
    public string ExperimentType { get; private set; } = string.Empty;

    /// <summary>Score geral de resiliência (0–100).</summary>
    public int ResilienceScore { get; private set; }

    /// <summary>Blast radius teórico esperado antes do experimento (JSONB).</summary>
    public string? TheoreticalBlastRadius { get; private set; }

    /// <summary>Blast radius real observado durante o experimento (JSONB).</summary>
    public string? ActualBlastRadius { get; private set; }

    /// <summary>Desvio percentual entre blast radius teórico e real.</summary>
    public decimal? BlastRadiusDeviation { get; private set; }

    /// <summary>Observações de telemetria durante o experimento (JSONB).</summary>
    public string? TelemetryObservations { get; private set; }

    /// <summary>Degradação de latência observada em milissegundos.</summary>
    public decimal? LatencyImpactMs { get; private set; }

    /// <summary>Variação na taxa de erro observada.</summary>
    public decimal? ErrorRateImpact { get; private set; }

    /// <summary>Tempo de recuperação após o experimento em segundos.</summary>
    public int? RecoveryTimeSeconds { get; private set; }

    /// <summary>Pontos fortes identificados no serviço (JSONB).</summary>
    public string? Strengths { get; private set; }

    /// <summary>Pontos fracos identificados no serviço (JSONB).</summary>
    public string? Weaknesses { get; private set; }

    /// <summary>Recomendações de melhoria (JSONB).</summary>
    public string? Recommendations { get; private set; }

    /// <summary>Estado do relatório no ciclo de vida.</summary>
    public ResilienceReportStatus Status { get; private set; }

    /// <summary>Identificador do utilizador que revisou o relatório.</summary>
    public string? ReviewedByUserId { get; private set; }

    /// <summary>Data/hora UTC da revisão.</summary>
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>Comentário de revisão.</summary>
    public string? ReviewComment { get; private set; }

    /// <summary>Data/hora UTC em que o relatório foi gerado.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Identificador do tenant.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Token de concorrência otimista.</summary>
    public uint RowVersion { get; private set; }

    /// <summary>
    /// Gera um novo relatório de resiliência a partir dos resultados de um experimento de chaos.
    /// </summary>
    public static ResilienceReport Generate(
        Guid chaosExperimentId,
        string serviceName,
        string environment,
        string experimentType,
        int resilienceScore,
        string? theoreticalBlastRadius,
        string? actualBlastRadius,
        decimal? blastRadiusDeviation,
        string? telemetryObservations,
        decimal? latencyImpactMs,
        decimal? errorRateImpact,
        int? recoveryTimeSeconds,
        string? strengths,
        string? weaknesses,
        string? recommendations,
        string tenantId,
        DateTimeOffset generatedAt)
    {
        Guard.Against.Default(chaosExperimentId, nameof(chaosExperimentId));
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.OutOfRange(serviceName.Length, nameof(serviceName), 1, MaxServiceNameLength);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.OutOfRange(environment.Length, nameof(environment), 1, MaxEnvironmentLength);
        Guard.Against.NullOrWhiteSpace(experimentType);
        Guard.Against.OutOfRange(experimentType.Length, nameof(experimentType), 1, MaxExperimentTypeLength);
        Guard.Against.OutOfRange(resilienceScore, nameof(resilienceScore), 0, 100);
        Guard.Against.NullOrWhiteSpace(tenantId);

        return new ResilienceReport
        {
            Id = ResilienceReportId.New(),
            ChaosExperimentId = chaosExperimentId,
            ServiceName = serviceName,
            Environment = environment,
            ExperimentType = experimentType,
            ResilienceScore = resilienceScore,
            TheoreticalBlastRadius = theoreticalBlastRadius,
            ActualBlastRadius = actualBlastRadius,
            BlastRadiusDeviation = blastRadiusDeviation,
            TelemetryObservations = telemetryObservations,
            LatencyImpactMs = latencyImpactMs,
            ErrorRateImpact = errorRateImpact,
            RecoveryTimeSeconds = recoveryTimeSeconds,
            Strengths = strengths,
            Weaknesses = weaknesses,
            Recommendations = recommendations,
            Status = ResilienceReportStatus.Generated,
            TenantId = tenantId,
            GeneratedAt = generatedAt
        };
    }

    /// <summary>
    /// Marca o relatório como revisado por um utilizador.
    /// Apenas relatórios no estado Generated podem ser revisados.
    /// </summary>
    public Result<Unit> Review(string userId, string comment, DateTimeOffset reviewedAt)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(comment);

        if (Status == ResilienceReportStatus.Reviewed)
            return RuntimeIntelligenceErrors.ResilienceReportAlreadyReviewed(Id.Value.ToString());

        if (Status == ResilienceReportStatus.Archived)
            return RuntimeIntelligenceErrors.ResilienceReportAlreadyArchived(Id.Value.ToString());

        Status = ResilienceReportStatus.Reviewed;
        ReviewedByUserId = userId;
        ReviewComment = comment;
        ReviewedAt = reviewedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Arquiva o relatório. Permitido a partir de Generated ou Reviewed.
    /// </summary>
    public Result<Unit> Archive()
    {
        if (Status == ResilienceReportStatus.Archived)
            return RuntimeIntelligenceErrors.ResilienceReportAlreadyArchived(Id.Value.ToString());

        Status = ResilienceReportStatus.Archived;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de ResilienceReport.</summary>
public sealed record ResilienceReportId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ResilienceReportId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ResilienceReportId From(Guid id) => new(id);
}
