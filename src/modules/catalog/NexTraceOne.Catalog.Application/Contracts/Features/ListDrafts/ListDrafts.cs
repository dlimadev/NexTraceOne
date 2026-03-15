using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Application.Features.ListDrafts;

/// <summary>
/// Feature: ListDrafts — lista drafts de contrato com filtros opcionais e paginação.
/// Oferece a visão principal de rascunhos no Contract Studio.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListDrafts
{
    /// <summary>Query de listagem de drafts com filtros e paginação.</summary>
    public sealed record Query(
        DraftStatus? Status,
        Guid? ServiceId,
        string? Author,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de listagem de drafts.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista drafts de contrato com filtros e paginação.</summary>
    public sealed class Handler(
        IContractDraftRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var items = await repository.ListAsync(
                request.Status,
                request.ServiceId,
                request.Author,
                request.Page,
                request.PageSize,
                cancellationToken);

            var totalCount = await repository.CountAsync(
                request.Status,
                request.ServiceId,
                request.Author,
                cancellationToken);

            var drafts = items
                .Select(d => new DraftListItem(
                    d.Id.Value,
                    d.Title,
                    d.ContractType.ToString(),
                    d.Protocol.ToString(),
                    d.Status.ToString(),
                    d.Author,
                    d.ServiceId,
                    d.ProposedVersion,
                    d.IsAiGenerated,
                    d.LastEditedAt,
                    d.LastEditedBy))
                .ToList();

            return new Response(drafts, totalCount, request.Page, request.PageSize);
        }
    }

    /// <summary>Item de draft na listagem do Contract Studio.</summary>
    public sealed record DraftListItem(
        Guid DraftId,
        string Title,
        string ContractType,
        string Protocol,
        string Status,
        string Author,
        Guid? ServiceId,
        string ProposedVersion,
        bool IsAiGenerated,
        DateTimeOffset? LastEditedAt,
        string? LastEditedBy);

    /// <summary>Resposta paginada da listagem de drafts.</summary>
    public sealed record Response(
        IReadOnlyList<DraftListItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
