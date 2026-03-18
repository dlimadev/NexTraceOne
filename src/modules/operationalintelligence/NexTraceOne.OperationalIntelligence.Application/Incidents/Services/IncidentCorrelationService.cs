using MediatR;

using Microsoft.Extensions.Logging;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusReport;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListChanges;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Services;

/// <summary>
/// Correlation engine simples e explicável.
/// Critérios: proximidade temporal, interseção de serviço e blast radius.
/// </summary>
internal sealed class IncidentCorrelationService(
    IIncidentStore store,
    ISender sender,
    ILogger<IncidentCorrelationService> logger) : IIncidentCorrelationService
{
    public async Task<GetIncidentCorrelation.Response?> RecomputeAsync(string incidentId, CancellationToken cancellationToken)
    {
        var context = store.GetIncidentCorrelationContext(incidentId);
        if (context is null)
            return null;

        var from = context.DetectedAtUtc.AddHours(-12);
        var to = context.DetectedAtUtc.AddHours(2);

        var byServiceId = await sender.Send(new ListChanges.Query(
            ServiceName: null,
            TeamName: null,
            Environment: context.Environment,
            ChangeType: null,
            ConfidenceStatus: null,
            DeploymentStatus: null,
            SearchTerm: context.ServiceId,
            From: from,
            To: to,
            Page: 1,
            PageSize: 20), cancellationToken);

        var byServiceName = await sender.Send(new ListChanges.Query(
            ServiceName: context.ServiceDisplayName,
            TeamName: null,
            Environment: context.Environment,
            ChangeType: null,
            ConfidenceStatus: null,
            DeploymentStatus: null,
            SearchTerm: null,
            From: from,
            To: to,
            Page: 1,
            PageSize: 20), cancellationToken);

        var candidates = byServiceId.Value.Changes
            .Concat(byServiceName.Value.Changes)
            .GroupBy(c => c.ChangeId)
            .Select(g => g.First())
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var correlated = new List<(GetIncidentCorrelation.CorrelatedChange Change, decimal Score, IReadOnlyList<string> Direct, IReadOnlyList<string> Transitive)>();

        foreach (var candidate in candidates)
        {
            var temporalScore = ComputeTemporalScore(context.DetectedAtUtc, candidate.CreatedAt);
            var serviceScore = ComputeServiceIntersectionScore(context.ServiceId, context.ServiceDisplayName, candidate.ServiceName, candidate.Description);

            int blastScore = 0;
            IReadOnlyList<string> direct = [];
            IReadOnlyList<string> transitive = [];

            var blast = await sender.Send(new GetBlastRadiusReport.Query(candidate.ChangeId), cancellationToken);
            if (blast.IsSuccess && blast.Value is not null)
            {
                direct = blast.Value.DirectConsumers;
                transitive = blast.Value.TransitiveConsumers;
                blastScore = ComputeBlastRadiusScore(context.ServiceId, context.ServiceDisplayName, blast.Value.TotalAffectedConsumers, direct, transitive);
            }

            var totalScore = Math.Min(100m, temporalScore + serviceScore + blastScore);
            if (totalScore <= 0m)
                continue;

            var confidenceStatus = totalScore >= 75m
                ? "HighEvidence"
                : totalScore >= 45m
                    ? "PartialEvidence"
                    : "WeakEvidence";

            correlated.Add((
                new GetIncidentCorrelation.CorrelatedChange(
                    candidate.ChangeId,
                    candidate.Description ?? $"Release {candidate.Version} on {candidate.ServiceName}",
                    candidate.ChangeType,
                    confidenceStatus,
                    candidate.CreatedAt),
                totalScore,
                direct,
                transitive));
        }

        var top = correlated
            .OrderByDescending(c => c.Score)
            .Take(5)
            .ToList();

        var bestScore = top.Count == 0 ? 0m : top[0].Score;
        var confidence = MapConfidence(bestScore);

        var relatedServices = BuildRelatedServices(context, top);
        var dependencies = BuildDependencies(top);

        var reason = top.Count == 0
            ? "No correlated changes were found using temporal proximity, service intersection and blast radius criteria. Score=0."
            : $"Correlation score={bestScore:0}. Criteria: temporal proximity + service intersection + blast radius. Candidates={top.Count}.";

        var response = new GetIncidentCorrelation.Response(
            context.IncidentId,
            confidence,
            bestScore,
            reason,
            top.Select(t => t.Change).ToArray(),
            relatedServices,
            dependencies,
            []);

        store.SaveIncidentCorrelation(incidentId, response);

        logger.LogInformation(
            "Incident correlation recomputed. IncidentId={IncidentId}, Score={Score}, Confidence={Confidence}, Candidates={Candidates}",
            context.IncidentId,
            bestScore,
            confidence,
            top.Count);

        return response;
    }

    private static int ComputeTemporalScore(DateTimeOffset incidentDetectedAt, DateTimeOffset changeCreatedAt)
    {
        var diffHours = Math.Abs((incidentDetectedAt - changeCreatedAt).TotalHours);
        if (diffHours <= 1) return 50;
        if (diffHours <= 4) return 35;
        if (diffHours <= 12) return 20;
        if (diffHours <= 24) return 10;
        return 0;
    }

    private static int ComputeServiceIntersectionScore(string serviceId, string serviceDisplayName, string candidateServiceName, string? candidateDescription)
    {
        if (candidateServiceName.Equals(serviceId, StringComparison.OrdinalIgnoreCase))
            return 35;

        if (candidateServiceName.Equals(serviceDisplayName, StringComparison.OrdinalIgnoreCase))
            return 35;

        if (candidateServiceName.Contains(serviceId, StringComparison.OrdinalIgnoreCase)
            || candidateServiceName.Contains(serviceDisplayName, StringComparison.OrdinalIgnoreCase))
            return 25;

        if (!string.IsNullOrWhiteSpace(candidateDescription)
            && (candidateDescription.Contains(serviceId, StringComparison.OrdinalIgnoreCase)
                || candidateDescription.Contains(serviceDisplayName, StringComparison.OrdinalIgnoreCase)))
            return 15;

        return 0;
    }

    private static int ComputeBlastRadiusScore(
        string serviceId,
        string serviceDisplayName,
        int totalAffectedConsumers,
        IReadOnlyList<string> direct,
        IReadOnlyList<string> transitive)
    {
        var baseScore = totalAffectedConsumers switch
        {
            >= 20 => 20,
            >= 10 => 15,
            >= 5 => 10,
            > 0 => 5,
            _ => 0,
        };

        var serviceReferenced = direct.Any(c => c.Contains(serviceId, StringComparison.OrdinalIgnoreCase)
                                                || c.Contains(serviceDisplayName, StringComparison.OrdinalIgnoreCase))
            || transitive.Any(c => c.Contains(serviceId, StringComparison.OrdinalIgnoreCase)
                                   || c.Contains(serviceDisplayName, StringComparison.OrdinalIgnoreCase));

        if (serviceReferenced)
            baseScore = Math.Min(25, baseScore + 5);

        return baseScore;
    }

    private static CorrelationConfidence MapConfidence(decimal score)
    {
        if (score >= 80m) return CorrelationConfidence.High;
        if (score >= 45m) return CorrelationConfidence.Medium;
        if (score >= 20m) return CorrelationConfidence.Low;
        return CorrelationConfidence.NotAssessed;
    }

    private static IReadOnlyList<GetIncidentCorrelation.CorrelatedService> BuildRelatedServices(
        IncidentCorrelationContext context,
        IReadOnlyList<(GetIncidentCorrelation.CorrelatedChange Change, decimal Score, IReadOnlyList<string> Direct, IReadOnlyList<string> Transitive)> top)
    {
        var services = new List<GetIncidentCorrelation.CorrelatedService>
        {
            new(context.ServiceId, context.ServiceDisplayName, "Primary impacted service")
        };

        foreach (var consumer in top.SelectMany(t => t.Direct).Distinct(StringComparer.OrdinalIgnoreCase).Take(3))
        {
            services.Add(new GetIncidentCorrelation.CorrelatedService(
                ServiceId: consumer,
                DisplayName: consumer,
                ImpactDescription: "Direct consumer in blast radius"));
        }

        return services;
    }

    private static IReadOnlyList<GetIncidentCorrelation.CorrelatedDependency> BuildDependencies(
        IReadOnlyList<(GetIncidentCorrelation.CorrelatedChange Change, decimal Score, IReadOnlyList<string> Direct, IReadOnlyList<string> Transitive)> top)
    {
        var deps = new List<GetIncidentCorrelation.CorrelatedDependency>();

        foreach (var consumer in top.SelectMany(t => t.Direct).Distinct(StringComparer.OrdinalIgnoreCase).Take(3))
        {
            deps.Add(new GetIncidentCorrelation.CorrelatedDependency(
                consumer,
                consumer,
                "Direct consumer affected by correlated change"));
        }

        foreach (var consumer in top.SelectMany(t => t.Transitive).Distinct(StringComparer.OrdinalIgnoreCase).Take(2))
        {
            deps.Add(new GetIncidentCorrelation.CorrelatedDependency(
                consumer,
                consumer,
                "Transitive consumer in blast radius"));
        }

        return deps;
    }
}
