# 06 — 73 Null Repositories a Implementar

> Lista completa dos repositórios com implementação nula (`Null*`) que ficam bloqueados por falta
> de um analytics store. Com `IAnalyticsStore` disponível (Fase 0), cada um destes passa a ter
> uma implementação real na Fase 4.

---

## Como ler esta lista

| Coluna | Significado |
|--------|-------------|
| Interface | Contrato do repositório (no domínio) |
| Null class actual | Classe placeholder que existe hoje |
| Real implementation | Nome sugerido para a implementação real |
| Collection | Nome da collection/tabela no Analytics Store |
| Prioridade | P0=crítico, P1=alto, P2=médio, P3=baixo |

---

## Módulo AI Knowledge / Governance (18 repos)

| Interface | Null class actual | Real implementation | Collection | P |
|-----------|------------------|---------------------|-----------|---|
| `ITokenUsageLedgerReader` | `NullTokenUsageLedgerReader` | `AnalyticsTokenUsageLedgerReader` | `token_usage_ledger` | P0 |
| `IAgentTokenBudgetReader` | `NullAgentTokenBudgetReader` | `AnalyticsAgentTokenBudgetReader` | `token_usage_ledger` | P0 |
| `ITokenUsageSummaryReader` | `NullTokenUsageSummaryReader` | `AnalyticsTokenUsageSummaryReader` | `token_usage_ledger` | P0 |
| `IExternalInferenceReader` | `NullExternalInferenceReader` | `AnalyticsExternalInferenceReader` | `external_inference_records` | P0 |
| `IInferenceLatencyReader` | `NullInferenceLatencyReader` | `AnalyticsInferenceLatencyReader` | `external_inference_records` | P0 |
| `IModelPredictionReader` | `NullModelPredictionReader` | `AnalyticsModelPredictionReader` | `model_prediction_samples` | P0 |
| `IModelDriftReader` | `NullModelDriftReader` | `AnalyticsModelDriftReader` | `model_prediction_samples` | P0 |
| `IAgentBenchmarkReader` | `NullAgentBenchmarkReader` | `AnalyticsAgentBenchmarkReader` | `benchmark_snapshots` | P0 |
| `IBenchmarkTrendReader` | `NullBenchmarkTrendReader` | `AnalyticsBenchmarkTrendReader` | `benchmark_snapshots` | P0 |
| `IOrganizationalMemoryReader` | `NullOrganizationalMemoryReader` | `AnalyticsOrganizationalMemoryReader` | `knowledge_document_content` | P0 |
| `IKnowledgeSearchReader` | `NullKnowledgeSearchReader` | `AnalyticsKnowledgeSearchReader` | `knowledge_document_content` | P0 |
| `IKnowledgeGraphReader` | `NullKnowledgeGraphReader` | `AnalyticsKnowledgeGraphReader` | `knowledge_document_content` | P0 |
| `IRlFeedbackReader` | `NullRlFeedbackReader` | `AnalyticsRlFeedbackReader` | `benchmark_snapshots` | P0 |
| `IAgentPerformanceReader` | `NullAgentPerformanceReader` | `AnalyticsAgentPerformanceReader` | `benchmark_snapshots` | P0 |
| `ISkillUsageReader` | `NullSkillUsageReader` | `AnalyticsSkillUsageReader` | `agent_query_records` | P0 |
| `ICapabilityMaturityReader` | `NullCapabilityMaturityReader` | `AnalyticsCapabilityMaturityReader` | `benchmark_snapshots` | P0 |
| `IWaveProgressReader` | `NullWaveProgressReader` | `AnalyticsWaveProgressReader` | `analytics_events` | P0 |
| `IGovernanceHealthReader` | `NullGovernanceHealthReader` | `AnalyticsGovernanceHealthReader` | `benchmark_snapshots` | P0 |

---

## Módulo Cost Management (12 repos)

| Interface | Null class actual | Real implementation | Collection | P |
|-----------|------------------|---------------------|-----------|---|
| `ICostRecordReader` | `NullCostRecordReader` | `AnalyticsCostRecordReader` | `cost_records` | P0 |
| `ICostSummaryReader` | `NullCostSummaryReader` | `AnalyticsCostSummaryReader` | `cost_records` | P0 |
| `ICostTrendReader` | `NullCostTrendReader` | `AnalyticsCostTrendReader` | `cost_records` | P0 |
| `ICostCenterBreakdownReader` | `NullCostCenterBreakdownReader` | `AnalyticsCostCenterBreakdownReader` | `cost_records` | P0 |
| `IBurnRateReader` | `NullBurnRateReader` | `AnalyticsBurnRateReader` | `burn_rate_snapshots` | P0 |
| `IBurnRateTrendReader` | `NullBurnRateTrendReader` | `AnalyticsBurnRateTrendReader` | `burn_rate_snapshots` | P0 |
| `IBudgetForecastReader` | `NullBudgetForecastReader` | `AnalyticsBudgetForecastReader` | `burn_rate_snapshots` | P0 |
| `ICostAllocationReader` | `NullCostAllocationReader` | `AnalyticsCostAllocationReader` | `cost_allocation_events` | P0 |
| `IChargebackReader` | `NullChargebackReader` | `AnalyticsChargebackReader` | `cost_allocation_events` | P0 |
| `IInfraCostReader` | `NullInfraCostReader` | `AnalyticsInfraCostReader` | `cost_records` | P0 |
| `IAiCostReader` | `NullAiCostReader` | `AnalyticsAiCostReader` | `token_usage_ledger` | P0 |
| `ICostAnomalyReader` | `NullCostAnomalyReader` | `AnalyticsCostAnomalyReader` | `cost_records` | P0 |

