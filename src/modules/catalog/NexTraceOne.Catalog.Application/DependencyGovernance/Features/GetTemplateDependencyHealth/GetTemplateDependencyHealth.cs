using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetTemplateDependencyHealth;

/// <summary>
/// Feature: GetTemplateDependencyHealth — resumo de saúde de dependências para todos os serviços
/// que usam um determinado template.
/// </summary>
public static class GetTemplateDependencyHealth
{
    public sealed record Query(Guid TemplateId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.TemplateId).NotEmpty();
    }

    public sealed class Handler(IServiceDependencyProfileRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var profiles = await repository.ListByTemplateIdAsync(request.TemplateId, cancellationToken);

            if (profiles.Count == 0)
                return Result<Response>.Success(new Response(
                    TemplateId: request.TemplateId,
                    AverageHealthScore: 0,
                    ServiceCount: 0,
                    ServiceHealthScores: Array.Empty<ServiceHealthScore>(),
                    MostCommonVulnerabilities: Array.Empty<CommonVulnerability>()));

            var avgScore = (int)profiles.Average(p => p.HealthScore);
            var serviceScores = profiles
                .Select(p => new ServiceHealthScore(p.ServiceId, p.HealthScore, p.LastScanAt))
                .ToList();

            var commonVulns = profiles
                .SelectMany(p => p.Dependencies)
                .SelectMany(d => d.Vulnerabilities)
                .GroupBy(v => v.CveId)
                .Select(g => new CommonVulnerability(
                    CveId: g.Key,
                    Severity: g.First().Severity,
                    OccurrenceCount: g.Count()))
                .OrderByDescending(v => v.OccurrenceCount)
                .Take(10)
                .ToList();

            return Result<Response>.Success(new Response(
                TemplateId: request.TemplateId,
                AverageHealthScore: avgScore,
                ServiceCount: profiles.Count,
                ServiceHealthScores: serviceScores,
                MostCommonVulnerabilities: commonVulns));
        }
    }

    public sealed record Response(
        Guid TemplateId,
        int AverageHealthScore,
        int ServiceCount,
        IReadOnlyList<ServiceHealthScore> ServiceHealthScores,
        IReadOnlyList<CommonVulnerability> MostCommonVulnerabilities);

    public sealed record ServiceHealthScore(
        Guid ServiceId,
        int HealthScore,
        DateTimeOffset LastScanAt);

    public sealed record CommonVulnerability(
        string CveId,
        VulnerabilitySeverity Severity,
        int OccurrenceCount);
}
