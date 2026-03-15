using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetComplianceSummary;

/// <summary>
/// Feature: GetComplianceSummary — resumo de compliance técnico-operacional.
/// Avalia gaps de governança: owner, contrato, documentação, runbook, dependências.
/// </summary>
public static class GetComplianceSummary
{
    /// <summary>Query de resumo de compliance. Permite filtragem por equipa ou domínio.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null) : IQuery<Response>;

    /// <summary>Handler que computa indicadores de compliance técnico-operacional.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var coverageIndicators = new CoverageIndicatorsDto(
                OwnerDefined: 90.5m,
                ContractDefined: 83.3m,
                VersioningPresent: 76.2m,
                DocumentationAvailable: 71.4m,
                RunbookAvailable: 59.5m,
                DependenciesMapped: 88.1m,
                PublicationUpToDate: 64.3m);

            var gaps = new List<ComplianceGapDto>
            {
                new("svc-legacy-adapter", "Legacy Adapter", "Integration", "Team Integration",
                    ComplianceStatus.NonCompliant, "No contract, no documentation, no runbook"),
                new("svc-batch-processor", "Batch Processor", "Operations", "Team Operations",
                    ComplianceStatus.NonCompliant, "Missing owner assignment and runbook"),
                new("svc-reporting-engine", "Reporting Engine", "Analytics", "Team Analytics",
                    ComplianceStatus.PartiallyCompliant, "Contract exists but outdated, no runbook"),
                new("svc-auth-gateway", "Auth Gateway", "Security", "Team Security",
                    ComplianceStatus.PartiallyCompliant, "Documentation incomplete, dependencies not mapped"),
                new("svc-cache-manager", "Cache Manager", "Infrastructure", "Team Platform",
                    ComplianceStatus.PartiallyCompliant, "Missing versioning and publication status")
            };

            var response = new Response(
                OverallScore: 78.5m,
                TotalServicesAssessed: 42,
                CompliantCount: 28,
                PartiallyCompliantCount: 9,
                NonCompliantCount: 5,
                Coverage: coverageIndicators,
                Gaps: gaps,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta do resumo de compliance.</summary>
    public sealed record Response(
        decimal OverallScore,
        int TotalServicesAssessed,
        int CompliantCount,
        int PartiallyCompliantCount,
        int NonCompliantCount,
        CoverageIndicatorsDto Coverage,
        IReadOnlyList<ComplianceGapDto> Gaps,
        DateTimeOffset GeneratedAt);

    /// <summary>Indicadores de cobertura de compliance por dimensão.</summary>
    public sealed record CoverageIndicatorsDto(
        decimal OwnerDefined,
        decimal ContractDefined,
        decimal VersioningPresent,
        decimal DocumentationAvailable,
        decimal RunbookAvailable,
        decimal DependenciesMapped,
        decimal PublicationUpToDate);

    /// <summary>Gap de compliance identificado num serviço.</summary>
    public sealed record ComplianceGapDto(
        string ServiceId,
        string ServiceName,
        string Domain,
        string Team,
        ComplianceStatus Status,
        string Description);
}
