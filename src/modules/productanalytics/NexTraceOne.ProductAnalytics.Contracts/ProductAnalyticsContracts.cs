using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.ProductAnalytics.Contracts;

/// <summary>
/// Contratos públicos do módulo ProductAnalytics para comunicação entre módulos.
/// Integration Events e DTOs partilhados.
/// </summary>
public static class ProductAnalyticsContracts
{
    /// <summary>Nome do módulo para identificação em contratos.</summary>
    public const string ModuleName = "ProductAnalytics";
}

// ── Service Interfaces ──────────────────────────────────────────────────────

/// <summary>
/// Interface pública do módulo ProductAnalytics.
/// Outros módulos que precisarem de métricas de uso de produto devem usar este contrato —
/// nunca acessar o DbContext ou repositórios diretamente.
/// Garante isolamento de base de dados entre serviços.
/// </summary>
public interface IProductAnalyticsModule
{
    /// <summary>
    /// Obtém a contagem de eventos de um módulo específico num período.
    /// Utilizado pelo módulo Governance para métricas de adoção.
    /// </summary>
    Task<long> GetModuleEventCountAsync(string moduleName, DateTimeOffset from, DateTimeOffset until, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém as personas ativas (que geraram eventos) num período.
    /// Utilizado pelo módulo Governance para métricas de adoção por persona.
    /// </summary>
    Task<IReadOnlyList<string>> GetActivePersonasAsync(DateTimeOffset from, DateTimeOffset until, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o sumário analítico consolidado para um módulo e período.
    /// </summary>
    Task<AnalyticsSummaryDto?> GetModuleSummaryAsync(string moduleName, DateTimeOffset from, DateTimeOffset until, CancellationToken cancellationToken = default);
}

// ── Shared DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Sumário analítico consolidado para consumo cross-module.
/// Inclui métricas de adoção, utilização e valor por módulo/período.
/// </summary>
public sealed record AnalyticsSummaryDto(
    string ModuleName,
    long TotalEvents,
    int UniqueUsers,
    int UniquePersonas,
    decimal AdoptionRate,
    DateTimeOffset From,
    DateTimeOffset Until);

// ── Integration Events ──────────────────────────────────────────────────────

/// <summary>
/// Publicado quando um marco de valor é atingido (ex: primeiro deploy, primeiro SLO configurado).
/// Consumidores: módulo de notificações (informar utilizadores sobre progresso).
/// </summary>
public sealed record ValueMilestoneReachedIntegrationEvent(
    string MilestoneType,
    string ModuleName,
    string? UserId,
    Guid? TenantId) : IntegrationEventBase("ProductAnalytics");

/// <summary>
/// Publicado quando um sinal de fricção significativo é detetado.
/// Consumidores: módulo Governance (enriquecer métricas de maturidade).
/// </summary>
public sealed record FrictionSignalDetectedIntegrationEvent(
    string SignalType,
    string ModuleName,
    string? Route,
    int OccurrenceCount,
    Guid? TenantId) : IntegrationEventBase("ProductAnalytics");
