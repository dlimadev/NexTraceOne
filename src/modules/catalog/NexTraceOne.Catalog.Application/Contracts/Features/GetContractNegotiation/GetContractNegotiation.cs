using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractNegotiation;

/// <summary>
/// Feature: GetContractNegotiation — obtém os detalhes de uma negociação de contrato pelo seu ID.
/// Permite consultar o estado, participantes, deadline e especificação proposta.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetContractNegotiation
{
    /// <summary>Query para obter uma negociação de contrato.</summary>
    public sealed record Query(Guid NegotiationId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de negociação.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.NegotiationId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que obtém os detalhes de uma negociação de contrato persistida.
    /// </summary>
    public sealed class Handler(IContractNegotiationRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var negotiation = await repository.GetByIdAsync(
                ContractNegotiationId.From(request.NegotiationId), cancellationToken);

            if (negotiation is null)
                return ContractsErrors.ContractNegotiationNotFound(request.NegotiationId.ToString());

            return new Response(
                negotiation.Id.Value,
                negotiation.ContractId,
                negotiation.ProposedByTeamId,
                negotiation.ProposedByTeamName,
                negotiation.Title,
                negotiation.Description,
                negotiation.Status,
                negotiation.Deadline,
                negotiation.Participants,
                negotiation.ParticipantCount,
                negotiation.CommentCount,
                negotiation.ProposedContractSpec,
                negotiation.CreatedAt,
                negotiation.LastActivityAt,
                negotiation.ResolvedAt,
                negotiation.ResolvedByUserId,
                negotiation.InitiatedByUserId);
        }
    }

    /// <summary>Resposta completa de uma negociação de contrato.</summary>
    public sealed record Response(
        Guid NegotiationId,
        Guid? ContractId,
        Guid ProposedByTeamId,
        string ProposedByTeamName,
        string Title,
        string Description,
        NegotiationStatus Status,
        DateTimeOffset? Deadline,
        string Participants,
        int ParticipantCount,
        int CommentCount,
        string? ProposedContractSpec,
        DateTimeOffset CreatedAt,
        DateTimeOffset LastActivityAt,
        DateTimeOffset? ResolvedAt,
        string? ResolvedByUserId,
        string InitiatedByUserId);
}
