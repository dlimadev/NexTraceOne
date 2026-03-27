# Matriz de Capacidades e Lacunas

**Projeto:** NexTraceOne
**Data da Avaliação:** 2026-03-27
**Escopo:** Mapeamento completo de capacidades vs. estado de implementação

---

## 1. Visão Geral

Esta matriz mapeia cada capacidade oficial do NexTraceOne ao seu estado real de implementação, identificando lacunas concretas em backend, frontend, persistência e documentação. O objectivo é fornecer uma visão factual e accionável para priorização de trabalho.

### Legenda de Estados

| Estado | Significado |
|--------|-------------|
| **READY** | Funcionalidade implementada, persistência real, fluxo end-to-end funcional |
| **PARTIAL** | Entidades/handlers existem mas com lacunas significativas (dados simulados, handlers incompletos, UI parcial) |
| **INCOMPLETE** | Estrutura criada mas sem funcionalidade mínima viável (sem migrações, sem handlers reais) |
| **NOT IMPLEMENTED** | Módulo ou capacidade não existe no código |

---

## 2. Foundation — Identidade, Organização e Infraestrutura

| Capacidade | Módulo Backend | Frontend Existe? | Persistência? | Documentação? | Estado | Gaps Principais | Prioridade |
|---|---|---|---|---|---|---|---|
| **Identity** | `identityaccess` — `IdentityDbContext` | ✅ Login, registo, perfil | ✅ Tabelas `iam_users`, `iam_security_events` com migrações | ✅ Inline | **READY** | SSO/OIDC como extensão futura; login local funcional | Baixa |
| **Organization** | `identityaccess` — entidades de tenant/org | ✅ Páginas de gestão | ✅ Tabelas `iam_*` | ✅ Inline | **READY** | Multi-tenancy básico funcional | Baixa |
| **Teams** | `governance` — `GovernanceDbContext` | ✅ `TeamsOverviewPage`, `TeamDetailPage` | ✅ Tabela `gov_teams`, `gov_team_domain_links` | ✅ Inline | **PARTIAL** | Teams no módulo Governance; deveria ter visibilidade forte no Catalog; ownership cross-module limitado | Média |
| **Ownership** | `governance` + `catalog` | ✅ Parcial nas páginas de serviço | ✅ Referências em `cat_service_assets` e `gov_teams` | Parcial | **PARTIAL** | Ownership definido mas sem enforcement automático cross-module; sem vista consolidada de ownership | Média |
| **Environments** | `identityaccess` — `IdentityDbContext` | ✅ `EnvironmentsPage` | ✅ Tabelas `iam_*` com suporte a ambientes | ✅ Inline | **READY** | Ambientes como entidades first-class; distinction dev/pre-prod/prod funcional | Baixa |
| **Integrations** | `integrations` — `IntegrationsDbContext` | ✅ `ConnectorDetailPage`, `IngestionExecutionsPage` | ⚠️ Tabelas `int_connectors`, `int_ingestion_sources`, `int_ingestion_executions` definidas mas **sem migrações EF formais** | Parcial | **INCOMPLETE** | Sem provider adapters reais (GitLab, Jenkins, GitHub, Azure DevOps); estrutura existe mas sem implementação funcional; sem migrações | **Alta** |
| **Licensing & Entitlements** | ❌ Não existe módulo | ❌ | ❌ | ❌ | **NOT IMPLEMENTED** | Módulo completo por criar: activação, validação, heartbeat, machine fingerprinting, entitlements por capacidade | **Alta** |
| **Audit & Traceability** | `auditcompliance` — `AuditDbContext` | ✅ Parcial | ✅ Tabelas `aud_audit_events`, `aud_compliance_results`, `aud_compliance_policies` | ✅ Inline | **PARTIAL** | Chain integrity e retenção implementados; falta enforcement completo de auditoria cross-module | Média |

### Resumo Foundation

```
READY:            3/8 (Identity, Organization, Environments)
PARTIAL:          3/8 (Teams, Ownership, Audit)
INCOMPLETE:       1/8 (Integrations)
NOT IMPLEMENTED:  1/8 (Licensing)
```

