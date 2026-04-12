using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RefreshIncidentNarrative;

/// <summary>
/// Feature: RefreshIncidentNarrative — regenera a narrativa de incidente com dados atualizados.
/// Reutiliza o template estruturado e incrementa o contador de refresh.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RefreshIncidentNarrative
{
    /// <summary>Comando para regenerar a narrativa de um incidente.</summary>
    public sealed record Command(Guid IncidentId) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que regenera a narrativa de um incidente a partir dos dados actuais.
    /// Valida que o incidente e a narrativa existem.
    /// </summary>
    public sealed class Handler(
        IIncidentStore incidentStore,
        IIncidentNarrativeRepository narrativeRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var incidentId = request.IncidentId.ToString();

            if (!incidentStore.IncidentExists(incidentId))
            {
                return IncidentErrors.IncidentNotFound(incidentId);
            }

            var narrative = await narrativeRepository.GetByIncidentIdAsync(request.IncidentId, cancellationToken);
            if (narrative is null)
            {
                return IncidentErrors.NarrativeNotFound(incidentId);
            }

            var detail = incidentStore.GetIncidentDetail(incidentId);
            var now = dateTimeProvider.UtcNow;

            var symptomsSection = detail?.Identity.Summary ?? "No symptoms description available.";
            var timelineSection = $"Incident reported at {detail?.Identity.CreatedAt:u}.";
            var affectedServicesSection = $"Service: {detail?.LinkedServices.FirstOrDefault()?.DisplayName ?? "N/A"} " +
                $"(Team: {detail?.OwnerTeam ?? "N/A"})";
            var probableCauseSection = $"Under investigation. Severity: {detail?.Identity.Severity}.";
            var mitigationSection = $"Status: {detail?.Identity.Status}.";

            var narrativeText = $"## Incident Narrative: {detail?.Identity.Title ?? "Unknown"}\n\n" +
                $"### Symptoms\n{symptomsSection}\n\n" +
                $"### Timeline\n{timelineSection}\n\n" +
                $"### Affected Services\n{affectedServicesSection}\n\n" +
                $"### Probable Cause\n{probableCauseSection}\n\n" +
                $"### Mitigation\n{mitigationSection}\n";

            narrative.Refresh(
                narrativeText,
                symptomsSection,
                timelineSection,
                probableCauseSection,
                mitigationSection,
                relatedChangesSection: null,
                affectedServicesSection,
                narrative.ModelUsed,
                tokensUsed: 0,
                now);

            await narrativeRepository.UpdateAsync(narrative, cancellationToken);

            return new Response(
                narrative.Id.Value,
                narrative.NarrativeText,
                narrative.RefreshCount,
                narrative.LastRefreshedAt!.Value);
        }
    }

    /// <summary>Resposta da regeneração da narrativa.</summary>
    public sealed record Response(
        Guid NarrativeId,
        string NarrativeText,
        int RefreshCount,
        DateTimeOffset RefreshedAt);
}
