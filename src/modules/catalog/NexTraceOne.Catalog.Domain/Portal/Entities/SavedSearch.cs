using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Portal.Entities;

/// <summary>
/// Aggregate Root que representa uma pesquisa salva pelo utilizador no catálogo do portal.
/// Permite que desenvolvedores guardem critérios de pesquisa frequentes (queries, filtros)
/// para reutilização rápida, melhorando a experiência de descoberta de APIs. Regista também
/// a data de última utilização para fins de analytics e ordenação por relevância.
/// </summary>
public sealed class SavedSearch : AggregateRoot<SavedSearchId>
{
    private SavedSearch() { }

    /// <summary>Identificador do utilizador que criou a pesquisa salva.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Nome descritivo dado pelo utilizador à pesquisa salva.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Query de pesquisa textual utilizada no catálogo.</summary>
    public string SearchQuery { get; private set; } = string.Empty;

    /// <summary>Filtros adicionais serializados como JSON (tags, categorias, owners, etc.).</summary>
    public string? Filters { get; private set; }

    /// <summary>Data/hora UTC de criação da pesquisa salva.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora UTC da última utilização da pesquisa salva.</summary>
    public DateTimeOffset LastUsedAt { get; private set; }

    /// <summary>
    /// Cria uma nova pesquisa salva associada a um utilizador.
    /// A data de última utilização é inicializada com a data de criação.
    /// </summary>
    public static SavedSearch Create(
        Guid userId,
        string name,
        string searchQuery,
        string? filters,
        DateTimeOffset createdAt)
    {
        Guard.Against.Default(userId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(searchQuery);

        return new SavedSearch
        {
            Id = SavedSearchId.New(),
            UserId = userId,
            Name = name,
            SearchQuery = searchQuery,
            Filters = filters,
            CreatedAt = createdAt,
            LastUsedAt = createdAt
        };
    }

    /// <summary>Regista a data/hora da última utilização desta pesquisa salva.</summary>
    public void MarkUsed(DateTimeOffset timestamp)
    {
        LastUsedAt = timestamp;
    }

    /// <summary>
    /// Atualiza o nome, query e filtros da pesquisa salva.
    /// Valida que nome e query não estão vazios.
    /// </summary>
    public Result<Unit> UpdateQuery(string name, string searchQuery, string? filters)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(searchQuery);

        Name = name;
        SearchQuery = searchQuery;
        Filters = filters;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de SavedSearch.</summary>
public sealed record SavedSearchId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SavedSearchId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SavedSearchId From(Guid id) => new(id);
}
