# Relatório de Alinhamento Frontend ↔ Backend (Base de Dados) — NexTraceOne

> **Data:** 2025-01-XX  
> **Escopo:** Cruzamento entre páginas/rotas do frontend e suporte de persistência no backend  
> **Método:** Análise estática de rotas, páginas, API clients e DbContexts  
> **Contexto Frontend:** 14 features, 108 páginas, 130+ rotas, 12/14 módulos com API clients reais  
> **Contexto Backend:** 20 DbContexts, 129+ DbSets, 4 bases de dados lógicas

---

## 1. Resumo Executivo

| Métrica Frontend | Valor |
|---|---|
| Features/Módulos frontend | 14 |
| Páginas | 108 |
| Rotas | 130+ |
| Rotas quebradas | 3 (Contracts: governance, spectral, canonical) |
| Páginas órfãs | 9 |
| Módulos com API clients | 12 de 14 |

| Métrica de Alinhamento | Valor |
|---|---|
| UI_WITH_DB_SUPPORT | 7 módulos |
| UI_WITH_PARTIAL_DB_SUPPORT | 4 módulos |
| UI_WITH_WEAK_DB_SUPPORT | 2 módulos |
| NO_CLEAR_ALIGNMENT | 1 módulo |

### Classificação

| Classificação | Significado |
|---|---|
| **UI_WITH_DB_SUPPORT** | Frontend tem páginas com API clients e o backend tem DbContext + entidades completas |
| **UI_WITH_PARTIAL_DB_SUPPORT** | Frontend tem páginas mas o backend cobre apenas parte das funcionalidades exibidas |
| **UI_WITH_WEAK_DB_SUPPORT** | Frontend tem páginas mas o backend tem entidades mínimas ou fragmentadas |
| **NO_CLEAR_ALIGNMENT** | Frontend tem páginas sem correspondência clara no backend |

---

## 2. Análise por Módulo Frontend

### 2.1 Identity & Access — **UI_WITH_DB_SUPPORT** ✅

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Login, Register, Profile, Teams, Roles, Permissions, Tenants, Environments | IdentityDbContext (16 DbSets) |
| API Client | ✅ Existe | ✅ Endpoints completos |
| Rotas | Funcionais | — |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| User Management | User, UserRole | ✅ Suportado |
| Team Management | Team, TeamMember | ✅ Suportado |
| Role Management | Role, RolePermission, Permission | ✅ Suportado |
| Tenant Settings | Tenant, TenantMember | ✅ Suportado |
| Environment Management | EnvironmentProfile | ✅ Suportado |
| API Keys | ApiKey | ✅ Suportado |
| Sessions | Session, RefreshToken | ✅ Suportado |

**Avaliação:** Alinhamento completo — todas as páginas de identity têm suporte DB direto.

---

### 2.2 Service Catalog — **UI_WITH_DB_SUPPORT** ✅

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Service List, Service Detail, Dependencies, Topology, Health | CatalogGraphDbContext (8 DbSets) |
| API Client | ✅ Existe | ✅ Endpoints completos |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Service List | ServiceDefinition | ✅ Suportado |
| Service Detail | ServiceDefinition, ServiceOwnership, ServiceTag | ✅ Suportado |
| Dependencies View | ServiceDependency, DependencyEdge | ✅ Suportado |
| Topology Map | TopologySnapshot, DependencyEdge | ✅ Suportado |
| Service Health | ServiceHealth | ✅ Suportado |
| Endpoints | ServiceEndpoint | ✅ Suportado |

**Avaliação:** Forte — grafo de serviços bem suportado por 8 entidades.

---

### 2.3 Contracts — **UI_WITH_PARTIAL_DB_SUPPORT** ⚠️

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Contract List, Contract Detail, Contract Studio, Versions, Diff, Governance, Spectral, Canonical | ContractsDbContext (7 DbSets) |
| API Client | ✅ Existe | ✅ Endpoints parciais |
| Rotas quebradas | 3 (governance, spectral, canonical) | — |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Contract List | ApiContract, SoapContract, EventContract, BackgroundServiceContract | ✅ Suportado |
| Contract Detail | ContractVersion (Signature, Provenance) | ✅ Suportado |
| Contract Studio (create/edit) | ContractVersion, ContractSchema | ✅ Suportado |
| Versions | ContractVersion | ✅ Suportado |
| Diff View | — | ❌ Diff não persistido (computado) |
| **Governance** (rota quebrada) | ContractPolicy | ⚠️ Entidade existe mas rota quebrada |
| **Spectral** (rota quebrada) | — | ❌ Sem entidade de validação Spectral |
| **Canonical** (rota quebrada) | — | ❌ Sem entidade de canonical objects |

