using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GenerateIncidentNarrative;

/// <summary>
/// Feature: GenerateIncidentNarrative — gera uma narrativa estruturada de incidente
/// com base nos dados do incidente. Utiliza template estruturado (futuro: IA real).
/// Valida que o incidente existe e que não há narrativa duplicada.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GenerateIncidentNarrative
{
    /// <summary>Comando para gerar narrativa de incidente.</summary>
    public sealed record Command(
        Guid IncidentId,
        string? ModelPreference) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty();
            RuleFor(x => x.ModelPreference).MaximumLength(200).When(x => x.ModelPreference is not null);
        }
    }

    /// <summary>
    /// Handler que gera a narrativa estruturada de um incidente.
    /// Valida que o incidente existe e que não há narrativa duplicada.
    /// </summary>
    public sealed class Handler(
        IIncidentStore incidentStore,
        IIncidentNarrativeRepository narrativeRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var incidentId = request.IncidentId.ToString();

            if (!incidentStore.IncidentExists(incidentId))
            {
                return IncidentErrors.IncidentNotFound(incidentId);
            }

            var existing = await narrativeRepository.GetByIncidentIdAsync(request.IncidentId, cancellationToken);
            if (existing is not null)
            {
                return IncidentErrors.NarrativeAlreadyExists(incidentId);
            }

            var detail = incidentStore.GetIncidentDetail(incidentId);
            var modelUsed = request.ModelPreference ?? "template-v1";
            var now = dateTimeProvider.UtcNow;
            var tenantId = currentTenant.IsActive ? currentTenant.Id : (Guid?)null;

            var symptomsSection = detail?.Identity.Summary ?? "No symptoms description available.";
            var timelineSection = $"Incident reported at {detail?.Identity.CreatedAt:u}.";
            var affectedServicesSection = $"Service: {(detail?.LinkedServices is { Count: > 0 } ls ? ls[0].DisplayName : "N/A")} " +
                $"(Team: {detail?.OwnerTeam ?? "N/A"})";
            var probableCauseSection = $"Under investigation. Severity: {detail?.Identity.Severity}.";
            var mitigationSection = $"Status: {detail?.Identity.Status}.";

            var narrativeText = $"## Incident Narrative: {detail?.Identity.Title ?? "Unknown"}\n\n" +
                $"### Symptoms\n{symptomsSection}\n\n" +
                $"### Timeline\n{timelineSection}\n\n" +
                $"### Affected Services\n{affectedServicesSection}\n\n" +
                $"### Probable Cause\n{probableCauseSection}\n\n" +
                $"### Mitigation\n{mitigationSection}\n";

            var narrative = IncidentNarrative.Create(
                IncidentNarrativeId.New(),
                request.IncidentId,
                narrativeText,
                symptomsSection,
                timelineSection,
                probableCauseSection,
                mitigationSection,
                relatedChangesSection: null,
                affectedServicesSection,
                modelUsed,
                tokensUsed: 0,
                NarrativeStatus.Draft,
                tenantId,
                now);

            await narrativeRepository.AddAsync(narrative, cancellationToken);

            return new Response(
                narrative.Id.Value,
                narrative.NarrativeText,
                narrative.ModelUsed,
                narrative.GeneratedAt);
        }
    }

    /// <summary>Resposta da geração da narrativa.</summary>
    public sealed record Response(
        Guid NarrativeId,
        string NarrativeText,
        string ModelUsed,
        DateTimeOffset GeneratedAt);
}
