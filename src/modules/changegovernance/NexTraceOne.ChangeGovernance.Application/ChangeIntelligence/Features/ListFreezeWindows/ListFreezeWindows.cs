using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListFreezeWindows;

/// <summary>
/// Feature: ListFreezeWindows — lista janelas de freeze com filtros por janela temporal, ambiente e estado.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListFreezeWindows
{
    /// <summary>Query para listar janelas de freeze.</summary>
    public sealed record Query(
        DateTimeOffset From,
        DateTimeOffset To,
        string? Environment,
        bool? IsActive) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.To).GreaterThan(x => x.From)
                .WithMessage("End date must be after start date.");
        }
    }

    /// <summary>Handler que lista janelas de freeze com filtros.</summary>
    public sealed class Handler(IFreezeWindowRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var windows = await repository.ListInRangeAsync(
                request.From, request.To, request.Environment, request.IsActive, cancellationToken);

            var dtos = windows.Select(w => new FreezeWindowDto(
                w.Id.Value,
                w.Name,
                w.Reason,
                w.Scope.ToString(),
                w.ScopeValue,
                w.StartsAt,
                w.EndsAt,
                w.IsActive,
                w.CreatedBy,
                w.CreatedAt)).ToList();

            return new Response(dtos);
        }
    }

    /// <summary>DTO de janela de freeze para listagem.</summary>
    public sealed record FreezeWindowDto(
        Guid Id,
        string Name,
        string Reason,
        string Scope,
        string? ScopeValue,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        bool IsActive,
        string CreatedBy,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta da listagem de janelas de freeze.</summary>
    public sealed record Response(IReadOnlyList<FreezeWindowDto> Items);
}
