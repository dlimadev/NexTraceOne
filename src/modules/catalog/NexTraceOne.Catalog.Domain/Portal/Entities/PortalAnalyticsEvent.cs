using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.DeveloperPortal.Domain.Entities;

/// <summary>
/// Entidade que regista um evento de analytics gerado pela utilização do portal do desenvolvedor.
/// Captura ações como pesquisas, visualizações de API, execuções no playground, gerações de código
/// e fluxos de onboarding. Serve como base para dashboards de adoção, métricas de engagement
/// e identificação de APIs com baixa descoberta ou alto abandono.
/// </summary>
public sealed class PortalAnalyticsEvent : Entity<PortalAnalyticsEventId>
{
    private PortalAnalyticsEvent() { }

    /// <summary>Identificador do utilizador que gerou o evento, ou null para eventos anónimos.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Tipo do evento de analytics (Search, ApiView, PlaygroundExecution, etc.).</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>Identificador opcional da entidade relacionada ao evento (ex: ApiAssetId).</summary>
    public string? EntityId { get; private set; }

    /// <summary>Tipo da entidade relacionada ao evento (ex: "ApiAsset", "Contract").</summary>
    public string? EntityType { get; private set; }

    /// <summary>Query de pesquisa utilizada, quando o evento é do tipo Search.</summary>
    public string? SearchQuery { get; private set; }

    /// <summary>Indica se a pesquisa retornou zero resultados, para análise de lacunas no catálogo.</summary>
    public bool? ZeroResults { get; private set; }

    /// <summary>Duração da ação em milissegundos, quando aplicável.</summary>
    public long? DurationMs { get; private set; }

    /// <summary>Metadados adicionais serializados como JSON (filtros, contexto, user-agent, etc.).</summary>
    public string? Metadata { get; private set; }

    /// <summary>Data/hora UTC em que o evento ocorreu.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Cria um novo evento de analytics do portal do desenvolvedor.
    /// O tipo do evento é obrigatório; os demais campos são opcionais conforme o contexto.
    /// </summary>
    public static PortalAnalyticsEvent Create(
        Guid? userId,
        string eventType,
        string? entityId,
        string? entityType,
        string? searchQuery,
        bool? zeroResults,
        long? durationMs,
        string? metadata,
        DateTimeOffset occurredAt)
    {
        Guard.Against.NullOrWhiteSpace(eventType);

        return new PortalAnalyticsEvent
        {
            Id = PortalAnalyticsEventId.New(),
            UserId = userId,
            EventType = eventType,
            EntityId = entityId,
            EntityType = entityType,
            SearchQuery = searchQuery,
            ZeroResults = zeroResults,
            DurationMs = durationMs,
            Metadata = metadata,
            OccurredAt = occurredAt
        };
    }
}

/// <summary>Identificador fortemente tipado de PortalAnalyticsEvent.</summary>
public sealed record PortalAnalyticsEventId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PortalAnalyticsEventId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PortalAnalyticsEventId From(Guid id) => new(id);
}
