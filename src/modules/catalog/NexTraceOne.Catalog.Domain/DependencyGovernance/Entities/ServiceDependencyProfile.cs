using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;

/// <summary>
/// Perfil de dependências de um serviço — aggregate root para governança de dependências.
/// Agrega PackageDependency com vulnerabilidades, licenças e estado de saúde das dependências.
/// </summary>
public sealed class ServiceDependencyProfile : AuditableEntity<ServiceDependencyProfileId>
{
    private List<PackageDependency> _dependencies = new();
    private ServiceDependencyProfile() { }

    public Guid ServiceId { get; private set; }
    public Guid? TemplateId { get; private set; }
    public DateTimeOffset LastScanAt { get; private set; }
    public SbomFormat SbomFormat { get; private set; }
    public string? SbomContent { get; private set; }
    public int HealthScore { get; private set; } = 100;
    public int TotalDependencies { get; private set; }
    public int DirectDependencies { get; private set; }
    public int TransitiveDependencies { get; private set; }
    public IReadOnlyList<PackageDependency> Dependencies => _dependencies.AsReadOnly();

    public static ServiceDependencyProfile Create(Guid serviceId, Guid? templateId = null)
    {
        Guard.Against.Default(serviceId);
        return new ServiceDependencyProfile
        {
            Id = ServiceDependencyProfileId.New(),
            ServiceId = serviceId,
            TemplateId = templateId,
            LastScanAt = DateTimeOffset.UtcNow,
            SbomFormat = SbomFormat.CycloneDx,
            HealthScore = 100
        };
    }

    public void UpdateScan(IReadOnlyList<PackageDependency> dependencies, string? sbomContent, SbomFormat format)
    {
        Guard.Against.Null(dependencies);
        _dependencies.Clear();
        _dependencies.AddRange(dependencies);

        LastScanAt = DateTimeOffset.UtcNow;
        SbomContent = sbomContent;
        SbomFormat = format;
        TotalDependencies = dependencies.Count;
        DirectDependencies = dependencies.Count(d => d.IsDirect);
        TransitiveDependencies = TotalDependencies - DirectDependencies;

        HealthScore = CalculateHealthScore(dependencies);
    }

    public void UpdateSbomContent(string sbomContent, SbomFormat format)
    {
        SbomContent = sbomContent;
        SbomFormat = format;
    }

    private static int CalculateHealthScore(IReadOnlyList<PackageDependency> dependencies)
    {
        var score = 100;
        foreach (var dep in dependencies)
        {
            foreach (var vuln in dep.Vulnerabilities)
            {
                score -= vuln.Severity switch
                {
                    VulnerabilitySeverity.Critical => 25,
                    VulnerabilitySeverity.High => 15,
                    VulnerabilitySeverity.Medium => 5,
                    _ => 0
                };
            }
        }
        if (dependencies.Count > 0)
        {
            var outdatedRatio = (double)dependencies.Count(d => d.IsOutdated) / dependencies.Count;
            if (outdatedRatio > 0.20) score -= 10;
        }
        return Math.Max(0, score);
    }
}
