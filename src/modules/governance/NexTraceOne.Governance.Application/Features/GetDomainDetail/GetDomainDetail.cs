using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetDomainDetail;

/// <summary>
/// Feature: GetDomainDetail — detalhe completo de um domínio de negócio incluindo equipas, serviços e dependências cross-domain.
/// Centraliza a visão de governança, criticidade e fiabilidade ao nível do domínio.
/// </summary>
public static class GetDomainDetail
{
    /// <summary>Query para obter detalhe de um domínio pelo ID.</summary>
    public sealed record Query(string DomainId) : IQuery<Response>;

    /// <summary>Handler que retorna detalhe completo de um domínio com equipas, serviços e dependências.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var teams = new List<DomainTeamDto>
            {
                new("team-commerce", "commerce-squad", "Commerce", 5, "Primary"),
                new("team-platform", "platform-squad", "Platform", 2, "Shared"),
                new("team-data", "data-squad", "Data & Analytics", 1, "Delegated")
            };

            var services = new List<DomainServiceDto>
            {
                new("svc-payment-gateway", "Payment Gateway", "Commerce", "Critical", "Active"),
                new("svc-order-api", "Order API", "Commerce", "High", "Active"),
                new("svc-catalog-sync", "Catalog Sync", "Platform", "Medium", "Active"),
                new("svc-pricing-engine", "Pricing Engine", "Commerce", "High", "Active")
            };

            var crossDomainDeps = new List<CrossDomainDependencyDto>
            {
                new("dep-cd-001", "Payment Gateway", "Commerce", "Identity Service", "domain-identity", "Identity", "Synchronous"),
                new("dep-cd-002", "Order API", "Commerce", "Notification Worker", "domain-platform", "Platform", "Asynchronous")
            };

            var response = new Response(
                DomainId: request.DomainId,
                Name: "commerce",
                DisplayName: "Commerce",
                Description: "Domínio de comércio eletrónico, pagamentos e gestão de encomendas.",
                Criticality: "Critical",
                CapabilityClassification: "Revenue-Generating",
                TeamCount: 3,
                ServiceCount: 4,
                ActiveIncidentCount: 2,
                RecentChangeCount: 11,
                MaturityLevel: "Defined",
                ReliabilityScore: 92.3m,
                Teams: teams,
                Services: services,
                CrossDomainDependencies: crossDomainDeps,
                CreatedAt: DateTimeOffset.UtcNow.AddMonths(-12));

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com detalhe completo de um domínio.</summary>
    public sealed record Response(
        string DomainId,
        string Name,
        string DisplayName,
        string? Description,
        string Criticality,
        string? CapabilityClassification,
        int TeamCount,
        int ServiceCount,
        int ActiveIncidentCount,
        int RecentChangeCount,
        string MaturityLevel,
        decimal ReliabilityScore,
        IReadOnlyList<DomainTeamDto> Teams,
        IReadOnlyList<DomainServiceDto> Services,
        IReadOnlyList<CrossDomainDependencyDto> CrossDomainDependencies,
        DateTimeOffset CreatedAt);

    /// <summary>DTO de equipa associada a um domínio.</summary>
    public sealed record DomainTeamDto(
        string TeamId,
        string Name,
        string DisplayName,
        int ServiceCount,
        string OwnershipType);

    /// <summary>DTO de serviço pertencente a um domínio.</summary>
    public sealed record DomainServiceDto(
        string ServiceId,
        string Name,
        string TeamName,
        string Criticality,
        string Status);

    /// <summary>DTO de dependência cross-domain.</summary>
    public sealed record CrossDomainDependencyDto(
        string DependencyId,
        string SourceServiceName,
        string SourceDomainName,
        string TargetServiceName,
        string TargetDomainId,
        string TargetDomainName,
        string DependencyType);
}