---

## 3. Services — Catálogo, Topologia e Fiabilidade

| Capacidade | Módulo Backend | Frontend Existe? | Persistência? | Documentação? | Estado | Gaps Principais | Prioridade |
|---|---|---|---|---|---|---|---|
| **Service Catalog** | `catalog` — `CatalogGraphDbContext` | ✅ `ServiceCatalogListPage` | ✅ Tabelas `cat_service_assets` com migrações | ✅ Inline | **READY** | Catálogo funcional com CRUD, metadata, classificação | Baixa |
| **Team Services** | `catalog` + `governance` | ✅ `TeamDetailPage` com lista de serviços | ✅ Referências cross-module | Parcial | **PARTIAL** | Vista de serviços por equipa existe mas sem drill-down completo; depende de ownership no Governance | Média |
| **Dependencies / Topology** | `catalog` — `CatalogGraphDbContext` | ✅ Parcial | ✅ Entidades de grafo/dependências | Parcial | **PARTIAL** | Entidades existem mas visualização de topologia limitada; sem discovery automático | Média |
| **Service Reliability** | `operationalintelligence` — `ReliabilityDbContext` | ✅ `ServiceReliabilityDetailPage`, `TeamReliabilityPage` | ✅ Tabelas `ops_reliability_snapshots` | ✅ Inline | **PARTIAL** | **13 handlers usam `IsSimulated`**; SLO/SLA entities existem mas dados são fabricados; burn rates e error budgets simulados | **Alta** |
| **Service Lifecycle** | `catalog` | ✅ Parcial | ✅ Entidades de lifecycle | Parcial | **PARTIAL** | Lifecycle states definidos mas sem automação de transições | Baixa |
| **Service Metadata & Classification** | `catalog` — `CatalogGraphDbContext` | ✅ Parcial | ✅ Metadata em `cat_service_assets` | ✅ Inline | **READY** | Classificação e metadata funcionais | Baixa |

### Resumo Services

```
READY:    2/6 (Service Catalog, Metadata & Classification)
PARTIAL:  4/6 (Team Services, Dependencies, Reliability, Lifecycle)
```

**Ficheiros de referência — Service Reliability (IsSimulated):**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Reliability/Persistence/ReliabilityDbContext.cs`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Features/` — handlers de reliability

---

## 4. Contracts — Governança de Contratos

| Capacidade | Módulo Backend | Frontend Existe? | Persistência? | Documentação? | Estado | Gaps Principais | Prioridade |
|---|---|---|---|---|---|---|---|
| **API Contracts (REST)** | `catalog` — `ContractsDbContext` | ✅ `ContractListPage` | ✅ Tabelas `ctr_contract_versions`, `ctr_contract_drafts` | ✅ Inline | **READY** | Contratos REST com CRUD, versionamento, draft workflow | Baixa |
| **SOAP Contracts** | `catalog` — `ContractsDbContext` | ✅ Parcial | ✅ Mesmas tabelas `ctr_*` | ✅ Inline | **READY** | Suporte a WSDL como tipo de contrato | Baixa |
| **Event Contracts** | `catalog` — `ContractsDbContext` | ✅ Parcial | ✅ Mesmas tabelas `ctr_*` | ✅ Inline | **READY** | AsyncAPI e Kafka contracts suportados | Baixa |
| **Background Service Contracts** | `catalog` — `ContractsDbContext` | ✅ Parcial | ✅ Mesmas tabelas `ctr_*` | ✅ Inline | **READY** | Background services como tipo de contrato | Baixa |
| **Contract Studio** | `catalog` | ✅ UI de edição de drafts | ✅ `ctr_contract_drafts` | ✅ Inline | **READY** | Draft workflow com validação Spectral funcional | Baixa |
| **Versioning & Compatibility** | `catalog` — `ContractsDbContext` | ✅ Parcial | ✅ `ctr_contract_versions` | ✅ Inline | **READY** | Versionamento semântico com diff implementado | Baixa |
| **Publication Center** | `catalog` — `DeveloperPortalDbContext` | ✅ `DeveloperPortalPage` parcial | ✅ Tabelas de portal | Parcial | **PARTIAL** | Fluxo de publicação existe mas approval workflow pode ser mais robusto; portal developer básico | Média |
| **Contract Policies** | `catalog` — `ContractsDbContext` | ✅ `SpectralRulesetManagerPage` | ✅ `ctr_spectral_rulesets` | ✅ Inline | **PARTIAL** | Spectral rulesets geridos na BD; enforcement automático parcial; falta policy enforcement cross-module | Média |
| **Examples & Schemas** | `catalog` | ✅ Parcial | ✅ Dentro de contract versions | Parcial | **PARTIAL** | Exemplos podem ser associados a contratos; UX de gestão de exemplos básica | Baixa |

