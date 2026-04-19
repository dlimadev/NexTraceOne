using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para DemoSeedState.</summary>
public sealed record DemoSeedStateId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Estado persistido do seed de dados de demonstração.
/// Substitui o campo estático em memória para suportar múltiplas instâncias.
/// </summary>
public sealed class DemoSeedState : Entity<DemoSeedStateId>
{
    private DemoSeedState() { }

    /// <summary>Estado: NotSeeded | Seeding | Seeded | Failed | Cleared.</summary>
    public string State { get; private set; } = "NotSeeded";

    /// <summary>Data/hora em que o seed foi executado.</summary>
    public DateTimeOffset? SeededAt { get; private set; }

    /// <summary>Número de entidades inseridas durante o seed.</summary>
    public int EntitiesCount { get; private set; }

    /// <summary>Identificador do tenant.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Data de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data da última atualização.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Cria um registo de estado de seed para um tenant.</summary>
    public static DemoSeedState Create(Guid? tenantId, DateTimeOffset now)
        => new()
        {
            Id = new DemoSeedStateId(Guid.NewGuid()),
            State = "NotSeeded",
            TenantId = tenantId,
            CreatedAt = now,
            UpdatedAt = now
        };

    /// <summary>Marca o seed como em execução.</summary>
    public void MarkSeeding(DateTimeOffset now) { State = "Seeding"; UpdatedAt = now; }

    /// <summary>Marca o seed como concluído.</summary>
    public void MarkSeeded(int entitiesCount, DateTimeOffset now)
    {
        Guard.Against.Negative(entitiesCount);
        State = "Seeded";
        SeededAt = now;
        EntitiesCount = entitiesCount;
        UpdatedAt = now;
    }

    /// <summary>Marca o seed como falhado.</summary>
    public void MarkFailed(DateTimeOffset now) { State = "Failed"; UpdatedAt = now; }

    /// <summary>Limpa o estado do seed.</summary>
    public void Clear(DateTimeOffset now) { State = "NotSeeded"; SeededAt = null; EntitiesCount = 0; UpdatedAt = now; }
}
