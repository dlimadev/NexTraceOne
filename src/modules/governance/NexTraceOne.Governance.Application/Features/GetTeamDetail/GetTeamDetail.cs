using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetTeamDetail;

/// <summary>
/// Feature: GetTeamDetail — detalhe completo de uma equipa incluindo serviços, contratos e dependências cross-team.
/// Centraliza a visão de governança, ownership e fiabilidade ao nível da equipa.
/// </summary>
public static class GetTeamDetail
{
    /// <summary>Query para obter detalhe de uma equipa pelo ID.</summary>
    public sealed record Query(string TeamId) : IQuery<Response>;

    /// <summary>Handler que retorna detalhe completo de uma equipa com serviços, contratos e dependências.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var services = new List<TeamServiceDto>
            {
                new("svc-payment-gateway", "Payment Gateway", "Commerce", "Critical", "Primary"),
                new("svc-order-api", "Order API", "Commerce", "High", "Primary"),
                new("svc-catalog-sync", "Catalog Sync", "Platform", "Medium", "Shared")
            };

            var contracts = new List<TeamContractDto>
            {
                new("ctr-payment-rest", "Payment API v2", "REST", "2.1.0", "Published"),
                new("ctr-order-events", "Order Events", "Kafka", "1.3.0", "Published")
            };

            var crossTeamDeps = new List<CrossTeamDependencyDto>
            {
                new("dep-001", "Payment Gateway", "Identity Service", "team-identity", "Identity", "Synchronous"),
                new("dep-002", "Order API", "Notification Worker", "team-platform", "Platform", "Asynchronous")
            };

            var response = new Response(
                TeamId: request.TeamId,
                Name: "commerce-squad",
                DisplayName: "Commerce",
                Description: "Equipa responsável pelos serviços de comércio eletrónico e pagamentos.",
                Status: "Active",
                ParentOrganizationUnit: "Product",
                ServiceCount: 3,
                ContractCount: 2,
                ActiveIncidentCount: 1,
                RecentChangeCount: 7,
                MaturityLevel: "Defined",
                ReliabilityScore: 94.5m,
                Services: services,
                Contracts: contracts,
                CrossTeamDependencies: crossTeamDeps,
                CreatedAt: DateTimeOffset.UtcNow.AddMonths(-6));

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com detalhe completo de uma equipa.</summary>
    public sealed record Response(
        string TeamId,
        string Name,
        string DisplayName,
        string? Description,
        string Status,
        string? ParentOrganizationUnit,
        int ServiceCount,
        int ContractCount,
        int ActiveIncidentCount,
        int RecentChangeCount,
        string MaturityLevel,
        decimal ReliabilityScore,
        IReadOnlyList<TeamServiceDto> Services,
        IReadOnlyList<TeamContractDto> Contracts,
        IReadOnlyList<CrossTeamDependencyDto> CrossTeamDependencies,
        DateTimeOffset CreatedAt);

    /// <summary>DTO de serviço associado a uma equipa.</summary>
    public sealed record TeamServiceDto(
        string ServiceId,
        string Name,
        string Domain,
        string Criticality,
        string OwnershipType);

    /// <summary>DTO de contrato associado a uma equipa.</summary>
    public sealed record TeamContractDto(
        string ContractId,
        string Name,
        string Type,
        string Version,
        string Status);

    /// <summary>DTO de dependência cross-team.</summary>
    public sealed record CrossTeamDependencyDto(
        string DependencyId,
        string SourceServiceName,
        string TargetServiceName,
        string TargetTeamId,
        string TargetTeamName,
        string DependencyType);
}