### Resumo Contracts

```
READY:    6/9 (REST, SOAP, Event, Background, Studio, Versioning)
PARTIAL:  3/9 (Publication Center, Policies, Examples)
```

**Ficheiros de referência:**
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/ContractsDbContext.cs`
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Portal/Persistence/DeveloperPortalDbContext.cs`
- `src/frontend/src/features/contracts/spectral/SpectralRulesetManagerPage.tsx`

---

## 5. Changes — Change Intelligence e Promoção

| Capacidade | Módulo Backend | Frontend Existe? | Persistência? | Documentação? | Estado | Gaps Principais | Prioridade |
|---|---|---|---|---|---|---|---|
| **Change Intelligence** | `changegovernance` — `ChangeIntelligenceDbContext` | ✅ Parcial | ✅ Tabelas `chg_*` com migrações | ✅ Inline | **PARTIAL** | Entidades existem; scoring de confiança precisa de telemetria real; correlações com dados simulados | **Alta** |
| **Change Validation** | `changegovernance` — `RulesetGovernanceDbContext` | ✅ Parcial | ✅ `chg_lint_results` | ✅ Inline | **PARTIAL** | Lint results persistidos; validação via rulesets funcional; falta validação pós-change com dados reais | Média |
| **Promotion Governance** | `changegovernance` — `PromotionDbContext` | ✅ Parcial | ✅ `chg_promotion_requests` | ✅ Inline | **PARTIAL** | Gates e requests de promoção existem; workflow entre ambientes funcional; approval workflow parcial | Média |
| **Production Change Confidence** | `changegovernance` | ✅ Parcial | ✅ Entidades de scoring | Parcial | **PARTIAL** | Scoring existe como entidade mas depende de dados simulados; sem ingestão real de métricas pós-deploy | **Alta** |
| **Blast Radius** | `changegovernance` — `ChangeIntelligenceDbContext` | ✅ Parcial | ✅ Entidade de blast radius | Parcial | **PARTIAL** | Entidade existe mas é computada sem dados reais de topologia/telemetria; análise estática apenas | **Alta** |
| **Change-to-Incident Correlation** | `changegovernance` + `operationalintelligence` | ✅ Parcial | ✅ Referências cross-module | Parcial | **PARTIAL** | Correlação conceptual existe; sem dados reais de timeline para correlação temporal | **Alta** |
| **Release Identity** | `changegovernance` — `WorkflowDbContext` | ✅ Parcial | ✅ Entidades de release | ✅ Inline | **PARTIAL** | Releases como entidades com identidade; workflow de lifecycle parcial | Média |
| **Evidence Pack** | `changegovernance` | ✅ `EvidencePackagesPage` | ✅ Entidade de evidence pack | Parcial | **PARTIAL** | Entidade existe; UI existe; falta agregação automática de evidências de múltiplas fontes | Média |
| **Rollback Intelligence** | `changegovernance` | ✅ Parcial | ✅ Entidade de rollback assessment | Parcial | **PARTIAL** | Assessment entity existe; falta lógica real de recomendação baseada em dados | Média |
| **Release Calendar** | `changegovernance` | ❌ Sem UI de calendário | ✅ Freeze windows como entidades | Parcial | **INCOMPLETE** | Freeze windows existem na BD mas **não há UI de calendário**; não é possível visualizar releases no tempo | Média |

