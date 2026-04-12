using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Entidade que representa uma regra de alerta personalizada de um utilizador.
/// Permite que utilizadores definam condições para serem notificados sobre eventos
/// específicos na plataforma.
///
/// Invariantes:
/// - Nome não pode exceder 100 caracteres.
/// - Condition é JSON com entity, field, operator, value.
/// - Channel deve ser "in-app", "email" ou "webhook".
/// </summary>
public sealed class UserAlertRule : Entity<UserAlertRuleId>
{
    private const int MaxNameLength = 100;
    private static readonly string[] ValidChannels = ["in-app", "email", "webhook"];

    private UserAlertRule() { }

    /// <summary>Identificador do tenant.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Identificador do utilizador.</summary>
    public string UserId { get; private init; } = string.Empty;

    /// <summary>Nome descritivo da regra.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Condição em JSON: {"entity":"service","field":"risk_level","operator":">=","value":"high"}.</summary>
    public string Condition { get; private set; } = string.Empty;

    /// <summary>Canal de notificação: in-app, email, webhook.</summary>
    public string Channel { get; private set; } = "in-app";

    /// <summary>Indica se a regra está activa.</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última actualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Cria uma nova regra de alerta.</summary>
    public static UserAlertRule Create(
        string userId,
        string tenantId,
        string name,
        string condition,
        string channel,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Guard.Against.NullOrWhiteSpace(condition, nameof(condition));

        return new UserAlertRule
        {
            Id = new UserAlertRuleId(Guid.NewGuid()),
            UserId = userId.Trim(),
            TenantId = tenantId.Trim(),
            Name = name.Trim(),
            Condition = condition.Trim(),
            Channel = NormalizeChannel(channel),
            IsEnabled = true,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Actualiza os detalhes da regra.</summary>
    public void UpdateDetails(string name, string condition, string channel, DateTimeOffset updatedAt)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Guard.Against.NullOrWhiteSpace(condition, nameof(condition));

        Name = name.Trim();
        Condition = condition.Trim();
        Channel = NormalizeChannel(channel);
        UpdatedAt = updatedAt;
    }

    /// <summary>Alterna o estado activo/inactivo da regra.</summary>
    public void Toggle(bool enabled, DateTimeOffset updatedAt)
    {
        IsEnabled = enabled;
        UpdatedAt = updatedAt;
    }

    private static string NormalizeChannel(string? channel) =>
        ValidChannels.Contains(channel?.Trim().ToLower(), StringComparer.OrdinalIgnoreCase)
            ? channel!.Trim().ToLower()
            : "in-app";
}
