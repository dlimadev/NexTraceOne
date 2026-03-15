using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListEvidencePackages;

/// <summary>
/// Feature: ListEvidencePackages — lista pacotes de evidência disponíveis.
/// Pacotes agrupam evidências de aprovações, mudanças, contratos, IA e mitigação.
/// </summary>
public static class ListEvidencePackages
{
    /// <summary>Query para listar pacotes de evidência.</summary>
    public sealed record Query(
        string? Scope = null,
        string? Status = null) : IQuery<Response>;

    /// <summary>Handler que retorna pacotes de evidência.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var packages = new List<EvidencePackageDto>
            {
                new("evp-001", "Q1 2026 Compliance Evidence", "Quarterly compliance evidence package for production services",
                    "quarterly-review", EvidencePackageStatus.Sealed, 24,
                    new[] { "Approvals", "Change History", "Contract Publications", "Compliance Results" },
                    "auditor@nextraceone.com", DateTimeOffset.UtcNow.AddDays(-15), DateTimeOffset.UtcNow.AddDays(-14)),
                new("evp-002", "Payment Gateway Security Review", "Security compliance evidence for PCI-DSS audit",
                    "security-audit", EvidencePackageStatus.Exported, 18,
                    new[] { "Security Reviews", "Change Validations", "AI Usage Records", "Mitigation Records" },
                    "security@nextraceone.com", DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-28)),
                new("evp-003", "AI Governance Audit Pack", "Evidence package for AI model usage and policy compliance",
                    "ai-governance", EvidencePackageStatus.Sealed, 35,
                    new[] { "AI Usage Records", "Policy Decisions", "Model Registry Snapshots", "Token Usage" },
                    "ai-governance@nextraceone.com", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow.AddDays(-6)),
                new("evp-004", "Change Governance March 2026", "Change validation and blast radius evidence for March releases",
                    "change-governance", EvidencePackageStatus.Draft, 12,
                    new[] { "Change History", "Blast Radius", "Approval History", "Rollback Records" },
                    "release-mgr@nextraceone.com", DateTimeOffset.UtcNow.AddDays(-2), null),
                new("evp-005", "Incident Mitigation Evidence", "Post-incident mitigation and resolution evidence pack",
                    "incident-review", EvidencePackageStatus.Sealed, 8,
                    new[] { "Mitigation Records", "Approval History", "Post-mortem References", "Audit Trails" },
                    "ops-lead@nextraceone.com", DateTimeOffset.UtcNow.AddDays(-20), DateTimeOffset.UtcNow.AddDays(-19))
            };

            var response = new Response(
                TotalPackages: packages.Count,
                SealedCount: packages.Count(p => p.Status == EvidencePackageStatus.Sealed),
                ExportedCount: packages.Count(p => p.Status == EvidencePackageStatus.Exported),
                DraftCount: packages.Count(p => p.Status == EvidencePackageStatus.Draft),
                Packages: packages);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com lista de pacotes de evidência.</summary>
    public sealed record Response(
        int TotalPackages,
        int SealedCount,
        int ExportedCount,
        int DraftCount,
        IReadOnlyList<EvidencePackageDto> Packages);

    /// <summary>DTO de pacote de evidência.</summary>
    public sealed record EvidencePackageDto(
        string PackageId,
        string Name,
        string Description,
        string Scope,
        EvidencePackageStatus Status,
        int ItemCount,
        string[] IncludedTypes,
        string CreatedBy,
        DateTimeOffset CreatedAt,
        DateTimeOffset? SealedAt);
}
