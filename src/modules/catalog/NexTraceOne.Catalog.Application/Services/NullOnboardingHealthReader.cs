using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services;

/// <summary>
/// Implementação nula de <see cref="IOnboardingHealthReader"/> com dados de teste codificados.
///
/// Devolve cinco serviços com completude variada para validar o comportamento
/// do handler <c>GetOnboardingHealthReport</c> sem necessidade de base de dados.
/// Substituir por implementação real baseada em EF/PostgreSQL em produção.
///
/// Wave AC.1 — GetOnboardingHealthReport.
/// </summary>
public sealed class NullOnboardingHealthReader : IOnboardingHealthReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ServiceOnboardingEntry>> ListByTenantAsync(
        string tenantId,
        CancellationToken ct)
    {
        IReadOnlyList<ServiceOnboardingEntry> entries =
        [
            // Serviço completamente integrado (todas as dimensões presentes)
            new ServiceOnboardingEntry(
                ServiceName: "order-service",
                TeamName: "team-payments",
                ServiceTier: "Critical",
                HasOwnership: true,
                HasApprovedContract: true,
                HasApprovedRunbook: true,
                HasRecentSloObservation: true,
                HasRecentProfiling: true),

            // Serviço sem profiling e sem SLO recente
            new ServiceOnboardingEntry(
                ServiceName: "inventory-service",
                TeamName: "team-logistics",
                ServiceTier: "Standard",
                HasOwnership: true,
                HasApprovedContract: true,
                HasApprovedRunbook: true,
                HasRecentSloObservation: false,
                HasRecentProfiling: false),

            // Serviço sem contrato e sem runbook
            new ServiceOnboardingEntry(
                ServiceName: "notification-service",
                TeamName: "team-platform",
                ServiceTier: "Standard",
                HasOwnership: true,
                HasApprovedContract: false,
                HasApprovedRunbook: false,
                HasRecentSloObservation: true,
                HasRecentProfiling: false),

            // Serviço sem ownership (mínimo possível)
            new ServiceOnboardingEntry(
                ServiceName: "legacy-gateway",
                TeamName: null,
                ServiceTier: "Experimental",
                HasOwnership: false,
                HasApprovedContract: false,
                HasApprovedRunbook: false,
                HasRecentSloObservation: false,
                HasRecentProfiling: false),

            // Serviço em estágio avançado — apenas sem profiling
            new ServiceOnboardingEntry(
                ServiceName: "auth-service",
                TeamName: "team-security",
                ServiceTier: "Critical",
                HasOwnership: true,
                HasApprovedContract: true,
                HasApprovedRunbook: true,
                HasRecentSloObservation: true,
                HasRecentProfiling: false),
        ];

        return Task.FromResult(entries);
    }
}
