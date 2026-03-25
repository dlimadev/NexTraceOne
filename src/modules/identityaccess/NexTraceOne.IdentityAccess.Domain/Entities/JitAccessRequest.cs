using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Aggregate Root que representa uma solicitação de acesso privilegiado temporário (Just-in-Time).
///
/// Fluxo:
/// 1. Usuário solicita permissão específica para ação específica, com justificativa.
/// 2. Sistema valida elegibilidade do solicitante.
/// 3. Aprovador confirma em prazo configurável (padrão: 4 horas).
/// 4. Permissão concedida por tempo limitado (padrão: 8 horas).
/// 5. Permissão revogada automaticamente ao fim do prazo.
/// 6. AuditEvent completo do ciclo de vida.
///
/// Regras:
/// - Escopo deve ser específico (permissão concreta, não genérica).
/// - Aprovação por responsável apropriado — não auto-aprovação.
/// - Expiração automática obrigatória.
/// - Revogação manual antecipada permitida.
/// - Trilha completa auditável.
/// </summary>
public sealed class JitAccessRequest : AggregateRoot<JitAccessRequestId>
{
    /// <summary>Tempo padrão de espera pela aprovação.</summary>
    public static readonly TimeSpan DefaultApprovalTimeout = TimeSpan.FromHours(4);

    /// <summary>Duração padrão do acesso após aprovação.</summary>
    public static readonly TimeSpan DefaultAccessDuration = TimeSpan.FromHours(8);

    private JitAccessRequest() { }

    /// <summary>Usuário que solicita o acesso privilegiado.</summary>
    public UserId RequestedBy { get; private set; } = null!;

    /// <summary>Tenant no qual o acesso é solicitado.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>
    /// Código da permissão solicitada (e.g., "promotion:promote", "workflow:approve").
    /// Deve ser uma permissão válida do catálogo.
    /// </summary>
    public string PermissionCode { get; private set; } = string.Empty;

    /// <summary>
    /// Escopo ou recurso específico ao qual a permissão se aplica.
    /// Exemplo: "Release 'API Pagamentos v2.3' → Promoção para Production".
    /// </summary>
    public string Scope { get; private set; } = string.Empty;

    /// <summary>Justificativa obrigatória do motivo da solicitação.</summary>
    public string Justification { get; private set; } = string.Empty;

    /// <summary>Estado atual da solicitação.</summary>
    public JitAccessStatus Status { get; private set; }

    /// <summary>Data/hora UTC da solicitação.</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Data/hora limite para aprovação antes de expirar automaticamente.</summary>
    public DateTimeOffset ApprovalDeadline { get; private set; }

    /// <summary>Usuário que aprovou ou rejeitou a solicitação.</summary>
    public UserId? DecidedBy { get; private set; }

    /// <summary>Data/hora UTC da decisão (aprovação ou rejeição).</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>Motivo da rejeição, quando aplicável.</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>Data/hora UTC de início do acesso concedido.</summary>
    public DateTimeOffset? GrantedFrom { get; private set; }

    /// <summary>Data/hora UTC de fim do acesso concedido.</summary>
    public DateTimeOffset? GrantedUntil { get; private set; }

    /// <summary>Data/hora UTC de revogação antecipada, quando aplicável.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Usuário que revogou antecipadamente, quando aplicável.</summary>
    public UserId? RevokedBy { get; private set; }

    /// <summary>Concurrency token (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Cria uma nova solicitação de acesso JIT.</summary>
    public static JitAccessRequest Create(
        UserId requestedBy,
        TenantId tenantId,
        string permissionCode,
        string scope,
        string justification,
        DateTimeOffset now,
        TimeSpan? approvalTimeout = null)
    {
        Guard.Against.Null(requestedBy);
        Guard.Against.Null(tenantId);
        Guard.Against.NullOrWhiteSpace(permissionCode);
        Guard.Against.NullOrWhiteSpace(scope);
        Guard.Against.NullOrWhiteSpace(justification);

        return new JitAccessRequest
        {
            Id = JitAccessRequestId.New(),
            RequestedBy = requestedBy,
            TenantId = tenantId,
            PermissionCode = permissionCode,
            Scope = scope,
            Justification = justification,
            Status = JitAccessStatus.Pending,
            RequestedAt = now,
            ApprovalDeadline = now.Add(approvalTimeout ?? DefaultApprovalTimeout)
        };
    }

    /// <summary>Aprova a solicitação e concede acesso temporário.</summary>
    public void Approve(UserId approvedBy, DateTimeOffset now, TimeSpan? accessDuration = null)
    {
        Guard.Against.Null(approvedBy);

        if (Status != JitAccessStatus.Pending)
            return;

        // Não permite auto-aprovação
        if (approvedBy == RequestedBy)
            return;

        var duration = accessDuration ?? DefaultAccessDuration;

        Status = JitAccessStatus.Approved;
        DecidedBy = approvedBy;
        DecidedAt = now;
        GrantedFrom = now;
        GrantedUntil = now.Add(duration);
    }

    /// <summary>Rejeita a solicitação com motivo obrigatório.</summary>
    public void Reject(UserId rejectedBy, string reason, DateTimeOffset now)
    {
        Guard.Against.Null(rejectedBy);
        Guard.Against.NullOrWhiteSpace(reason);

        if (Status != JitAccessStatus.Pending)
            return;

        Status = JitAccessStatus.Rejected;
        DecidedBy = rejectedBy;
        DecidedAt = now;
        RejectionReason = reason;
    }

    /// <summary>Revoga antecipadamente o acesso já concedido.</summary>
    public void Revoke(UserId revokedBy, DateTimeOffset now)
    {
        Guard.Against.Null(revokedBy);

        if (Status != JitAccessStatus.Approved)
            return;

        Status = JitAccessStatus.Revoked;
        RevokedBy = revokedBy;
        RevokedAt = now;
    }

    /// <summary>Marca o acesso como expirado automaticamente.</summary>
    public void Expire(DateTimeOffset now)
    {
        if (Status == JitAccessStatus.Pending && now > ApprovalDeadline)
        {
            Status = JitAccessStatus.Expired;
            return;
        }

        if (Status == JitAccessStatus.Approved && GrantedUntil.HasValue && now > GrantedUntil.Value)
        {
            Status = JitAccessStatus.Expired;
        }
    }

    /// <summary>Indica se o acesso JIT está ativo na data informada.</summary>
    public bool IsAccessActiveAt(DateTimeOffset now)
        => Status == JitAccessStatus.Approved
           && GrantedFrom.HasValue && GrantedFrom.Value <= now
           && GrantedUntil.HasValue && GrantedUntil.Value > now;
}

/// <summary>Estados possíveis de uma solicitação JIT.</summary>
public enum JitAccessStatus
{
    /// <summary>Aguardando aprovação.</summary>
    Pending = 0,

    /// <summary>Aprovada com acesso temporário ativo.</summary>
    Approved = 1,

    /// <summary>Rejeitada pelo aprovador.</summary>
    Rejected = 2,

    /// <summary>Expirada (sem aprovação no prazo ou acesso encerrado).</summary>
    Expired = 3,

    /// <summary>Revogada antecipadamente.</summary>
    Revoked = 4
}

/// <summary>Identificador fortemente tipado de JitAccessRequest.</summary>
public sealed record JitAccessRequestId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static JitAccessRequestId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static JitAccessRequestId From(Guid id) => new(id);
}
