using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Aggregate Root que representa uma delegação formal de permissões entre usuários.
///
/// Fluxo:
/// 1. Delegante (e.g., João Silva, TechLead) delega permissões específicas ao delegatário.
/// 2. Delegatário (e.g., Maria Costa) recebe permissões temporárias com escopo definido.
/// 3. Vigência definida com data de início e fim.
/// 4. Revogação automática no prazo ou manual antecipada.
/// 5. Toda ação do delegatário é registrada como "acting on behalf of [delegante]".
///
/// Limitações obrigatórias:
/// - Não é permitido delegar permissão que o delegante não possui.
/// - Não é permitido delegar permissão de administração de sistema (PlatformAdmin).
/// - Auditoria completa do ciclo de delegação.
/// </summary>
public sealed class Delegation : AggregateRoot<DelegationId>
{
    private readonly List<string> _delegatedPermissions = [];

    private Delegation() { }

    /// <summary>Usuário que delega (concede) as permissões.</summary>
    public UserId GrantorId { get; private set; } = null!;

    /// <summary>Usuário que recebe as permissões delegadas.</summary>
    public UserId DelegateeId { get; private set; } = null!;

    /// <summary>Tenant no qual a delegação é válida.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>
    /// Lista de permissões delegadas (códigos do catálogo).
    /// Exemplo: ["workflow:approve", "promotion:promote"].
    /// </summary>
    public IReadOnlyList<string> DelegatedPermissions => _delegatedPermissions.AsReadOnly();

    /// <summary>Razão/contexto da delegação, para auditoria.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Estado atual da delegação.</summary>
    public DelegationStatus Status { get; private set; }

    /// <summary>Data/hora UTC de início da vigência.</summary>
    public DateTimeOffset ValidFrom { get; private set; }

    /// <summary>Data/hora UTC de fim da vigência.</summary>
    public DateTimeOffset ValidUntil { get; private set; }

    /// <summary>Data/hora UTC de criação da delegação.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora UTC de revogação, quando aplicável.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Usuário que revogou a delegação (pode ser o delegante ou admin).</summary>
    public UserId? RevokedBy { get; private set; }

    /// <summary>
    /// Cria uma nova delegação formal.
    /// O chamador deve validar que o delegante possui as permissões sendo delegadas
    /// e que nenhuma delas é do escopo de administração de sistema.
    /// </summary>
    public static Delegation Create(
        UserId grantorId,
        UserId delegateeId,
        TenantId tenantId,
        IReadOnlyList<string> permissions,
        string reason,
        DateTimeOffset validFrom,
        DateTimeOffset validUntil,
        DateTimeOffset now)
    {
        Guard.Against.Null(grantorId);
        Guard.Against.Null(delegateeId);
        Guard.Against.Null(tenantId);
        Guard.Against.NullOrWhiteSpace(reason);

        if (grantorId == delegateeId)
            throw new InvalidOperationException("A user cannot delegate permissions to themselves.");

        if (validUntil <= validFrom)
            throw new InvalidOperationException("Delegation 'validUntil' must be after 'validFrom'.");

        if (permissions is null || permissions.Count == 0)
            throw new InvalidOperationException("At least one permission must be delegated.");

        var delegation = new Delegation
        {
            Id = DelegationId.New(),
            GrantorId = grantorId,
            DelegateeId = delegateeId,
            TenantId = tenantId,
            Reason = reason,
            Status = DelegationStatus.Active,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            CreatedAt = now
        };

        delegation._delegatedPermissions.AddRange(permissions);

        return delegation;
    }

    /// <summary>Revoga a delegação antecipadamente.</summary>
    public void Revoke(UserId revokedBy, DateTimeOffset now)
    {
        Guard.Against.Null(revokedBy);

        if (Status != DelegationStatus.Active)
            return;

        Status = DelegationStatus.Revoked;
        RevokedBy = revokedBy;
        RevokedAt = now;
    }

    /// <summary>Marca a delegação como expirada automaticamente.</summary>
    public void Expire(DateTimeOffset now)
    {
        if (Status != DelegationStatus.Active)
            return;

        if (now >= ValidUntil)
        {
            Status = DelegationStatus.Expired;
        }
    }

    /// <summary>Indica se a delegação está ativa na data informada.</summary>
    public bool IsActiveAt(DateTimeOffset now)
        => Status == DelegationStatus.Active && now >= ValidFrom && now < ValidUntil;
}

/// <summary>Estados possíveis de uma delegação.</summary>
public enum DelegationStatus
{
    /// <summary>Delegação ativa e dentro da vigência.</summary>
    Active = 0,

    /// <summary>Delegação expirada automaticamente.</summary>
    Expired = 1,

    /// <summary>Delegação revogada manualmente.</summary>
    Revoked = 2
}

/// <summary>Identificador fortemente tipado de Delegation.</summary>
public sealed record DelegationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static DelegationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static DelegationId From(Guid id) => new(id);
}
