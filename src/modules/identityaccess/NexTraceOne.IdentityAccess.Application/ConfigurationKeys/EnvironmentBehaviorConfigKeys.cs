namespace NexTraceOne.IdentityAccess.Application.ConfigurationKeys;

/// <summary>
/// Constantes para as chaves de configuração de comportamento por ambiente.
/// Estes parâmetros controlam o comportamento operacional da plataforma
/// de acordo com o ambiente (Development, Staging, Production, etc.)
/// cadastrado pelo utilizador.
///
/// Regra arquitetural: configurações de IA NÃO fazem parte deste conjunto.
/// IA é parametrizada ao nível de System e Tenant — o ambiente é passado
/// como contexto de execução, mas não altera o modelo, budget ou políticas de IA.
/// </summary>
public static class EnvironmentBehaviorConfigKeys
{
    // ── Notificações ───────────────────────────────────────────────────────

    /// <summary>
    /// Habilita o envio de notificações para canais externos (e-mail, Teams)
    /// neste ambiente. Por padrão desabilitado em ambientes não produtivos.
    /// </summary>
    public const string NotificationsExternalChannelsEnabled = "env.behavior.notifications.external_channels.enabled";

    /// <summary>
    /// Severidade mínima para enviar notificações externas neste ambiente.
    /// Valores: Info, ActionRequired, Warning, Critical.
    /// Ambientes não produtivos tipicamente usam Warning ou Critical.
    /// </summary>
    public const string NotificationsMinimumSeverity = "env.behavior.notifications.minimum_severity";

    // ── Métricas e DORA ────────────────────────────────────────────────────

    /// <summary>
    /// Habilita o cálculo de métricas DORA (Deployment Frequency, Lead Time,
    /// Change Failure Rate, MTTR) para este ambiente.
    /// </summary>
    public const string MetricsDoraEnabled = "env.behavior.metrics.dora.enabled";

    /// <summary>
    /// Habilita o cálculo e rastreamento de SLOs neste ambiente.
    /// </summary>
    public const string MetricsSloEnabled = "env.behavior.metrics.slo.enabled";

    /// <summary>
    /// Habilita o scoring de confiança em mudanças (Change Confidence Score)
    /// para este ambiente.
    /// </summary>
    public const string MetricsChangeConfidenceEnabled = "env.behavior.metrics.change_confidence.enabled";

    /// <summary>
    /// Habilita a análise de blast radius para mudanças neste ambiente.
    /// </summary>
    public const string MetricsBlastRadiusEnabled = "env.behavior.metrics.blast_radius.enabled";

    // ── Background Jobs ────────────────────────────────────────────────────

    /// <summary>
    /// Habilita o job de retenção de telemetria para este ambiente.
    /// Quando desabilitado, a telemetria não é expirada automaticamente.
    /// </summary>
    public const string JobsTelemetryRetentionEnabled = "env.behavior.jobs.telemetry_retention.enabled";

    /// <summary>
    /// Habilita a geração de relatórios agendados para este ambiente.
    /// Ambientes não produtivos tipicamente têm este job desabilitado.
    /// </summary>
    public const string JobsScheduledReportsEnabled = "env.behavior.jobs.scheduled_reports.enabled";

    /// <summary>
    /// Habilita o processamento de expiração de waivers de governança
    /// para este ambiente.
    /// </summary>
    public const string JobsGovernanceWaiverExpiryEnabled = "env.behavior.jobs.governance_waiver_expiry.enabled";

    /// <summary>
    /// Indica se o scheduler de ambientes não produtivos (ex: suspensão nocturna, limpeza de dados,
    /// rotação de segredos) está habilitado para este ambiente.
    /// Deve ser false em ambientes de produção.
    /// </summary>
    public const string JobsNonProdSchedulerEnabled = "env.behavior.jobs.non_prod_scheduler.enabled";

    // ── Retenção de Dados ──────────────────────────────────────────────────

    /// <summary>
    /// Número de dias para reter dados de telemetria neste ambiente.
    /// Ambientes de desenvolvimento tipicamente têm retenção menor.
    /// </summary>
    public const string DataTelemetryRetentionDays = "env.behavior.data.telemetry_retention_days";

    /// <summary>
    /// Número de dias para reter histórico de mudanças neste ambiente.
    /// </summary>
    public const string DataChangeHistoryDays = "env.behavior.data.change_history_days";

    /// <summary>
    /// Número de dias para reter histórico de incidentes neste ambiente.
    /// </summary>
    public const string DataIncidentHistoryDays = "env.behavior.data.incident_history_days";

    // ── Alertas Operacionais ───────────────────────────────────────────────

    /// <summary>
    /// Habilita alertas automáticos de violação de SLO neste ambiente.
    /// </summary>
    public const string AlertsSloBreachEnabled = "env.behavior.alerts.slo_breach.enabled";

    /// <summary>
    /// Habilita a detecção de anomalias e criação automática de incidentes
    /// neste ambiente.
    /// </summary>
    public const string AlertsAnomalyDetectionEnabled = "env.behavior.alerts.anomaly_detection.enabled";

    // ── Webhooks e Integrações Externas ────────────────────────────────────

    /// <summary>
    /// Habilita o disparo de webhooks de saída para sistemas externos
    /// neste ambiente. Tipicamente desabilitado em ambientes não produtivos.
    /// </summary>
    public const string WebhooksOutboundEnabled = "env.behavior.webhooks.outbound.enabled";

    // ── Pipeline de Mudanças ───────────────────────────────────────────────

    /// <summary>
    /// Habilita a aplicação de gates de promoção (Promotion Gates) para
    /// mudanças neste ambiente. Controla se as validações de promoção são
    /// aplicadas ou apenas registradas.
    /// </summary>
    public const string ChangePromotionGatesEnabled = "env.behavior.change.promotion_gates.enabled";

    /// <summary>
    /// Habilita a ingestão de eventos de deploy e change para este ambiente.
    /// Quando desabilitado, eventos de ingestão são ignorados.
    /// </summary>
    public const string ChangeIngestEnabled = "env.behavior.change.ingest.enabled";

    /// <summary>
    /// Habilita a verificação pós-mudança automática neste ambiente.
    /// </summary>
    public const string ChangePostChangeVerificationEnabled = "env.behavior.change.post_change_verification.enabled";
}
