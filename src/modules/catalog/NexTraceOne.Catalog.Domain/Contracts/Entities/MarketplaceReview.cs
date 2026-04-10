using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Avaliação de um contrato publicado no marketplace interno.
/// Captura rating numérico, comentário e autoria para alimentar métricas de qualidade
/// e reputação dos contratos reutilizáveis na organização.
/// Suporta os pilares de Contract Governance e Source of Truth do NexTraceOne.
/// </summary>
public sealed class MarketplaceReview : AuditableEntity<MarketplaceReviewId>
{
    private MarketplaceReview() { }

    /// <summary>Identificador da listagem avaliada no marketplace.</summary>
    public ContractListingId ListingId { get; private set; } = null!;

    /// <summary>Identificador do autor da avaliação.</summary>
    public string AuthorId { get; private set; } = string.Empty;

    /// <summary>Nota atribuída de 1 a 5.</summary>
    public int Rating { get; private set; }

    /// <summary>Comentário opcional do avaliador.</summary>
    public string? Comment { get; private set; }

    /// <summary>Momento em que a avaliação foi submetida.</summary>
    public DateTimeOffset ReviewedAt { get; private set; }

    /// <summary>Identificador do tenant (multi-tenancy).</summary>
    public string? TenantId { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Submete uma nova avaliação de contrato no marketplace interno,
    /// validando autor, rating e limites de comentário.
    /// </summary>
    public static MarketplaceReview Submit(
        ContractListingId listingId,
        string authorId,
        int rating,
        string? comment,
        DateTimeOffset reviewedAt,
        string? tenantId = null)
    {
        Guard.Against.Null(listingId);
        Guard.Against.NullOrWhiteSpace(authorId);
        Guard.Against.StringTooLong(authorId, 200);
        Guard.Against.OutOfRange(rating, nameof(rating), 1, 5);

        if (comment is not null)
            Guard.Against.StringTooLong(comment, 2000);

        return new MarketplaceReview
        {
            Id = MarketplaceReviewId.New(),
            ListingId = listingId,
            AuthorId = authorId.Trim(),
            Rating = rating,
            Comment = comment?.Trim(),
            ReviewedAt = reviewedAt,
            TenantId = tenantId?.Trim()
        };
    }
}

/// <summary>Identificador fortemente tipado de MarketplaceReview.</summary>
public sealed record MarketplaceReviewId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static MarketplaceReviewId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static MarketplaceReviewId From(Guid id) => new(id);
}