**Rotas quebradas identificadas:**
1. `/contracts/governance` — rota existe no router mas componente não carrega
2. `/contracts/spectral` — rota aponta para componente inexistente
3. `/contracts/canonical` — rota sem componente correspondente

**Avaliação:** Core de contratos bem suportado. 3 rotas quebradas precisam de correção. Faltam entidades para Spectral validation e Canonical objects.

---

### 2.4 Change Intelligence — **UI_WITH_DB_SUPPORT** ✅

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Releases, Blast Radius, Change Score, Validations, Correlations, Timeline | ChangeIntelligenceDbContext (10 DbSets) |
| API Client | ✅ Existe | ✅ Endpoints completos |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Release List | Release | ✅ Suportado |
| Release Detail | Release, ReleaseChange | ✅ Suportado |
| Blast Radius View | BlastRadius, ChangeImpactAssessment | ✅ Suportado |
| Change Score | ChangeScore | ✅ Suportado |
| Post-Deploy Validation | ChangeValidation | ✅ Suportado |
| Incident Correlation | ChangeCorrelation | ✅ Suportado |
| Change Timeline | ChangeTimeline, ChangeMetric | ✅ Suportado |

**Avaliação:** Excelente — 10 entidades cobrem todas as páginas de Change Intelligence.

---

### 2.5 Workflows & Promotions — **UI_WITH_PARTIAL_DB_SUPPORT** ⚠️

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Workflow Templates, Active Workflows, Approvals, Promotions, SLAs | WorkflowDbContext (6) + PromotionDbContext (4) |
| API Client | ✅ Existe | ✅ Endpoints parciais |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Workflow Templates | WorkflowTemplate | ✅ Suportado |
| Active Workflows | WorkflowInstance, WorkflowStage | ✅ Suportado |
| Approval Queue | WorkflowApproval | ✅ Suportado |
| Promotions | PromotionRequest, PromotionApproval | ✅ Suportado |
| SLA Dashboard | PromotionSLA | ✅ Suportado |
| Rollback View | — | ❌ Sem modelo de rollback |
| Escalation View | — | ❌ Sem modelo de escalation |

---

### 2.6 Governance — **UI_WITH_DB_SUPPORT** ✅

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Policies, Standards, Violations, Reports, Dashboard, Reviews | GovernanceDbContext (12 DbSets) |
| API Client | ✅ Existe | ✅ Endpoints completos |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Policy List | GovernancePolicy | ✅ Suportado |
| Policy Detail | GovernancePolicy, PolicyRule | ✅ Suportado |
| Standards | GovernanceStandard | ✅ Suportado |
| Violations | PolicyViolation | ✅ Suportado |
| Compliance Reports | ComplianceReport | ✅ Suportado |
| Governance Dashboard | GovernanceDashboard, GovernanceMetric | ✅ Suportado |
| Reviews | GovernanceReview | ✅ Suportado |
| Alerts | GovernanceAlert | ✅ Suportado |
| Risk Center | — | ❌ Sem modelo de risco dedicado |

---

### 2.7 Audit & Compliance — **UI_WITH_DB_SUPPORT** ✅

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Audit Log, Compliance Dashboard, Rules, Violations | AuditDbContext (6 DbSets) |
| API Client | ✅ Existe | ✅ Endpoints completos |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Audit Log | AuditEvent | ✅ Suportado |
| Audit Chain | AuditChainLink | ✅ Suportado |
| Compliance Dashboard | ComplianceReport, ComplianceSnapshot | ✅ Suportado |
| Compliance Rules | ComplianceRule | ✅ Suportado |
| Violations | ComplianceViolation | ✅ Suportado |

---

### 2.8 AI Assistant — **UI_WITH_PARTIAL_DB_SUPPORT** ⚠️

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Chat, Conversations, Agents, Models, Providers, Policies, Budgets, Knowledge, Guardrails | AiGovernanceDb (19+), AiOrchestrationDb (4), ExternalAiDb (4) |
| API Client | ✅ Existe | ✅ Endpoints parciais |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Chat Interface | Conversation, ConversationMessage | ✅ Suportado |
| Conversation History | Conversation, ConversationFeedback | ✅ Suportado |
| Agent Management | AiAgent, AiAgentCapability, AiAgentTool | ✅ Suportado |
| Model Registry | AiModel, AiModelVersion, AiProvider | ✅ Suportado |
| Access Policies | AiAccessPolicy | ✅ Suportado |
| Token Budgets | AiTokenBudget, AiTokenUsage | ✅ Suportado |
| Knowledge Sources | AiKnowledgeSource, AiKnowledgeIndex | ✅ Suportado |
| Guardrails | AiGuardrail, AiGuardrailViolation | ✅ Suportado |
| Experiments | AiExperiment, AiExperimentResult | ✅ Suportado |
| Agent Execution Log | — | ❌ Sem AiAgentExecution |
| Tool Usage Log | — | ❌ Sem ToolExecutionLog |
| IDE Extensions | — | ❌ Sem modelo de extensões |

