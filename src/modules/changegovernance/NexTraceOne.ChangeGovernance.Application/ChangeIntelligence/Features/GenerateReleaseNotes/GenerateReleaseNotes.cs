using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GenerateReleaseNotes;

/// <summary>
/// Feature: GenerateReleaseNotes — gera release notes estruturadas por IA para uma release.
/// Utiliza template estruturado (futuro: IA real via modelo governado).
/// Valida que a release existe e que não há release notes duplicadas.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GenerateReleaseNotes
{
    /// <summary>Comando para gerar release notes de uma release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        string? PersonaMode) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.PersonaMode).MaximumLength(200).When(x => x.PersonaMode is not null);
        }
    }

    /// <summary>
    /// Handler que gera release notes estruturadas para uma release.
    /// Valida que a release existe e que não há release notes duplicadas.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IReleaseNotesRepository releaseNotesRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var existing = await releaseNotesRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            if (existing is not null)
                return ChangeIntelligenceErrors.ReleaseNotesAlreadyExist(request.ReleaseId.ToString());

            var modelUsed = "template-v1";
            var now = dateTimeProvider.UtcNow;
            var tenantId = currentTenant.IsActive ? currentTenant.Id : (Guid?)null;

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

            string? executiveSummary = null;
            if (string.Equals(request.PersonaMode, "executive", StringComparison.OrdinalIgnoreCase))
            {
                executiveSummary = $"Release {release.Version} of {release.ServiceName} deployed to {release.Environment}. " +
                    $"Change level: {release.ChangeLevel}. Status: {release.Status}.";
            }

            var releaseNotes = ReleaseNotes.Create(
                ReleaseNotesId.New(),
                releaseId,
                technicalSummary,
                executiveSummary,
                newEndpointsSection: null,
                breakingChangesSection,
                affectedServicesSection,
                confidenceMetricsSection,
                evidenceLinksSection: null,
                modelUsed,
                tokensUsed: 0,
                ReleaseNotesStatus.Draft,
                tenantId,
                now);

            await releaseNotesRepository.AddAsync(releaseNotes, cancellationToken);

            return new Response(
                releaseNotes.Id.Value,
                releaseNotes.TechnicalSummary,
                releaseNotes.ExecutiveSummary,
                releaseNotes.ModelUsed,
                releaseNotes.GeneratedAt);
        }
    }

    /// <summary>Resposta da geração de release notes.</summary>
    public sealed record Response(
        Guid ReleaseNotesId,
        string TechnicalSummary,
        string? ExecutiveSummary,
        string ModelUsed,
        DateTimeOffset GeneratedAt);
}
