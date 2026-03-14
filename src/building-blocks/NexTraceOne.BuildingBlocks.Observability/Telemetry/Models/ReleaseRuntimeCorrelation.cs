namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

/// <summary>
/// Correlação entre release/deployment e sinais de runtime.
/// Vincula mudanças no catálogo a impactos operacionais observados via telemetria.
///
/// Tabela-alvo: release_runtime_correlation (Product Store — PostgreSQL).
///
/// É o coração da inteligência de mudança (Change Intelligence):
/// permite responder "esta release causou degradação?" cruzando
/// timestamps de deploy com desvios de baseline de telemetria.
///
/// Alimenta: Módulo 10 (Runtime Intelligence), Módulo 12 (AI Orchestration),
/// Módulo 14 (Audit/Traceability), blast radius pós-deploy.
/// </summary>
public sealed record ReleaseRuntimeCorrelation
{
    /// <summary>Identificador único da correlação.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>ID da release no módulo ChangeGovernance.</summary>
    public required Guid ReleaseId { get; init; }

    /// <summary>ID do serviço afetado.</summary>
    public required Guid ServiceId { get; init; }

    /// <summary>Nome do serviço afetado.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Ambiente onde o deploy ocorreu.</summary>
    public required string Environment { get; init; }

    /// <summary>Tenant ID para isolamento multi-tenant.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Timestamp do deploy/promoção.</summary>
    public required DateTimeOffset DeployedAt { get; init; }

    /// <summary>
    /// Tipo de marcador: "deployment", "promotion", "rollback", "canary", "feature_flag".
    /// </summary>
    public required string MarkerType { get; init; }

    // ── Métricas PRÉ-deploy (baseline) ───────────────────────────────

    /// <summary>Error rate percentual antes do deploy (janela configurável, ex: 30 min).</summary>
    public double PreDeployErrorRate { get; init; }

    /// <summary>Latência P95 antes do deploy em milissegundos.</summary>
    public double PreDeployLatencyP95Ms { get; init; }

    /// <summary>Throughput (req/min) antes do deploy.</summary>
    public double PreDeployRequestsPerMinute { get; init; }

    // ── Métricas PÓS-deploy ──────────────────────────────────────────

    /// <summary>Error rate percentual após o deploy (janela configurável, ex: 30 min).</summary>
    public double PostDeployErrorRate { get; init; }

    /// <summary>Latência P95 após o deploy em milissegundos.</summary>
    public double PostDeployLatencyP95Ms { get; init; }

    /// <summary>Throughput (req/min) após o deploy.</summary>
    public double PostDeployRequestsPerMinute { get; init; }

    // ── Análise de impacto ───────────────────────────────────────────

    /// <summary>
    /// Score de impacto do deploy (0.0 a 1.0).
    /// 0.0 = sem impacto detectado, 1.0 = impacto severo.
    /// Calculado a partir dos deltas entre pré e pós-deploy.
    /// </summary>
    public double ImpactScore { get; init; }

    /// <summary>
    /// Classificação do impacto: "none", "positive", "neutral", "degradation", "incident".
    /// </summary>
    public required string ImpactClassification { get; init; }

    /// <summary>
    /// Referências para traces/logs crus no Telemetry Store capturados
    /// durante a janela pós-deploy. Permite drill-down investigativo.
    /// </summary>
    public List<Guid> TelemetryReferenceIds { get; init; } = [];

    /// <summary>Timestamp de criação da correlação.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
