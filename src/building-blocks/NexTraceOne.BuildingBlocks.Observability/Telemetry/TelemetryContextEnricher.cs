using System.Diagnostics;
using NexTraceOne.BuildingBlocks.Observability.Tracing;

namespace NexTraceOne.BuildingBlocks.Observability.Telemetry;

/// <summary>
/// Enriquece Activities (spans OpenTelemetry) com atributos de contexto distribuído.
///
/// Adiciona como tags nas Activities:
/// - nexttrace.tenant_id: identificador do tenant (se disponível)
/// - nexttrace.environment_id: identificador do ambiente (se disponível)
/// - nexttrace.correlation_id: identificador de correlação distribuída
/// - nexttrace.service_origin: serviço de origem da operação
///
/// PADRÃO DE NOMENCLATURA:
/// Prefixo "nexttrace." para todos os atributos NexTraceOne — evita colisão com
/// atributos padrão OpenTelemetry (http.*, db.*, cloud.*, etc.).
///
/// USO:
/// Chamado pelos pipeline behaviors e handlers após popular o contexto.
/// Não deve ser chamado diretamente no código de domínio.
/// </summary>
public static class TelemetryContextEnricher
{
    // ── Nomes dos atributos ──────────────────────────────────────────────

    /// <summary>Atributo com o TenantId do contexto operacional.</summary>
    public const string TenantIdAttribute = "nexttrace.tenant_id";

    /// <summary>Atributo com o EnvironmentId do contexto operacional.</summary>
    public const string EnvironmentIdAttribute = "nexttrace.environment_id";

    /// <summary>Atributo indicando se o ambiente é similar à produção.</summary>
    public const string IsProductionLikeAttribute = "nexttrace.environment.is_production_like";

    /// <summary>Atributo com o CorrelationId distribuído.</summary>
    public const string CorrelationIdAttribute = "nexttrace.correlation_id";

    /// <summary>Atributo com o nome do serviço/módulo de origem.</summary>
    public const string ServiceOriginAttribute = "nexttrace.service_origin";

    /// <summary>Atributo com o UserId autenticado.</summary>
    public const string UserIdAttribute = "nexttrace.user_id";

    // ── Métodos de enriquecimento ────────────────────────────────────────

    /// <summary>
    /// Enriquece a Activity atual com atributos de contexto distribuído.
    /// É seguro chamar quando Activity.Current é null — retorna sem fazer nada.
    /// </summary>
    public static void EnrichCurrentActivity(
        Guid? tenantId,
        Guid? environmentId,
        bool? isProductionLike = null,
        string? correlationId = null,
        string? serviceOrigin = null,
        string? userId = null)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        if (tenantId.HasValue && tenantId != Guid.Empty)
            activity.SetTag(TenantIdAttribute, tenantId.Value.ToString());

        if (environmentId.HasValue && environmentId != Guid.Empty)
            activity.SetTag(EnvironmentIdAttribute, environmentId.Value.ToString());

        if (isProductionLike.HasValue)
            activity.SetTag(IsProductionLikeAttribute, isProductionLike.Value.ToString().ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(correlationId))
            activity.SetTag(CorrelationIdAttribute, correlationId);

        if (!string.IsNullOrWhiteSpace(serviceOrigin))
            activity.SetTag(ServiceOriginAttribute, serviceOrigin);

        if (!string.IsNullOrWhiteSpace(userId))
            activity.SetTag(UserIdAttribute, userId);
    }

    /// <summary>
    /// Cria um novo span filho com atributos de contexto distribuído preenchidos.
    /// Encapsulamento conveniente para não repetir código de enriquecimento.
    /// </summary>
    public static Activity? StartEnrichedActivity(
        ActivitySource source,
        string operationName,
        Guid? tenantId,
        Guid? environmentId,
        string? correlationId = null,
        string? serviceOrigin = null,
        ActivityKind kind = ActivityKind.Internal)
    {
        var activity = source.StartActivity(operationName, kind);
        if (activity is null)
            return null;

        if (tenantId.HasValue && tenantId != Guid.Empty)
            activity.SetTag(TenantIdAttribute, tenantId.Value.ToString());

        if (environmentId.HasValue && environmentId != Guid.Empty)
            activity.SetTag(EnvironmentIdAttribute, environmentId.Value.ToString());

        if (!string.IsNullOrWhiteSpace(correlationId))
            activity.SetTag(CorrelationIdAttribute, correlationId);

        if (!string.IsNullOrWhiteSpace(serviceOrigin))
            activity.SetTag(ServiceOriginAttribute, serviceOrigin);

        return activity;
    }
}
