namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

/// <summary>
/// Contexto investigativo que agrupa dados de telemetria relevantes
/// para uma investigação de incidente, anomalia ou mudança.
///
/// Tabela-alvo: investigation_context (Product Store — PostgreSQL).
///
/// O investigation context é um "bundle" que a IA e analistas humanos usam
/// para entender o que aconteceu. Contém referências para métricas, anomalias,
/// correlações e ponteiros para traces/logs crus no Telemetry Store.
///
/// Não armazena dados crus — apenas referências e metadados para navegação.
/// Alimenta: Módulo 12 (AI Orchestration), Módulo 14 (Audit/Traceability).
/// </summary>
public sealed record InvestigationContext
{
    /// <summary>Identificador único do contexto investigativo.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Título descritivo da investigação (inglês, para logs e IA).</summary>
    public required string Title { get; init; }

    /// <summary>Chave i18n do título para exibição no frontend.</summary>
    public string? TitleMessageKey { get; init; }

    /// <summary>
    /// Tipo de investigação: "anomaly", "deployment_impact", "incident",
    /// "drift_analysis", "cost_anomaly", "security_event".
    /// </summary>
    public required string InvestigationType { get; init; }

    /// <summary>Identificador do serviço principal sob investigação.</summary>
    public required Guid PrimaryServiceId { get; init; }

    /// <summary>Nome do serviço principal.</summary>
    public required string PrimaryServiceName { get; init; }

    /// <summary>Ambiente.</summary>
    public required string Environment { get; init; }

    /// <summary>Tenant ID para isolamento multi-tenant.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Início da janela temporal da investigação.</summary>
    public required DateTimeOffset TimeWindowStart { get; init; }

    /// <summary>Fim da janela temporal da investigação.</summary>
    public required DateTimeOffset TimeWindowEnd { get; init; }

    // ── Referências para dados relacionados ──────────────────────────

    /// <summary>IDs de anomalias correlacionadas neste contexto.</summary>
    public List<Guid> AnomalySnapshotIds { get; init; } = [];

    /// <summary>IDs de correlações release/runtime neste contexto.</summary>
    public List<Guid> ReleaseCorrelationIds { get; init; } = [];

    /// <summary>IDs de referências para traces/logs crus no Telemetry Store.</summary>
    public List<Guid> TelemetryReferenceIds { get; init; } = [];

    /// <summary>IDs de serviços afetados (blast radius observado).</summary>
    public List<Guid> AffectedServiceIds { get; init; } = [];

    // ── Sumário para consumo por IA ──────────────────────────────────

    /// <summary>
    /// Sumário estruturado do contexto para consumo pelo módulo AI Orchestration.
    /// Formato JSON com: métricas-chave, anomalias, serviços afetados, timeline.
    /// Limitado a 4KB para eficiência de prompt/context window.
    /// </summary>
    public string? AiSummaryJson { get; init; }

    /// <summary>
    /// Status da investigação: "open", "in_progress", "resolved", "dismissed".
    /// </summary>
    public required string Status { get; init; } = "open";

    /// <summary>Timestamp de criação do contexto investigativo.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Timestamp da última atualização.</summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
