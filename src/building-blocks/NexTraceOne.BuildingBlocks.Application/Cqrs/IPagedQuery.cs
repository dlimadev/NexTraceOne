namespace NexTraceOne.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Contrato para Queries com paginação padronizada.
/// Todos os endpoints de listagem devem implementar este contrato.
/// </summary>
public interface IPagedQuery
{
    /// <summary>Número da página atual. Começa em 1.</summary>
    int Page { get; }

    /// <summary>Itens por página. Máximo: 100. Padrão: 20.</summary>
    int PageSize { get; }
}
