using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetDependencyHealthDashboard;

/// <summary>
/// Feature: GetDependencyHealthDashboard — retorna métricas consolidadas de saúde de dependências.
/// </summary>
public static class GetDependencyHealthDashboard
{
    public sealed record Query(Guid ServiceId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.ServiceId).NotEmpty();
    }

    public sealed class Handler(IServiceDependencyProfileRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var profile = await repository.FindByServiceIdAsync(request.ServiceId, cancellationToken);
            if (profile is null)
                return Error.NotFound("DependencyGovernance.ProfileNotFound",
                    $"No dependency profile found for service '{request.ServiceId}'.");

            var allVulns = profile.Dependencies.SelectMany(d => d.Vulnerabilities).ToList();
            var licenseRiskCounts = profile.Dependencies
                .GroupBy(d => d.LicenseRisk)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            return Result<Response>.Success(new Response(
                ServiceId: request.ServiceId,
                HealthScore: profile.HealthScore,
                LastScanAt: profile.LastScanAt,
                TotalDeps: profile.TotalDependencies,
                DirectDeps: profile.DirectDependencies,
                TransitiveDeps: profile.TransitiveDependencies,
                CriticalVulnCount: allVulns.Count(v => v.Severity == VulnerabilitySeverity.Critical),
                HighVulnCount: allVulns.Count(v => v.Severity == VulnerabilitySeverity.High),
                MediumVulnCount: allVulns.Count(v => v.Severity == VulnerabilitySeverity.Medium),
                LowVulnCount: allVulns.Count(v => v.Severity == VulnerabilitySeverity.Low),
                OutdatedCount: profile.Dependencies.Count(d => d.IsOutdated),
                DeprecatedCount: profile.Dependencies.Count(d => d.DeprecationNotice is not null),
                LicenseRiskCounts: licenseRiskCounts));
        }
    }

    public sealed record Response(
        Guid ServiceId,
        int HealthScore,
        DateTimeOffset LastScanAt,
        int TotalDeps,
        int DirectDeps,
        int TransitiveDeps,
        int CriticalVulnCount,
        int HighVulnCount,
        int MediumVulnCount,
        int LowVulnCount,
        int OutdatedCount,
        int DeprecatedCount,
        IReadOnlyDictionary<string, int> LicenseRiskCounts);
}
