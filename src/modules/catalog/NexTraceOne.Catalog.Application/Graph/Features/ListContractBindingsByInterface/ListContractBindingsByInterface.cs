using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;

namespace NexTraceOne.Catalog.Application.Graph.Features.ListContractBindingsByInterface;

/// <summary>
/// Feature: ListContractBindingsByInterface — lista todos os vínculos de contrato de uma interface.
/// Estrutura VSA: Query + Handler + Response em ficheiro único.
/// </summary>
public static class ListContractBindingsByInterface
{
    /// <summary>Consulta de listagem de vínculos de contrato por interface.</summary>
    public sealed record Query(Guid ServiceInterfaceId) : IQuery<IReadOnlyList<Response>>;

    /// <summary>Handler que lista os vínculos de contrato de uma interface.</summary>
    public sealed class Handler(
        IContractBindingRepository contractBindingRepository) : IQueryHandler<Query, IReadOnlyList<Response>>
    {
        public async Task<Result<IReadOnlyList<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var bindings = await contractBindingRepository.ListByInterfaceAsync(
                request.ServiceInterfaceId,
                cancellationToken);

            var result = bindings
                .Select(b => new Response(
                    b.Id.Value,
                    b.ContractVersionId,
                    b.Status.ToString(),
                    b.BindingEnvironment,
                    b.IsDefaultVersion,
                    b.ActivatedAt))
                .ToList();

            return (IReadOnlyList<Response>)result;
        }
    }

    /// <summary>Resposta de listagem de vínculo de contrato.</summary>
    public sealed record Response(
        Guid BindingId,
        Guid ContractVersionId,
        string Status,
        string BindingEnvironment,
        bool IsDefaultVersion,
        DateTimeOffset? ActivatedAt);
}
