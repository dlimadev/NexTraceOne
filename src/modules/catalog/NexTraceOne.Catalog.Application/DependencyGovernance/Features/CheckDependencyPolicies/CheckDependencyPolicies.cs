using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.CheckDependencyPolicies;

/// <summary>
/// Feature: CheckDependencyPolicies — verifica violações de políticas de dependência no serviço.
/// </summary>
public static class CheckDependencyPolicies
{
    public sealed record Query(Guid ServiceId) : IQuery<IReadOnlyList<PolicyViolation>>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.ServiceId).NotEmpty();
    }

    public sealed class Handler(IServiceDependencyProfileRepository repository)
        : IQueryHandler<Query, IReadOnlyList<PolicyViolation>>
    {
        public async Task<Result<IReadOnlyList<PolicyViolation>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var profile = await repository.FindByServiceIdAsync(request.ServiceId, cancellationToken);
            if (profile is null)
                return Error.NotFound("DependencyGovernance.ProfileNotFound",
                    $"No dependency profile found for service '{request.ServiceId}'.");

            var violations = new List<PolicyViolation>();

            foreach (var dep in profile.Dependencies)
            {
                foreach (var vuln in dep.Vulnerabilities.Where(v => v.Severity == VulnerabilitySeverity.Critical))
                {
                    violations.Add(new PolicyViolation(
                        PolicyId: "POLICY-001",
                        Policy: "No critical vulnerabilities",
                        Severity: "Critical",
                        PackageName: dep.PackageName,
                        Detail: $"Critical vulnerability {vuln.CveId} (CVSS {vuln.CvssScore}) in {dep.PackageName}@{dep.Version}.",
                        Recommendation: vuln.FixedInVersion is not null
                            ? $"Upgrade to {vuln.FixedInVersion}"
                            : "No fix available — consider replacing the package."));
                }

                if (dep.LicenseRisk == LicenseRiskLevel.Critical)
                {
                    violations.Add(new PolicyViolation(
                        PolicyId: "POLICY-002",
                        Policy: "No critical-risk licenses",
                        Severity: "High",
                        PackageName: dep.PackageName,
                        Detail: $"Package {dep.PackageName}@{dep.Version} has a critical license risk (License: {dep.License ?? "unknown"}).",
                        Recommendation: "Review license compatibility or replace with an approved alternative."));
                }

                if (dep.License is null)
                {
                    violations.Add(new PolicyViolation(
                        PolicyId: "POLICY-003",
                        Policy: "All packages must have known licenses",
                        Severity: "Medium",
                        PackageName: dep.PackageName,
                        Detail: $"Package {dep.PackageName}@{dep.Version} has no declared license.",
                        Recommendation: "Verify the package license and update the dependency profile."));
                }

                if (dep.DeprecationNotice is not null)
                {
                    violations.Add(new PolicyViolation(
                        PolicyId: "POLICY-004",
                        Policy: "No deprecated packages",
                        Severity: "Medium",
                        PackageName: dep.PackageName,
                        Detail: $"Package {dep.PackageName}@{dep.Version} is deprecated: {dep.DeprecationNotice}",
                        Recommendation: dep.LatestStableVersion is not null
                            ? $"Upgrade to {dep.LatestStableVersion} or find an alternative."
                            : "Find an alternative package."));
                }
            }

            return Result<IReadOnlyList<PolicyViolation>>.Success(violations);
        }
    }

    public sealed record PolicyViolation(
        string PolicyId,
        string Policy,
        string Severity,
        string PackageName,
        string Detail,
        string Recommendation);
}
