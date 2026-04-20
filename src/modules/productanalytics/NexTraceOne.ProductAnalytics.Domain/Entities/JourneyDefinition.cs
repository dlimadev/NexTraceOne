using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Domain.Entities;

/// <summary>Identificador fortemente tipado para JourneyDefinition.</summary>
public sealed record JourneyDefinitionId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Define uma jornada ou funil configurável do produto.
/// Permite que administradores do tenant personalizem os fluxos de valor
/// sem necessidade de redeploy — substituindo as definições estáticas em GetJourneys.
///
/// Scope: null tenant_id = definição global (padrão da plataforma)
///        tenant_id preenchido = personalização para aquele tenant.
/// </summary>
public sealed class JourneyDefinition : Entity<JourneyDefinitionId>
{
    private JourneyDefinition() { }

    /// <summary>Tenant dono desta definição. Null = global/default da plataforma.</summary>
    public Guid? TenantId { get; private init; }

    /// <summary>Chave única da jornada (ex: "search_to_entity").</summary>
    public string Key { get; private init; } = string.Empty;

    /// <summary>Nome legível da jornada (ex: "Search to Entity View").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Steps da jornada serializados como JSON.</summary>
    public string StepsJson { get; private set; } = "[]";

    /// <summary>Indica se esta definição está activa.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Data de criação (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data da última actualização (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Cria uma nova definição de jornada.</summary>
    public static JourneyDefinition Create(
        Guid? tenantId,
        string key,
        string name,
        string stepsJson,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(key);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(stepsJson);

        return new JourneyDefinition
        {
            Id = new JourneyDefinitionId(Guid.NewGuid()),
            TenantId = tenantId,
            Key = key.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            StepsJson = stepsJson,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Actualiza nome e steps da jornada.</summary>
    public void Update(string name, string stepsJson, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(stepsJson);
        Name = name.Trim();
        StepsJson = stepsJson;
        UpdatedAt = now;
    }

    /// <summary>Desactiva esta definição de jornada.</summary>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>Reactiva esta definição de jornada.</summary>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }
}