---

## Módulo SLO / Reliability (10 repos)

| Interface | Null class actual | Real implementation | Collection | P |
|-----------|------------------|---------------------|-----------|---|
| `IErrorBudgetReader` | `NullErrorBudgetReader` | `AnalyticsErrorBudgetReader` | `error_budget_snapshots` | P1 |
| `IErrorBudgetTrendReader` | `NullErrorBudgetTrendReader` | `AnalyticsErrorBudgetTrendReader` | `error_budget_snapshots` | P1 |
| `IBurnRateAlertsReader` | `NullBurnRateAlertsReader` | `AnalyticsBurnRateAlertsReader` | `error_budget_snapshots` | P1 |
| `ISliMeasurementReader` | `NullSliMeasurementReader` | `AnalyticsSliMeasurementReader` | `sli_measurements` | P1 |
| `ISloComplianceReader` | `NullSloComplianceReader` | `AnalyticsSloComplianceReader` | `slo_compliance_daily` | P1 |
| `ISloHistoryReader` | `NullSloHistoryReader` | `AnalyticsSloHistoryReader` | `slo_compliance_weekly` | P1 |
| `ISloMonthlyReportReader` | `NullSloMonthlyReportReader` | `AnalyticsSloMonthlyReportReader` | `slo_compliance_monthly` | P1 |
| `IReliabilityTrendReader` | `NullReliabilityTrendReader` | `AnalyticsReliabilityTrendReader` | `reliability_snapshots` | P1 |
| `IMttrReader` | `NullMttrReader` | `AnalyticsMttrReader` | `reliability_snapshots` | P1 |
| `IIncidentImpactReader` | `NullIncidentImpactReader` | `AnalyticsIncidentImpactReader` | `reliability_snapshots` | P1 |

---

## Módulo Observability (9 repos)

| Interface | Null class actual | Real implementation | Collection | P |
|-----------|------------------|---------------------|-----------|---|
| `IServiceMetricsReader` | `NullServiceMetricsReader` | `AnalyticsServiceMetricsReader` | `service_metrics_snapshots` | P1 |
| `IMetricsTrendReader` | `NullMetricsTrendReader` | `AnalyticsMetricsTrendReader` | `service_metrics_snapshots` | P1 |
| `ILatencyPercentileReader` | `NullLatencyPercentileReader` | `AnalyticsLatencyPercentileReader` | `service_metrics_snapshots` | P1 |
| `IRuntimeMetricsReader` | `NullRuntimeMetricsReader` | `AnalyticsRuntimeMetricsReader` | `runtime_snapshots` | P1 |
| `IAlertHistoryReader` | `NullAlertHistoryReader` | `AnalyticsAlertHistoryReader` | `alert_firing_records` | P1 |
| `IAlertFrequencyReader` | `NullAlertFrequencyReader` | `AnalyticsAlertFrequencyReader` | `alert_firing_records` | P1 |
| `IAlertCorrelationReader` | `NullAlertCorrelationReader` | `AnalyticsAlertCorrelationReader` | `alert_firing_records` | P1 |
| `IReliabilitySnapshotReader` | `NullReliabilitySnapshotReader` | `AnalyticsReliabilitySnapshotReader` | `reliability_snapshots` | P1 |
| `IServiceHealthReader` | `NullServiceHealthReader` | `AnalyticsServiceHealthReader` | `service_metrics_snapshots` | P1 |

---

## Módulo Security (8 repos)