### Resumo Changes

```
READY:      0/10
PARTIAL:    9/10
INCOMPLETE: 1/10 (Release Calendar)
```

**Ficheiros de referência:**
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/ChangeIntelligence/Persistence/ChangeIntelligenceDbContext.cs`
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/Promotion/Persistence/PromotionDbContext.cs`
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/RulesetGovernance/Persistence/RulesetGovernanceDbContext.cs`
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/Workflow/Persistence/WorkflowDbContext.cs`

---

## 6. Operations — Incidentes, Runbooks e Consistência

| Capacidade | Módulo Backend | Frontend Existe? | Persistência? | Documentação? | Estado | Gaps Principais | Prioridade |
|---|---|---|---|---|---|---|---|
| **Incidents & Mitigation** | `operationalintelligence` — `IncidentDbContext` | ✅ Parcial | ✅ Tabelas `ops_incidents` | ✅ Inline | **PARTIAL** | Entidades e handlers existem; incident lifecycle parcial; correlação com mudanças limitada por dados simulados | Média |
| **Runbooks** | `operationalintelligence` — `AutomationDbContext` | ✅ `RunbooksPage` | ✅ Tabelas de runbooks | ✅ Inline | **PARTIAL** | Runbooks como entidades; UI existe; falta execução automatizada real; conexão com incidentes parcial | Média |
| **Operational Consistency** | `operationalintelligence` | ✅ `AutomationWorkflowsPage`, `AutomationAdminPage` | ✅ Tabelas de automação | Parcial | **PARTIAL** | Workflows de automação existem mas com handlers parcialmente vazios; enforcement de consistência conceptual | Média |
| **AIOps Insights** | `aiknowledge` + `operationalintelligence` | ✅ Parcial | ✅ Cross-module | Parcial | **PARTIAL** | Insights dependem de AI grounding que é básico; sem retrieval cross-module real | **Alta** |
| **Monitoring Contextualizado** | `operationalintelligence` — `RuntimeIntelligenceDbContext` | ✅ Parcial | ✅ Tabelas `ops_*` de runtime | Parcial | **PARTIAL** | Runtime Intelligence DbContext existe; sem ingestão real de telemetria; sem contextualização por serviço/contrato/mudança | **Alta** |
| **Post-change Verification** | `changegovernance` + `operationalintelligence` | ✅ Parcial | ✅ Referências cross-module | Parcial | **PARTIAL** | Verificação pós-change conceptual; sem dados reais de comportamento pós-deploy | **Alta** |

### Resumo Operations

```
READY:   0/6
PARTIAL: 6/6
```

**Ficheiros de referência:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Incidents/Persistence/IncidentDbContext.cs`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Automation/Persistence/AutomationDbContext.cs`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Runtime/Persistence/RuntimeIntelligenceDbContext.cs`

---

## 7. Knowledge — Documentação e Conhecimento Operacional

| Capacidade | Módulo Backend | Frontend Existe? | Persistência? | Documentação? | Estado | Gaps Principais | Prioridade |
|---|---|---|---|---|---|---|---|
| **Documentation & Knowledge Hub** | `knowledge` — `KnowledgeDbContext` | ❌ Sem páginas dedicadas | ⚠️ DbContext existe mas **sem migrações EF** | Parcial | **INCOMPLETE** | Módulo novo; sem persistência real; sem UI; sem operações CRUD completas; sem relações cross-module | **Alta** |
| **Source of Truth Views** | `catalog` + `governance` + cross-module | ✅ `GlobalSearchPage` | ✅ PostgreSQL FTS implementado | Parcial | **PARTIAL** | Global Search funcional; falta consolidação de vistas por serviço/contrato/mudança numa única fonte de verdade | Média |
| **Changelog** | Parcial em múltiplos módulos | ✅ Parcial | ✅ Audit events | Parcial | **PARTIAL** | Audit trail existe; falta vista de changelog orientada ao utilizador | Baixa |
| **Operational Notes** | `knowledge` | ❌ | ⚠️ Sem migrações | ❌ | **INCOMPLETE** | Conceptual; sem implementação real | Média |
| **Search / Command Palette** | `catalog` + cross-module | ✅ `GlobalSearchPage` | ✅ PostgreSQL FTS | Parcial | **PARTIAL** | FTS funcional; sem command palette; search limitado ao que tem dados reais | Média |
| **Knowledge Relations** | `knowledge` | ❌ | ⚠️ Sem migrações | ❌ | **INCOMPLETE** | Relações knowledge ↔ serviço/contrato/mudança não implementadas | **Alta** |

### Resumo Knowledge

```
READY:      0/6
PARTIAL:    3/6 (Source of Truth Views, Changelog, Search)
INCOMPLETE: 3/6 (Knowledge Hub, Operational Notes, Relations)
```

**Ficheiros de referência:**
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/KnowledgeDbContext.cs`

