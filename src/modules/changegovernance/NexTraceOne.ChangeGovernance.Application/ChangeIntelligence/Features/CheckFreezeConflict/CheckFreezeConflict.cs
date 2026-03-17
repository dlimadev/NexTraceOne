using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CheckFreezeConflict;

/// <summary>
/// Feature: CheckFreezeConflict — verifica se há conflito com janela de freeze num determinado momento.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CheckFreezeConflict
{
    /// <summary>Query para verificar se há conflito com janela de freeze num determinado momento.</summary>
    public sealed record Query(
        DateTimeOffset At,
        string? Environment) : IQuery<Response>;

    /// <summary>Valida a entrada da query de verificação de freeze.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            // At é obrigatório por ser DateTimeOffset (sempre tem valor)
        }
    }

    /// <summary>
    /// Handler que verifica se há janelas de freeze ativas num determinado momento.
    /// Permite que a inteligência de mudança alerte e eleve risco quando necessário.
    /// </summary>
    public sealed class Handler(
        IFreezeWindowRepository freezeRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var activeFreezes = await freezeRepository.ListActiveAtAsync(request.At, cancellationToken);

            var filtered = request.Environment is not null
                ? activeFreezes.Where(f =>
                    f.Scope == FreezeScope.Global ||
                    (f.Scope == FreezeScope.Environment &&
                     string.Equals(f.ScopeValue, request.Environment, StringComparison.OrdinalIgnoreCase)))
                    .ToList()
                : activeFreezes.ToList();

            var dtos = filtered.Select(f => new FreezeWindowDto(
                f.Id.Value,
                f.Name,
                f.Reason,
                f.Scope.ToString(),
                f.ScopeValue,
                f.StartsAt,
                f.EndsAt)).ToList();

            return new Response(dtos.Count > 0, dtos);
        }
    }

    /// <summary>DTO de janela de freeze ativa.</summary>
    public sealed record FreezeWindowDto(
        Guid Id,
        string Name,
        string Reason,
        string Scope,
        string? ScopeValue,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt);

    /// <summary>Resposta da verificação de conflito com freeze.</summary>
    public sealed record Response(
        bool HasConflict,
        IReadOnlyList<FreezeWindowDto> ActiveFreezes);
}
