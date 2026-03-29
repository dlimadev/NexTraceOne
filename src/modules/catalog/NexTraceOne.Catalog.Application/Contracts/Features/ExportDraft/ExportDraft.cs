using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ExportDraft;

/// <summary>
/// Feature: ExportDraft — exporta o conteúdo bruto da especificação de um draft de contrato.
/// Permite que o utilizador obtenha o YAML/JSON/XML do draft para uso com
/// ferramentas externas (dotnet-openapi, NSwag, Kiota) antes da publicação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ExportDraft
{
    /// <summary>Query de exportação de conteúdo de draft de contrato.</summary>
    public sealed record Query(Guid DraftId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de exportação de draft.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DraftId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna o conteúdo bruto e metadados de um draft de contrato.</summary>
    public sealed class Handler(IContractDraftRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var draft = await repository.GetByIdAsync(ContractDraftId.From(request.DraftId), cancellationToken);
            if (draft is null)
                return ContractsErrors.DraftNotFound(request.DraftId.ToString());

            return new Response(
                draft.Id.Value,
                draft.Title,
                draft.SpecContent,
                draft.Format,
                draft.ProposedVersion,
                draft.Protocol.ToString(),
                draft.ContractType.ToString());
        }
    }

    /// <summary>Resposta da exportação do conteúdo de draft de contrato.</summary>
    public sealed record Response(
        Guid DraftId,
        string Title,
        string SpecContent,
        string Format,
        string ProposedVersion,
        string Protocol,
        string ContractType);
}
