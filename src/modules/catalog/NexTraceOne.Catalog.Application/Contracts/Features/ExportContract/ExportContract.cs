using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ExportContract;

/// <summary>
/// Feature: ExportContract — exporta o conteúdo bruto da especificação de uma versão de contrato.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ExportContract
{
    /// <summary>Query de exportação de conteúdo de versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de exportação de contrato.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna o conteúdo bruto e metadados de uma versão de contrato.</summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(ContractVersionId.From(request.ContractVersionId), cancellationToken);
            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            return new Response(version.Id.Value, version.SemVer, version.SpecContent, version.Format);
        }
    }

    /// <summary>Resposta da exportação do conteúdo de versão de contrato.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        string SemVer,
        string SpecContent,
        string Format);
}

