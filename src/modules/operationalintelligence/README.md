# NexTraceOne — Operational Intelligence Module

## Visão Geral

O módulo Operational Intelligence é o núcleo de inteligência operacional do NexTraceOne.
Composto por 5 subdomínios integrados, sustenta a visão operacional dos serviços em tempo real:
incidentes, mitigação, automação, confiabilidade, runtime e custos.

## Subdomínios

| Subdomínio | Responsabilidade |
|------------|-----------------|
| **Incidents** | Gestão de incidentes, correlação, evidência, mitigação, runbooks |
| **Automation** | Automações controladas com aprovação, auditoria e validação |
| **Reliability** | Scoring de confiabilidade por serviço e equipa |
| **Runtime** | Snapshots de saúde, baselines, drift detection, observability |
| **Cost** | Perfis de custo, snapshots, atribuição, tendências, importação |

## Arquitetura

```
NexTraceOne.OperationalIntelligence.Domain/
├── Incidents/     → IncidentRecord (AR), Mitigation*, Runbook
├── Automation/    → AutomationWorkflowRecord (AR), Validation, Audit
├── Reliability/   → ReliabilitySnapshot (AR)
├── Runtime/       → RuntimeSnapshot (AR), Baseline, Drift, Observability
└── Cost/          → CostSnapshot (AR), Attribution, Trend, Profile, Import

NexTraceOne.OperationalIntelligence.Infrastructure/
├── Incidents/     → IncidentDbContext (5 DbSets)
├── Automation/    → AutomationDbContext (3 DbSets)
├── Reliability/   → ReliabilityDbContext (1 DbSet)
├── Runtime/       → RuntimeIntelligenceDbContext (4 DbSets)
└── Cost/          → CostIntelligenceDbContext (6 DbSets)

NexTraceOne.OperationalIntelligence.API/
├── Incidents/     → 3 endpoint modules (Incident, Mitigation, Runbook)
├── Automation/    → 1 endpoint module
├── Reliability/   → 1 endpoint module
├── Runtime/       → 1 endpoint module
└── Cost/          → 1 endpoint module
```

## Entidades

| Entidade | Tipo | Subdomínio | Tabela |
|----------|------|------------|--------|
| `IncidentRecord` | Aggregate Root | Incidents | `ops_incidents` |
| `MitigationWorkflowRecord` | Entity | Incidents | `ops_mitigation_workflows` |
| `MitigationWorkflowActionLog` | Entity | Incidents | `ops_mitigation_workflow_actions` |
| `MitigationValidationLog` | Entity | Incidents | `ops_mitigation_validations` |
| `RunbookRecord` | Entity | Incidents | `ops_runbooks` |
| `AutomationWorkflowRecord` | Aggregate Root | Automation | `ops_automation_workflows` |
| `AutomationValidationRecord` | Entity | Automation | `ops_automation_validations` |
| `AutomationAuditRecord` | Entity | Automation | `ops_automation_audit_records` |
| `ReliabilitySnapshot` | Aggregate Root | Reliability | `ops_reliability_snapshots` |
| `RuntimeSnapshot` | Aggregate Root | Runtime | `ops_runtime_snapshots` |
| `RuntimeBaseline` | Entity | Runtime | `ops_runtime_baselines` |
| `DriftFinding` | Entity | Runtime | `ops_drift_findings` |
| `ObservabilityProfile` | Entity | Runtime | `ops_observability_profiles` |
| `CostSnapshot` | Aggregate Root | Cost | `ops_cost_snapshots` |
| `CostAttribution` | Entity | Cost | `ops_cost_attributions` |
| `CostTrend` | Entity | Cost | `ops_cost_trends` |
| `ServiceCostProfile` | Entity | Cost | `ops_service_cost_profiles` |
| `CostImportBatch` | Entity | Cost | `ops_cost_import_batches` |
| `CostRecord` | Entity | Cost | `ops_cost_records` |

## Concorrência Otimista

PostgreSQL xmin via `RowVersion` nos 5 aggregate roots.

## Check Constraints

- `CK_ops_incidents_severity`: Severity ∈ [0..3]
- `CK_ops_incidents_status`: Status ∈ [0..5]
- `CK_ops_incidents_type`: Type ∈ [0..6]
- `CK_ops_automation_workflows_status`: Status ∈ 11 valores string
- `CK_ops_automation_workflows_approval_status`: ApprovalStatus ∈ 5 valores
- `CK_ops_automation_workflows_risk_level`: RiskLevel ∈ 4 valores
- `CK_ops_reliability_snapshots_trend`: TrendDirection ∈ [0..2]
- `CK_ops_runtime_snapshots_health`: HealthStatus ∈ [0..3]

## Permissões

| Permissão | Escopo |
|-----------|--------|
| `operations:incidents:read` | Consultar incidentes |
| `operations:incidents:write` | Criar/atualizar incidentes |
| `operations:mitigation:read` | Consultar mitigação |
| `operations:mitigation:write` | Executar mitigação |
| `operations:runbooks:read` | Consultar runbooks |
| `operations:runbooks:write` | Gerir runbooks |
| `operations:reliability:read` | Consultar confiabilidade |
| `operations:runtime:read` | Consultar runtime |
| `operations:runtime:write` | Ingerir snapshots |
| `operations:cost:read` | Consultar custos |
| `operations:cost:write` | Importar/atribuir custos |
| `operations:automation:read` | Consultar automações |
| `operations:automation:write` | Criar automações |
| `operations:automation:execute` | Executar automações |
| `operations:automation:approve` | Aprovar automações |

## PostgreSQL vs Elasticsearch

- **PostgreSQL**: Estado operacional atual, incidentes, automações, runbooks, baselines
- **Elasticsearch** (futuro): Telemetria de alto volume, snapshots históricos, séries temporais, métricas agregadas

## Testes

323 testes cobrindo: Domain, Application features, Incidents, Automation, Reliability, Runtime, Cost.
