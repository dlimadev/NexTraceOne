# Matriz de Capacidades vs. Implementação Real — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Legenda de Status
- ✅ **PRONTO** — implementação real, funcional, persistida, conectada E2E
- ⚠️ **PARCIAL** — valor real mas partes críticas ausentes ou sem ligação E2E
- 🟡 **MOCK** — dados simulados/hardcoded; `IsSimulated: true` ou inline mock
- ❌ **PLAN / BROKEN** — interface/contrato definido sem implementação; ou quebrado
- 🚫 **AUSENTE** — não existe no repositório

---

## Matriz Completa

| Capacidade | Módulo Esperado | Backend | Frontend | Persistência | Docs | Integração Cross-module | Estado | Principais Gaps | Prioridade |
|---|---|---|---|---|---|---|---|---|---|
| **SERVICE CATALOG** | catalog | ✅ Real | ✅ Real | ✅ CatalogGraphDbContext | ✅ | ⚠️ Parcial | ✅ PRONTO | Developer Portal 7 stubs aguardam IContractsModule | Baixa |
| **OWNERSHIP** | catalog | ✅ Real | ✅ Real | ✅ CatalogGraphDbContext | ✅ | ⚠️ Parcial | ✅ PRONTO | IContractsModule PLAN | Média |
| **TEAMS** | identityaccess + governance | ✅ Identity real; Governance mock | ✅ Real (dados mock) | ✅ IdentityDbContext | ✅ | ❌ | 🟡 MOCK | Governance sem persistência própria | Alta |
| **ENVIRONMENTS** | identityaccess | ✅ Real | ✅ Real | ✅ IdentityDbContext | ✅ | — | ✅ PRONTO | — | — |
| **CONTRACT GOVERNANCE (REST)** | catalog | ✅ Real | ✅ Real | ✅ ContractsDbContext | ✅ | ⚠️ SearchCatalog stub | ✅ PRONTO | SearchCatalog cross-module stub | Baixa |
| **CONTRACT GOVERNANCE (SOAP/WSDL)** | catalog | ✅ Real | ✅ Real | ✅ ContractsDbContext | ✅ | — | ✅ PRONTO | — | — |
| **EVENT CONTRACTS (AsyncAPI/Kafka)** | catalog | ✅ Real | ✅ Real | ✅ ContractsDbContext | ✅ | — | ✅ PRONTO | — | — |
| **BACKGROUND SERVICE CONTRACTS** | catalog | ✅ Real | ✅ Real | ✅ ContractsDbContext | ✅ | — | ✅ PRONTO | — | — |
| **VERSIONING & COMPATIBILITY** | catalog | ✅ Real | ✅ Real | ✅ ContractsDbContext | ✅ | — | ✅ PRONTO | ComputeSemanticDiff, EvaluateCompatibility reais | — |
| **CONTRACT STUDIO** | catalog | ✅ Real | ⚠️ Parcial | ✅ ContractsDbContext | ✅ | — | ⚠️ PARCIAL | UX precisa polish | Média |
| **PUBLICATION CENTER** | catalog | ✅ Real (PublishDraft, SignContractVersion) | ✅ Real | ✅ ContractsDbContext | ✅ | — | ✅ PRONTO | — | — |
| **CONTRACT POLICIES (Spectral Lint)** | changegovernance | ✅ Real | ✅ Real | ✅ RulesetGovernanceDbContext | ✅ | — | ✅ PRONTO | — | — |
| **EXAMPLES & SCHEMAS** | catalog | ✅ Real | ⚠️ Parcial | ✅ ContractsDbContext | ✅ | — | ⚠️ PARCIAL | UI parcial | Baixa |
| **DEVELOPER PORTAL** | catalog | ⚠️ 7 stubs | ⚠️ Parcial | ✅ DeveloperPortalDbContext | ✅ | ❌ IContractsModule | ⚠️ PARCIAL | SearchCatalog, RenderOpenApiContract, GetApiHealth, GetMyApis, GetApisIConsume, GetApiDetail, GetAssetTimeline aguardam IContractsModule | Alta |
| **CHANGE INTELLIGENCE** | changegovernance | ✅ Real | ✅ Real | ✅ ChangeIntelligenceDbContext | ✅ | ⚠️ CI/CD stub | ✅ PRONTO | Integração CI/CD externa stub | Média |
| **BLAST RADIUS** | changegovernance | ✅ Real | ✅ Real | ✅ ChangeIntelligenceDbContext | ✅ | — | ✅ PRONTO | — | — |
| **PROMOTION GOVERNANCE** | changegovernance | ✅ Real | ✅ Real | ✅ PromotionDbContext | ✅ | ⚠️ IPromotionModule PLAN | ✅ PRONTO | IPromotionModule cross-module PLAN | Média |
| **EVIDENCE PACK** | changegovernance | ✅ Real | ✅ Real | ✅ WorkflowDbContext | ✅ | — | ✅ PRONTO | GenerateEvidencePack real | — |
| **ROLLBACK INTELLIGENCE** | changegovernance | ✅ Real | ✅ Real | ✅ ChangeIntelligenceDbContext | ✅ | — | ✅ PRONTO | — | — |
| **RELEASE CALENDAR** | changegovernance | ✅ Real (FreezeWindow) | ⚠️ Parcial | ✅ ChangeIntelligenceDbContext | ✅ | — | ⚠️ PARCIAL | Calendar UI não auditada E2E | Média |
| **INCIDENT CORRELATION** | operationalintelligence | ⚠️ EfIncidentStore real; correlação mock | ❌ mockIncidents inline | ✅ IncidentDbContext | ✅ | — | ❌ BROKEN | Engine de correlação dinâmica ausente; frontend não conectado | **Crítica** |
| **RUNBOOKS** | operationalintelligence | 🟡 3 hardcoded | ❌ Mock UI | ✅ IncidentDbContext (RunbookRecord) | ✅ | — | 🟡 MOCK | RunbookRecord existe no schema; handlers não usam DB | Alta |
| **MITIGATION WORKFLOWS** | operationalintelligence | ⚠️ CreateMitigationWorkflow existe mas não persiste | ❌ Mock UI | ✅ IncidentDbContext (MitigationRecord) | ✅ | — | ❌ BROKEN | CreateMitigationWorkflow descarta dados; sem persistência real | Alta |
| **POST-CHANGE VERIFICATION** | changegovernance + ops | ⚠️ Gates reais; RecordMitigationValidation mock | ⚠️ Parcial | ✅ PromotionDbContext | ✅ | — | ⚠️ PARCIAL | RecordMitigationValidation não persiste | Alta |
| **AUTOMATION WORKFLOWS** | operationalintelligence | 🟡 Retornam PreviewOnly | 🟡 PreviewOnly flag | ✅ AutomationDbContext | ✅ | — | 🟡 MOCK | Handlers retornam PreviewOnly; sem automação real | Média |
| **SERVICE RELIABILITY (SLO/SLA)** | operationalintelligence | 🟡 8 serviços hardcoded | ✅ Real (dados mock) | ✅ ReliabilityDbContext | ✅ | — | 🟡 MOCK | ReliabilityDbContext existe; handlers não consultam DB | Média |
| **KNOWLEDGE HUB** | knowledge | ⚠️ Notes e changelog via eventos | ⚠️ Parcial | ❌ KnowledgeDbContext sem migrações geradas | ⚠️ | — | ⚠️ PARCIAL | Sem schema deployável; módulo incompleto | Alta |
| **OPERATIONAL NOTES** | knowledge | ⚠️ Parcial | ⚠️ Parcial | ❌ Sem migrações | ⚠️ | — | ⚠️ PARCIAL | Depende de KnowledgeDbContext | Alta |
| **CHANGELOG** | knowledge + changegovernance | ⚠️ Parcial via events | ⚠️ Parcial | ❌ Sem migrações Knowledge | ✅ | — | ⚠️ PARCIAL | Knowledge sem schema | Média |
| **SOURCE OF TRUTH VIEWS** | catalog | ✅ Real | ✅ Real | ✅ Múltiplos DbContexts | ✅ | — | ✅ PRONTO | ServiceSourceOfTruth, ContractSourceOfTruth funcionais | — |
| **GLOBAL SEARCH** | catalog | ⚠️ GlobalSearch real; SearchCatalog stub | ✅ Real | ✅ Catalog DBs | ✅ | — | ⚠️ PARCIAL | SearchCatalog cross-module stub | Média |
| **AI ASSISTANT** | aiknowledge | ❌ SendAssistantMessage hardcoded | ❌ mockConversations | ⚠️ AiOrchestrationDbContext | ✅ | Ollama configurado | ❌ BROKEN | Sem LLM real integrado E2E | **Crítica** |
| **AI AGENTS** | aiknowledge | ⚠️ Parcial | ✅ Real (dados parciais) | ⚠️ AiOrchestrationDbContext | ✅ | — | ⚠️ PARCIAL | IAiOrchestrationModule PLAN | Alta |
| **MODEL REGISTRY** | aiknowledge | ✅ Real | ✅ Real | ✅ AiGovernanceDbContext | ✅ | — | ⚠️ PARCIAL | Campos deferred; routing desconectado do assistant | Média |
| **AI ACCESS POLICIES** | aiknowledge | ✅ Real | ✅ Real | ✅ AiGovernanceDbContext | ✅ | — | ✅ PRONTO | Não aplicadas ao assistant (que é mock) | Média |
| **AI TOKEN & BUDGET GOVERNANCE** | aiknowledge | ✅ Real (AiTokenUsageLedger) | ✅ Real | ✅ AiGovernanceDbContext | ✅ | — | ⚠️ PARCIAL | Contabilizado apenas se assistant enviar tokens reais | Média |
| **AI KNOWLEDGE SOURCES** | aiknowledge | ✅ Context builders reais | ⚠️ Parcial | ✅ AiGovernanceDbContext | ✅ | — | ⚠️ PARCIAL | Enriquecimento de contexto sem retrieval real/RAG | Alta |
| **AI AUDIT & USAGE** | aiknowledge | ✅ Real | ✅ Real | ✅ AiGovernanceDbContext | ✅ | — | ⚠️ PARCIAL | Audita o quê? Assistant retorna dados hardcoded | Alta |
| **EXTERNAL AI INTEGRATIONS** | aiknowledge | ❌ 8 handlers TODO stub | ⚠️ Parcial | ❌ ExternalAiDbContext sem migrações | ✅ | OpenAI disabled | ❌ PLAN | IExternalAiModule.ExternalAiModule stub | Alta |
| **IDE EXTENSIONS MANAGEMENT** | aiknowledge | ⚠️ AiIdeEndpointModule existe | ⚠️ Parcial | ✅ AiGovernanceDbContext | ✅ | — | ⚠️ PARCIAL | Extensão IDE não avaliada E2E | Baixa |
| **FINOPS CONTEXTUAL** | governance + operationalintelligence | 🟡 Governance handlers mock; CostIntelligenceDbContext real mas não consumido | ✅ Real (dados mock) | ✅ CostIntelligenceDbContext | ✅ | ❌ ICostIntelligenceModule PLAN | 🟡 MOCK | ICostIntelligenceModule PLAN; dados fabricados com IsSimulated | Alta |
| **REPORTS / RISK / COMPLIANCE** | governance | 🟡 ~74 handlers mock | ✅ Real (dados mock) | ✅ GovernanceDbContext | ✅ | — | 🟡 MOCK | Governance sem persistência de dados reais | Alta |
| **RISK CENTER** | governance | 🟡 Mock | ✅ RiskCenterPage | ✅ GovernanceDbContext | ✅ | — | 🟡 MOCK | Dados simulados | Alta |
| **COMPLIANCE** | governance | 🟡 Mock | ✅ CompliancePage | ✅ GovernanceDbContext | ✅ | — | 🟡 MOCK | Dados simulados | Alta |
| **POLICY MANAGEMENT** | governance | 🟡 Mock | ✅ PolicyCatalogPage | ✅ GovernanceDbContext | ✅ | — | 🟡 MOCK | Dados simulados | Alta |
| **EXECUTIVE VIEWS** | governance | 🟡 Mock | ✅ ExecutiveOverviewPage | ✅ GovernanceDbContext | ✅ | — | 🟡 MOCK | Dados simulados | Média |
| **AUDIT & TRACEABILITY** | auditcompliance | ✅ Real | ✅ Real | ✅ AuditDbContext (hash chain SHA-256) | ✅ | — | ✅ PRONTO | — | — |
| **LICENSING & ENTITLEMENTS** | (removido PR-17) | 🚫 Ausente | 🚫 Ausente | 🚫 | ⚠️ Parcial | — | 🚫 AUSENTE | Módulo removido sem substituto ativo | **Estratégica** |
| **NOTIFICATIONS** | notifications | ⚠️ Parcial | ⚠️ Parcial | ✅ NotificationsDbContext (9 migrações) | ✅ | — | ⚠️ PARCIAL | Cobertura funcional E2E não validada | Média |
| **INTEGRATIONS** | integrations | ⚠️ DbContext existe; conectores stub | ⚠️ Parcial | ✅ IntegrationsDbContext (3 migrações) | ✅ | — | ⚠️ STUB | Conectores sem lógica real | Média |
| **PRODUCT ANALYTICS** | productanalytics | 🟡 Mock completo | ✅ Real (backend mock) | ❌ Sem migrações | ⚠️ | — | 🟡 MOCK | Event tracking real ausente | Baixa |

