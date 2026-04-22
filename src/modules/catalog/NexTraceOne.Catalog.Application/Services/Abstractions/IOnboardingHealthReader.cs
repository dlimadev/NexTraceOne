namespace NexTraceOne.Catalog.Application.Services.Abstractions;

/// <summary>
/// Abstração de leitura do estado de onboarding de serviços para um tenant.
///
/// Fornece dados agregados por serviço cobrindo as cinco dimensões de completude:
/// ownership, contrato aprovado, runbook aprovado, observação de SLO recente e profiling recente.
/// Desacopla o handler de onboarding health das implementações concretas de repositório.
///
/// Wave AC.1 — GetOnboardingHealthReport.
/// </summary>
public interface IOnboardingHealthReader
{
    /// <summary>
    /// Lista todas as entradas de onboarding de serviços para um tenant.
    /// Cada entrada agrega o estado das cinco dimensões de completude de um serviço.
    /// </summary>
    Task<IReadOnlyList<ServiceOnboardingEntry>> ListByTenantAsync(string tenantId, CancellationToken ct);
}

/// <summary>
/// Entrada de onboarding de um serviço — agrega o estado das cinco dimensões de completude.
/// Wave AC.1.
/// </summary>
public sealed record ServiceOnboardingEntry(
    /// <summary>Nome do serviço.</summary>
    string ServiceName,
    /// <summary>Nome da equipa proprietária, ou null se não atribuído.</summary>
    string? TeamName,
    /// <summary>Tier do serviço: Critical, Standard ou Experimental.</summary>
    string ServiceTier,
    /// <summary>Indica se o serviço tem ownership atribuído.</summary>
    bool HasOwnership,
    /// <summary>Indica se o serviço tem pelo menos um contrato aprovado.</summary>
    bool HasApprovedContract,
    /// <summary>Indica se o serviço tem runbook aprovado.</summary>
    bool HasApprovedRunbook,
    /// <summary>Indica se o serviço tem observação de SLO registada nos últimos 30 dias.</summary>
    bool HasRecentSloObservation,
    /// <summary>Indica se o serviço tem sessão de profiling registada nos últimos 90 dias.</summary>
    bool HasRecentProfiling);