---

## 8. AI — Assistente, Agentes e Governança

| Capacidade | Módulo Backend | Frontend Existe? | Persistência? | Documentação? | Estado | Gaps Principais | Prioridade |
|---|---|---|---|---|---|---|---|
| **AI Assistant** | `aiknowledge` — `AiOrchestrationDbContext` | ✅ Chat interface | ✅ Sessões e resultados persistidos | ✅ Inline | **PARTIAL** | Streaming one-shot funcional; grounding básico; sem retrieval avançado; sem cross-module entity lookup | **Alta** |
| **AI Agents** | `aiknowledge` — `AiOrchestrationDbContext` | ✅ Parcial | ✅ Tool execution persistido | Parcial | **PARTIAL** | 3 ferramentas reais implementadas; tool execution funcional; agentes especializados não implementados; **5 handlers de orquestração vazios** | **Alta** |
| **Model Registry** | `aiknowledge` — `AiGovernanceDbContext` | ✅ Parcial | ✅ Modelos registados na BD | ✅ Inline | **READY** | Registry funcional com metadata de modelos | Baixa |
| **AI Access Policies** | `aiknowledge` — `AiGovernanceDbContext` | ✅ Parcial | ✅ Entidades de políticas | Parcial | **PARTIAL** | Políticas como entidades; enforcement parcial; sem UI completa de gestão | Média |
| **External AI Integrations** | `aiknowledge` — `ExternalAiDbContext` | ✅ Parcial | ✅ Tabelas de integração | Parcial | **PARTIAL** | **6 handlers ExternalAI vazios** (D-024 a D-029); sem integração real com providers; DbContext e entidades existem | **Alta** |
| **AI Token & Budget Governance** | `aiknowledge` — `AiGovernanceDbContext` | ✅ Parcial | ✅ Entidades de budgets/quotas | Parcial | **PARTIAL** | Budgets como entidades; sem tracking real de consumo; sem enforcement de limites | Média |
| **AI Knowledge Sources** | `aiknowledge` + `knowledge` | ✅ Parcial | ⚠️ Knowledge sem migrações | Parcial | **PARTIAL** | Grounding básico funcional; sem vector search; sem indexação de Knowledge Hub (módulo incompleto) | **Alta** |
| **AI Audit & Usage** | `aiknowledge` — `AiGovernanceDbContext` | ✅ Parcial | ✅ Entidades de auditoria | Parcial | **PARTIAL** | Auditoria de uso como entidades; sem dashboard de usage analytics completo | Média |
| **IDE Extensions Management** | `aiknowledge` | ✅ Parcial | ✅ Handlers existem | Parcial | **PARTIAL** | Handlers de gestão de extensões existem; sem implementação de extensões VS Code/Visual Studio reais | Baixa |

### Resumo AI

```
READY:   1/9 (Model Registry)
PARTIAL: 8/9
```

