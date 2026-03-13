using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Entidade que vincula um usuário a um tenant e a um papel específico.
/// </summary>
public sealed class TenantMembership : Entity<TenantMembershipId>
{
    private TenantMembership() { }

    /// <summary>Usuário vinculado ao tenant.</summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>Tenant associado ao vínculo.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Papel do usuário dentro do tenant.</summary>
    public RoleId RoleId { get; private set; } = null!;

    /// <summary>Data/hora UTC em que o vínculo foi criado.</summary>
    public DateTimeOffset JoinedAt { get; private set; }

    /// <summary>Indica se o vínculo está ativo.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Cria um novo vínculo de usuário com tenant e papel.</summary>
    public static TenantMembership Create(UserId userId, TenantId tenantId, RoleId roleId, DateTimeOffset joinedAt)
        => new()
        {
            Id = TenantMembershipId.New(),
            UserId = Guard.Against.Null(userId),
            TenantId = Guard.Against.Null(tenantId),
            RoleId = Guard.Against.Null(roleId),
            JoinedAt = joinedAt,
            IsActive = true
        };

    /// <summary>Altera o papel associado ao vínculo.</summary>
    public void ChangeRole(RoleId roleId)
        => RoleId = Guard.Against.Null(roleId);

    /// <summary>Desativa o vínculo do usuário com o tenant.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reativa um vínculo previamente desativado.</summary>
    public void Activate() => IsActive = true;
}

/// <summary>Identificador fortemente tipado de TenantMembership.</summary>
public sealed record TenantMembershipId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static TenantMembershipId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static TenantMembershipId From(Guid id) => new(id);
}

/// <summary>Identificador fortemente tipado de Tenant.</summary>
public sealed record TenantId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static TenantId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static TenantId From(Guid id) => new(id);
}
