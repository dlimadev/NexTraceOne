using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.DetectLicenseConflicts;

/// <summary>
/// Feature: DetectLicenseConflicts — detecta conflitos de licença entre dependências do serviço.
/// GPL licenças são incompatíveis com MIT/Apache/BSD em muitos casos.
/// </summary>
public static class DetectLicenseConflicts
{
    public sealed record Query(Guid ServiceId) : IQuery<IReadOnlyList<LicenseConflict>>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.ServiceId).NotEmpty();
    }

    public sealed class Handler(IServiceDependencyProfileRepository repository)
        : IQueryHandler<Query, IReadOnlyList<LicenseConflict>>
    {
        private static readonly HashSet<string> CopyLeftLicenses = new(StringComparer.OrdinalIgnoreCase)
        {
            "GPL-2.0", "GPL-3.0", "GPL-2.0-only", "GPL-3.0-only",
            "LGPL-2.1", "LGPL-3.0", "AGPL-3.0"
        };

        private static readonly HashSet<string> PermissiveLicenses = new(StringComparer.OrdinalIgnoreCase)
        {
            "MIT", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause",
            "ISC", "Unlicense", "0BSD"
        };

        public async Task<Result<IReadOnlyList<LicenseConflict>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var profile = await repository.FindByServiceIdAsync(request.ServiceId, cancellationToken);
            if (profile is null)
                return Error.NotFound("DependencyGovernance.ProfileNotFound",
                    $"No dependency profile found for service '{request.ServiceId}'.");

            var conflicts = new List<LicenseConflict>();
            var depsWithLicenses = profile.Dependencies
                .Where(d => d.License is not null)
                .ToList();

            var copyLeft = depsWithLicenses.Where(d => IsCopyLeft(d.License!)).ToList();
            var permissive = depsWithLicenses.Where(d => IsPermissive(d.License!)).ToList();

            foreach (var cl in copyLeft)
            {
                foreach (var perm in permissive)
                {
                    conflicts.Add(new LicenseConflict(
                        PackageA: cl.PackageName,
                        LicenseA: cl.License!,
                        PackageB: perm.PackageName,
                        LicenseB: perm.License!,
                        Reason: $"Copyleft license '{cl.License}' on {cl.PackageName} may be incompatible with permissive license '{perm.License}' on {perm.PackageName}.",
                        Severity: "High"));
                }
            }

            return Result<IReadOnlyList<LicenseConflict>>.Success(conflicts);
        }

        private static bool IsCopyLeft(string license) => CopyLeftLicenses.Contains(license);
        private static bool IsPermissive(string license) => PermissiveLicenses.Contains(license);
    }

    public sealed record LicenseConflict(
        string PackageA,
        string LicenseA,
        string PackageB,
        string LicenseB,
        string Reason,
        string Severity);
}
