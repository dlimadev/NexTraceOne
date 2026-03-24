# Revisão Modular — Change Governance

> **Data:** 2026-03-24  
> **Prioridade:** P2 (Pilar Core — Change Confidence)  
> **Módulo Backend:** `src/modules/changegovernance/`  
> **Módulo Frontend:** `src/frontend/src/features/change-governance/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **Change Governance** é o pilar de **Change Confidence** do NexTraceOne. Cobre 4 subdomínios:

- **ChangeIntelligence** — Tracking de releases, risk scoring, blast radius, post-release review, rollback assessment, freeze windows, deployment state
- **Workflow** — Templates de aprovação, instâncias de workflow, estágios, decisões, evidence packs, SLA
- **Promotion** — Promoção entre ambientes, gates de avaliação, aprovações
- **RulesetGovernance** — Linting, regras de validação, bindings, scores

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento com a visão | ✅ Muito Forte | Change Confidence é narrativa central do produto |
| Completude funcional | ✅ Alta | 232 ficheiros backend, 4 DbContexts, 179+ testes |
| Maturidade do domínio | ✅ Alta | 27 entidades, 40+ features CQRS, transitions completas |
| Frontend | ✅ Funcional | 6 páginas, 5 API clients, integração completa |

---

## 3. Páginas e Ações do Frontend

| Página | Rota | Permissão | Estado | Funcionalidade |
|--------|------|-----------|--------|----------------|
| ChangeCatalogPage | `/changes` | change-intelligence:read | ✅ Funcional | Catálogo de mudanças com filtros (service, team, environment, changeType, confidenceStatus) |
| ChangeDetailPage | `/changes/:changeId` | change-intelligence:read | ✅ Funcional | Blast radius, advisory, decisions, AI context |
| ReleasesPage | `/releases` | change-intelligence:releases:read | ✅ Funcional | Releases com tabs (Overview/Intelligence/Timeline/Freeze), deployment status |
| WorkflowPage | `/workflow` | workflow:read | ✅ Funcional | Instâncias de workflow, approve/reject, SLA tracking |
| PromotionPage | `/promotion` | promotion:read | ✅ Funcional | Promoção cross-environment, gates |
| WorkflowConfigurationPage | `/platform/configuration/workflows` | platform:admin:read | ✅ Funcional | 7 secções: templates, stages, approvers, SLA, gates, promotion, freeze |

---

## 4. Backend — Entidades de Domínio

### 4.1 ChangeIntelligence (11 entidades)

| Entidade | Propósito | Status Transitions |
|----------|-----------|-------------------|
| **Release** (Aggregate Root) | Tracking de releases com risk assessment | Pending → Running → Succeeded/Failed → RolledBack |
| ChangeEvent | Eventos de lifecycle | — |
| BlastRadiusReport | Impacto: consumers diretos e transitivos | — |
| ChangeIntelligenceScore | Risk scoring (0.0-1.0) | — |
| FreezeWindow | Controle de deployment freezes | — |
| ReleaseBaseline | Métricas pré-release (RPM, error rate, latência) | — |
| ObservationWindow | Janela pós-release | — |
| PostReleaseReview | Review automático pós-release | — |
| RollbackAssessment | Viabilidade de rollback | — |
| ExternalMarker | Markers de CI/CD externo | — |
| DeploymentState | Estado de deployment | — |

### 4.2 Workflow (6 entidades)

| Entidade | Propósito | Status Transitions |
|----------|-----------|-------------------|
| **WorkflowTemplate** (Aggregate Root) | Templates reutilizáveis | — |
| **WorkflowInstance** (Aggregate Root) | Instância runtime | Draft → Pending → InReview → Approved/Rejected/Cancelled |
| WorkflowStage | Estágios sequenciais | — |
| ApprovalDecision | Decisões de aprovação | — |
| EvidencePack | Artefactos de auditoria | — |
| SlaPolicy | Regras de SLA | — |

### 4.3 Promotion (4 entidades)

| Entidade | Propósito | Status Transitions |
|----------|-----------|-------------------|
| **PromotionRequest** (Aggregate Root) | Promoção cross-env | Pending → InEvaluation → Approved/Rejected/Blocked/Cancelled |
| PromotionGate | Gates de avaliação | — |
| GateEvaluation | Resultados de avaliação | — |
| DeploymentEnvironment | Ambientes de deployment | — |

### 4.4 RulesetGovernance (6 entidades)

| Entidade | Propósito |
|----------|-----------|
| **Ruleset** (Aggregate Root) | Regras de linting (JSON/YAML) |
| RulesetBinding | Ligação regra → asset type |
| LintResult | Resultados de linting |
| LintFinding | Violações individuais |
| LintExecution | Tracking de execução |
| RulesetScore | Score agregado |

---

## 5. Endpoints API

| Módulo | Endpoints Chave | Total |
|--------|----------------|-------|
| ChangeIntelligence | releases, markers, intelligence, baseline, review, rollback-assessment, changes, advisory, decisions, deployments, freeze-windows, analysis (classify, score, blast-radius) | ~20 |
| Workflow | templates, instances, approvals, status, evidence-packs | ~10 |
| Promotion | environments, requests, evaluate-gates, approve, block, gate-evaluations, override | ~9 |
| RulesetGovernance | upload, archive, bind, execute-lint, findings, scores | ~7 |

**Total: ~46 endpoints**

---

## 6. Banco de Dados

| DbContext | Outbox Table | Entidades |
|-----------|-------------|-----------|
| ChangeIntelligenceDbContext | ci_outbox_messages | Release, BlastRadiusReport, ChangeIntelligenceScore, ChangeEvent, ExternalMarker, FreezeWindow, ReleaseBaseline, ObservationWindow, PostReleaseReview, RollbackAssessment |
| WorkflowDbContext | wf_outbox_messages | WorkflowTemplate, WorkflowInstance, WorkflowStage, EvidencePack, SlaPolicy, ApprovalDecision |
| PromotionDbContext | prm_outbox_messages | DeploymentEnvironment, PromotionRequest, PromotionGate, GateEvaluation |
| RulesetGovernanceDbContext | rg_outbox_messages | Ruleset, RulesetBinding, LintResult |

---

## 7. Integrações

| Módulo Externo | Tipo | Propósito |
|---------------|------|-----------|
| Catalog Graph | Query | APIs afetadas para blast radius |
| Jira | Sync | AttachWorkItemContext, SyncJiraWorkItems |
| AI Hub | UI | AssistantPanel para recomendações |
| Configuration | Settings | Workflow & promotion config |
| Identity | Context | User, permissions |
| Event Bus | Outbox | Eventos de integração |

---

## 8. Resumo de Ações

### Ações de Validação (P1)

| # | Ação | Esforço |
|---|------|---------|
| 1 | Validar fluxo completo: Release → Score → BlastRadius → Review → Decision | 3h |
| 2 | Validar fluxo Workflow: Template → Instance → Stages → Approval | 2h |
| 3 | Validar fluxo Promotion: Request → Gates → Approve/Block | 2h |
| 4 | Validar Freeze Windows e conflitos | 1h |
| 5 | Verificar integração Jira (AttachWorkItemContext) | 1h |

### Ações de Melhoria (P2)

| # | Ação | Esforço |
|---|------|---------|
| 6 | Documentar fluxo completo de Change Confidence com diagrama | 3h |
| 7 | Criar API reference para os 46 endpoints | 3h |
| 8 | Verificar cobertura de testes (179+ existentes) | 2h |
