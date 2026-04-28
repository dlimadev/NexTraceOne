using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services;

/// <summary>
/// Implementação nula de <see cref="IRetirementReadinessReader"/> com dados de teste codificados.
///
/// Devolve dados de prontidão para retirada simulados com alguns cenários para validar 
/// o comportamento do handler <c>GetServiceRetirementReadinessReport</c> sem necessidade 
/// de base de dados. Substituir por implementação real baseada em EF/PostgreSQL em produção.
///
/// Wave AF.2 — GetServiceRetirementReadinessReport.
/// </summary>
public sealed class NullRetirementReadinessReader : IRetirementReadinessReader
{
    /// <inheritdoc />
    public Task<RetirementReadinessData?> GetByServiceAsync(
        string tenantId,
        string serviceId,
        CancellationToken ct)
    {
        // Simulated data for known service IDs
        return serviceId switch
        {
            // Serviço em estado de alta prontidão (score ≈ 85+)
            "legacy-auth-service" => Task.FromResult<RetirementReadinessData?>(
                new RetirementReadinessData(
                    ServiceId: "legacy-auth-service",
                    ServiceName: "Legacy Authentication Service",
                    TeamName: "Identity Team",
                    CurrentLifecycleState: "Sunset",
                    TotalConsumers: 10,
                    MigratedConsumers: 9,
                    TotalContracts: 3,
                    DeprecatedContracts: 3,
                    HasApprovedDecommissionRunbook: true,
                    TotalConsumerTeams: 5,
                    NotifiedConsumerTeams: 5,
                    UnmigratedConsumers:
                    [
                        new BlockerConsumerInfo(
                            ConsumerServiceName: "payments-gateway",
                            ConsumerTeamName: "Payments Team",
                            ConsumerTier: "Critical",
                            IsNotified: true)
                    ]
                )),

            // Serviço em estado médio de prontidão (score ≈ 65)
            "old-report-service" => Task.FromResult<RetirementReadinessData?>(
                new RetirementReadinessData(
                    ServiceId: "old-report-service",
                    ServiceName: "Old Report Service",
                    TeamName: "Analytics Team",
                    CurrentLifecycleState: "Deprecated",
                    TotalConsumers: 8,
                    MigratedConsumers: 4,
                    TotalContracts: 5,
                    DeprecatedContracts: 2,
                    HasApprovedDecommissionRunbook: false,
                    TotalConsumerTeams: 4,
                    NotifiedConsumerTeams: 2,
                    UnmigratedConsumers:
                    [
                        new BlockerConsumerInfo(
                            ConsumerServiceName: "dashboard-service",
                            ConsumerTeamName: "Dashboard Team",
                            ConsumerTier: "Standard",
                            IsNotified: true),
                        new BlockerConsumerInfo(
                            ConsumerServiceName: "internal-portal",
                            ConsumerTeamName: "Platform Team",
                            ConsumerTier: "High",
                            IsNotified: false)
                    ]
                )),

            // Serviço com baixa prontidão (score < 40)
            "monitoring-legacy-system" => Task.FromResult<RetirementReadinessData?>(
                new RetirementReadinessData(
                    ServiceId: "monitoring-legacy-system",
                    ServiceName: "Monitoring Legacy System",
                    TeamName: "Operations Team",
                    CurrentLifecycleState: "Active",
                    TotalConsumers: 15,
                    MigratedConsumers: 1,
                    TotalContracts: 12,
                    DeprecatedContracts: 0,
                    HasApprovedDecommissionRunbook: false,
                    TotalConsumerTeams: 8,
                    NotifiedConsumerTeams: 0,
                    UnmigratedConsumers:
                    [
                        new BlockerConsumerInfo(
                            ConsumerServiceName: "backend-service",
                            ConsumerTeamName: "Backend Team",
                            ConsumerTier: "High",
                            IsNotified: false),
                        new BlockerConsumerInfo(
                            ConsumerServiceName: "frontend-service",
                            ConsumerTeamName: "Frontend Team",
                            ConsumerTier: "Standard",
                            IsNotified: false),
                        new BlockerConsumerInfo(
                            ConsumerServiceName: "api-gateway",
                            ConsumerTeamName: "Platform Team",
                            ConsumerTier: "Critical",
                            IsNotified: false)
                    ]
                )),

            // Serviço não encontrado
            _ => Task.FromResult<RetirementReadinessData?>(null)
        };
    }
}
