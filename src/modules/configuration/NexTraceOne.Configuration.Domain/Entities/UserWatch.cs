using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Entidade que representa um item de watch list de um utilizador.
/// O utilizador pode seguir serviços, contratos, mudanças, incidentes ou runbooks.
/// Quando a entidade associada sofre alterações relevantes, o utilizador é notificado
/// conforme o nível configurado.
///
/// Invariantes:
/// - EntityType deve ser um dos valores suportados.
/// - NotifyLevel controla a granularidade das notificações (all, critical, none).
/// </summary>
public sealed class UserWatch : Entity<UserWatchId>
{
    private static readonly string[] ValidEntityTypes = ["service", "contract", "change", "incident", "runbook"];

    private UserWatch() { }

    /// <summary>Identificador do tenant.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Identificador do utilizador.</summary>
    public string UserId { get; private init; } = string.Empty;

    /// <summary>Tipo de entidade seguida (service, contract, change, incident, runbook).</summary>
    public string EntityType { get; private init; } = string.Empty;

    /// <summary>Identificador da entidade seguida.</summary>
    public string EntityId { get; private init; } = string.Empty;

    /// <summary>Nível de notificação: all, critical, none.</summary>
    public string NotifyLevel { get; private set; } = "all";

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última actualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Cria um novo watch para uma entidade.</summary>
    public static UserWatch Create(
        string userId,
        string tenantId,
        string entityType,
        string entityId,
        string notifyLevel,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(entityType, nameof(entityType));
        Guard.Against.NullOrWhiteSpace(entityId, nameof(entityId));

        if (!ValidEntityTypes.Contains(entityType.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"EntityType must be one of: {string.Join(", ", ValidEntityTypes)}", nameof(entityType));

        return new UserWatch
        {
            Id = new UserWatchId(Guid.NewGuid()),
            UserId = userId.Trim(),
            TenantId = tenantId.Trim(),
            EntityType = entityType.Trim().ToLowerInvariant(),
            EntityId = entityId.Trim(),
            NotifyLevel = NormalizeNotifyLevel(notifyLevel),
            CreatedAt = createdAt,
        };
    }

    /// <summary>Actualiza o nível de notificação.</summary>
    public void UpdateNotifyLevel(string notifyLevel, DateTimeOffset updatedAt)
    {
        NotifyLevel = NormalizeNotifyLevel(notifyLevel);
        UpdatedAt = updatedAt;
    }

    private static string NormalizeNotifyLevel(string? level) =>
        level?.Trim().ToLowerInvariant() switch
        {
            "all" or "critical" or "none" => level.Trim().ToLowerInvariant(),
            _ => "all"
        };
}