**Avaliação:** Core de IA bem suportado (27+ entidades). Gaps em execution logging e IDE extensions.

---

### 2.9 Incidents & Operations — **UI_WITH_PARTIAL_DB_SUPPORT** ⚠️

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Incidents, Runbooks, Automation, Runtime, SLOs | IncidentDb (5), AutomationDb (3), RuntimeDb (4), ReliabilityDb (1) |
| API Client | ✅ Existe | ⚠️ Endpoints fragmentados |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Incident List | Incident | ✅ Suportado |
| Incident Detail | Incident, IncidentTimeline, IncidentCorrelation | ✅ Suportado |
| Runbook List | Runbook | ✅ Suportado |
| Runbook Execution | RunbookExecution | ✅ Suportado |
| Automation Rules | AutomationRule | ✅ Suportado |
| Runtime Metrics | RuntimeMetric, RuntimeBaseline | ✅ Suportado |
| Runtime Alerts | RuntimeAlert, RuntimeAnomaly | ✅ Suportado |
| SLO Dashboard | SLODefinition | ⚠️ Parcial (apenas 1 entidade) |
| Post-mortem | — | ❌ Sem modelo |
| Error Budget | — | ❌ Sem modelo |
| Burn Rate | — | ❌ Sem modelo |

---

### 2.10 FinOps / Cost Intelligence — **UI_WITH_WEAK_DB_SUPPORT** ⚠️

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Cost Dashboard, Allocations, Reports, Budgets, Anomalies | CostIntelligenceDbContext (6 DbSets) |
| API Client | ✅ Existe | ⚠️ Endpoints básicos |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Cost Dashboard | CostAllocation, CostReport | ✅ Suportado |
| Budget Alerts | BudgetAlert | ✅ Suportado |
| Cost Anomalies | CostAnomaly | ✅ Suportado |
| Import Pipeline | CostImportPipeline | ✅ Suportado |
| Cost by Team | — | ❌ Não modelado |
| Cost by Operation | — | ❌ Não modelado |
| Cost by Change | — | ❌ Não modelado |
| Waste Detection | — | ❌ Não modelado |

**Avaliação:** Schema básico existe mas não suporta FinOps contextualizado (por equipa, operação, mudança). Frontend pode mostrar dados genéricos mas não a narrativa diferenciadora do NexTraceOne.

---

### 2.11 Developer Portal — **UI_WITH_DB_SUPPORT** ✅

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Portal Pages, Documentation, Navigation | DeveloperPortalDbContext (5 DbSets) |
| API Client | ✅ Existe | ✅ Endpoints completos |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Portal Pages | PortalPage, PortalSection | ✅ Suportado |
| Assets | PortalAsset | ✅ Suportado |
| Navigation | PortalNavigation | ✅ Suportado |
| Settings | PortalSetting | ✅ Suportado |

---

### 2.12 Notifications — **UI_WITH_WEAK_DB_SUPPORT** ⚠️

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Notification Center, Preferences, Templates | NotificationsDbContext (3 DbSets) |
| API Client | ⚠️ Parcial | ⚠️ Sem migrations |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Notification Center | NotificationDelivery | ⚠️ Sem migrations |
| Templates | NotificationTemplate | ⚠️ Sem migrations |
| Channels | NotificationChannel | ⚠️ Sem migrations |
| User Preferences | — | ❌ Não modelado |

**Avaliação:** Entidades existem mas sem migrations formais (`EnsureCreated`). Frontend depende de schema instável.

---

### 2.13 Configuration — **UI_WITH_WEAK_DB_SUPPORT** ⚠️

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | System Config, Feature Flags, Overrides | ConfigurationDbContext (3 DbSets) |
| API Client | ✅ Existe | ⚠️ Sem migrations |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Configuration List | ConfigurationDefinition | ⚠️ Sem migrations |
| Configuration Values | ConfigurationValue | ⚠️ Sem migrations |
| Overrides | ConfigurationOverride | ⚠️ Sem migrations |

---

### 2.14 Knowledge Hub — **NO_CLEAR_ALIGNMENT** ❌

| Aspeto | Frontend | Backend |
|---|---|---|
| Páginas | Documentation Hub, Search, Changelog | Sem DbContext dedicado |
| API Client | ⚠️ Parcial | ❌ Sem entidades |

