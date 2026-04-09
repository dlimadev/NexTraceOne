using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListContractNegotiations;

/// <summary>
/// Feature: ListContractNegotiations — lista negociações de contrato com filtros opcionais.
/// Suporta filtro por estado (NegotiationStatus) e por equipa (TeamId).
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ListContractNegotiations
{
    /// <summary>Query para listar negociações de contrato.</summary>
    public sealed record Query(NegotiationStatus? Status, Guid? TeamId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem de negociações.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId)
                .Must(id => id != Guid.Empty)
                .When(x => x.TeamId.HasValue)
                .WithMessage("TeamId must not be empty when provided.");
        }
    }

    /// <summary>
    /// Handler que lista negociações de contrato com filtros opcionais.
    /// </summary>
    public sealed class Handler(IContractNegotiationRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var negotiations = await repository.ListAsync(request.Status, request.TeamId, cancellationToken);

            var items = negotiations.Select(n => new NegotiationSummary(
                n.Id.Value,
                n.ContractId,
                n.ProposedByTeamId,
                n.ProposedByTeamName,
                n.Title,
                n.Status,
                n.Deadline,
                n.ParticipantCount,
                n.CommentCount,
                n.CreatedAt,
                n.LastActivityAt,
                n.InitiatedByUserId)).ToList();

            return new Response(items);
        }
    }

    /// <summary>Resumo de uma negociação de contrato para listagem.</summary>
    public sealed record NegotiationSummary(
        Guid NegotiationId,
        Guid? ContractId,
        Guid ProposedByTeamId,
        string ProposedByTeamName,
        string Title,
        NegotiationStatus Status,
        DateTimeOffset? Deadline,
        int ParticipantCount,
        int CommentCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset LastActivityAt,
        string InitiatedByUserId);

    /// <summary>Resposta da listagem de negociações de contrato.</summary>
    public sealed record Response(IReadOnlyList<NegotiationSummary> Items);
}
