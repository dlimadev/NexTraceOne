namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Serviço de governança do catálogo de notificações.
/// Phase 7 — controla tipos, templates, canais e regras da plataforma.
/// </summary>
public interface INotificationCatalogGovernance
{
    /// <summary>
    /// Obtém o sumário de governança do catálogo de notificações.
    /// Inclui: tipos registados, cobertura de templates, canais ativos, regras obrigatórias.
    /// </summary>
    Task<CatalogGovernanceSummary> GetGovernanceSummaryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida se um tipo de notificação tem toda a cobertura necessária
    /// (template, categoria, severidade, canais permitidos).
    /// </summary>
    Task<CatalogValidationResult> ValidateEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Sumário de governança do catálogo de notificações.
/// </summary>
public sealed record CatalogGovernanceSummary
{
    /// <summary>Total de tipos de notificação registados.</summary>
    public int TotalEventTypes { get; init; }

    /// <summary>Tipos com template completo.</summary>
    public int TypesWithTemplate { get; init; }

    /// <summary>Tipos sem template (gaps).</summary>
    public IReadOnlyList<string> TypesWithoutTemplate { get; init; } = [];

    /// <summary>Tipos marcados como obrigatórios (mandatory).</summary>
    public int MandatoryTypes { get; init; }

    /// <summary>Canais configurados e seu estado.</summary>
    public IReadOnlyDictionary<string, bool> ChannelStatus { get; init; } = new Dictionary<string, bool>();

    /// <summary>Total de categorias em uso.</summary>
    public int TotalCategories { get; init; }

    /// <summary>Data do último levantamento.</summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Resultado da validação de um tipo de notificação.
/// </summary>
public sealed record CatalogValidationResult
{
    /// <summary>Se a validação passou (sem gaps).</summary>
    public bool IsValid { get; init; }

    /// <summary>Tipo de evento validado.</summary>
    public required string EventType { get; init; }

    /// <summary>Se tem template registado.</summary>
    public bool HasTemplate { get; init; }

    /// <summary>Se é tipo obrigatório.</summary>
    public bool IsMandatory { get; init; }

    /// <summary>Mensagens de validação (warnings/errors).</summary>
    public IReadOnlyList<string> Messages { get; init; } = [];
}
