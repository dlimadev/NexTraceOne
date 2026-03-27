# Matriz de Capacidades vs. Implementação Real — NexTraceOne
**Auditoria Forense | Março 2026**

---

## Legenda de Status
- ✅ **PRONTO** — implementação real, funcional, persistida
- ⚠️ **PARCIAL** — valor real mas partes críticas ausentes
- 🟡 **MOCK** — dados simulados/hardcoded; `IsSimulated: true`
- ❌ **PLAN** — interface/contrato definido, sem implementação
- 🚫 **AUSENTE** — não existe

---

## Matriz Completa

| Capacidade | Módulo Esperado | Backend? | Frontend? | Persistência? | Docs? | Integração? | Estado | Principais Gaps | Prioridade |
|---|---|---|---|---|---|---|---|---|---|
| **SERVICE CATALOG** | catalog | ✅ Real | ✅ Real | ✅ CatalogGraphDbContext | ✅ | Parcial | ✅ PRONTO | Developer Portal 7 stubs | Baixa |
| **OWNERSHIP** | catalog | ✅ Real | ✅ Real | ✅ CatalogGraphDbContext | ✅ | Parcial | ✅ PRONTO | Cross-module IContractsModule PLAN | Média |
| **TEAMS** | identityaccess + governance | ✅ Identity real; Governance mock | ✅ Real (backend mock) | ✅ IdentityDbContext | ✅ | ❌ | 🟡 MOCK | Governance sem persistência própria | Alta |
| **ENVIRONMENTS** | identityaccess | ✅ Real | ✅ Real | ✅ IdentityDbContext | ✅ | — | ✅ PRONTO | IsProductionLike logic parcial | Baixa |
| **CONTRACT GOVERNANCE (REST)** | catalog | ✅ Real | ✅ Real | ✅ ContractsDbContext | ✅ | Parcial | ✅ PRONTO | SearchCatalog stub | Baixa |
| **CONTRACT GOVERNANCE (SOAP)** | catalog | ✅ Real | ✅ Real | ✅ ContractsDbContext | ✅ | — | ✅ PRONTO | — | — |
| **EVENT CONTRACTS (AsyncAPI/Kafka)** | catalog | ✅ Real | ✅ Real | ✅ ContractsDbContext | ✅ | — | ✅ PRONTO | — | — |
| **BACKGROUND SERVICE CONTRACTS** | catalog | ✅ Real | ✅ Real | ✅ ContractsDbContext | ✅ | — | ✅ PRONTO | — | — |
| **VERSIONING & COMPATIBILITY** | catalog | ✅ Real | ✅ Real | ✅ ContractsDbContext | ✅ | — | ✅ PRONTO | ComputeSemanticDiff, EvaluateCompatibility | — |
| **CONTRACT STUDIO** | catalog | ✅ Real | ⚠️ Parcial | ✅ ContractsDbContext | ✅ | — | ⚠️ PARCIAL | UX precisa polish | Média |
| **PUBLICATION CENTER** | catalog | ✅ Real (PublishDraft) | ✅ Real | ✅ ContractsDbContext | ✅ | — | ✅ PRONTO | — | — |
| **CONTRACT POLICIES (Spectral)** | changegovernance | ✅ Real | ✅ Real | ✅ RulesetGovernanceDbContext | ✅ | — | ✅ PRONTO | — | — |
| **DEVELOPER PORTAL** | catalog | ⚠️ 7 stubs | ⚠️ Parcial | ✅ DeveloperPortalDbContext | ✅ | — | ⚠️ PARCIAL | 7 endpoints aguardam IContractsModule | Alta |
| **CHANGE INTELLIGENCE** | changegovernance | ✅ Real | ✅ Real | ✅ ChangeIntelligenceDbContext | ✅ | Stub CI/CD | ✅ PRONTO | Integração CI/CD stub | Média |
| **BLAST RADIUS** | changegovernance | ✅ Real | ✅ Real | ✅ ChangeIntelligenceDbContext | ✅ | — | ✅ PRONTO | — | — |
| **PROMOTION GOVERNANCE** | changegovernance | ✅ Real | ✅ Real | ✅ PromotionDbContext | ✅ | — | ✅ PRONTO | IPromotionModule cross-module PLAN | Média |
| **EVIDENCE PACK** | changegovernance | ✅ Real | ✅ Real | ✅ WorkflowDbContext | ✅ | — | ✅ PRONTO | — | — |
| **ROLLBACK INTELLIGENCE** | changegovernance | ✅ Real | ✅ Real | ✅ ChangeIntelligenceDbContext | ✅ | — | ✅ PRONTO | — | — |
| **RELEASE CALENDAR** | changegovernance | ✅ Real (FreezeWindow) | ⚠️ Parcial | ✅ ChangeIntelligenceDbContext | ✅ | — | ⚠️ PARCIAL | Calendar UI não auditada | Média |
| **INCIDENT CORRELATION** | operationalintelligence | ⚠️ EfIncidentStore real; correlação mock | ❌ mockIncidents inline | ✅ IncidentDbContext | ✅ | — | ❌ BROKEN | Engine de correlação dinâmica ausente | Crítica |
| **RUNBOOKS** | operationalintelligence | 🟡 3 hardcoded | ❌ Mock UI | ✅ IncidentDbContext (RunbookRecord) | ✅ | — | 🟡 MOCK | RunbookRecord existe; handlers não usam | Alta |
| **MITIGATION WORKFLOWS** | operationalintelligence | ⚠️ Existe mas não persiste | ❌ Mock UI | ✅ IncidentDbContext (MitigationRecord) | ✅ | — | ❌ BROKEN | CreateMitigationWorkflow descarta dados | Alta |
| **POST-CHANGE VERIFICATION** | changegovernance + ops | ⚠️ Gates reais; mitigation validation mock | ⚠️ Parcial | ✅ PromotionDbContext | ✅ | — | ⚠️ PARCIAL | RecordMitigationValidation mock | Alta |
| **AUTOMATION WORKFLOWS** | operationalintelligence | 🟡 Mock completo | 🟡 PREV (PreviewOnly error) | ✅ AutomationDbContext | ✅ | — | 🟡 MOCK | Handlers retornam PreviewOnly | Média |
| **SERVICE RELIABILITY** | operationalintelligence | 🟡 8 serviços hardcoded | ✅ Real (backend mock) | ✅ ReliabilityDbContext | ✅ | — | 🟡 MOCK | ReliabilityDbContext existe; handlers não usam | Média |
| **KNOWLEDGE HUB** | knowledge | ⚠️ Notes e changelog via events | ⚠️ Parcial | ❌ Sem migrações | ⚠️ | — | ⚠️ PARCIAL | KnowledgeDbContext sem migrações | Alta |
| **SOURCE OF TRUTH VIEWS** | catalog | ✅ Real | ✅ Real | ✅ Múltiplos DbContexts | ✅ | — | ✅ PRONTO | — | — |
| **GLOBAL SEARCH** | catalog | ⚠️ GlobalSearch real; SearchCatalog stub | ✅ Real | ✅ Catalog | ✅ | — | ⚠️ PARCIAL | SearchCatalog cross-module stub | Média |
| **AI ASSISTANT** | aiknowledge | ❌ SendAssistantMessage hardcoded | ❌ mockConversations | ⚠️ AiOrchestrationDbContext | ✅ | Ollama config | ❌ BROKEN | Sem LLM real integrado | Crítica |
| **AI AGENTS** | aiknowledge | ⚠️ Parcial | ✅ Real | ⚠️ AiOrchestrationDbContext | ✅ | — | ⚠️ PARCIAL | IAiOrchestrationModule PLAN | Alta |
| **MODEL REGISTRY** | aiknowledge | ✅ Real | ✅ Real (backend mock em campos) | ✅ AiGovernanceDbContext | ✅ | — | ⚠️ PARCIAL | Campos deferred; sem conexão a routing | Média |
| **AI ACCESS POLICIES** | aiknowledge | ✅ Real | ✅ Real | ✅ AiGovernanceDbContext | ✅ | — | ✅ PRONTO | Não conectadas ao assistant mock | Média |
| **AI AUDIT** | aiknowledge | ✅ Real | ✅ Real | ✅ AiGovernanceDbContext | ✅ | — | ⚠️ PARCIAL | Audita o quê? Assistant é mock | Alta |
| **AI KNOWLEDGE SOURCES** | aiknowledge | ✅ Context builders | ⚠️ Parcial | ✅ AiGovernanceDbContext | ✅ | — | ⚠️ PARCIAL | Enriquecimento de contexto sem retrieval real | Alta |
| **EXTERNAL AI INTEGRATIONS** | aiknowledge | ❌ 8 TODO stubs | ⚠️ Parcial | ❌ ExternalAiDbContext sem migração | ✅ | OpenAI disabled | ❌ PLAN | IExternalAiModule empty | Alta |
| **FINOPS CONTEXTUAL** | governance + operationalintelligence | 🟡 Governance mock; CostIntelligence real | ✅ Real (backend mock) | ✅ CostIntelligenceDbContext | ✅ | — | 🟡 MOCK | ICostIntelligenceModule PLAN; Governance não consome | Alta |
| **REPORTS / RISK / COMPLIANCE** | governance | 🟡 74 handlers mock | ✅ Real (backend mock) | ✅ GovernanceDbContext | ✅ | — | 🟡 MOCK | Governance sem persistência própria | Alta |
| **LICENSING & ENTITLEMENTS** | (removido PR-17) | 🚫 Ausente | 🚫 Ausente | 🚫 | Parcial | — | 🚫 AUSENTE | Módulo removido sem substituto | Definir estratégia |
| **AUDIT & TRACEABILITY** | auditcompliance | ✅ Real | ✅ Real | ✅ AuditDbContext | ✅ | — | ✅ PRONTO | Hash chain SHA-256 | — |
| **NOTIFICATIONS** | notifications | ⚠️ Parcial | ⚠️ Parcial | ✅ NotificationsDbContext | ✅ | — | ⚠️ PARCIAL | Cobertura E2E não verificada | Média |
| **PRODUCT ANALYTICS** | productanalytics | 🟡 Mock | ✅ Real (backend mock) | ❌ Sem migrações | ⚠️ | — | 🟡 MOCK | Sem event tracking real | Baixa |

---

## Sumário por Estado

| Estado | Capacidades | % |
|---|---|---|
| ✅ PRONTO | 13 | 30% |
| ⚠️ PARCIAL | 12 | 28% |
| 🟡 MOCK | 8 | 19% |
| ❌ BROKEN / PLAN | 7 | 16% |
| 🚫 AUSENTE | 1 | 2% |
| **TOTAL** | **41** | **100%** |

---

## Capacidades Críticas Ausentes para a Visão Oficial

As seguintes capacidades são pilares da visão e estão em estado inaceitável:

1. **Incident Correlation**: BROKEN — correlação dinâmica ausente
2. **AI Assistant**: BROKEN — sem LLM real
3. **External AI Integration**: PLAN — 8 stubs
4. **FinOps contextual**: MOCK — dados fabricados
5. **Governance real**: MOCK — 74 handlers mock
6. **Licensing**: AUSENTE — módulo removido sem substituto
