using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Entidade que representa um pedido de aprovação de release — interno ou externo.
///
/// Para aprovações externas, o NexTraceOne envia um webhook outbound com um
/// CallbackToken (UUID v4 armazenado em hash) e aguarda que o sistema
/// externo chame o endpoint de callback com a decisão (Approved/Rejected).
///
/// O token autentica a resposta do sistema externo sem necessitar de credenciais adicionais.
/// </summary>
public sealed class ReleaseApprovalRequest : Entity<ReleaseApprovalRequestId>
{
    private ReleaseApprovalRequest() { }

    /// <summary>Identificador da release a que este pedido de aprovação pertence.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Tipo de aprovação — Internal, ExternalWebhook, ServiceNow, AutoApprove.</summary>
    public ExternalApprovalType ApprovalType { get; private set; }

    /// <summary>Sistema externo responsável pela aprovação (ex: "ServiceNow", "Teams", "Slack").</summary>
    public string? ExternalSystem { get; private set; }

    /// <summary>ID da mudança no sistema externo (ex: CHG0012345 no ServiceNow).</summary>
    public string? ExternalRequestId { get; private set; }

    /// <summary>
    /// Token de callback único (UUID v4 em plain text apenas na criação).
    /// Armazenado em hash SHA-256 para segurança; o plain text é retornado apenas uma vez.
    /// </summary>
    public string CallbackTokenHash { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC de expiração do token de callback.</summary>
    public DateTimeOffset CallbackTokenExpiresAt { get; private set; }

    /// <summary>Estado actual do pedido de aprovação.</summary>
    public ApprovalRequestStatus Status { get; private set; } = ApprovalRequestStatus.Pending;

    /// <summary>Ambiente alvo da promoção que gerou este pedido.</summary>
    public string TargetEnvironment { get; private set; } = string.Empty;

    /// <summary>URL do webhook outbound para onde o pedido foi enviado.</summary>
    public string? OutboundWebhookUrl { get; private set; }

    /// <summary>Data/hora UTC em que o pedido foi criado.</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Data/hora UTC em que o pedido foi respondido (Approved/Rejected/Bypassed).</summary>
    public DateTimeOffset? RespondedAt { get; private set; }

    /// <summary>Utilizador ou sistema que respondeu ao pedido.</summary>
    public string? RespondedBy { get; private set; }

    /// <summary>Comentários do aprovador ou sistema externo.</summary>
    public string? Comments { get; private set; }

    /// <summary>
    /// Cria um novo ReleaseApprovalRequest.
    /// </summary>
    public static ReleaseApprovalRequest Create(
        Guid tenantId,
        ReleaseId releaseId,
        ExternalApprovalType approvalType,
        string targetEnvironment,
        string callbackTokenHash,
        DateTimeOffset callbackTokenExpiresAt,
        DateTimeOffset requestedAt,
        string? externalSystem = null,
        string? outboundWebhookUrl = null)
    {
        Guard.Against.Null(releaseId);
        Guard.Against.NullOrWhiteSpace(targetEnvironment);
        Guard.Against.NullOrWhiteSpace(callbackTokenHash);

        return new ReleaseApprovalRequest
        {
            Id = ReleaseApprovalRequestId.New(),
            TenantId = tenantId,
            ReleaseId = releaseId,
            ApprovalType = approvalType,
            TargetEnvironment = targetEnvironment,
            CallbackTokenHash = callbackTokenHash,
            CallbackTokenExpiresAt = callbackTokenExpiresAt,
            RequestedAt = requestedAt,
            Status = ApprovalRequestStatus.Pending,
            ExternalSystem = externalSystem,
            OutboundWebhookUrl = outboundWebhookUrl,
        };
    }

    /// <summary>
    /// Regista a resposta de aprovação.
    /// Retorna false se o status não for Pending (operação idempotente).
    /// </summary>
    public bool Respond(
        ApprovalRequestStatus decision,
        string respondedBy,
        DateTimeOffset respondedAt,
        string? comments = null,
        string? externalRequestId = null)
    {
        Guard.Against.NullOrWhiteSpace(respondedBy);
        if (Status != ApprovalRequestStatus.Pending) return false;

        Status = decision;
        RespondedBy = respondedBy;
        RespondedAt = respondedAt;
        Comments = comments;
        ExternalRequestId = externalRequestId;
        return true;
    }

    /// <summary>Expira o pedido se o token tiver passado da data de validade.</summary>
    public bool ExpireIfOverdue(DateTimeOffset now)
    {
        if (Status != ApprovalRequestStatus.Pending) return false;
        if (now < CallbackTokenExpiresAt) return false;

        Status = ApprovalRequestStatus.Expired;
        return true;
    }

    /// <summary>Contorna o fluxo de aprovação por utilizador com papel de bypass.</summary>
    public void Bypass(string bypassedBy, string reason, DateTimeOffset bypassedAt)
    {
        Guard.Against.NullOrWhiteSpace(bypassedBy);
        Guard.Against.NullOrWhiteSpace(reason);

        Status = ApprovalRequestStatus.Bypassed;
        RespondedBy = bypassedBy;
        RespondedAt = bypassedAt;
        Comments = $"[BYPASS] {reason}";
    }
}

/// <summary>Identificador fortemente tipado de ReleaseApprovalRequest.</summary>
public sealed record ReleaseApprovalRequestId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ReleaseApprovalRequestId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ReleaseApprovalRequestId From(Guid id) => new(id);
}
