using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractHistory;

/// <summary>
/// Feature: GetContractHistory — lista o histórico de versões de contrato de um ativo de API.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetContractHistory
{
    /// <summary>Query de histórico de versões de contrato.</summary>
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de histórico de contratos.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>Handler que lista o histórico de versões de um ativo de API.</summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var versions = await repository.ListByApiAssetAsync(request.ApiAssetId, cancellationToken);

            var summaries = versions
                .Select(v => new ContractVersionSummary(
                    v.Id.Value,
                    v.SemVer,
                    v.IsLocked,
                    v.CreatedAt,
                    v.ImportedFrom))
                .ToList()
                .AsReadOnly();

            return new Response(request.ApiAssetId, summaries);
        }
    }

    /// <summary>Resumo de uma versão de contrato para listagem.</summary>
    public sealed record ContractVersionSummary(
        Guid VersionId,
        string SemVer,
        bool IsLocked,
        DateTimeOffset CreatedAt,
        string ImportedFrom);

    /// <summary>Resposta do histórico de versões de contrato.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        IReadOnlyList<ContractVersionSummary> Versions);
}

