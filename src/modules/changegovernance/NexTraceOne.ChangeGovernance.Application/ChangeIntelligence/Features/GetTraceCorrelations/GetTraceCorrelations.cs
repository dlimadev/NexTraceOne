using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetTraceCorrelations;

/// <summary>
/// Feature: GetTraceCorrelations — lista os traces correlacionados a uma release específica.
/// Consulta os ChangeEvents de tipo "trace_correlated" no PostgreSQL (Source = traceId).
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetTraceCorrelations
{
    /// <summary>Query para listar correlações trace → release de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que consulta os eventos de correlação de trace registados para uma release.
    /// Os eventos são do tipo "trace_correlated" e carregam o traceId no campo Source.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeEventRepository changeEventRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var events = await changeEventRepository.ListByReleaseIdAndEventTypeAsync(
                releaseId, "trace_correlated", cancellationToken);

            var correlations = events.Select(e => new TraceCorrelationDto(
                TraceId: e.Source,
                CorrelatedAt: e.OccurredAt,
                Description: e.Description)).ToList();

            return new Response(
                ReleaseId: request.ReleaseId,
                ServiceName: release.ServiceName,
                Version: release.Version,
                Environment: release.Environment,
                Correlations: correlations);
        }
    }

    /// <summary>DTO de uma correlação trace individual.</summary>
    public sealed record TraceCorrelationDto(
        string TraceId,
        DateTimeOffset CorrelatedAt,
        string Description);

    /// <summary>Resposta com todas as correlações trace → release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        string ServiceName,
        string Version,
        string Environment,
        IReadOnlyList<TraceCorrelationDto> Correlations);
}
