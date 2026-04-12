using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseNotes;

/// <summary>
/// Feature: GetReleaseNotes — consulta as release notes geradas para uma release.
/// Retorna todos os campos estruturados das release notes incluindo secções individuais.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetReleaseNotes
{
    /// <summary>Query para obter as release notes de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que consulta as release notes pela release associada.</summary>
    public sealed class Handler(
        IReleaseNotesRepository releaseNotesRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var notes = await releaseNotesRepository.GetByReleaseIdAsync(releaseId, cancellationToken);

            if (notes is null)
                return ChangeIntelligenceErrors.ReleaseNotesNotFound(request.ReleaseId.ToString());

            return new Response(
                notes.Id.Value,
                notes.ReleaseId.Value,
                notes.TechnicalSummary,
                notes.ExecutiveSummary,
                notes.NewEndpointsSection,
                notes.BreakingChangesSection,
                notes.AffectedServicesSection,
                notes.ConfidenceMetricsSection,
                notes.EvidenceLinksSection,
                notes.ModelUsed,
                notes.TokensUsed,
                notes.Status.ToString(),
                notes.GeneratedAt,
                notes.LastRegeneratedAt,
                notes.RegenerationCount);
        }
    }

    /// <summary>Resposta completa das release notes.</summary>
    public sealed record Response(
        Guid ReleaseNotesId,
        Guid ReleaseId,
        string TechnicalSummary,
        string? ExecutiveSummary,
        string? NewEndpointsSection,
        string? BreakingChangesSection,
        string? AffectedServicesSection,
        string? ConfidenceMetricsSection,
        string? EvidenceLinksSection,
        string ModelUsed,
        int TokensUsed,
        string Status,
        DateTimeOffset GeneratedAt,
        DateTimeOffset? LastRegeneratedAt,
        int RegenerationCount);
}
