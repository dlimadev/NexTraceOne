using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.ListVulnerableDependencies;

/// <summary>
/// Feature: ListVulnerableDependencies — lista todos os perfis de serviços com vulnerabilidades
/// acima de uma severidade mínima.
/// </summary>
public static class ListVulnerableDependencies
{
    public sealed record Query(VulnerabilitySeverity MinSeverity = VulnerabilitySeverity.High) : IQuery<IReadOnlyList<Response>>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.MinSeverity).IsInEnum();
    }

    public sealed class Handler(IServiceDependencyProfileRepository repository)
        : IQueryHandler<Query, IReadOnlyList<Response>>
    {
        public async Task<Result<IReadOnlyList<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var profiles = await repository.ListWithVulnerabilitiesAsync(request.MinSeverity, cancellationToken);
            var result = profiles.Select(p => new Response(
                ProfileId: p.Id.Value,
                ServiceId: p.ServiceId,
                HealthScore: p.HealthScore,
                CriticalCount: p.Dependencies.Sum(d => d.Vulnerabilities.Count(v => v.Severity == VulnerabilitySeverity.Critical)),
                HighCount: p.Dependencies.Sum(d => d.Vulnerabilities.Count(v => v.Severity == VulnerabilitySeverity.High)),
                MediumCount: p.Dependencies.Sum(d => d.Vulnerabilities.Count(v => v.Severity == VulnerabilitySeverity.Medium)),
                LastScanAt: p.LastScanAt
            )).ToList();

            return Result<IReadOnlyList<Response>>.Success(result);
        }
    }

    public sealed record Response(
        Guid ProfileId,
        Guid ServiceId,
        int HealthScore,
        int CriticalCount,
        int HighCount,
        int MediumCount,
        DateTimeOffset LastScanAt);
}
