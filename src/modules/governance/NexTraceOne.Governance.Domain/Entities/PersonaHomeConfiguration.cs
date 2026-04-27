using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para PersonaHomeConfiguration.</summary>
public sealed record PersonaHomeConfigurationId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Configuração persistida da home personalizada por utilizador e persona (V3.10).
/// Permite ao utilizador reorganizar e fixar os cards da sua home.
/// Cada configuração é per-user + per-persona; sistema providencia defaults quando não existe.
/// </summary>
public sealed class PersonaHomeConfiguration : Entity<PersonaHomeConfigurationId>
{
    /// <summary>Utilizador proprietário desta configuração.</summary>
    public string UserId { get; private init; } = string.Empty;

    /// <summary>Persona desta configuração (ex: "engineer", "tech-lead", "executive").</summary>
    public string Persona { get; private init; } = string.Empty;

    /// <summary>Tenant proprietário.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Layout de cards da home (JSON array de { cardKey, position, visible, pinnedDashboardId? }).</summary>
    public string CardLayoutJson { get; private set; } = "[]";

    /// <summary>Quick actions fixadas pelo utilizador (JSON array de { actionKey, label, url }).</summary>
    public string QuickActionsJson { get; private set; } = "[]";

    /// <summary>Escopo padrão do utilizador: teamId e serviceIds preferidos (JSON object).</summary>
    public string DefaultScopeJson { get; private set; } = "{}";

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última modificação.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    private PersonaHomeConfiguration() { }

    public static PersonaHomeConfiguration Create(
        string userId,
        string persona,
        string tenantId,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(persona, nameof(persona));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));

        return new PersonaHomeConfiguration
        {
            Id = new PersonaHomeConfigurationId(Guid.NewGuid()),
            UserId = userId,
            Persona = persona.ToLowerInvariant(),
            TenantId = tenantId,
            CardLayoutJson = "[]",
            QuickActionsJson = "[]",
            DefaultScopeJson = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateLayout(string cardLayoutJson, string quickActionsJson, string defaultScopeJson, DateTimeOffset now)
    {
        CardLayoutJson = cardLayoutJson;
        QuickActionsJson = quickActionsJson;
        DefaultScopeJson = defaultScopeJson;
        UpdatedAt = now;
    }
}
