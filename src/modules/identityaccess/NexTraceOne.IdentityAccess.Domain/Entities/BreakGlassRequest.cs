using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Aggregate Root que representa uma solicitação de acesso emergencial (Break Glass).
///
/// Fluxo:
/// 1. Usuário solicita acesso emergencial com justificativa obrigatória.
/// 2. Sistema concede acesso imediato sem aprovação prévia.
/// 3. Notificação instantânea para admins e supervisores.
/// 4. Janela de acesso padrão de 2 horas (configurável).
/// 5. Post-mortem obrigatório em até 24 horas.
/// 6. Trilha imutável de todas as ações realizadas durante o período.
///
/// Regras de segurança:
/// - Máximo 3 usos por usuário por trimestre antes de escalar para revisão obrigatória.
/// - Toda ação realizada sob Break Glass é marcada com o BreakGlassRequestId.
/// - O escopo concedido não é irrestrito — aplica-se o perfil PlatformAdmin temporariamente.
/// - A revogação pode ser manual (admin) ou automática (expiração).
/// </summary>
public sealed class BreakGlassRequest : AggregateRoot<BreakGlassRequestId>
{
    /// <summary>Duração padrão da janela de acesso emergencial.</summary>
    public static readonly TimeSpan DefaultAccessWindow = TimeSpan.FromHours(2);

    /// <summary>Prazo padrão para post-mortem obrigatório após o encerramento.</summary>
    public static readonly TimeSpan DefaultPostMortemDeadline = TimeSpan.FromHours(24);

    /// <summary>Limite trimestral de usos por usuário antes de escalar para revisão.</summary>
    public const int QuarterlyUsageLimit = 3;

    private BreakGlassRequest() { }

    /// <summary>Usuário que solicitou o acesso emergencial.</summary>
    public UserId RequestedBy { get; private set; } = null!;

    /// <summary>Tenant no qual o acesso emergencial foi solicitado.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Justificativa obrigatória do motivo do acesso emergencial.</summary>
    public string Justification { get; private set; } = string.Empty;

    /// <summary>Estado atual da solicitação.</summary>
    public BreakGlassStatus Status { get; private set; }

    /// <summary>Data/hora UTC da solicitação.</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Data/hora UTC de ativação do acesso.</summary>
    public DateTimeOffset? ActivatedAt { get; private set; }

    /// <summary>Data/hora UTC de expiração automática do acesso.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>Data/hora UTC de revogação (manual ou automática).</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Usuário que revogou manualmente, quando aplicável.</summary>
    public UserId? RevokedBy { get; private set; }

    /// <summary>Conteúdo do post-mortem registrado após encerramento.</summary>
    public string? PostMortemNotes { get; private set; }

    /// <summary>Data/hora UTC do registro do post-mortem.</summary>
    public DateTimeOffset? PostMortemAt { get; private set; }

    /// <summary>IP de origem da solicitação.</summary>
    public string IpAddress { get; private set; } = string.Empty;

    /// <summary>User agent de origem da solicitação.</summary>
    public string UserAgent { get; private set; } = string.Empty;

    /// <summary>Solicita acesso emergencial. O acesso é ativado imediatamente.</summary>
    public static BreakGlassRequest Create(
        UserId requestedBy,
        TenantId tenantId,
        string justification,
        string ipAddress,
        string userAgent,
        DateTimeOffset now,
        TimeSpan? accessWindow = null)
    {
        Guard.Against.Null(requestedBy);
        Guard.Against.Null(tenantId);
        Guard.Against.NullOrWhiteSpace(justification);

        var window = accessWindow ?? DefaultAccessWindow;

        return new BreakGlassRequest
        {
            Id = BreakGlassRequestId.New(),
            RequestedBy = requestedBy,
            TenantId = tenantId,
            Justification = justification,
            Status = BreakGlassStatus.Active,
            RequestedAt = now,
            ActivatedAt = now,
            ExpiresAt = now.Add(window),
            IpAddress = ipAddress ?? "unknown",
            UserAgent = userAgent ?? "unknown"
        };
    }

    /// <summary>Revoga manualmente o acesso emergencial antes da expiração.</summary>
    public void Revoke(UserId revokedBy, DateTimeOffset now)
    {
        Guard.Against.Null(revokedBy);

        if (Status != BreakGlassStatus.Active)
            return;

        Status = BreakGlassStatus.Revoked;
        RevokedAt = now;
        RevokedBy = revokedBy;
    }

    /// <summary>Marca o acesso como expirado automaticamente.</summary>
    public void Expire(DateTimeOffset now)
    {
        if (Status != BreakGlassStatus.Active)
            return;

        Status = BreakGlassStatus.Expired;
        RevokedAt = now;
    }

    /// <summary>Registra o post-mortem obrigatório após encerramento do acesso.</summary>
    public void RecordPostMortem(string notes, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(notes);

        PostMortemNotes = notes;
        PostMortemAt = now;
        Status = BreakGlassStatus.PostMortemCompleted;
    }

    /// <summary>Indica se o acesso emergencial está ativo na data informada.</summary>
    public bool IsActiveAt(DateTimeOffset now)
        => Status == BreakGlassStatus.Active && ExpiresAt.HasValue && ExpiresAt.Value > now;

    /// <summary>Indica se o post-mortem está pendente (acesso encerrado sem post-mortem).</summary>
    public bool IsPostMortemPending
        => Status is BreakGlassStatus.Expired or BreakGlassStatus.Revoked
           && PostMortemNotes is null;
}

/// <summary>Estados possíveis de uma solicitação Break Glass.</summary>
public enum BreakGlassStatus
{
    /// <summary>Acesso emergencial ativo.</summary>
    Active = 0,

    /// <summary>Acesso expirado automaticamente.</summary>
    Expired = 1,

    /// <summary>Acesso revogado manualmente por administrador.</summary>
    Revoked = 2,

    /// <summary>Post-mortem registrado após encerramento do acesso.</summary>
    PostMortemCompleted = 3
}

/// <summary>Identificador fortemente tipado de BreakGlassRequest.</summary>
public sealed record BreakGlassRequestId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static BreakGlassRequestId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static BreakGlassRequestId From(Guid id) => new(id);
}
