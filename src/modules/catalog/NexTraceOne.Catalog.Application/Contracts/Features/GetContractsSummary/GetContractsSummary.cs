using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Contracts.Application.Abstractions;

namespace NexTraceOne.Contracts.Application.Features.GetContractsSummary;

/// <summary>
/// Feature: GetContractsSummary — obtém resumos agregados de contratos para o dashboard de governança.
/// Retorna contagens por protocolo, ciclo de vida, e métricas de maturidade contratual.
/// Estrutura VSA: Query + Handler + Response em um único arquivo.
/// </summary>
public static class GetContractsSummary
{
    /// <summary>Query de resumo agregado de contratos.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que calcula resumos agregados de contratos.</summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var summary = await repository.GetSummaryAsync(cancellationToken);

            var byProtocol = summary.ByProtocol
                .Select(p => new ProtocolSummary(p.Protocol, p.Count))
                .ToList();

            return new Response(
                summary.TotalVersions,
                summary.DistinctContracts,
                summary.DraftCount,
                summary.InReviewCount,
                summary.ApprovedCount,
                summary.LockedCount,
                summary.DeprecatedCount,
                byProtocol);
        }
    }

    /// <summary>Contagem por protocolo.</summary>
    public sealed record ProtocolSummary(string Protocol, int Count);

    /// <summary>Resposta com resumos agregados de contratos.</summary>
    public sealed record Response(
        int TotalVersions,
        int DistinctContracts,
        int DraftCount,
        int InReviewCount,
        int ApprovedCount,
        int LockedCount,
        int DeprecatedCount,
        IReadOnlyList<ProtocolSummary> ByProtocol);
}