| Página Frontend | Entidade DB | Estado |
|---|---|---|
| Documentation Hub | — | ❌ Sem modelo |
| Knowledge Search | — | ❌ Sem modelo |
| Changelog | — | ❌ Sem modelo |
| Operational Notes | — | ❌ Sem modelo |

**Avaliação:** Knowledge Hub é um pilar do produto (Source of Truth & Operational Knowledge) mas não tem persistência dedicada. Pode usar parcialmente AiKnowledgeSource do módulo AI.

---

## 3. Rotas Quebradas — Detalhe

| # | Rota | Módulo | Problema | Impacto |
|---|---|---|---|---|
| 1 | `/contracts/governance` | Contracts | Componente não carrega | Médio — governance de contratos inacessível |
| 2 | `/contracts/spectral` | Contracts | Componente inexistente | Médio — validação Spectral inacessível |
| 3 | `/contracts/canonical` | Contracts | Componente inexistente | Baixo — canonical objects não implementados |

---

## 4. Páginas Órfãs (sem rota ativa)

9 páginas identificadas sem rota ativa apontando para elas. Estas páginas existem no código mas não são acessíveis via navegação.

| Prioridade de Correção | Ação |
|---|---|
| Alta | Verificar se devem ter rota e adicionar |
| Média | Verificar se são sub-componentes (não precisam de rota) |
| Baixa | Remover se forem código morto |

---

## 5. Matriz de Alinhamento

| Módulo Frontend | Classificação | DbContext(s) | Páginas | Cobertura |
|---|---|---|---|---|
| Identity | UI_WITH_DB_SUPPORT | IdentityDb | 8+ | 100% |
| Service Catalog | UI_WITH_DB_SUPPORT | CatalogGraphDb | 6+ | 100% |
| Contracts | UI_WITH_PARTIAL_DB_SUPPORT | ContractsDb | 8+ | 70% (3 rotas quebradas) |
| Change Intelligence | UI_WITH_DB_SUPPORT | ChangeIntelDb | 7+ | 100% |
| Workflows | UI_WITH_PARTIAL_DB_SUPPORT | WorkflowDb, PromotionDb | 7+ | 75% |
| Governance | UI_WITH_DB_SUPPORT | GovernanceDb | 8+ | 90% (falta Risk) |
| Audit | UI_WITH_DB_SUPPORT | AuditDb | 5+ | 100% |
| AI Assistant | UI_WITH_PARTIAL_DB_SUPPORT | AiGovDb, AiOrchDb, ExtAiDb | 12+ | 80% |
| Incidents | UI_WITH_PARTIAL_DB_SUPPORT | IncidentDb, AutoDb, RuntimeDb, ReliabilityDb | 11+ | 70% |
| FinOps | UI_WITH_WEAK_DB_SUPPORT | CostIntelDb | 6+ | 50% |
| Developer Portal | UI_WITH_DB_SUPPORT | DeveloperPortalDb | 4+ | 100% |
| Notifications | UI_WITH_WEAK_DB_SUPPORT | NotificationsDb | 3+ | 40% (sem migrations) |
| Configuration | UI_WITH_WEAK_DB_SUPPORT | ConfigurationDb | 3+ | 40% (sem migrations) |
| Knowledge Hub | NO_CLEAR_ALIGNMENT | — | 4+ | 0% |

---

## 6. Recomendações

### 🔴 Prioridade Alta

| # | Ação | Justificação |
|---|---|---|
| 1 | Corrigir 3 rotas quebradas em Contracts | Funcionalidade inacessível |
| 2 | Criar DbContext para Knowledge Hub | Pilar Source of Truth sem persistência |
| 3 | Migrar Notifications para migrations formais | Frontend depende de schema instável |

### 🟡 Prioridade Média

| # | Ação | Justificação |
|---|---|---|
| 4 | Migrar Configuration para migrations formais | Mesmo problema de Notifications |
| 5 | Resolver 9 páginas órfãs | Código morto ou funcionalidade perdida |
| 6 | Expandir FinOps para suportar contextualização | Frontend mostra dados genéricos |
| 7 | Adicionar execution logging ao AI module | Frontend pode mostrar mas BD não persiste |

### 🟢 Prioridade Baixa

| # | Ação | Justificação |
|---|---|---|
| 8 | Adicionar Risk Center ao Governance | Página sem modelo |
| 9 | Adicionar error budget/burn rate ao Reliability | Dashboard limitado |
| 10 | Adicionar post-mortem ao Incidents | Funcionalidade esperada |

---

*Relatório gerado como parte da auditoria modular de governança do NexTraceOne.*
