using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.RuntimeIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root que representa o perfil de maturidade de observabilidade de um serviço.
/// Avalia a presença de capacidades essenciais (tracing, metrics, logging, alerting, dashboard)
/// e calcula um score ponderado de 0 a 1 que reflete o nível de observabilidade do serviço.
/// Utilizado para identificar gaps de observabilidade e priorizar investimentos em monitoramento.
/// </summary>
public sealed class ObservabilityProfile : AuditableEntity<ObservabilityProfileId>
{
    /// <summary>Peso do tracing distribuído no cálculo do score de observabilidade.</summary>
    private const decimal TracingWeight = 0.25m;

    /// <summary>Peso das métricas no cálculo do score de observabilidade.</summary>
    private const decimal MetricsWeight = 0.25m;

    /// <summary>Peso do logging estruturado no cálculo do score de observabilidade.</summary>
    private const decimal LoggingWeight = 0.20m;

    /// <summary>Peso do alerting no cálculo do score de observabilidade.</summary>
    private const decimal AlertingWeight = 0.15m;

    /// <summary>Peso dos dashboards no cálculo do score de observabilidade.</summary>
    private const decimal DashboardWeight = 0.15m;

    private ObservabilityProfile() { }

    /// <summary>Nome do serviço avaliado.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente avaliado (dev, staging, prod).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Indica se o serviço possui tracing distribuído configurado.</summary>
    public bool HasTracing { get; private set; }

    /// <summary>Indica se o serviço possui coleta de métricas configurada.</summary>
    public bool HasMetrics { get; private set; }

    /// <summary>Indica se o serviço possui logging estruturado configurado.</summary>
    public bool HasLogging { get; private set; }

    /// <summary>Indica se o serviço possui alerting configurado.</summary>
    public bool HasAlerting { get; private set; }

    /// <summary>Indica se o serviço possui dashboard de monitoramento configurado.</summary>
    public bool HasDashboard { get; private set; }

    /// <summary>
    /// Score de observabilidade calculado como soma ponderada das capacidades presentes.
    /// Varia entre 0 (nenhuma capacidade) e 1 (todas as capacidades presentes).
    /// Pesos: tracing=0.25, metrics=0.25, logging=0.20, alerting=0.15, dashboard=0.15.
    /// </summary>
    public decimal ObservabilityScore { get; private set; }

    /// <summary>Data/hora UTC da última avaliação de observabilidade.</summary>
    public DateTimeOffset LastAssessedAt { get; private set; }

    /// <summary>
    /// Indica se o serviço possui observabilidade mínima adequada (score ≥ 0.6).
    /// Serviços abaixo deste limiar têm dívida de observabilidade significativa.
    /// </summary>
    public bool HasAdequateObservability => ObservabilityScore >= 0.60m;

    /// <summary>
    /// Conta o número de capacidades de observabilidade presentes.
    /// </summary>
    public int CapabilityCount =>
        (HasTracing ? 1 : 0) + (HasMetrics ? 1 : 0) + (HasLogging ? 1 : 0) +
        (HasAlerting ? 1 : 0) + (HasDashboard ? 1 : 0);

    /// <summary>
    /// Cria uma nova avaliação de perfil de observabilidade para um serviço e ambiente.
    /// Calcula o score automaticamente — encapsulado no factory method.
    /// </summary>
    public static ObservabilityProfile Assess(
        string serviceName,
        string environment,
        bool hasTracing,
        bool hasMetrics,
        bool hasLogging,
        bool hasAlerting,
        bool hasDashboard,
        DateTimeOffset assessedAt)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);

        var profile = new ObservabilityProfile
        {
            Id = ObservabilityProfileId.New(),
            ServiceName = serviceName,
            Environment = environment,
            HasTracing = hasTracing,
            HasMetrics = hasMetrics,
            HasLogging = hasLogging,
            HasAlerting = hasAlerting,
            HasDashboard = hasDashboard,
            LastAssessedAt = assessedAt
        };

        profile.DeriveScore();

        return profile;
    }

    /// <summary>
    /// Atualiza as capacidades de observabilidade do serviço e recalcula o score.
    /// Permite reavaliação incremental conforme novas capacidades são adicionadas.
    /// </summary>
    public void UpdateCapabilities(
        bool hasTracing,
        bool hasMetrics,
        bool hasLogging,
        bool hasAlerting,
        bool hasDashboard,
        DateTimeOffset assessedAt)
    {
        HasTracing = hasTracing;
        HasMetrics = hasMetrics;
        HasLogging = hasLogging;
        HasAlerting = hasAlerting;
        HasDashboard = hasDashboard;
        LastAssessedAt = assessedAt;
        DeriveScore();
    }

    /// <summary>
    /// Calcula o score de observabilidade como soma ponderada das capacidades presentes.
    /// Encapsulado: chamado pelo factory e pelo UpdateCapabilities para garantir consistência.
    /// </summary>
    private void DeriveScore()
    {
        var score = 0m;

        if (HasTracing) score += TracingWeight;
        if (HasMetrics) score += MetricsWeight;
        if (HasLogging) score += LoggingWeight;
        if (HasAlerting) score += AlertingWeight;
        if (HasDashboard) score += DashboardWeight;

        ObservabilityScore = Math.Round(score, 2);
    }
}

/// <summary>Identificador fortemente tipado de ObservabilityProfile.</summary>
public sealed record ObservabilityProfileId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ObservabilityProfileId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ObservabilityProfileId From(Guid id) => new(id);
}
