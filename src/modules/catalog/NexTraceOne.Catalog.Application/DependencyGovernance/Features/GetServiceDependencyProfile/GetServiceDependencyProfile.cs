using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.DependencyGovernance.ValueObjects;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetServiceDependencyProfile;

/// <summary>
/// Feature: GetServiceDependencyProfile — retorna o perfil completo de dependências de um serviço.
/// </summary>
public static class GetServiceDependencyProfile
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

            return Result<Response>.Success(MapResponse(profile));
        }

        private static Response MapResponse(ServiceDependencyProfile p) => new(
            ProfileId: p.Id.Value,
            ServiceId: p.ServiceId,
            TemplateId: p.TemplateId,
            LastScanAt: p.LastScanAt,
            HealthScore: p.HealthScore,
            TotalDependencies: p.TotalDependencies,
            DirectDependencies: p.DirectDependencies,
            TransitiveDependencies: p.TransitiveDependencies,
            SbomFormat: p.SbomFormat,
            HasSbom: p.SbomContent is not null,
            Dependencies: p.Dependencies.Select(d => new DependencyDto(
                Id: d.Id,
                PackageName: d.PackageName,
                Version: d.Version,
                Ecosystem: d.Ecosystem,
                IsDirect: d.IsDirect,
                License: d.License,
                LicenseRisk: d.LicenseRisk,
                IsOutdated: d.IsOutdated,
                LatestStableVersion: d.LatestStableVersion,
                DeprecationNotice: d.DeprecationNotice,
                Vulnerabilities: d.Vulnerabilities.Select(v => new VulnerabilityDto(
                    CveId: v.CveId,
                    Severity: v.Severity,
                    CvssScore: v.CvssScore,
                    Description: v.Description,
                    FixedInVersion: v.FixedInVersion,
                    ExploitMaturity: v.ExploitMaturity)).ToList()
            )).ToList());
    }

    public sealed record Response(
        Guid ProfileId,
        Guid ServiceId,
        Guid? TemplateId,
        DateTimeOffset LastScanAt,
        int HealthScore,
        int TotalDependencies,
        int DirectDependencies,
        int TransitiveDependencies,
        SbomFormat SbomFormat,
        bool HasSbom,
        IReadOnlyList<DependencyDto> Dependencies);

    public sealed record DependencyDto(
        Guid Id,
        string PackageName,
        string Version,
        PackageEcosystem Ecosystem,
        bool IsDirect,
        string? License,
        LicenseRiskLevel LicenseRisk,
        bool IsOutdated,
        string? LatestStableVersion,
        string? DeprecationNotice,
        IReadOnlyList<VulnerabilityDto> Vulnerabilities);

    public sealed record VulnerabilityDto(
        string CveId,
        VulnerabilitySeverity Severity,
        decimal CvssScore,
        string Description,
        string? FixedInVersion,
        ExploitMaturity ExploitMaturity);
}
