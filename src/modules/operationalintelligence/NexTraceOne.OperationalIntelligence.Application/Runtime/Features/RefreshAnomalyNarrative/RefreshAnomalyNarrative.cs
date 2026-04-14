using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.RefreshAnomalyNarrative;

/// <summary>
/// Feature: RefreshAnomalyNarrative — regenera a narrativa de anomalia com dados atualizados
/// do drift finding. Reutiliza o template estruturado e incrementa o contador de refresh.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RefreshAnomalyNarrative
{
    /// <summary>Comando para regenerar a narrativa de uma anomalia.</summary>
    public sealed record Command(Guid DriftFindingId) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DriftFindingId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que regenera a narrativa de uma anomalia a partir dos dados actuais do drift finding.
    /// Valida que o drift finding e a narrativa existem.
    /// </summary>
    public sealed class Handler(
        IDriftFindingRepository driftFindingRepository,
        IAnomalyNarrativeRepository narrativeRepository,
        IRuntimeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
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

            var narrative = await narrativeRepository.GetByDriftFindingIdAsync(driftFindingId, cancellationToken);
            if (narrative is null)
            {
                return RuntimeIntelligenceErrors.AnomalyNarrativeNotFound(request.DriftFindingId.ToString());
            }

            var now = dateTimeProvider.UtcNow;

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

            narrative.Refresh(
                narrativeText,
                symptomsSection,
                baselineComparisonSection,
                probableCauseSection: null,
                correlatedChangesSection: null,
                recommendedActionsSection,
                severityJustificationSection,
                narrative.ModelUsed,
                tokensUsed: 0,
                now);

            narrativeRepository.Update(narrative);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                narrative.Id.Value,
                narrative.NarrativeText,
                narrative.RefreshCount,
                narrative.LastRefreshedAt!.Value);
        }
    }

    /// <summary>Resposta da regeneração da narrativa de anomalia.</summary>
    public sealed record Response(
        Guid NarrativeId,
        string NarrativeText,
        int RefreshCount,
        DateTimeOffset RefreshedAt);
}
