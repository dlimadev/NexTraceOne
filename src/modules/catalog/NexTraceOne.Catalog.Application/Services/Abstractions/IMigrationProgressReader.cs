namespace NexTraceOne.Catalog.Application.Services.Abstractions;

/// <summary>
/// Abstração de leitura de dados de progresso de migração de consumidores
/// de serviços deprecated/sunset para a alternativa designada.
///
/// Fornece entradas com TotalConsumers, MigratedConsumers, InProgressConsumers,
/// StuckConsumers e série temporal diária de 30 dias.
/// Desacopla o handler de migration progress das implementações concretas de repositório.
///
/// Wave AF.3 — GetServiceMigrationProgressReport.
/// </summary>
public interface IMigrationProgressReader
{
    /// <summary>
    /// Lista entradas de progresso de migração para todos os serviços
    /// Deprecated ou Sunset com alternativa designada no tenant.
    /// </summary>
    Task<IReadOnlyList<MigrationProgressEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);
}

/// <summary>
/// Ponto diário de progresso de migração (série temporal de 30 dias).
/// Wave AF.3.
/// </summary>
public sealed record DailyMigrationPoint(
    DateOnly Date,
    int CumulativeMigratedCount);

/// <summary>
/// Informação sobre um consumidor que não está a migrar no período.
/// Wave AF.3.
/// </summary>
public sealed record StuckConsumerInfo(
    string ConsumerServiceName,
    string ConsumerTeamName,
    string ConsumerTier,
    int DaysSinceLastActivity);

/// <summary>
/// Entrada de progresso de migração para um serviço deprecated/sunset.
/// Wave AF.3.
/// </summary>
public sealed record MigrationProgressEntry(
    /// <summary>Identificador único do serviço sendo retirado.</summary>
    string ServiceId,
    /// <summary>Nome técnico do serviço sendo retirado.</summary>
    string ServiceName,
    /// <summary>Nome técnico do serviço alternativo (successor).</summary>
    string SuccessorServiceName,
    /// <summary>Nome da equipa responsável.</summary>
    string TeamName,
    /// <summary>Estado actual do ciclo de vida: Deprecated ou Sunset.</summary>
    string CurrentLifecycleState,
    /// <summary>Data em que o serviço entrou no estado actual.</summary>
    DateTimeOffset StateEnteredAt,
    /// <summary>Total de consumidores com dependência registada no serviço antigo.</summary>
    int TotalConsumers,
    /// <summary>Consumidores com dependência confirmada no serviço successor.</summary>
    int MigratedConsumers,
    /// <summary>Consumidores com dependências em ambos (em transição).</summary>
    int InProgressConsumers,
    /// <summary>Lista de consumidores sem qualquer sinal de migração no período.</summary>
    IReadOnlyList<StuckConsumerInfo> StuckConsumerDetails,
    /// <summary>Série temporal de 30 dias de progresso de migração.</summary>
    IReadOnlyList<DailyMigrationPoint> DailyTimeline);
