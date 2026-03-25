# NexTraceOne — Change Governance Module

## Visão Geral

O módulo Change Governance é o núcleo de Change Intelligence do NexTraceOne.
Responsável pela gestão de mudanças, avaliação de risco, blast radius,
workflows de aprovação, promoção entre ambientes e governança de rulesets.

## Escopo do Módulo

### O que PERTENCE ao Change Governance:
- **Releases** — ciclo de vida de deployment (Pending→Running→Succeeded/Failed→RolledBack)
- **Score de Risco** (ChangeIntelligenceScore) — avaliação ponderada (breakingChange + blastRadius + environment)
- **Blast Radius** (BlastRadiusReport) — consumidores diretos e transitivos afetados
- **Workflow de Aprovação** — templates, instâncias, estágios, decisões
- **Promoção entre Ambientes** — gates, avaliações, pedidos de promoção
- **Ruleset Governance** — rulesets, bindings, linting execution
- **Freeze Windows** — janelas de congelamento de mudanças
- **Observation Windows** — observação pós-release
- **Post-Release Review** — revisão automática pós-release
- **Rollback Assessment** — viabilidade de rollback
- **Evidence Packs** — evidências da mudança
- **SLA Policies** — políticas de SLA por workflow

### O que NÃO PERTENCE ao Change Governance:
- **Ativos (APIs, serviços)** → Módulo Service Catalog
- **Contratos** → Módulo Contracts
- **Ambientes infra** → Módulo Environment Management
- **Incidentes** → Módulo Operational Intelligence
- **Auditoria cross-module** → Módulo Audit & Compliance

## Arquitetura

```
NexTraceOne.ChangeGovernance.Domain/
├── ChangeIntelligence/    → 11 entidades, 7 enums
├── Workflow/              → 6 entidades, 3 enums
├── Promotion/             → 4 entidades, 1 enum
└── RulesetGovernance/     → 6 entidades

NexTraceOne.ChangeGovernance.Infrastructure/
├── ChangeIntelligence/Persistence/  → ChangeIntelligenceDbContext (10 DbSets)
├── Workflow/Persistence/            → WorkflowDbContext (6 DbSets)
├── Promotion/Persistence/           → PromotionDbContext (4 DbSets)
└── RulesetGovernance/Persistence/   → RulesetGovernanceDbContext (3 DbSets)

NexTraceOne.ChangeGovernance.API/
├── ChangeIntelligence/Endpoints/  → 6 endpoint files
├── Workflow/Endpoints/            → 5 endpoint files
├── Promotion/Endpoints/           → 1 endpoint module
└── RulesetGovernance/Endpoints/   → 1 endpoint module
```

## Aggregate Roots

| Entidade | Responsabilidade |
|----------|-----------------|
| `Release` | Ciclo de vida do deployment com score, classificação e status |
| `WorkflowInstance` | Instância de workflow com estágios e aprovações |
| `PromotionRequest` | Pedido de promoção entre ambientes com gates |

## Regras de Negócio

### Release Lifecycle
- Status: Pending→Running→Succeeded/Failed→RolledBack
- `IsValidTransition()` — valida transições de estado
- `SetChangeScore()` — score normalizado [0.0, 1.0]
- `RegisterRollback()` — marca como rollback de outra release

### Score de Risco
- `ChangeIntelligenceScore.Compute()` — média ponderada de 3 fatores:
  - BreakingChangeWeight [0.0, 1.0]
  - BlastRadiusWeight [0.0, 1.0]
  - EnvironmentWeight [0.0, 1.0]
- Score final = (sum of weights) / 3, arredondado a 4 casas decimais

### Blast Radius
- `BlastRadiusReport.Calculate()` — consumidores diretos + transitivos
- TotalAffectedConsumers = DirectConsumers.Count + TransitiveConsumers.Count

## Base de Dados

### Tabelas (prefixo chg_)
| Tabela | Entidade | DbContext |
|--------|---------|-----------|
| `chg_releases` | Release | ChangeIntelligenceDbContext |
| `chg_change_scores` | ChangeIntelligenceScore | ChangeIntelligenceDbContext |
| `chg_blast_radius_reports` | BlastRadiusReport | ChangeIntelligenceDbContext |
| `chg_change_events` | ChangeEvent | ChangeIntelligenceDbContext |
| `chg_external_markers` | ExternalMarker | ChangeIntelligenceDbContext |
| `chg_freeze_windows` | FreezeWindow | ChangeIntelligenceDbContext |
| `chg_release_baselines` | ReleaseBaseline | ChangeIntelligenceDbContext |
| `chg_observation_windows` | ObservationWindow | ChangeIntelligenceDbContext |
| `chg_post_release_reviews` | PostReleaseReview | ChangeIntelligenceDbContext |
| `chg_rollback_assessments` | RollbackAssessment | ChangeIntelligenceDbContext |
| `chg_workflow_instances` | WorkflowInstance | WorkflowDbContext |
| `chg_workflow_templates` | WorkflowTemplate | WorkflowDbContext |
| `chg_workflow_stages` | WorkflowStage | WorkflowDbContext |
| `chg_approval_decisions` | ApprovalDecision | WorkflowDbContext |
| `chg_evidence_packs` | EvidencePack | WorkflowDbContext |
| `chg_sla_policies` | SlaPolicy | WorkflowDbContext |
| `chg_promotion_requests` | PromotionRequest | PromotionDbContext |
| `chg_promotion_gates` | PromotionGate | PromotionDbContext |
| `chg_gate_evaluations` | GateEvaluation | PromotionDbContext |
| `chg_deployment_environments` | DeploymentEnvironment | PromotionDbContext |
| `chg_rulesets` | Ruleset | RulesetGovernanceDbContext |
| `chg_ruleset_bindings` | RulesetBinding | RulesetGovernanceDbContext |
| `chg_lint_results` | LintResult | RulesetGovernanceDbContext |

### Concorrência Otimista
PostgreSQL xmin via `RowVersion` em: Release, ChangeIntelligenceScore, BlastRadiusReport, WorkflowInstance, PromotionRequest, Ruleset.

### Check Constraints
- `CK_chg_releases_status`: Status [0..4]
- `CK_chg_releases_change_level`: ChangeLevel [0..4]
- `CK_chg_releases_change_score`: ChangeScore [0.0, 1.0]
- `CK_chg_workflow_instances_status`: WorkflowStatus values
- `CK_chg_promotion_requests_status`: PromotionStatus values

## Permissões

| Permissão | Escopo |
|-----------|--------|
| `change-intelligence:read` | Consultar releases, scores, blast radius |
| `change-intelligence:write` | Criar/editar releases, calcular score |
| `workflow:read` | Consultar workflows e templates |
| `workflow:write` | Criar/aprovar/rejeitar workflows |
| `promotion:read` | Consultar promoções |
| `promotion:write` | Criar/executar promoções |
| `rulesets:read` | Consultar rulesets |
| `rulesets:write` | Criar/editar rulesets |

## Módulos Dependentes

| Módulo | Relação |
|--------|---------|
| Service Catalog | Release.ApiAssetId referencia ativo do Catalog |
| Contracts | Blast radius consume relações de contrato |
| Environment Management | PromotionRequest usa ambientes de destino |
| Operational Intelligence | Post-release review pode correlacionar com incidentes |
| Audit & Compliance | Workflow decisions auditáveis |
