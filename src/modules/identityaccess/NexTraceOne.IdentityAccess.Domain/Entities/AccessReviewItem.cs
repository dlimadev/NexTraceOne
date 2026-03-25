using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que representa um item individual dentro de uma campanha de access review.
///
/// Cada item corresponde a uma combinação (usuário + role) que precisa ser revisada
/// por um reviewer designado. A decisão pode ser:
/// - Confirmed: acesso mantido após verificação consciente.
/// - Revoked: acesso removido pelo reviewer.
/// - AutoRevoked: acesso removido automaticamente após expiração do prazo da campanha.
///
/// Itens pendentes além do deadline são auto-revogados pelo job periódico,
/// garantindo conformidade com políticas de recertificação (SOX, DORA, ISO 27001).
/// </summary>
public sealed class AccessReviewItem : Entity<AccessReviewItemId>
{
    private AccessReviewItem() { }

    /// <summary>Campanha à qual este item pertence.</summary>
    public AccessReviewCampaignId CampaignId { get; private set; } = null!;

    /// <summary>Usuário cujo acesso está sendo revisado.</summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>Role atribuída ao usuário e em revisão.</summary>
    public RoleId RoleId { get; private set; } = null!;

    /// <summary>Nome da role no momento da criação do item (snapshot para auditoria).</summary>
    public string RoleName { get; private set; } = string.Empty;

    /// <summary>Reviewer responsável por decidir sobre este item.</summary>
    public UserId ReviewerId { get; private set; } = null!;

    /// <summary>Decisão do reviewer sobre o acesso.</summary>
    public AccessReviewDecision Decision { get; private set; }

    /// <summary>Comentário opcional do reviewer justificando a decisão.</summary>
    public string? ReviewerComment { get; private set; }

    /// <summary>Data/hora UTC da decisão sobre o item.</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>Cria um novo item de revisão de acesso vinculado a uma campanha.</summary>
    internal static AccessReviewItem Create(
        AccessReviewCampaignId campaignId,
        UserId userId,
        RoleId roleId,
        string roleName,
        UserId reviewerId)
    {
        Guard.Against.Null(campaignId);
        Guard.Against.Null(userId);
        Guard.Against.Null(roleId);
        Guard.Against.NullOrWhiteSpace(roleName);
        Guard.Against.Null(reviewerId);

        return new AccessReviewItem
        {
            Id = AccessReviewItemId.New(),
            CampaignId = campaignId,
            UserId = userId,
            RoleId = roleId,
            RoleName = roleName,
            ReviewerId = reviewerId,
            Decision = AccessReviewDecision.Pending
        };
    }

    /// <summary>
    /// Confirma que o acesso do usuário é legítimo e deve ser mantido.
    /// Requer justificativa consciente pelo reviewer.
    /// </summary>
    public void Confirm(UserId decidedBy, string? comment, DateTimeOffset now)
    {
        Guard.Against.Null(decidedBy);

        Decision = AccessReviewDecision.Confirmed;
        ReviewerComment = comment;
        DecidedAt = now;
    }

    /// <summary>
    /// Revoga o acesso do usuário manualmente pelo reviewer.
    /// Disparará ação de remoção da role quando integrado ao workflow.
    /// </summary>
    public void Revoke(UserId decidedBy, string? comment, DateTimeOffset now)
    {
        Guard.Against.Null(decidedBy);

        Decision = AccessReviewDecision.Revoked;
        ReviewerComment = comment;
        DecidedAt = now;
    }

    /// <summary>
    /// Auto-revoga o acesso quando o prazo da campanha é atingido sem decisão.
    /// Garante que nenhum acesso permanece sem revisão além do período de compliance.
    /// </summary>
    internal void AutoRevoke(DateTimeOffset now)
    {
        Decision = AccessReviewDecision.AutoRevoked;
        DecidedAt = now;
        ReviewerComment = "Auto-revoked: review deadline exceeded.";
    }

    /// <summary>Concurrency token (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }
}

/// <summary>
/// Decisões possíveis sobre um item de access review.
/// Valores armazenados como string no banco para legibilidade em auditoria.
/// </summary>
public enum AccessReviewDecision
{
    /// <summary>Aguardando decisão do reviewer.</summary>
    Pending = 0,

    /// <summary>Acesso confirmado como legítimo pelo reviewer.</summary>
    Confirmed = 1,

    /// <summary>Acesso revogado manualmente pelo reviewer.</summary>
    Revoked = 2,

    /// <summary>Acesso revogado automaticamente por expiração do prazo.</summary>
    AutoRevoked = 3
}

/// <summary>Identificador fortemente tipado de AccessReviewItem.</summary>
public sealed record AccessReviewItemId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AccessReviewItemId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AccessReviewItemId From(Guid id) => new(id);
}
