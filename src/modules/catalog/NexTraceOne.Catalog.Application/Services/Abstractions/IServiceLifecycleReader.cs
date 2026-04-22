namespace NexTraceOne.Catalog.Application.Services.Abstractions;

/// <summary>
/// Abstração de leitura de dados do ciclo de vida dos serviços para relatórios de transições.
///
/// Fornece entradas agregadas por serviço, incluindo estado actual, data da última transição,
/// número de transições no período e sinais de consumidores bloqueadores.
/// Desacopla o handler de lifecycle transition das implementações concretas de repositório.
///
/// Wave AF.1 — GetServiceLifecycleTransitionReport.
/// </summary>
public interface IServiceLifecycleReader
{
    /// <summary>
    /// Lista entradas do ciclo de vida dos serviços do tenant para análise de transições.
    /// </summary>
    Task<IReadOnlyList<ServiceLifecycleEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);
}

/// <summary>
/// Estado de ciclo de vida de um serviço — alinhado com o domínio do NexTraceOne.
/// Wave AF.1.
/// </summary>
public enum ServiceLifecycleState
{
    /// <summary>Serviço em planeamento ou desenvolvimento — pré-produção.</summary>
    PreProduction,
    /// <summary>Serviço activo em produção.</summary>
    Active,
    /// <summary>Serviço em processo de descontinuação (Deprecating).</summary>
    Deprecating,
    /// <summary>Serviço descontinuado — não deve receber novas dependências.</summary>
    Deprecated,
    /// <summary>Serviço retirado — não mais operacional.</summary>
    Retired
}

/// <summary>
/// Entrada de ciclo de vida de um serviço com histórico de transições e sinais de bloqueio.
/// Wave AF.1.
/// </summary>
public sealed record ServiceLifecycleEntry(
    /// <summary>Identificador único do serviço.</summary>
    string ServiceId,
    /// <summary>Nome técnico do serviço.</summary>
    string ServiceName,
    /// <summary>Nome da equipa responsável.</summary>
    string TeamName,
    /// <summary>Tier operacional do serviço: Critical, Standard ou Experimental.</summary>
    string ServiceTier,
    /// <summary>Estado actual do ciclo de vida.</summary>
    ServiceLifecycleState CurrentState,
    /// <summary>Data e hora em que o estado actual foi atingido.</summary>
    DateTimeOffset StateEnteredAt,
    /// <summary>Número de transições de estado no período lookback.</summary>
    int TransitionCount,
    /// <summary>
    /// Número de consumidores com tier Critical ou Standard ainda activos
    /// (ConsumerExpectation activa). Usado para BlockedTransitionFlag.
    /// </summary>
    int ActiveCriticalConsumerCount,
    /// <summary>
    /// Número de consumidores que iniciaram migração no período
    /// (com dependência no serviço substituto). Usado para StagnationFlag.
    /// </summary>
    int MigratingConsumerCount);