| Interface | Null class actual | Real implementation | Collection | P |
|-----------|------------------|---------------------|-----------|---|
| `ISecurityEventReader` | `NullSecurityEventReader` | `AnalyticsSecurityEventReader` | `security_events` | P2 |
| `ISecurityEventSearchReader` | `NullSecurityEventSearchReader` | `AnalyticsSecurityEventSearchReader` | `security_events` | P2 |
| `ISecurityEventTimelineReader` | `NullSecurityEventTimelineReader` | `AnalyticsSecurityEventTimelineReader` | `security_events` | P2 |
| `IThreatSignalReader` | `NullThreatSignalReader` | `AnalyticsThreatSignalReader` | `threat_signals` | P2 |
| `IThreatCorrelationReader` | `NullThreatCorrelationReader` | `AnalyticsThreatCorrelationReader` | `threat_signals` | P2 |
| `IRiskScoreReader` | `NullRiskScoreReader` | `AnalyticsRiskScoreReader` | `threat_signals` | P2 |
| `IAuditEventSearchReader` | `NullAuditEventSearchReader` | `AnalyticsAuditEventSearchReader` | `audit_event_search` | P2 |
| `ISecurityComplianceReader` | `NullSecurityComplianceReader` | `AnalyticsSecurityComplianceReader` | `security_events` | P2 |

---

## Módulo Analytics / Product (7 repos)

| Interface | Null class actual | Real implementation | Collection | P |
|-----------|------------------|---------------------|-----------|---|
| `IAnalyticsEventReader` | `NullAnalyticsEventReader` | `AnalyticsEventReader` | `analytics_events` | P2 |
| `IProductFunnelReader` | `NullProductFunnelReader` | `AnalyticsProductFunnelReader` | `analytics_events` | P2 |
| `IFeatureAdoptionReader` | `NullFeatureAdoptionReader` | `AnalyticsFeatureAdoptionReader` | `analytics_events` | P2 |
| `IDashboardUsageReader` | `NullDashboardUsageReader` | `AnalyticsDashboardUsageReader` | `dashboard_usage_events` | P2 |
| `IProductivitySnapshotReader` | `NullProductivitySnapshotReader` | `AnalyticsProductivitySnapshotReader` | `productivity_snapshots` | P2 |
| `IDoraMetricsReader` | `NullDoraMetricsReader` | `AnalyticsDoraMetricsReader` | `productivity_snapshots` | P2 |
| `IUserEngagementReader` | `NullUserEngagementReader` | `AnalyticsUserEngagementReader` | `analytics_events` | P2 |

---

## Módulo Developer Productivity (9 repos)

| Interface | Null class actual | Real implementation | Collection | P |
|-----------|------------------|---------------------|-----------|---|
| `IAgentQueryReader` | `NullAgentQueryReader` | `AnalyticsAgentQueryReader` | `agent_query_records` | P3 |
| `IAgentSatisfactionReader` | `NullAgentSatisfactionReader` | `AnalyticsAgentSatisfactionReader` | `agent_query_records` | P3 |
| `ICodeReviewCycleReader` | `NullCodeReviewCycleReader` | `AnalyticsCodeReviewCycleReader` | `code_review_cycles` | P3 |
| `ICodeReviewTrendReader` | `NullCodeReviewTrendReader` | `AnalyticsCodeReviewTrendReader` | `code_review_cycles` | P3 |
| `IDeploymentFrequencyReader` | `NullDeploymentFrequencyReader` | `AnalyticsDeploymentFrequencyReader` | `deployment_records` | P3 |
| `IDeploymentSuccessReader` | `NullDeploymentSuccessReader` | `AnalyticsDeploymentSuccessReader` | `deployment_records` | P3 |
| `IPipelinePerformanceReader` | `NullPipelinePerformanceReader` | `AnalyticsPipelinePerformanceReader` | `pipeline_run_records` | P3 |
| `IPipelineTrendReader` | `NullPipelineTrendReader` | `AnalyticsPipelineTrendReader` | `pipeline_run_records` | P3 |
| `ILeadTimeReader` | `NullLeadTimeReader` | `AnalyticsLeadTimeReader` | `deployment_records` | P3 |

---

## Template de implementação real

```csharp
// Padrão a seguir para cada null repo → real reader
// Ficheiro: src/modules/{module}/Infrastructure/Analytics/{Interface}Impl.cs

public sealed class Analytics{Name}Reader(IAnalyticsStore store) : I{Name}Reader
{
    public async Task<{ReturnType}> {Method}Async(
        {Parameters},
        CancellationToken ct = default)
    {
        var records = await store.QueryAsync<{RecordDto}>(new AnalyticsQuery
        {
            Collection = "{collection_name}",
            From = /* from param */,
            To = /* to param */,
            Filters = new Dictionary<string, object?>
            {
                ["tenant_id"] = /* tenant id */,
                // outros filtros
            },
            Limit = /* limit */
        }, ct);

        return {ReturnType}.From(records);
    }
}
```

---

## Contagem por prioridade

| Prioridade | Repos | Módulos |
|-----------|-------|---------|
| P0 — Crítico | 30 | AI Knowledge + Cost Management |
| P1 — Alto | 19 | SLO + Observability |
| P2 — Médio | 15 | Security + Analytics |
| P3 — Baixo | 9 | Developer Productivity |
| **Total** | **73** | **7 módulos** |
