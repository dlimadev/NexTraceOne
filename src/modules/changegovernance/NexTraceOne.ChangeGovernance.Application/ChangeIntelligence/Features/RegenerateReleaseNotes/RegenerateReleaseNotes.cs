using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RegenerateReleaseNotes;

/// <summary>
/// Feature: RegenerateReleaseNotes — regenera release notes existentes para uma release
/// com dados atualizados. Incrementa o contador de regeneração e atualiza o timestamp.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegenerateReleaseNotes
{
    /// <summary>Comando para regenerar release notes de uma release.</summary>
    public sealed record Command(Guid ReleaseId) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que regenera release notes existentes com dados atualizados da release.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IReleaseNotesRepository releaseNotesRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var notes = await releaseNotesRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            if (notes is null)
                return ChangeIntelligenceErrors.ReleaseNotesNotFound(request.ReleaseId.ToString());

            var now = dateTimeProvider.UtcNow;
            var modelUsed = "template-v1";

            var breakingChangesSection = release.HasBreakingChanges
                ? "This release contains breaking changes in contracts."
                : null;

            var affectedServicesSection = $"Service: {release.ServiceName} (Environment: {release.Environment})";

            var confidenceMetricsSection = $"Change Score: {release.ChangeScore:F2}, " +
                $"Confidence Status: {release.ConfidenceStatus}, " +
                $"Validation Status: {release.ValidationStatus}";

            var technicalSummary = $"## Release Notes: {release.ReleaseName}\n\n" +
                $"**Version:** {release.Version}\n" +
                $"**Service:** {release.ServiceName}\n" +
                $"**Environment:** {release.Environment}\n" +
                $"**Commit:** {release.CommitSha}\n" +
                $"**Pipeline:** {release.PipelineSource}\n" +
                $"**Status:** {release.Status}\n" +
                $"**Change Level:** {release.ChangeLevel}\n\n" +
                $"### Affected Services\n{affectedServicesSection}\n\n" +
                $"### Confidence Metrics\n{confidenceMetricsSection}\n";

            notes.Regenerate(
                technicalSummary,
                notes.ExecutiveSummary,
                newEndpointsSection: null,
                breakingChangesSection,
                affectedServicesSection,
                confidenceMetricsSection,
                evidenceLinksSection: null,
                modelUsed,
                tokensUsed: 0,
                now);

            releaseNotesRepository.Update(notes);

            return new Response(
                notes.Id.Value,
                notes.TechnicalSummary,
                notes.RegenerationCount,
                now);
        }
    }

    /// <summary>Resposta da regeneração de release notes.</summary>
    public sealed record Response(
        Guid ReleaseNotesId,
        string TechnicalSummary,
        int RegenerationCount,
        DateTimeOffset RegeneratedAt);
}