---

## Sumário por Estado

| Estado | Capacidades | % |
|---|---|---|
| ✅ PRONTO | 14 | 28% |
| ⚠️ PARCIAL | 17 | 34% |
| 🟡 MOCK | 9 | 18% |
| ❌ BROKEN / PLAN | 6 | 12% |
| 🚫 AUSENTE | 1 | 2% |
| **TOTAL** | **50** | **100%** |

---

## Capacidades Críticas Inaceitáveis para a Visão Oficial

As seguintes capacidades são pilares da visão e estão em estado inaceitável para entrega enterprise:

| Capacidade | Estado | Evidência | Prioridade |
|---|---|---|---|
| Incident Correlation | ❌ BROKEN | `IncidentsPage.tsx` `mockIncidents`; engine ausente | **Crítica** |
| AI Assistant | ❌ BROKEN | `AssistantPanel.tsx` `mockConversations`; SendAssistantMessage hardcoded | **Crítica** |
| External AI Integration | ❌ PLAN | 8 stubs TODO em ExternalAI | Alta |
| FinOps Contextual | 🟡 MOCK | `GetDomainFinOps`, `GetServiceFinOps`, `GetFinOpsTrends` — `IsSimulated: true` | Alta |
| Governance real (Reports/Risk/Compliance) | 🟡 MOCK | ~74 handlers `IsSimulated: true` | Alta |
| Runbooks | 🟡 MOCK | 3 hardcoded; RunbookRecord não utilizado | Alta |
| Licensing | 🚫 AUSENTE | Módulo removido (PR-17) sem substituto | **Estratégica** |

---

## Ficheiros-Chave de Evidência por Capacidade

| Capacidade | Ficheiro Principal |
|---|---|
| Service Catalog | `src/modules/catalog/NexTraceOne.Catalog.Application/Features/` |
| Contract Governance | `src/modules/catalog/NexTraceOne.Catalog.Application/Features/Contracts/` |
| Change Governance | `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Application/` |
| Identity & Auth | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/` |
| Audit Trail | `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Application/` |
| IncidentsPage mock | `src/frontend/src/features/operations/pages/` |
| AI Assistant mock | `src/frontend/src/features/ai-hub/components/AssistantPanel.tsx` |
| Governance IsSimulated | `src/modules/governance/NexTraceOne.Governance.Application/Features/` |
| FinOps IsSimulated | `src/modules/governance/NexTraceOne.Governance.Application/Features/GetDomainFinOps/` |
| Cross-module IContractsModule | `src/modules/catalog/NexTraceOne.Catalog.Contracts/Contracts/ServiceInterfaces/IContractsModule.cs` |
| ExternalAI stubs | `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/ExternalAI/Services/ExternalAiModule.cs` |

---

*Data: 28 de Março de 2026 | Ver `final-project-state-assessment.md` para veredito global*
