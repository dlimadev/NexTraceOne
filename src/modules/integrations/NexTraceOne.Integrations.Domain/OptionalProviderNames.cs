namespace NexTraceOne.Integrations.Domain;

/// <summary>
/// Nomes canónicos dos providers opcionais da plataforma.
///
/// São usados como chave no payload de <c>GET /api/v1/platform/optional-providers</c>,
/// na página <c>SystemHealthPage</c> do frontend e nos logs de arranque do ApiHost.
/// Manter este único ficheiro como fonte da verdade evita drift entre a superfície de
/// diagnóstico (Governance.Application), o logger de arranque (ApiHost) e o i18n do frontend.
/// </summary>
public static class OptionalProviderNames
{
    /// <summary>Canary rollouts (Argo Rollouts, Flagger, LaunchDarkly, …).</summary>
    public const string Canary = "canary";

    /// <summary>Database backup posture (pg_dump, pgBackRest, Barman, Velero, …).</summary>
    public const string Backup = "backup";

    /// <summary>Kafka event producer; <c>NullKafkaEventProducer</c> descarta silenciosamente eventos quando ausente.</summary>
    public const string Kafka = "kafka";

    /// <summary>Cloud billing ingestion (AWS CUR, Azure Cost Management, GCP BigQuery Billing Export).</summary>
    public const string CloudBilling = "cloudBilling";

    /// <summary>SAML 2.0 SSO IdP integration; returns <c>SamlNotConfigured</c> error while absent.</summary>
    public const string Saml = "saml";
}
