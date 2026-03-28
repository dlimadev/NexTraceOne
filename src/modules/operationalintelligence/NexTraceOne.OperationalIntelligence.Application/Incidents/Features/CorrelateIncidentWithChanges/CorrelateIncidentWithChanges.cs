using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CorrelateIncidentWithChanges;

/// <summary>
/// Feature: CorrelateIncidentWithChanges — motor dinâmico de correlação incidente↔mudança.
/// Consulta ChangeIntelligenceDbContext para encontrar releases dentro da janela temporal,
/// aplica scoring de confiança por critério de correspondência e persiste os resultados.
/// Duplicações são ignoradas: a correlação incidente+mudança só é persistida uma vez.
/// Novas correlações são persistidas em lote num único commit para melhor performance.
/// </summary>
public static class CorrelateIncidentWithChanges
{
    private const int DefaultTimeWindowHours = 24;
    private const string FallbackDescriptionPrefix = "Release on ";

    /// <summary>Comando para executar a correlação dinâmica de um incidente.</summary>
    public sealed record Command(
        Guid IncidentId,
        int? TimeWindowHours = null) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty();
            RuleFor(x => x.TimeWindowHours)
                .InclusiveBetween(1, 168)
                .When(x => x.TimeWindowHours.HasValue);
        }
    }

    /// <summary>Handler do motor de correlação dinâmica.</summary>
    public sealed class Handler(
        IIncidentStore incidentStore,
        IChangeIntelligenceReader changeReader,
        IIncidentCorrelationRepository correlationRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var context = incidentStore.GetIncidentCorrelationContext(request.IncidentId.ToString());
            if (context is null)
                return IncidentErrors.IncidentNotFound(request.IncidentId.ToString());

            var windowHours = request.TimeWindowHours ?? DefaultTimeWindowHours;
            var from = context.DetectedAtUtc.AddHours(-windowHours);
            var to = context.DetectedAtUtc;
            var tenantId = currentTenant.IsActive ? currentTenant.Id : (Guid?)null;

            var releases = await changeReader.GetReleasesInWindowAsync(
                context.Environment,
                from,
                to,
                tenantId,
                cancellationToken);

            var correlatedAt = clock.UtcNow;
            var results = new List<CorrelationResult>();
            var toAdd = new List<IncidentChangeCorrelation>();

            foreach (var release in releases)
            {
                var isDuplicate = await correlationRepository.ExistsByIncidentAndChangeAsync(
                    context.IncidentId,
                    release.ReleaseId,
                    cancellationToken);

                if (isDuplicate)
                {
                    results.Add(BuildResult(release, isDuplicate: true));
                    continue;
                }

                var (confidenceLevel, matchType) = ComputeScore(
                    context.ServiceId, context.ServiceDisplayName, release.ServiceName);

                toAdd.Add(IncidentChangeCorrelation.Create(
                    context.IncidentId,
                    release.ReleaseId,
                    release.ApiAssetId,
                    confidenceLevel,
                    matchType,
                    windowHours,
                    correlatedAt,
                    tenantId,
                    release.ServiceName,
                    GetDescription(release),
                    release.Environment,
                    release.CreatedAt));

                results.Add(BuildResult(release, isDuplicate: false, confidenceLevel, matchType));
            }

            if (toAdd.Count > 0)
                await correlationRepository.AddRangeAsync(toAdd, cancellationToken);

            return Result<Response>.Success(new Response(
                context.IncidentId,
                windowHours,
                results.Count,
                toAdd.Count,
                results));
        }

        private static (CorrelationConfidenceLevel, CorrelationMatchType) ComputeScore(
            string incidentServiceId,
            string incidentServiceDisplayName,
            string releaseServiceName)
        {
            if (releaseServiceName.Equals(incidentServiceId, StringComparison.OrdinalIgnoreCase)
                || releaseServiceName.Equals(incidentServiceDisplayName, StringComparison.OrdinalIgnoreCase))
                return (CorrelationConfidenceLevel.High, CorrelationMatchType.ExactServiceMatch);

            if (releaseServiceName.Contains(incidentServiceId, StringComparison.OrdinalIgnoreCase)
                || releaseServiceName.Contains(incidentServiceDisplayName, StringComparison.OrdinalIgnoreCase)
                || incidentServiceId.Contains(releaseServiceName, StringComparison.OrdinalIgnoreCase)
                || incidentServiceDisplayName.Contains(releaseServiceName, StringComparison.OrdinalIgnoreCase))
                return (CorrelationConfidenceLevel.Medium, CorrelationMatchType.DependencyMatch);

            return (CorrelationConfidenceLevel.Low, CorrelationMatchType.TimeProximity);
        }

        private static string GetDescription(ChangeReleaseDto release)
            => release.Description ?? (FallbackDescriptionPrefix + release.ServiceName);

        private static CorrelationResult BuildResult(
            ChangeReleaseDto release,
            bool isDuplicate,
            CorrelationConfidenceLevel confidenceLevel = CorrelationConfidenceLevel.Low,
            CorrelationMatchType matchType = CorrelationMatchType.TimeProximity)
            => new(
                release.ReleaseId,
                release.ServiceName,
                GetDescription(release),
                release.Environment,
                release.CreatedAt,
                confidenceLevel,
                matchType,
                isDuplicate);
    }

    /// <summary>Resposta com os resultados da correlação executada.</summary>
    public sealed record Response(
        Guid IncidentId,
        int TimeWindowHours,
        int TotalCandidates,
        int NewCorrelations,
        IReadOnlyList<CorrelationResult> Correlations);

    /// <summary>Resultado de uma correlação individual.</summary>
    public sealed record CorrelationResult(
        Guid ChangeId,
        string ServiceName,
        string Description,
        string Environment,
        DateTimeOffset OccurredAt,
        CorrelationConfidenceLevel ConfidenceLevel,
        CorrelationMatchType MatchType,
        bool IsDuplicate);
}
