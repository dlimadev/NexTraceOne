using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que vincula um usuário a um papel dentro de um tenant.
///
/// Ao contrário do modelo anterior (TenantMembership com 1 role por user/tenant),
/// esta entidade permite múltiplos papéis por usuário por tenant.
/// Exemplo: um usuário pode ser Developer e Auditor no mesmo tenant.
///
/// As permissões efetivas do usuário são a UNIÃO das permissões de todos
/// os papéis ativos atribuídos no tenant.
///
/// Suporta atribuições temporais via ValidFrom/ValidUntil para cenários
/// como auditorias periódicas ou elevações temporárias de acesso.
///
/// Regras de negócio:
/// - Um usuário pode ter N papéis no mesmo tenant.
/// - O mesmo par (user, tenant, role) não pode ser duplicado (índice único).
/// - Atribuições com ValidUntil expirado são consideradas inativas.
/// - A trilha de auditoria (AssignedBy, AssignedAt) é sempre preenchida.
/// </summary>
public sealed class UserRoleAssignment : Entity<UserRoleAssignmentId>
{
    private UserRoleAssignment() { }

    /// <summary>Identificador do usuário associado.</summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>Identificador do tenant onde a atribuição é válida.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Identificador do papel atribuído.</summary>
    public RoleId RoleId { get; private set; } = null!;

    /// <summary>Data/hora UTC em que a atribuição foi criada.</summary>
    public DateTimeOffset AssignedAt { get; private set; }

    /// <summary>Identificador de quem realizou a atribuição (auditoria).</summary>
    public string AssignedBy { get; private set; } = string.Empty;

    /// <summary>Indica se a atribuição está ativa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Início da vigência da atribuição, nulo se imediata.
    /// Permite agendar atribuições futuras de forma governada.
    /// </summary>
    public DateTimeOffset? ValidFrom { get; private set; }

    /// <summary>
    /// Fim da vigência da atribuição, nulo se permanente.
    /// Permite atribuições temporais (ex.: auditor por 30 dias).
    /// </summary>
    public DateTimeOffset? ValidUntil { get; private set; }

    /// <summary>Concurrency token (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria uma nova atribuição de papel a um usuário em um tenant.
    /// </summary>
    /// <param name="userId">Usuário que receberá o papel.</param>
    /// <param name="tenantId">Tenant onde o papel será válido.</param>
    /// <param name="roleId">Papel a ser atribuído.</param>
    /// <param name="assignedAt">Data/hora UTC da atribuição.</param>
    /// <param name="assignedBy">Identificador de quem atribuiu (auditoria).</param>
    /// <param name="validFrom">Início da vigência, nulo para imediata.</param>
    /// <param name="validUntil">Fim da vigência, nulo para permanente.</param>
    public static UserRoleAssignment Create(
        UserId userId,
        TenantId tenantId,
        RoleId roleId,
        DateTimeOffset assignedAt,
        string assignedBy,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validUntil = null)
    {
        Guard.Against.Null(userId, message: "UserId is required.");
        Guard.Against.Null(tenantId, message: "TenantId is required.");
        Guard.Against.Null(roleId, message: "RoleId is required.");
        Guard.Against.NullOrWhiteSpace(assignedBy, message: "AssignedBy is required for audit trail.");

        if (validFrom.HasValue && validUntil.HasValue && validUntil.Value <= validFrom.Value)
            throw new ArgumentException("ValidUntil must be after ValidFrom.");

        return new UserRoleAssignment
        {
            Id = UserRoleAssignmentId.New(),
            UserId = userId,
            TenantId = tenantId,
            RoleId = roleId,
            AssignedAt = assignedAt,
            AssignedBy = assignedBy,
            IsActive = true,
            ValidFrom = validFrom,
            ValidUntil = validUntil
        };
    }

    /// <summary>
    /// Verifica se a atribuição está efetivamente ativa considerando vigência temporal.
    /// </summary>
    /// <param name="now">Data/hora UTC atual.</param>
    /// <returns>True se a atribuição está ativa e dentro da vigência.</returns>
    public bool IsEffectivelyActive(DateTimeOffset now)
    {
        if (!IsActive) return false;
        if (ValidFrom.HasValue && now < ValidFrom.Value) return false;
        if (ValidUntil.HasValue && now >= ValidUntil.Value) return false;
        return true;
    }

    /// <summary>Desativa a atribuição sem remoção física.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reativa uma atribuição previamente desativada.</summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// Atualiza o período de vigência da atribuição.
    /// Permite estender ou restringir atribuições temporais.
    /// </summary>
    public void UpdateValidity(DateTimeOffset? validFrom, DateTimeOffset? validUntil)
    {
        if (validFrom.HasValue && validUntil.HasValue && validUntil.Value <= validFrom.Value)
            throw new ArgumentException("ValidUntil must be after ValidFrom.");

        ValidFrom = validFrom;
        ValidUntil = validUntil;
    }
}

/// <summary>Identificador fortemente tipado de UserRoleAssignment.</summary>
public sealed record UserRoleAssignmentId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static UserRoleAssignmentId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static UserRoleAssignmentId From(Guid id) => new(id);
}
