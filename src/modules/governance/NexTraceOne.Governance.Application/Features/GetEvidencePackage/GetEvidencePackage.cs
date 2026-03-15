using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetEvidencePackage;

/// <summary>
/// Feature: GetEvidencePackage — detalhe de um pacote de evidência.
/// </summary>
public static class GetEvidencePackage
{
    /// <summary>Query para obter detalhe de um pacote de evidência.</summary>
    public sealed record Query(string PackageId) : IQuery<Response>;

    /// <summary>Handler que retorna detalhe de um pacote de evidência.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var items = new List<EvidenceItemDto>
            {
                new("evi-001", EvidenceType.Approval, "Change Approval #CH-2026-0142",
                    "Production deployment approved by Tech Lead", "change-governance",
                    "CH-2026-0142", "techlead@nextraceone.com", DateTimeOffset.UtcNow.AddDays(-16)),
                new("evi-002", EvidenceType.ChangeHistory, "Release v3.2.0 Deployment",
                    "Successful production deployment with blast radius assessment", "change-governance",
                    "REL-2026-0089", "ci-system", DateTimeOffset.UtcNow.AddDays(-16)),
                new("evi-003", EvidenceType.ContractPublication, "Order API Contract v2.1.0",
                    "Contract published with breaking change notification", "catalog",
                    "CTR-ORDER-API-2.1.0", "architect@nextraceone.com", DateTimeOffset.UtcNow.AddDays(-18)),
                new("evi-004", EvidenceType.ComplianceResult, "Q1 Compliance Check Run",
                    "Quarterly compliance evaluation: 78% coverage", "governance",
                    "CHK-RUN-2026-Q1", "system", DateTimeOffset.UtcNow.AddDays(-15)),
                new("evi-005", EvidenceType.AiUsageRecord, "AI Assistant Usage Summary",
                    "AI assistant usage within approved policy limits", "ai-governance",
                    "AI-USAGE-2026-03", "system", DateTimeOffset.UtcNow.AddDays(-15)),
                new("evi-006", EvidenceType.MitigationRecord, "Incident INC-2026-0034 Mitigation",
                    "Incident resolved with documented mitigation steps", "operations",
                    "INC-2026-0034", "oncall@nextraceone.com", DateTimeOffset.UtcNow.AddDays(-20)),
                new("evi-007", EvidenceType.PolicyDecision, "AI External Model Block",
                    "External AI model usage blocked per policy pol-005", "ai-governance",
                    "POL-DEC-2026-0012", "system", DateTimeOffset.UtcNow.AddDays(-17)),
                new("evi-008", EvidenceType.AuditReference, "Audit Chain Verification",
                    "Audit chain integrity verified for Q1 2026", "audit",
                    "AUDIT-VERIFY-2026-Q1", "system", DateTimeOffset.UtcNow.AddDays(-15))
            };

            var detail = new EvidencePackageDetailDto(
                PackageId: request.PackageId,
                Name: "Q1 2026 Compliance Evidence",
                Description: "Quarterly compliance evidence package for production services",
                Scope: "quarterly-review",
                Status: EvidencePackageStatus.Sealed,
                CreatedBy: "auditor@nextraceone.com",
                CreatedAt: DateTimeOffset.UtcNow.AddDays(-15),
                SealedAt: DateTimeOffset.UtcNow.AddDays(-14),
                Items: items);

            return Task.FromResult(Result<Response>.Success(new Response(detail)));
        }
    }

    /// <summary>Resposta com detalhe do pacote de evidência.</summary>
    public sealed record Response(EvidencePackageDetailDto Package);

    /// <summary>DTO de detalhe de pacote de evidência.</summary>
    public sealed record EvidencePackageDetailDto(
        string PackageId,
        string Name,
        string Description,
        string Scope,
        EvidencePackageStatus Status,
        string CreatedBy,
        DateTimeOffset CreatedAt,
        DateTimeOffset? SealedAt,
        IReadOnlyList<EvidenceItemDto> Items);

    /// <summary>DTO de item de evidência.</summary>
    public sealed record EvidenceItemDto(
        string ItemId,
        EvidenceType Type,
        string Title,
        string Description,
        string SourceModule,
        string ReferenceId,
        string RecordedBy,
        DateTimeOffset RecordedAt);
}
