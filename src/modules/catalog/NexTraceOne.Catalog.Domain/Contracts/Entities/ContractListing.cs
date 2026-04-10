using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa a publicação de um contrato no marketplace interno.
/// Centraliza metadados de descoberta, métricas de adoção (consumidores, avaliação)
/// e promoção para aumentar a reutilização governada de contratos na organização.
/// Suporta os pilares de Contract Governance e Source of Truth do NexTraceOne.
/// </summary>
public sealed class ContractListing : AuditableEntity<ContractListingId>
{
    private ContractListing() { }

    /// <summary>Identificador do contrato publicado no marketplace (referência ao API Asset ou versão).</summary>
    public string ContractId { get; private set; } = string.Empty;

    /// <summary>Categoria de classificação da listagem no marketplace (ex: Pagamentos, Logística).</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Tags de descoberta em formato JSON para pesquisa (JSONB).</summary>
    public string? Tags { get; private set; }

    /// <summary>Número de consumidores conhecidos do contrato.</summary>
    public int ConsumerCount { get; private set; }

    /// <summary>Avaliação média do contrato (0 a 5).</summary>
    public decimal Rating { get; private set; }

    /// <summary>Total de avaliações recebidas.</summary>
    public int TotalReviews { get; private set; }

    /// <summary>Indica se a listagem está promovida/destacada no marketplace.</summary>
    public bool IsPromoted { get; private set; }

    /// <summary>Descrição livre da listagem para contexto no marketplace.</summary>
    public string? Description { get; private set; }

    /// <summary>Estado atual da listagem no marketplace.</summary>
    public MarketplaceListingStatus Status { get; private set; }

    /// <summary>Identificador do utilizador que publicou a listagem.</summary>
    public string? PublishedBy { get; private set; }

    /// <summary>Momento em que a listagem foi publicada no marketplace.</summary>
    public DateTimeOffset PublishedAt { get; private set; }

    /// <summary>Identificador do tenant (multi-tenancy).</summary>
    public string? TenantId { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Publica um contrato no marketplace interno, validando todos os parâmetros obrigatórios.
    /// </summary>
    public static ContractListing Publish(
        string contractId,
        string category,
        string? tags,
        bool isPromoted,
        string? description,
        MarketplaceListingStatus status,
        string? publishedBy,
        DateTimeOffset publishedAt,
        string? tenantId = null)
    {
        Guard.Against.NullOrWhiteSpace(contractId);
        Guard.Against.StringTooLong(contractId, 200);
        Guard.Against.NullOrWhiteSpace(category);
        Guard.Against.StringTooLong(category, 100);
        Guard.Against.EnumOutOfRange(status);

        if (description is not null)
            Guard.Against.StringTooLong(description, 4000);

        if (publishedBy is not null)
            Guard.Against.StringTooLong(publishedBy, 200);

        return new ContractListing
        {
            Id = ContractListingId.New(),
            ContractId = contractId.Trim(),
            Category = category.Trim(),
            Tags = tags,
            ConsumerCount = 0,
            Rating = 0m,
            TotalReviews = 0,
            IsPromoted = isPromoted,
            Description = description?.Trim(),
            Status = status,
            PublishedBy = publishedBy?.Trim(),
            PublishedAt = publishedAt,
            TenantId = tenantId?.Trim()
        };
    }

    /// <summary>
    /// Atualiza as métricas de adoção da listagem (consumidores, avaliação, total de reviews).
    /// Validações: consumerCount >= 0, rating entre 0 e 5, totalReviews >= 0.
    /// </summary>
    public void UpdateMetrics(int consumerCount, decimal rating, int totalReviews)
    {
        Guard.Against.Negative(consumerCount);
        Guard.Against.OutOfRange(rating, nameof(rating), 0m, 5m);
        Guard.Against.Negative(totalReviews);

        ConsumerCount = consumerCount;
        Rating = rating;
        TotalReviews = totalReviews;
    }
}

/// <summary>Identificador fortemente tipado de ContractListing.</summary>
public sealed record ContractListingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractListingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractListingId From(Guid id) => new(id);
}
