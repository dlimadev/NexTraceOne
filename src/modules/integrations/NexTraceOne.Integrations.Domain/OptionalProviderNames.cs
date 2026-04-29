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

    /// <summary>Runtime intelligence agent (CLR profiler, eBPF, OpenTelemetry runtime); DEG-03.</summary>
    public const string Runtime = "runtime";

    /// <summary>Chaos engineering engine (Litmus, Chaos Mesh, Gremlin); DEG-04.</summary>
    public const string Chaos = "chaos";

    /// <summary>PKI certificate manager (cert-manager, Vault PKI, AWS ACM); DEG-05.</summary>
    public const string Certificate = "certificate";

    /// <summary>Multi-tenant schema planner IaC executor (Terraform, Pulumi, Flyway); DEG-06.</summary>
    public const string SchemaPlanner = "schemaPlanner";
}
