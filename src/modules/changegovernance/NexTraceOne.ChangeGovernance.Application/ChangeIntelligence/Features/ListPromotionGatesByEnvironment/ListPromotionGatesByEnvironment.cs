using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListPromotionGatesByEnvironment;

/// <summary>
/// Feature: ListPromotionGatesByEnvironment — lista gates de promoção filtrados
/// pelo par de ambientes (origem → destino).
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListPromotionGatesByEnvironment
{
    /// <summary>Query de listagem de gates por par de ambientes.</summary>
    public sealed record Query(
        string EnvironmentFrom,
        string EnvironmentTo) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.EnvironmentFrom).NotEmpty().MaximumLength(100);
            RuleFor(x => x.EnvironmentTo).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler que retorna gates de promoção filtrados por ambientes.</summary>
    public sealed class Handler(IPromotionGateRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var gates = await repository.ListByEnvironmentAsync(
                request.EnvironmentFrom, request.EnvironmentTo, cancellationToken);

            return new Response(gates.Select(g => new GateItem(
                g.Id.Value,
                g.Name,
                g.Description,
                g.EnvironmentFrom,
                g.EnvironmentTo,
                g.IsActive,
                g.BlockOnFailure,
                g.CreatedAt)).ToList());
        }
    }

    /// <summary>Resposta com a lista de gates de promoção.</summary>
    public sealed record Response(IReadOnlyList<GateItem> Gates);

    /// <summary>Item individual de gate de promoção na listagem.</summary>
    public sealed record GateItem(
        Guid GateId,
        string Name,
        string? Description,
        string EnvironmentFrom,
        string EnvironmentTo,
        bool IsActive,
        bool BlockOnFailure,
        DateTimeOffset CreatedAt);
}
