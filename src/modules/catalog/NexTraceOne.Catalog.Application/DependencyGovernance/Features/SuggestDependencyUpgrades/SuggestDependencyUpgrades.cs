using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.SuggestDependencyUpgrades;

/// <summary>
/// Feature: SuggestDependencyUpgrades — sugere upgrades para dependências desatualizadas ou vulneráveis.
/// </summary>
public static class SuggestDependencyUpgrades
{
    public sealed record Query(Guid ServiceId) : IQuery<IReadOnlyList<UpgradeSuggestion>>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.ServiceId).NotEmpty();
    }

    public sealed class Handler(IServiceDependencyProfileRepository repository)
        : IQueryHandler<Query, IReadOnlyList<UpgradeSuggestion>>
    {
        public async Task<Result<IReadOnlyList<UpgradeSuggestion>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var profile = await repository.FindByServiceIdAsync(request.ServiceId, cancellationToken);
            if (profile is null)
                return Error.NotFound("DependencyGovernance.ProfileNotFound",
                    $"No dependency profile found for service '{request.ServiceId}'.");

            var suggestions = new List<UpgradeSuggestion>();

            foreach (var dep in profile.Dependencies)
            {
                var reasons = new List<string>();
                string? targetVersion = null;

                if (dep.IsOutdated && dep.LatestStableVersion is not null)
                {
                    reasons.Add($"Outdated: current {dep.Version}, latest {dep.LatestStableVersion}");
                    targetVersion = dep.LatestStableVersion;
                }

                var criticalVulns = dep.Vulnerabilities
                    .Where(v => v.Severity >= VulnerabilitySeverity.High && v.FixedInVersion is not null)
                    .ToList();
                if (criticalVulns.Count > 0)
                {
                    var fixVersions = criticalVulns.Select(v => v.FixedInVersion!).Distinct();
                    reasons.Add($"Vulnerabilities with fixes: {string.Join(", ", criticalVulns.Select(v => v.CveId))}");
                    targetVersion ??= criticalVulns.FirstOrDefault()?.FixedInVersion;
                }

                if (dep.DeprecationNotice is not null)
                    reasons.Add($"Deprecated: {dep.DeprecationNotice}");

                if (reasons.Count > 0)
                {
                    suggestions.Add(new UpgradeSuggestion(
                        PackageName: dep.PackageName,
                        CurrentVersion: dep.Version,
                        SuggestedVersion: targetVersion,
                        Ecosystem: dep.Ecosystem,
                        Reasons: reasons,
                        Priority: dep.Vulnerabilities.Any(v => v.Severity == VulnerabilitySeverity.Critical)
                            ? "Critical" : dep.Vulnerabilities.Any(v => v.Severity == VulnerabilitySeverity.High)
                            ? "High" : "Medium"));
                }
            }

            return Result<IReadOnlyList<UpgradeSuggestion>>.Success(
                suggestions.OrderByDescending(s => s.Priority).ToList());
        }
    }

    public sealed record UpgradeSuggestion(
        string PackageName,
        string CurrentVersion,
        string? SuggestedVersion,
        PackageEcosystem Ecosystem,
        IReadOnlyList<string> Reasons,
        string Priority);
}
