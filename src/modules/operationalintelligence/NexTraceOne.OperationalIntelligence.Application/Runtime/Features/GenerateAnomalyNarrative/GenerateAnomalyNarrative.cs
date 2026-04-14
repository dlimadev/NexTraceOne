using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GenerateAnomalyNarrative;

/// <summary>
/// Feature: GenerateAnomalyNarrative — gera uma narrativa estruturada de anomalia
/// com base nos dados do drift finding. Utiliza template estruturado (futuro: IA real).
/// Valida que o drift finding existe e que não há narrativa duplicada.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GenerateAnomalyNarrative
{
    /// <summary>Comando para gerar narrativa de anomalia.</summary>
    public sealed record Command(
        Guid DriftFindingId,
        string? ModelPreference) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DriftFindingId).NotEmpty();
            RuleFor(x => x.ModelPreference).MaximumLength(200).When(x => x.ModelPreference is not null);
        }
    }

    /// <summary>
    /// Handler que gera a narrativa estruturada de uma anomalia (drift finding).
    /// Valida que o drift finding existe e que não há narrativa duplicada.
    /// </summary>
    public sealed class Handler(
        IDriftFindingRepository driftFindingRepository,
        IAnomalyNarrativeRepository narrativeRepository,
        IRuntimeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var driftFindingId = DriftFindingId.From(request.DriftFindingId);

            var driftFinding = await driftFindingRepository.GetByIdAsync(driftFindingId, cancellationToken);
            if (driftFinding is null)
            {
                return RuntimeIntelligenceErrors.DriftNotFound(request.DriftFindingId.ToString());
            }

            var existing = await narrativeRepository.GetByDriftFindingIdAsync(driftFindingId, cancellationToken);
            if (existing is not null)
            {
                return RuntimeIntelligenceErrors.AnomalyNarrativeAlreadyExists(request.DriftFindingId.ToString());
            }

            var modelUsed = request.ModelPreference ?? "template-v1";
            var now = dateTimeProvider.UtcNow;
            var tenantId = currentTenant.IsActive ? currentTenant.Id : (Guid?)null;

            var symptomsSection = $"Anomaly detected on metric '{driftFinding.MetricName}' " +
                $"for service '{driftFinding.ServiceName}' in environment '{driftFinding.Environment}'.";
            var baselineComparisonSection = $"Expected value: {driftFinding.ExpectedValue}, " +
                $"Actual value: {driftFinding.ActualValue}, " +
                $"Deviation: {driftFinding.DeviationPercent}%.";
            var severityJustificationSection = $"Severity '{driftFinding.Severity}' assigned based on " +
                $"{driftFinding.DeviationPercent}% deviation from baseline.";
            var recommendedActionsSection = $"Investigate service '{driftFinding.ServiceName}' " +
                $"in environment '{driftFinding.Environment}' for recent changes affecting '{driftFinding.MetricName}'.";

            var narrativeText = $"## Anomaly Narrative: {driftFinding.MetricName}\n\n" +
                $"### Symptoms\n{symptomsSection}\n\n" +
                $"### Baseline Comparison\n{baselineComparisonSection}\n\n" +
                $"### Severity Justification\n{severityJustificationSection}\n\n" +
                $"### Recommended Actions\n{recommendedActionsSection}\n";

            var narrative = AnomalyNarrative.Create(
                AnomalyNarrativeId.New(),
                driftFindingId,
                narrativeText,
                symptomsSection,
                baselineComparisonSection,
                probableCauseSection: null,
                correlatedChangesSection: null,
                recommendedActionsSection,
                severityJustificationSection,
                modelUsed,
                tokensUsed: 0,
                AnomalyNarrativeStatus.Draft,
                tenantId,
                now);

            await narrativeRepository.AddAsync(narrative, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                narrative.Id.Value,
                narrative.NarrativeText,
                narrative.ModelUsed,
                narrative.GeneratedAt);
        }
    }

    /// <summary>Resposta da geração da narrativa de anomalia.</summary>
    public sealed record Response(
        Guid NarrativeId,
        string NarrativeText,
        string ModelUsed,
        DateTimeOffset GeneratedAt);
}
