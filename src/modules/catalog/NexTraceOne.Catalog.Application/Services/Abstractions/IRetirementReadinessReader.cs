namespace NexTraceOne.Catalog.Application.Services.Abstractions;

/// <summary>
/// Abstração de leitura de dados de prontidão para retirada de um serviço específico.
///
/// Agrega sinais de múltiplas fontes para suportar o cálculo do RetirementReadinessScore:
/// consumidores migrados, contratos deprecados, runbook de decommission e notificações enviadas.
/// Desacopla o handler de retirement readiness das implementações concretas de repositório.
///
/// Wave AF.2 — GetServiceRetirementReadinessReport.
/// </summary>
public interface IRetirementReadinessReader
{
    /// <summary>
    /// Obtém os dados de prontidão para retirada do serviço identificado por <paramref name="serviceId"/>.
    /// Retorna <c>null</c> quando o serviço não for encontrado no tenant.
    /// </summary>
    Task<RetirementReadinessData?> GetByServiceAsync(
        string tenantId,
        string serviceId,
        CancellationToken ct);
}

/// <summary>
/// Dados agregados para cálculo do RetirementReadinessScore de um serviço.
/// Wave AF.2.
/// </summary>
public sealed record RetirementReadinessData(
    /// <summary>Identificador único do serviço.</summary>
    string ServiceId,
    /// <summary>Nome técnico do serviço.</summary>
    string ServiceName,
    /// <summary>Nome da equipa responsável.</summary>
    string TeamName,
    /// <summary>Estado actual do ciclo de vida.</summary>
    string CurrentLifecycleState,
    /// <summary>Total de consumidores registados (ConsumerExpectation activas).</summary>
    int TotalConsumers,
    /// <summary>Consumidores com migração confirmada para alternativa.</summary>
    int MigratedConsumers,
    /// <summary>Total de contratos do serviço.</summary>
    int TotalContracts,
    /// <summary>Contratos em estado Deprecated ou Sunset (não Active/Approved).</summary>
    int DeprecatedContracts,
    /// <summary>true quando existe runbook de decommission aprovado para o serviço.</summary>
    bool HasApprovedDecommissionRunbook,
    /// <summary>Total de equipas consumidoras únicas.</summary>
    int TotalConsumerTeams,
    /// <summary>Equipas notificadas via OperationalNote ou canal configurado.</summary>
    int NotifiedConsumerTeams,
    /// <summary>Lista de consumidores que ainda não migraram.</summary>
    IReadOnlyList<BlockerConsumerInfo> UnmigratedConsumers);

/// <summary>
/// Informação sobre um consumidor que ainda não migrou do serviço em retirada.
/// Wave AF.2.
/// </summary>
public sealed record BlockerConsumerInfo(
    string ConsumerServiceName,
    string ConsumerTeamName,
    string ConsumerTier,
    bool IsNotified);
