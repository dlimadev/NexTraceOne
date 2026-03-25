# Matriz de Capacidades vs Implementação — NexTraceOne

**Data:** 25 de março de 2026

---

## Legenda

- ✅ Existe e funcional
- ⚠️ Existe parcialmente
- ❌ Ausente
- 🔶 Stub/Mock

---

## Matriz Principal

| Capacidade Oficial | Módulo Esperado | Backend | Frontend | Persistência | Documentação | Integração | Estado Geral | Gaps Principais | Ficheiros Chave | Prioridade |
|-------------------|-----------------|---------|----------|-------------|-------------|-----------|-------------|-----------------|----------------|-----------|
| **Service Catalog** | Catalog | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | PARTIAL | Source of Truth sem multi-module view | `Catalog.Infrastructure/Graph/`, `features/catalog/` | P1 |
| **Ownership** | Governance/Catalog | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | PARTIAL | Ownership workflow UI incompleto | `GovernanceDbContext`, `features/governance/` | P2 |
| **Teams** | Governance | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | READY | — | `GovernanceDbContext.Teams`, `features/governance/teams/` | P1 |
| **Environments** | IdentityAccess | ✅ | ✅ | ✅ | ✅ | ✅ | READY | — | `IdentityDbContext.Environments`, `features/identity-access/` | P1 |
| **Contract Governance REST** | Catalog | ✅ | ✅ | ✅ | ✅ | ⚠️ | READY | — | `ContractsDbContext`, `features/contracts/` | P1 |
| **Contract Governance SOAP** | Catalog | ⚠️ | ⚠️ | ⚠️ | ❌ | ❌ | INCOMPLETE | Sem workflow específico WSDL; apenas constraint de protocolo | `ContractsDbContext HasCheckConstraint Wsdl` | P2 |
| **Event Contracts / AsyncAPI** | Catalog | ⚠️ | ⚠️ | ⚠️ | ❌ | ❌ | INCOMPLETE | Sem entidades específicas para topics/bindings/schemas | `ContractsDbContext HasCheckConstraint AsyncApi` | P2 |
| **Background Service Contracts** | Catalog | ❌ | ❌ | ❌ | ❌ | ❌ | MISSING | Não encontrado | — | P3 |
| **Versioning & Compatibility** | Catalog | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | READY | — | `ContractDiff`, `ContractVersion`, `computeDiff()` | P1 |
| **Publication Center** | Catalog | ⚠️ | ⚠️ | ⚠️ | ❌ | ❌ | INCOMPLETE | Developer Portal existe; fluxo de publicação não verificado | `DeveloperPortalDbContext`, `features/contracts/portal/` | P2 |
| **Contract Policies (Spectral)** | Catalog | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | READY | — | `SpectralRuleset`, `features/contracts/spectral/` | P1 |
| **Change Intelligence** | ChangeGovernance | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | PARTIAL | Correlação automática telemetria→release não verificada | `ChangeIntelligenceDbContext`, `features/change-governance/` | P1 |
| **Blast Radius** | ChangeGovernance | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | PARTIAL | Manual via UI; sem automação no deploy | `BlastRadiusReport`, `serviceCatalogApi.getImpactPropagation()` | P1 |
| **Promotion Governance** | ChangeGovernance | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | PARTIAL | Gates existem; fluxo multi-gate não verificado end-to-end | `PromotionDbContext`, `features/change-governance/promotion/` | P1 |
| **Evidence Pack** | ChangeGovernance | ✅ | ⚠️ | ✅ | ⚠️ | ❌ | PARTIAL | Entidade existe; fluxo end-to-end não verificado | `WorkflowDbContext.EvidencePack`, `features/change-governance/workflow/` | P1 |
| **Rollback Intelligence** | ChangeGovernance | ✅ | ⚠️ | ✅ | ⚠️ | ❌ | PARTIAL | Entidade existe; fluxo guiado não verificado | `RollbackAssessment`, `features/change-governance/` | P2 |
| **Release Calendar** | ChangeGovernance | ✅ | ⚠️ | ✅ | ⚠️ | ❌ | PARTIAL | FreezeWindow existe; UI calendário não verificada | `FreezeWindow`, `features/change-governance/` | P2 |
| **Incident Correlation** | OperationalIntelligence | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | PARTIAL | correlatedChanges existe; correlação automática não verificada | `IncidentDbContext`, `incidentsApi.getDetail()` | P1 |
| **Runbooks** | OperationalIntelligence | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | PARTIAL | Entidade e UI existem; fluxo de execução não verificado | `RunbookRecord`, `features/operations/runbooks/` | P2 |
| **Service Reliability (SLO)** | OperationalIntelligence | ⚠️ | ⚠️ | ⚠️ | ❌ | ❌ | INCOMPLETE | Apenas 1 entidade ReliabilitySnapshot; sem SLO real | `ReliabilityDbContext` | P2 |
| **Knowledge Hub** | Knowledge (ausente) | ❌ | ❌ | ❌ | ⚠️ | ❌ | MISSING | Módulo Knowledge Hub não existe no backend | — | P2 |
| **Operational Notes** | Knowledge (ausente) | ❌ | ❌ | ❌ | ❌ | ❌ | MISSING | Sem entidade de Operational Notes | — | P3 |
| **Search / Command Palette** | Shared | ❌ | ⚠️ | ❌ | ❌ | ❌ | INCOMPLETE | CommandPalette.tsx existe no frontend; sem backend search cross-module | `components/CommandPalette.tsx` | P2 |
| **AI Assistant** | AIKnowledge | ✅ | ✅ | ✅ | ⚠️ | ✅ | PARTIAL | AssistantPanel usa mock; streaming ausente | `AiAssistantConversation`, `features/ai-hub/ai-assistant/` | P0 |
| **AI Agents** | AIKnowledge | ✅ | ✅ | ✅ | ⚠️ | ✅ | PARTIAL | Tools não executam; streaming ausente | `AiAgent`, `AiAgentRuntimeService`, `features/ai-hub/ai-agents/` | P1 |
| **Model Registry** | AIKnowledge | ✅ | ✅ | ✅ | ⚠️ | ✅ | READY | — | `AIModel (40+ props)`, `features/ai-hub/model-registry/` | P1 |
| **AI Access Policies** | AIKnowledge | ✅ | ✅ | ✅ | ⚠️ | ✅ | READY | — | `AIAccessPolicy`, `features/ai-hub/ai-policies/` | P1 |
| **AI Token & Budget Governance** | AIKnowledge | ✅ | ✅ | ✅ | ⚠️ | ✅ | READY | — | `AiTokenQuotaPolicy`, `AIBudget`, `features/ai-hub/token-budget/` | P1 |
| **AI Audit** | AIKnowledge | ✅ | ✅ | ✅ | ⚠️ | ✅ | READY | — | `AIUsageEntry`, `features/ai-hub/ai-audit/` | P1 |
| **External AI Governance** | AIKnowledge | 🔶 | ⚠️ | 🔶 | ⚠️ | ❌ | STUB | ExternalAiDbContext 0 DbSets; 7/8 features TODO | `AIKnowledge.Application/ExternalAI/` | P1 |
| **AI Orchestration** | AIKnowledge | 🔶 | ❌ | 🔶 | ⚠️ | ❌ | STUB | 0/8 features implementadas | `AIKnowledge.Application/Orchestration/` | P2 |
| **IDE Extensions** | AIKnowledge | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ❌ | INCOMPLETE | DB e UI existem; sem extensões reais | `AIIDEClientRegistration`, `features/ai-hub/ide-integrations/` | P2 |
| **FinOps Contextual** | OperationalIntelligence | ⚠️ | ✅ | ✅ | ⚠️ | ❌ | PARTIAL | Entidades existem; pipeline analítico não verificado | `CostIntelligenceDbContext`, `features/governance/finops/` | P2 |
| **Reports / Risk / Compliance** | Governance | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | PARTIAL | — | `GovernanceDbContext`, `features/governance/` | P2 |
| **Licensing & Entitlements** | Licensing (ausente) | ❌ | ❌ | ❌ | ❌ | ❌ | MISSING | Módulo não existe | — | P1 |
| **Audit & Traceability** | AuditCompliance | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | PARTIAL | Hash chain real; fluxos limitados | `AuditDbContext`, `features/audit-compliance/` | P2 |
| **Identity & SSO** | IdentityAccess | ✅ | ✅ | ✅ | ✅ | ⚠️ | PARTIAL | SSO schema existe; provider OIDC não pré-configurado | `IdentityDbContext.ExternalIdentity`, `features/identity-access/` | P1 |
| **Break Glass / JIT Access** | IdentityAccess | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | READY | — | `BreakGlassRequest`, `JitAccessRequest` | P1 |
| **Access Reviews** | IdentityAccess | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | READY | — | `AccessReviewCampaign`, `features/identity-access/access-reviews/` | P1 |
| **Notifications** | Notifications | ⚠️ | ✅ | ⚠️ | ⚠️ | ❌ | INCOMPLETE | 3 entidades mínimas; sem templates e canais ricos | `NotificationsDbContext`, `features/notifications/` | P2 |
| **Integrations Hub** | Integrations | ⚠️ | ✅ | ⚠️ | ⚠️ | ⚠️ | PARTIAL | Entidades em GovernanceDbContext; módulo não dedicado | `GovernanceDbContext.IntegrationConnector`, `features/integrations/` | P2 |
| **System Configuration** | Configuration | ⚠️ | ✅ | ⚠️ | ⚠️ | ⚠️ | INCOMPLETE | 3 entidades; sem hierarquia e feature flags | `ConfigurationDbContext`, `features/configuration/` | P2 |
| **Product Analytics** | ProductAnalytics | ⚠️ | ✅ | ⚠️ | ❌ | ❌ | PARTIAL | Entidades em GovernanceDbContext; pipeline incompleto | `GovernanceDbContext.AnalyticsEvent`, `features/product-analytics/` | P3 |

---

## Resumo por Estado

| Estado | Capacidades | % |
|--------|------------|---|
| READY | 11 | 25% |
| PARTIAL | 22 | 50% |
| INCOMPLETE | 6 | 14% |
| MISSING | 4 | 9% |
| STUB | 2 | 4% |
| **TOTAL** | **45** | **100%** |

---

## Top 10 Capacidades Mais Críticas para Fechar

1. **AI Assistant (AssistantPanel mock)** — P0 — remover mock, usar chat real
2. **Licensing & Entitlements** — P1 — criar módulo do zero
3. **External AI Governance** — P1 — completar ExternalAI domain
4. **Change Intelligence correlação** — P1 — pipeline telemetria→release
5. **SOAP/WSDL Contract workflow** — P2 — handlers específicos
6. **Event/AsyncAPI Contract workflow** — P2 — handlers específicos
7. **Knowledge Hub** — P2 — criar módulo dedicado
8. **Service Reliability (SLO)** — P2 — expandir ReliabilityDbContext
9. **AI Orchestration** — P2 — iniciar 8 features TODO
10. **FinOps pipeline analítico** — P2 — ligar entidades ao ClickHouse
