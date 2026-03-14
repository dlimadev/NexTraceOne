using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.RuntimeIntelligence.Domain.Enums;
using NexTraceOne.RuntimeIntelligence.Domain.Errors;

namespace NexTraceOne.RuntimeIntelligence.Domain.Entities;

/// <summary>
/// Entidade que representa um drift (desvio) detectado entre a baseline esperada
/// e os valores reais de runtime de um serviço. Cada finding corresponde a uma métrica
/// específica que ultrapassou o limiar de tolerância, com severidade calculada
/// automaticamente pelo percentual de desvio.
///
/// Ciclo de vida: Detected → (Acknowledged | Resolved).
/// Pode ser opcionalmente correlacionado a uma release para análise de impacto.
///
/// Invariantes:
/// - Severidade é derivada do desvio percentual — não pode ser definida externamente.
/// - Acknowledge e Resolve são idempotent-safe (retornam erro em duplicidade).
/// - Resolução requer justificativa (comentário obrigatório).
/// </summary>
public sealed class DriftFinding : AuditableEntity<DriftFindingId>
{
    /// <summary>Limiar de desvio percentual para severidade Medium (10%).</summary>
    private const decimal MediumDeviationThreshold = 10m;

    /// <summary>Limiar de desvio percentual para severidade High (25%).</summary>
    private const decimal HighDeviationThreshold = 25m;

    /// <summary>Limiar de desvio percentual para severidade Critical (50%).</summary>
    private const decimal CriticalDeviationThreshold = 50m;

    private DriftFinding() { }

    /// <summary>Nome do serviço onde o drift foi detectado.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente onde o drift foi detectado (dev, staging, prod).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Nome da métrica que apresentou desvio (ex: "AvgLatencyMs", "ErrorRate").</summary>
    public string MetricName { get; private set; } = string.Empty;

    /// <summary>Valor esperado da métrica conforme a baseline.</summary>
    public decimal ExpectedValue { get; private set; }

    /// <summary>Valor real observado da métrica no snapshot.</summary>
    public decimal ActualValue { get; private set; }

    /// <summary>Percentual de desvio entre o valor real e o esperado.</summary>
    public decimal DeviationPercent { get; private set; }

    /// <summary>Severidade do drift, calculada automaticamente com base no percentual de desvio.</summary>
    public DriftSeverity Severity { get; private set; } = DriftSeverity.Low;

    /// <summary>Data/hora UTC em que o drift foi detectado.</summary>
    public DateTimeOffset DetectedAt { get; private set; }

    /// <summary>
    /// Identificador opcional da release correlacionada ao drift.
    /// Permite análise de impacto de deploys na saúde do serviço.
    /// </summary>
    public Guid? ReleaseId { get; private set; }

    /// <summary>Indica se o drift foi reconhecido/aceito pela equipe responsável.</summary>
    public bool IsAcknowledged { get; private set; }

    /// <summary>Indica se o drift foi resolvido (remediado ou aceito como novo normal).</summary>
    public bool IsResolved { get; private set; }

    /// <summary>Comentário de resolução fornecido pela equipe ao resolver o drift.</summary>
    public string? ResolutionComment { get; private set; }

    /// <summary>Data/hora UTC em que o drift foi resolvido. Null se ainda aberto.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Indica se o drift ainda está aberto (nem reconhecido, nem resolvido).</summary>
    public bool IsOpen => !IsAcknowledged && !IsResolved;

    /// <summary>
    /// Detecta e registra um novo drift finding com validações de guarda.
    /// Calcula automaticamente o percentual de desvio e a severidade correspondente.
    /// REGRA DDD: A severidade é encapsulada — calculada apenas pelo factory method.
    /// </summary>
    public static DriftFinding Detect(
        string serviceName,
        string environment,
        string metricName,
        decimal expectedValue,
        decimal actualValue,
        DateTimeOffset detectedAt,
        Guid? releaseId = null)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.NullOrWhiteSpace(metricName);

        var deviationPercent = expectedValue == 0m
            ? (actualValue == 0m ? 0m : 100m)
            : Math.Abs(actualValue - expectedValue) / Math.Abs(expectedValue) * 100m;

        var finding = new DriftFinding
        {
            Id = DriftFindingId.New(),
            ServiceName = serviceName,
            Environment = environment,
            MetricName = metricName,
            ExpectedValue = expectedValue,
            ActualValue = actualValue,
            DeviationPercent = Math.Round(deviationPercent, 2),
            DetectedAt = detectedAt,
            ReleaseId = releaseId,
            IsAcknowledged = false,
            IsResolved = false
        };

        finding.DeriveServerity();

        return finding;
    }

    /// <summary>
    /// Marca o drift finding como reconhecido pela equipe.
    /// Retorna erro de conflito se já foi reconhecido ou resolvido.
    /// </summary>
    public Result<Unit> Acknowledge()
    {
        if (IsAcknowledged)
            return RuntimeIntelligenceErrors.AlreadyAcknowledged(Id.Value.ToString());

        if (IsResolved)
            return RuntimeIntelligenceErrors.AlreadyAcknowledged(Id.Value.ToString());

        IsAcknowledged = true;
        return Unit.Value;
    }

    /// <summary>
    /// Resolve o drift finding com justificativa obrigatória.
    /// Registra a data de resolução e o comentário explicativo.
    /// Retorna erro se já estiver resolvido.
    /// </summary>
    public Result<Unit> Resolve(string resolutionComment, DateTimeOffset resolvedAt)
    {
        Guard.Against.NullOrWhiteSpace(resolutionComment);

        if (IsResolved)
            return RuntimeIntelligenceErrors.AlreadyAcknowledged(Id.Value.ToString());

        IsResolved = true;
        IsAcknowledged = true;
        ResolutionComment = resolutionComment;
        ResolvedAt = resolvedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Correlaciona este drift finding com uma release específica.
    /// Permite análise de impacto pós-deploy — verificar se a release causou o desvio.
    /// Retorna erro se já estiver correlacionado com outra release.
    /// </summary>
    public Result<Unit> CorrelateWithRelease(Guid releaseId)
    {
        Guard.Against.Default(releaseId);

        if (ReleaseId.HasValue)
            return RuntimeIntelligenceErrors.DriftNotFound(Id.Value.ToString());

        ReleaseId = releaseId;
        return Unit.Value;
    }

    /// <summary>
    /// Calcula a severidade do drift com base no percentual de desvio.
    /// Encapsulado: chamado apenas pelo factory method para garantir invariante.
    /// </summary>
    private void DeriveServerity()
    {
        Severity = DeviationPercent switch
        {
            >= CriticalDeviationThreshold => DriftSeverity.Critical,
            >= HighDeviationThreshold => DriftSeverity.High,
            >= MediumDeviationThreshold => DriftSeverity.Medium,
            _ => DriftSeverity.Low
        };
    }
}

/// <summary>Identificador fortemente tipado de DriftFinding.</summary>
public sealed record DriftFindingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static DriftFindingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static DriftFindingId From(Guid id) => new(id);
}
