namespace NexTraceOne.BuildingBlocks.Application.Pagination;

/// <summary>
/// Container padronizado para respostas paginadas em toda a plataforma.
/// Metadados de paginação calculados automaticamente.
/// </summary>
public sealed record PagedList<T>
{
    /// <summary>Itens da página atual.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];
    /// <summary>Total de registros no banco (sem paginação).</summary>
    public int TotalCount { get; init; }
    /// <summary>Página atual (começa em 1).</summary>
    public int Page { get; init; }
    /// <summary>Itens por página.</summary>
    public int PageSize { get; init; }
    /// <summary>Total de páginas calculado automaticamente.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    /// <summary>Indica se existe página anterior.</summary>
    public bool HasPrevious => Page > 1;
    /// <summary>Indica se existe próxima página.</summary>
    public bool HasNext => Page < TotalPages;
    /// <summary>Indica se o resultado está vazio.</summary>
    public bool IsEmpty => !Items.Any();

    public static PagedList<T> Create(IReadOnlyList<T> items, int total, int page, int size)
        => new() { Items = items, TotalCount = total, Page = page, PageSize = size };

    public static PagedList<T> Empty(int page = 1, int size = 20)
        => Create([], 0, page, size);
}
