using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que representa a associação entre um papel e uma permissão,
/// permitindo personalização de autorização por tenant.
/// Substitui os mapeamentos estáticos de <see cref="RolePermissionCatalog"/>
/// por registos persistidos em base de dados, viabilizando customização
/// granular sem necessidade de redeploy.
/// </summary>
public sealed class RolePermission : Entity<RolePermissionId>
{
    private RolePermission() { }

    /// <summary>Identificador do papel associado.</summary>
    public RoleId RoleId { get; private set; } = default!;

    /// <summary>Código da permissão no formato módulo:recurso:ação (ex.: "identity:users:read").</summary>
    public string PermissionCode { get; private set; } = string.Empty;

    /// <summary>Identificador do tenant ao qual este mapeamento pertence, nulo para padrões do sistema.</summary>
    public TenantId? TenantId { get; private set; }

    /// <summary>Data e hora em que o mapeamento foi criado.</summary>
    public DateTimeOffset GrantedAt { get; private set; }

    /// <summary>Identificador de quem criou o mapeamento, quando disponível.</summary>
    public string? GrantedBy { get; private set; }

    /// <summary>Indica se o mapeamento está ativo. Permite desativação sem remoção física.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Cria um novo mapeamento papel→permissão.
    /// </summary>
    /// <param name="id">Identificador único do mapeamento.</param>
    /// <param name="roleId">Identificador do papel.</param>
    /// <param name="permissionCode">Código da permissão no formato módulo:recurso:ação.</param>
    /// <param name="tenantId">Tenant associado, nulo para mapeamentos padrão do sistema.</param>
    /// <param name="now">Data/hora UTC atual fornecida pelo IDateTimeProvider.</param>
    /// <param name="grantedBy">Identificador de quem concedeu o mapeamento.</param>
    public static RolePermission Create(
        RolePermissionId id,
        RoleId roleId,
        string permissionCode,
        TenantId? tenantId,
        DateTimeOffset now,
        string? grantedBy)
        => new()
        {
            Id = Guard.Against.Null(id),
            RoleId = Guard.Against.Null(roleId),
            PermissionCode = Guard.Against.NullOrWhiteSpace(permissionCode),
            TenantId = tenantId,
            GrantedAt = now,
            GrantedBy = grantedBy,
            IsActive = true
        };

    /// <summary>Desativa o mapeamento sem remoção física do registo.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reativa um mapeamento previamente desativado.</summary>
    public void Activate() => IsActive = true;
}

/// <summary>Identificador fortemente tipado de RolePermission.</summary>
public sealed record RolePermissionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RolePermissionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RolePermissionId From(Guid id) => new(id);
}
