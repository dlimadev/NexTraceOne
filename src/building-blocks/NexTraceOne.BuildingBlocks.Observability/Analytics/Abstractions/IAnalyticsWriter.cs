using NexTraceOne.BuildingBlocks.Observability.Analytics.Events;

namespace NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;

/// <summary>
/// Interface de escrita para a camada analítica ClickHouse do NexTraceOne.
///
/// Responsabilidade: receber eventos de domínio analítico e persistir no ClickHouse
/// (nextraceone_analytics). Esta interface é a fronteira entre os módulos de domínio
/// e o storage analítico.
///
/// Princípios:
/// - Append-only: nunca atualiza ou remove registos no ClickHouse
/// - Fire-and-forget em contextos de alta frequência (falha não bloqueia o domínio)
/// - Graceful degradation: NullAnalyticsWriter activo quando ClickHouse indisponível
/// - Tenant isolation: tenant_id obrigatório em todos os eventos
/// - CancellationToken em todas as operações async
/// </summary>
public interface IAnalyticsWriter
{
    /// <summary>
    /// Escreve um evento de produto analítico no ClickHouse (tabela pan_events).
    /// Usado pelo módulo Product Analytics para registar eventos de uso da plataforma.
    /// </summary>
    Task WriteProductEventAsync(ProductAnalyticsRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escreve um lote de eventos de produto analítico no ClickHouse (tabela pan_events).
    /// Preferível para ingestão de múltiplos eventos num único request HTTP ao ClickHouse.
    /// </summary>
    Task WriteProductEventsBatchAsync(IReadOnlyList<ProductAnalyticsRecord> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escreve uma métrica de runtime no ClickHouse (tabela ops_runtime_metrics).
    /// Usado pelo módulo Operational Intelligence para registar métricas de serviços.
    /// </summary>
    Task WriteRuntimeMetricAsync(RuntimeMetricRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escreve uma entrada de custo operacional no ClickHouse (tabela ops_cost_entries).
    /// Usado pelo módulo Operational Intelligence para registar dados de custo por serviço.
    /// </summary>
    Task WriteCostEntryAsync(CostEntryRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escreve um evento de tendência de incidente no ClickHouse (tabela ops_incident_trends).
    /// Nota: estado activo do incidente permanece no PostgreSQL.
    /// </summary>
    Task WriteIncidentTrendEventAsync(IncidentTrendRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escreve um log de execução de conector no ClickHouse (tabela int_execution_logs).
    /// Usado pelo módulo Integrations para mover execuções completadas para storage analítico.
    /// </summary>
    Task WriteIntegrationExecutionAsync(IntegrationExecutionRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escreve uma transição de health de conector no ClickHouse (tabela int_health_history).
    /// Registado quando o health status de um conector muda.
    /// </summary>
    Task WriteConnectorHealthEventAsync(ConnectorHealthRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escreve um snapshot de compliance no ClickHouse (tabela gov_compliance_trends).
    /// Usado pelo módulo Governance para registar scores de compliance ao longo do tempo.
    /// </summary>
    Task WriteComplianceTrendAsync(ComplianceTrendRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escreve uma agregação FinOps no ClickHouse (tabela gov_finops_aggregates).
    /// Usado pelo módulo Governance para registar custos contextualizados por equipa/serviço.
    /// </summary>
    Task WriteFinOpsAggregateAsync(FinOpsAggregateRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escreve um registo de correlação trace → release no ClickHouse (tabela chg_trace_release_mapping).
    /// Usado pelo módulo Change Governance para ligar traces distribuídos a releases.
    /// Permite análise de "quais traces pertencem a esta release?" e correlação de impacto.
    /// </summary>
    Task WriteTraceReleaseMappingAsync(TraceReleaseMappingRecord record, CancellationToken cancellationToken = default);
}