**Ficheiros de referência:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/ExternalAI/Persistence/ExternalAiDbContext.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Persistence/AiGovernanceDbContext.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Orchestration/Persistence/AiOrchestrationDbContext.cs`

---

## 9. Governance — Relatórios, Risco e Compliance

| Capacidade | Módulo Backend | Frontend Existe? | Persistência? | Documentação? | Estado | Gaps Principais | Prioridade |
|---|---|---|---|---|---|---|---|
| **Reports** | `governance` — `GovernanceDbContext` | ✅ `ReportsPage` | ✅ Tabelas `gov_*` | Parcial | **PARTIAL** | Executive overview e compliance reports parciais; **8 handlers com TODO** (D-039, D-040, D-047); dados simulados em vários handlers | Média |
| **Risk Center** | `governance` | ✅ `RiskHeatmapPage` | ✅ Entidades de risco | Parcial | **PARTIAL** | Risk heatmap como UI; avaliação de risco parcial; falta correlação real com mudanças e incidentes | Média |
| **Compliance** | `governance` + `auditcompliance` | ✅ `EnterpriseControlsPage`, `GovernancePacksOverviewPage` | ✅ `aud_compliance_policies`, `aud_compliance_results`, `gov_*` | Parcial | **PARTIAL** | Governance packs existem; compliance policies e results persistidos; falta enforcement automático completo | Média |
| **FinOps** | `governance` + `operationalintelligence` | ✅ `FinOpsPage`, `DomainFinOpsPage`, `ServiceFinOpsPage`, `TeamFinOpsPage`, `ExecutiveFinOpsPage`, `BenchmarkingPage` | ✅ `ops_cost_records` | Parcial | **PARTIAL** | **Todos os handlers usam dados simulados** (`IsSimulated`); 6 páginas com DemoBanner; sem integração real com fontes de custo | **Alta** |
| **Executive Views** | `governance` | ✅ `ExecutiveDrillDownPage`, `ExecutiveFinOpsPage` | ✅ Parcial | Parcial | **PARTIAL** | Vistas executivas existem mas consomem dados simulados; drill-down limitado | Média |
| **Policy Management** | `governance` + `auditcompliance` | ✅ `PolicyCatalogPage` | ✅ Entidades de políticas | Parcial | **PARTIAL** | Catálogo de políticas existe; enforcement parcial; falta gestão completa de lifecycle de políticas | Média |

### Resumo Governance

```
READY:   0/6
PARTIAL: 6/6
```

**Ficheiros de referência:**
- `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/GovernanceDbContext.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/` — handlers de FinOps e executive
- `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Infrastructure/Persistence/AuditDbContext.cs`

---

## 10. Capacidades Transversais

| Capacidade | Módulo Backend | Frontend Existe? | Persistência? | Documentação? | Estado | Gaps Principais | Prioridade |
|---|---|---|---|---|---|---|---|
| **Notifications** | `notifications` — `NotificationsDbContext` | ✅ Parcial | ✅ Tabelas `ntf_*` (22 tabelas) com 4 migrações | ✅ Inline | **READY** | Notificações com templates, preferências, canais de delivery, SMTP, retry history | Baixa |
| **Configuration** | `configuration` — `ConfigurationDbContext` | ✅ Parcial | ✅ Tabelas `cfg_*` com migração InitialCreate | ✅ Inline | **READY** | Feature flags, configuration entries, definitions, audit; migração aplicada | Baixa |
| **ProductAnalytics** | `productanalytics` — `ProductAnalyticsDbContext` | ✅ Parcial | ⚠️ Tabelas `pan_*` definidas mas **sem migrações EF formais** | Parcial | **INCOMPLETE** | Sem migrações; sem projecto Contracts; sem event publishing; `GetFrictionIndicators` usa `IsSimulated` | Média |
| **Global Search** | Cross-module (PostgreSQL FTS) | ✅ `GlobalSearchPage` | ✅ FTS indexes | Parcial | **PARTIAL** | FTS funcional; limitado ao que tem dados reais; sem command palette | Média |

### Resumo Transversais

```
READY:      2/4 (Notifications, Configuration)
PARTIAL:    1/4 (Global Search)
INCOMPLETE: 1/4 (ProductAnalytics)
```

---

## 11. Resumo Consolidado

### Por Estado

| Estado | Total | Percentagem | Capacidades |
|--------|-------|-------------|-------------|
| **READY** | 14 | 25% | Identity, Organization, Environments, Service Catalog, Metadata, REST/SOAP/Event/Background Contracts, Studio, Versioning, Model Registry, Notifications, Configuration |
| **PARTIAL** | 34 | 61% | Teams, Ownership, Audit, Team Services, Dependencies, Reliability, Lifecycle, Publication, Policies, Examples, Change Intelligence, Validation, Promotion, Confidence, Blast Radius, Correlation, Release Identity, Evidence Pack, Rollback, Incidents, Runbooks, Consistency, AIOps, Monitoring, Post-change, Source of Truth, Changelog, Search, AI Assistant, Agents, AI Policies, External AI, Budgets, AI Sources, AI Audit, IDE Extensions, Reports, Risk, Compliance, FinOps, Executive, Policy Management, Global Search |
| **INCOMPLETE** | 6 | 11% | Integrations, Knowledge Hub, Operational Notes, Knowledge Relations, Release Calendar, ProductAnalytics |
| **NOT IMPLEMENTED** | 1 | 2% | Licensing & Entitlements |

### Por Prioridade

| Prioridade | Capacidades |
|------------|-------------|
| **Alta** | Service Reliability (IsSimulated), Integrations, Licensing, Change Intelligence, Confidence, Blast Radius, Correlation, AIOps, Monitoring, Post-change, Knowledge Hub, Knowledge Relations, AI Assistant, AI Agents, External AI, AI Sources, FinOps |
| **Média** | Teams, Ownership, Audit, Team Services, Dependencies, Promotion, Validation, Release Identity, Evidence Pack, Rollback, Release Calendar, Incidents, Runbooks, Consistency, Operational Notes, Changelog, Search, AI Policies, Budgets, AI Audit, Reports, Risk, Compliance, Executive, Policy Management, ProductAnalytics, Global Search |
| **Baixa** | Identity, Organization, Environments, Service Catalog, Metadata, Lifecycle, REST/SOAP/Event/Background Contracts, Studio, Versioning, Publication, Examples, Model Registry, IDE Extensions, Notifications, Configuration |

---

## 12. Padrões Transversais Identificados

### 12.1 Módulos sem Migrações EF

| Módulo | DbContext | Prefixo de Tabelas | Estado |
|--------|-----------|---------------------|--------|
| Knowledge | `KnowledgeDbContext` | `knw_*` | Sem migrações |
| Integrations | `IntegrationsDbContext` | `int_*` | Sem migrações formais |
| ProductAnalytics | `ProductAnalyticsDbContext` | `pan_*` | Sem migrações formais |

### 12.2 Handlers com Dados Simulados (IsSimulated)

| Módulo | Handlers Afectados | Impacto |
|--------|--------------------|---------|
| Governance (FinOps) | GetDomainFinOps, GetServiceFinOps, GetTeamFinOps, GetFinOpsSummary, GetFinOpsTrends, GetBenchmarking, GetWasteSignals, GetEfficiencyIndicators, GetExecutiveTrends, GetExecutiveDrillDown | FinOps inteiro é demonstração |
| ProductAnalytics | GetFrictionIndicators | Analytics sem dados reais |

### 12.3 DbContexts por Módulo

| Módulo | Nº DbContexts | Nomes |
|--------|----------------|-------|
| operationalintelligence | 5 | Automation, Cost, Incidents, Reliability, Runtime |
| changegovernance | 4 | ChangeIntelligence, Promotion, RulesetGovernance, Workflow |
| catalog | 3 | Contracts, Graph, Portal |
| aiknowledge | 3 | ExternalAI, Governance, Orchestration |
| Outros (7 módulos) | 1 cada | Identity, Audit, Governance, Config, Integrations, Knowledge, Notifications, ProductAnalytics |
| **Total** | **23** | |

---

*Documento gerado como parte da avaliação de estado do projecto NexTraceOne em 2026-03-27.*
