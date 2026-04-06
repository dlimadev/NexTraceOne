using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.CompareDependencyVersions;

/// <summary>
/// Feature: CompareDependencyVersions — compara dependências entre dois serviços.
/// </summary>
public static class CompareDependencyVersions
{
    public sealed record Query(Guid ServiceIdA, Guid ServiceIdB) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceIdA).NotEmpty();
            RuleFor(x => x.ServiceIdB).NotEmpty();
            RuleFor(x => x).Must(x => x.ServiceIdA != x.ServiceIdB)
                .WithMessage("ServiceIdA and ServiceIdB must be different.");
        }
    }

    public sealed class Handler(IServiceDependencyProfileRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var profileA = await repository.FindByServiceIdAsync(request.ServiceIdA, cancellationToken);
            if (profileA is null)
                return Error.NotFound("DependencyGovernance.ProfileNotFound",
                    $"No dependency profile found for service '{request.ServiceIdA}'.");

            var profileB = await repository.FindByServiceIdAsync(request.ServiceIdB, cancellationToken);
            if (profileB is null)
                return Error.NotFound("DependencyGovernance.ProfileNotFound",
                    $"No dependency profile found for service '{request.ServiceIdB}'.");

            var depsA = profileA.Dependencies.ToDictionary(d => (d.PackageName, d.Ecosystem));
            var depsB = profileB.Dependencies.ToDictionary(d => (d.PackageName, d.Ecosystem));

            var onlyInA = depsA.Keys.Except(depsB.Keys)
                .Select(k => new PackageRef(depsA[k].PackageName, depsA[k].Version, depsA[k].Ecosystem))
                .ToList();

            var onlyInB = depsB.Keys.Except(depsA.Keys)
                .Select(k => new PackageRef(depsB[k].PackageName, depsB[k].Version, depsB[k].Ecosystem))
                .ToList();

            var inBoth = depsA.Keys.Intersect(depsB.Keys).ToList();
            var samePairs = inBoth
                .Where(k => depsA[k].Version == depsB[k].Version)
                .Select(k => new PackageRef(depsA[k].PackageName, depsA[k].Version, depsA[k].Ecosystem))
                .ToList();

            var diffPairs = inBoth
                .Where(k => depsA[k].Version != depsB[k].Version)
                .Select(k => new VersionDiff(
                    depsA[k].PackageName,
                    depsA[k].Ecosystem,
                    depsA[k].Version,
                    depsB[k].Version))
                .ToList();

            return Result<Response>.Success(new Response(
                OnlyInA: onlyInA,
                OnlyInB: onlyInB,
                InBothSameVersion: samePairs,
                InBothDifferentVersions: diffPairs));
        }
    }

    public sealed record PackageRef(string PackageName, string Version, PackageEcosystem Ecosystem);

    public sealed record VersionDiff(
        string PackageName,
        PackageEcosystem Ecosystem,
        string VersionA,
        string VersionB);

    public sealed record Response(
        IReadOnlyList<PackageRef> OnlyInA,
        IReadOnlyList<PackageRef> OnlyInB,
        IReadOnlyList<PackageRef> InBothSameVersion,
        IReadOnlyList<VersionDiff> InBothDifferentVersions);
}
