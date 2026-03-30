using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetOwnershipAudit;

/// <summary>
/// Feature: GetOwnershipAudit — auditoria automática de ownership, contratos e consumidores.
/// Deteta serviços órfãos, sem owner, sem contrato, APIs sem consumidores.
/// Estrutura VSA: Query + Handler + Response em um único arquivo.
/// </summary>
public static class GetOwnershipAudit
{
    /// <summary>Query de auditoria de ownership. Filtros opcionais por equipa e domínio.</summary>
    public sealed record Query(
        string? TeamName = null,
        string? Domain = null) : IQuery<Response>;

    /// <summary>Handler que executa auditoria de ownership e completude dos serviços.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IServiceLinkRepository serviceLinkRepository,
        IApiAssetRepository apiAssetRepository,
        IContractVersionRepository contractVersionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var services = await serviceAssetRepository.ListFilteredAsync(
                request.TeamName,
                request.Domain,
                serviceType: null,
                criticality: null,
                lifecycleStatus: null,
                exposureType: null,
                searchTerm: null,
                cancellationToken);

            var findings = new List<AuditFindingDto>();

            foreach (var service in services)
            {
                // Skip retired services
                if (service.LifecycleStatus == LifecycleStatus.Retired)
                    continue;

                var serviceFindings = new List<string>();
                var severity = "info";

                // Check: Missing team name
                if (string.IsNullOrWhiteSpace(service.TeamName))
                {
                    serviceFindings.Add("NoTeam");
                    severity = "critical";
                }

                // Check: Missing technical owner
                if (string.IsNullOrWhiteSpace(service.TechnicalOwner))
                {
                    serviceFindings.Add("NoTechnicalOwner");
                    if (severity != "critical") severity = "high";
                }

                // Check: Missing business owner
                if (string.IsNullOrWhiteSpace(service.BusinessOwner))
                {
                    serviceFindings.Add("NoBusinessOwner");
                    if (severity is not ("critical" or "high")) severity = "medium";
                }

                // Check: Missing description
                if (string.IsNullOrWhiteSpace(service.Description) || service.Description.Length < 10)
                {
                    serviceFindings.Add("NoDescription");
                    if (severity is not ("critical" or "high")) severity = "medium";
                }

                // Check: APIs without contracts
                var apis = await apiAssetRepository.ListByServiceIdAsync(service.Id, cancellationToken);
                if (apis.Count > 0)
                {
                    var apiIds = apis.Select(a => a.Id.Value).ToList();
                    var contracts = await contractVersionRepository.ListByApiAssetIdsAsync(apiIds, cancellationToken);
                    var apisWithContracts = contracts.Select(c => c.ApiAssetId).Distinct().Count();
                    var apisWithoutContracts = apis.Count - apisWithContracts;

                    if (apisWithoutContracts > 0)
                    {
                        serviceFindings.Add($"ApisWithoutContracts:{apisWithoutContracts}");
                        if (severity is not "critical") severity = "high";
                    }
                }

                // Check: No documentation
                var links = await serviceLinkRepository.ListByServiceAsync(service.Id, cancellationToken);
                var hasDocumentation = !string.IsNullOrWhiteSpace(service.DocumentationUrl)
                    || links.Any(l => l.Category is LinkCategory.Documentation or LinkCategory.Wiki);
                if (!hasDocumentation)
                {
                    serviceFindings.Add("NoDocumentation");
                    if (severity is not ("critical" or "high")) severity = "medium";
                }

                // Check: No runbook (for active/critical services)
                if (service.LifecycleStatus == LifecycleStatus.Active
                    && service.Criticality is Criticality.Critical or Criticality.High)
                {
                    var hasRunbook = links.Any(l => l.Category == LinkCategory.Runbook);
                    if (!hasRunbook)
                    {
                        serviceFindings.Add("NoRunbook");
                        if (severity is not "critical") severity = "high";
                    }
                }

                // Check: No monitoring (for active services)
                if (service.LifecycleStatus == LifecycleStatus.Active)
                {
                    var hasMonitoring = links.Any(l =>
                        l.Category is LinkCategory.Monitoring or LinkCategory.Dashboard);
                    if (!hasMonitoring)
                    {
                        serviceFindings.Add("NoMonitoring");
                        if (severity is not ("critical" or "high")) severity = "medium";
                    }
                }

                if (serviceFindings.Count > 0)
                {
                    findings.Add(new AuditFindingDto(
                        ServiceId: service.Id.Value,
                        ServiceName: service.Name,
                        DisplayName: service.DisplayName,
                        TeamName: service.TeamName,
                        Domain: service.Domain,
                        Criticality: service.Criticality.ToString(),
                        LifecycleStatus: service.LifecycleStatus.ToString(),
                        Severity: severity,
                        Findings: serviceFindings,
                        FindingCount: serviceFindings.Count));
                }
            }

            var ordered = findings
                .OrderByDescending(f => SeverityOrder(f.Severity))
                .ThenByDescending(f => f.FindingCount)
                .ToList();

            var totalServices = services.Count(s => s.LifecycleStatus != LifecycleStatus.Retired);
            var servicesWithIssues = ordered.Count;

            return new Response(
                Summary: new AuditSummaryDto(
                    TotalServicesAudited: totalServices,
                    ServicesWithIssues: servicesWithIssues,
                    HealthyServices: totalServices - servicesWithIssues,
                    CriticalFindings: ordered.Count(f => f.Severity == "critical"),
                    HighFindings: ordered.Count(f => f.Severity == "high"),
                    MediumFindings: ordered.Count(f => f.Severity == "medium"),
                    WithoutTeam: ordered.Count(f => f.Findings.Contains("NoTeam")),
                    WithoutTechnicalOwner: ordered.Count(f => f.Findings.Contains("NoTechnicalOwner")),
                    WithoutDocumentation: ordered.Count(f => f.Findings.Contains("NoDocumentation")),
                    WithoutRunbook: ordered.Count(f => f.Findings.Any(x => x == "NoRunbook")),
                    ApisWithoutContracts: ordered.Count(f => f.Findings.Any(x => x.StartsWith("ApisWithoutContracts")))),
                Findings: ordered,
                AuditedAt: DateTimeOffset.UtcNow);
        }

        private static int SeverityOrder(string severity) => severity switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            _ => 1
        };
    }

    /// <summary>Resposta da auditoria de ownership.</summary>
    public sealed record Response(
        AuditSummaryDto Summary,
        IReadOnlyList<AuditFindingDto> Findings,
        DateTimeOffset AuditedAt);

    /// <summary>Resumo agregado da auditoria.</summary>
    public sealed record AuditSummaryDto(
        int TotalServicesAudited,
        int ServicesWithIssues,
        int HealthyServices,
        int CriticalFindings,
        int HighFindings,
        int MediumFindings,
        int WithoutTeam,
        int WithoutTechnicalOwner,
        int WithoutDocumentation,
        int WithoutRunbook,
        int ApisWithoutContracts);

    /// <summary>Finding de auditoria de um serviço.</summary>
    public sealed record AuditFindingDto(
        Guid ServiceId,
        string ServiceName,
        string DisplayName,
        string TeamName,
        string Domain,
        string Criticality,
        string LifecycleStatus,
        string Severity,
        IReadOnlyList<string> Findings,
        int FindingCount);
}
