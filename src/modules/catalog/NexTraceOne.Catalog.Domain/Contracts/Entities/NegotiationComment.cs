using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa um comentário numa negociação cross-team de contrato.
/// Suporta comentários gerais e inline (com referência a linhas específicas do contrato).
/// </summary>
public sealed class NegotiationComment : AuditableEntity<NegotiationCommentId>
{
    private NegotiationComment() { }

    /// <summary>Identificador da negociação à qual este comentário pertence.</summary>
    public Guid NegotiationId { get; private set; }

    /// <summary>Identificador do autor do comentário.</summary>
    public string AuthorId { get; private set; } = string.Empty;

    /// <summary>Nome de exibição do autor do comentário.</summary>
    public string AuthorDisplayName { get; private set; } = string.Empty;

    /// <summary>Conteúdo textual do comentário.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Referência a linha específica do contrato para comentários inline (opcional).</summary>
    public string? LineReference { get; private set; }

    /// <summary>Momento de criação do comentário.</summary>
    public new DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria um novo comentário numa negociação de contrato.
    /// </summary>
    public static NegotiationComment Create(
        Guid negotiationId,
        string authorId,
        string authorDisplayName,
        string content,
        string? lineReference,
        DateTimeOffset createdAt)
    {
        Guard.Against.Default(negotiationId);
        Guard.Against.NullOrWhiteSpace(authorId);
        Guard.Against.NullOrWhiteSpace(authorDisplayName);
        Guard.Against.NullOrWhiteSpace(content);

        return new NegotiationComment
        {
            Id = NegotiationCommentId.New(),
            NegotiationId = negotiationId,
            AuthorId = authorId,
            AuthorDisplayName = authorDisplayName,
            Content = content,
            LineReference = lineReference,
            CreatedAt = createdAt
        };
    }
}

/// <summary>Identificador fortemente tipado de NegotiationComment.</summary>
public sealed record NegotiationCommentId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static NegotiationCommentId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static NegotiationCommentId From(Guid id) => new(id);
}
