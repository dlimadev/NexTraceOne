using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
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
        ICurrentTenant currentTenant,
        IAiExecutionGateway aiExecutionGateway) : ICommandHandler<Command, Response>
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
            var now = dateTimeProvider.UtcNow;
            var tenantId = currentTenant.IsActive ? currentTenant.Id : (Guid?)null;

            var affectedServiceName = (detail?.LinkedServices is { Count: > 0 } ls ? ls[0].DisplayName : "N/A");

            string narrativeText;
            string modelUsed;
            int tokensUsed = 0;

            var aiResult = await aiExecutionGateway.ExecuteAsync(
                new AiExecutionRequest(
                    FeatureKey: "operationalintelligence.incident-narrative",
                    RequestType: "chat",
                    UserPrompt: $"Generate an incident narrative for: {detail?.Identity.Title ?? "Unknown"}. Severity: {detail?.Identity.Severity}. Status: {detail?.Identity.Status}. Symptoms: {detail?.Identity.Summary ?? "N/A"}. Affected service: {affectedServiceName}.",
                    SystemPrompt: "You are an incident response assistant. Generate a structured incident narrative with sections: Symptoms, Timeline, Affected Services, Probable Cause, Mitigation. Use markdown headers. Be concise.",
                    Temperature: 0.3f,
                    MaxTokens: 1500),
                cancellationToken);

            if (aiResult.Success && !string.IsNullOrWhiteSpace(aiResult.Content))
            {
                narrativeText = aiResult.Content;
                modelUsed = $"{aiResult.ResolvedProviderId}/{aiResult.ResolvedModelId}";
                tokensUsed = aiResult.PromptTokens + aiResult.CompletionTokens;
            }
            else
            {
                var symptomsSection = detail?.Identity.Summary ?? "No symptoms description available.";
                var timelineSection = $"Incident reported at {detail?.Identity.CreatedAt:u}.";
                var affectedServicesSection = $"Service: {affectedServiceName} " +
                    $"(Team: {detail?.OwnerTeam ?? "N/A"})";
                var probableCauseSection = $"Under investigation. Severity: {detail?.Identity.Severity}.";
                var mitigationSection = $"Status: {detail?.Identity.Status}.";

                narrativeText = $"## Incident Narrative: {detail?.Identity.Title ?? "Unknown"}\n\n" +
                    $"### Symptoms\n{symptomsSection}\n\n" +
                    $"### Timeline\n{timelineSection}\n\n" +
                    $"### Affected Services\n{affectedServicesSection}\n\n" +
                    $"### Probable Cause\n{probableCauseSection}\n\n" +
                    $"### Mitigation\n{mitigationSection}\n";
                modelUsed = "template-v1";
            }

            var symptomsSectionForCreate = detail?.Identity.Summary ?? "No symptoms description available.";
            var timelineSectionForCreate = $"Incident reported at {detail?.Identity.CreatedAt:u}.";
            var affectedServicesSectionForCreate = $"Service: {affectedServiceName} " +
                $"(Team: {detail?.OwnerTeam ?? "N/A"})";
            var probableCauseSectionForCreate = $"Under investigation. Severity: {detail?.Identity.Severity}.";
            var mitigationSectionForCreate = $"Status: {detail?.Identity.Status}.";

            var narrative = IncidentNarrative.Create(
                IncidentNarrativeId.New(),
                request.IncidentId,
                narrativeText,
                symptomsSectionForCreate,
                timelineSectionForCreate,
                probableCauseSectionForCreate,
                mitigationSectionForCreate,
                relatedChangesSection: null,
                affectedServicesSectionForCreate,
                modelUsed,
                tokensUsed,
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
