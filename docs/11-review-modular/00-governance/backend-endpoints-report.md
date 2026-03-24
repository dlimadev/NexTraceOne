# Relatório de Endpoints do Backend — NexTraceOne

> **Data:** 2025-01  
> **Versão:** 1.0  
> **Tipo:** Inventário completo de endpoints  
> **Escopo:** Todos os endpoint modules por projecto API

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| API projects | 9 |
| Endpoint modules | 44+ |
| Endpoint files | 65+ |
| Permissões únicas | 73 |
| Rate limiting policies | 6 |
| Padrão de auto-discovery | Reflexão no ApiHost |

---

## 2. Políticas de Rate Limiting

| Política | Limite | Janela | Aplicação |
|----------|--------|--------|-----------|
| Global | 100 req/IP | 60s | Todos os endpoints |
| auth | 20 req/IP | 60s | Endpoints de autenticação |
| auth-sensitive | 10 req/IP | 60s | Operações sensíveis (password reset, MFA) |
| ai | 30 req/IP | 60s | Endpoints de IA |
| data-intensive | 50 req/IP | 60s | Consultas pesadas (relatórios, exports) |
| operations | 40 req/IP | 60s | Operações (incidents, automation) |

---

## 3. AIKnowledge.API — Endpoint Modules

### 3.1 ExternalAiEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/ai/external/providers | GET | ai:assistant:read | ai | JWT | ExternalAiDb |
| /api/ai/external/providers | POST | ai:assistant:write | ai | JWT | ExternalAiDb |
| /api/ai/external/providers/{id} | GET | ai:assistant:read | ai | JWT | ExternalAiDb |
| /api/ai/external/providers/{id} | PUT | ai:assistant:write | ai | JWT | ExternalAiDb |
| /api/ai/external/providers/{id} | DELETE | ai:assistant:write | ai | JWT | ExternalAiDb |
| /api/ai/external/chat | POST | ai:assistant:write | ai | JWT | ExternalAiDb |
| /api/ai/external/completions | POST | ai:assistant:write | ai | JWT | ExternalAiDb |

### 3.2 AiGovernanceEndpointModule

> ⚠️ **665 linhas** — necessita decomposição

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/ai/governance/models | GET | ai:governance:read | ai | JWT | AiGovernanceDb |
| /api/ai/governance/models | POST | ai:governance:write | ai | JWT | AiGovernanceDb |
| /api/ai/governance/models/{id} | PUT | ai:governance:write | ai | JWT | AiGovernanceDb |
| /api/ai/governance/policies | GET | ai:governance:read | ai | JWT | AiGovernanceDb |
| /api/ai/governance/policies | POST | ai:governance:write | ai | JWT | AiGovernanceDb |
| /api/ai/governance/budgets | GET | ai:governance:read | ai | JWT | AiGovernanceDb |
| /api/ai/governance/budgets | POST | ai:governance:write | ai | JWT | AiGovernanceDb |
| /api/ai/governance/audit | GET | ai:governance:read | data-intensive | JWT | AiGovernanceDb |
| /api/ai/governance/usage | GET | ai:governance:read | data-intensive | JWT | AiGovernanceDb |

### 3.3 AiIdeEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/ai/ide/extensions | GET | ai:ide:read | ai | JWT/API Key | AiGovernanceDb |
| /api/ai/ide/extensions | POST | ai:ide:write | ai | JWT | AiGovernanceDb |
| /api/ai/ide/context | POST | ai:ide:write | ai | JWT/API Key | AiOrchestrationDb |

### 3.4 AiOrchestrationEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/ai/orchestration/sessions | GET | ai:assistant:read | ai | JWT | AiOrchestrationDb |
| /api/ai/orchestration/sessions | POST | ai:assistant:write | ai | JWT | AiOrchestrationDb |
| /api/ai/orchestration/sessions/{id} | GET | ai:assistant:read | ai | JWT | AiOrchestrationDb |
| /api/ai/orchestration/knowledge | GET | ai:assistant:read | ai | JWT | AiOrchestrationDb |
| /api/ai/orchestration/knowledge | POST | ai:assistant:write | ai | JWT | AiOrchestrationDb |

### 3.5 AiRuntimeEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/ai/runtime/status | GET | ai:runtime:read | ai | JWT | AiOrchestrationDb |
| /api/ai/runtime/metrics | GET | ai:runtime:read | ai | JWT | AiOrchestrationDb |
| /api/ai/runtime/config | PUT | ai:runtime:write | ai | JWT | AiOrchestrationDb |

---

## 4. AuditCompliance.API — Endpoint Modules

### 4.1 AuditEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/audit/events | GET | audit:trail:read | data-intensive | JWT | AuditDb |
| /api/audit/events | POST | audit:events:write | Global | JWT | AuditDb |
| /api/audit/reports | GET | audit:reports:read | data-intensive | JWT | AuditDb |
| /api/audit/compliance | GET | audit:compliance:read | Global | JWT | AuditDb |
| /api/audit/compliance | POST | audit:compliance:write | Global | JWT | AuditDb |
| /api/audit/trail/{entityId} | GET | audit:trail:read | Global | JWT | AuditDb |
| /api/audit/export | POST | audit:reports:read | data-intensive | JWT | AuditDb |

---

## 5. Catalog.API — Endpoint Modules

### 5.1 ContractsEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/contracts | GET | contracts:read | Global | JWT | ContractsDb |
| /api/contracts | POST | contracts:write | Global | JWT | ContractsDb |
| /api/contracts/{id} | GET | contracts:read | Global | JWT | ContractsDb |
| /api/contracts/{id} | PUT | contracts:write | Global | JWT | ContractsDb |
| /api/contracts/{id} | DELETE | contracts:write | Global | JWT | ContractsDb |
| /api/contracts/{id}/versions | GET | contracts:read | Global | JWT | ContractsDb |
| /api/contracts/import | POST | contracts:import | Global | JWT | ContractsDb |

### 5.2 ContractStudioEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/contracts/studio/generate | POST | contracts:write | ai | JWT | ContractsDb |
| /api/contracts/studio/validate | POST | contracts:read | Global | JWT | ContractsDb |
| /api/contracts/studio/diff | POST | contracts:read | Global | JWT | ContractsDb |
| /api/contracts/studio/compatibility | POST | contracts:read | Global | JWT | ContractsDb |

### 5.3 ServiceCatalogEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/catalog/services | GET | catalog:assets:read | Global | JWT | CatalogGraphDb |
| /api/catalog/services | POST | catalog:assets:write | Global | JWT | CatalogGraphDb |
| /api/catalog/services/{id} | GET | catalog:assets:read | Global | JWT | CatalogGraphDb |
| /api/catalog/services/{id} | PUT | catalog:assets:write | Global | JWT | CatalogGraphDb |
| /api/catalog/services/{id}/dependencies | GET | catalog:assets:read | Global | JWT | CatalogGraphDb |
| /api/catalog/services/{id}/topology | GET | catalog:assets:read | data-intensive | JWT | CatalogGraphDb |

### 5.4 DeveloperPortalEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/portal/apis | GET | developer-portal:read | Global | JWT | DeveloperPortalDb |
| /api/portal/apis/{id} | GET | developer-portal:read | Global | JWT | DeveloperPortalDb |
| /api/portal/apis/{id}/try | POST | developer-portal:write | Global | JWT | DeveloperPortalDb |
| /api/portal/docs | GET | developer-portal:read | Global | JWT | DeveloperPortalDb |

### 5.5 SourceOfTruthEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/source-of-truth/search | GET | catalog:assets:read | data-intensive | JWT | CatalogGraphDb, ContractsDb |
| /api/source-of-truth/overview | GET | catalog:assets:read | Global | JWT | CatalogGraphDb |

---

## 6. ChangeGovernance.API — Endpoint Modules

### 6.1 ChangeIntelligenceEndpointModule → 6 Sub-endpoints

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/changes/intelligence | GET | change-intelligence:read | Global | JWT | ChangeIntelDb |
| /api/changes/intelligence | POST | change-intelligence:write | Global | JWT | ChangeIntelDb |
| /api/changes/intelligence/{id} | GET | change-intelligence:read | Global | JWT | ChangeIntelDb |
| /api/changes/blast-radius/{id} | GET | change-intelligence:read | data-intensive | JWT | ChangeIntelDb |
| /api/changes/correlation | GET | change-intelligence:read | data-intensive | JWT | ChangeIntelDb |
| /api/changes/confidence | GET | change-intelligence:read | Global | JWT | ChangeIntelDb |

### 6.2 PromotionEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/promotion/requests | GET | promotion:requests:read | Global | JWT | PromotionDb |
| /api/promotion/requests | POST | promotion:requests:write | Global | JWT | PromotionDb |
| /api/promotion/environments | PUT | promotion:environments:write | Global | JWT | PromotionDb |
| /api/promotion/gates/{id}/override | POST | promotion:gates:override | auth-sensitive | JWT | PromotionDb |

### 6.3 RulesetGovernanceEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/rulesets | GET | rulesets:read | Global | JWT | RulesetGovernanceDb |
| /api/rulesets | POST | rulesets:write | Global | JWT | RulesetGovernanceDb |
| /api/rulesets/{id} | PUT | rulesets:write | Global | JWT | RulesetGovernanceDb |
| /api/rulesets/{id}/execute | POST | rulesets:execute | operations | JWT | RulesetGovernanceDb |

### 6.4 WorkflowEndpointModule → 4 Sub-endpoints

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/workflows/templates | GET | workflow:templates:write | Global | JWT | WorkflowDb |
| /api/workflows/templates | POST | workflow:templates:write | Global | JWT | WorkflowDb |
| /api/workflows/instances | GET | workflow:instances:read | Global | JWT | WorkflowDb |
| /api/workflows/instances | POST | workflow:instances:write | Global | JWT | WorkflowDb |
| /api/workflows/instances/{id} | GET | workflow:instances:read | Global | JWT | WorkflowDb |
| /api/workflows/instances/{id}/approve | POST | workflow:instances:write | auth-sensitive | JWT | WorkflowDb |

---

## 7. Configuration.API — Endpoint Modules

### 7.1 ConfigurationEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/configuration | GET | configuration:read | Global | JWT | ConfigurationDb |
| /api/configuration | PUT | configuration:write | Global | JWT | ConfigurationDb |
| /api/configuration/definitions | GET | configuration:read | Global | JWT | ConfigurationDb |
| /api/configuration/phases | GET | configuration:read | Global | JWT | ConfigurationDb |
| /api/configuration/reset | POST | configuration:write | auth-sensitive | JWT | ConfigurationDb |

---

## 8. Governance.API — 18 Endpoint Modules

### 8.1 TeamsEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/teams | GET | governance:teams:read | Global | JWT | GovernanceDb |
| /api/governance/teams | POST | governance:teams:write | Global | JWT | GovernanceDb |
| /api/governance/teams/{id} | GET | governance:teams:read | Global | JWT | GovernanceDb |
| /api/governance/teams/{id} | PUT | governance:teams:write | Global | JWT | GovernanceDb |

### 8.2 DomainsEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/domains | GET | governance:domains:read | Global | JWT | GovernanceDb |
| /api/governance/domains | POST | governance:domains:write | Global | JWT | GovernanceDb |
| /api/governance/domains/{id} | GET | governance:domains:read | Global | JWT | GovernanceDb |

### 8.3 PoliciesEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/policies | GET | governance:policies:read | Global | JWT | GovernanceDb |

### 8.4 PacksEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/packs | GET | governance:packs:read | Global | JWT | GovernanceDb |
| /api/governance/packs | POST | governance:packs:write | Global | JWT | GovernanceDb |
| /api/governance/packs/{id} | PUT | governance:packs:write | Global | JWT | GovernanceDb |

### 8.5 WaiversEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/waivers | GET | governance:waivers:read | Global | JWT | GovernanceDb |
| /api/governance/waivers | POST | governance:waivers:write | Global | JWT | GovernanceDb |

### 8.6 FinOpsEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/finops | GET | governance:finops:read | data-intensive | JWT | GovernanceDb |

### 8.7 RiskEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/risk | GET | governance:risk:read | Global | JWT | GovernanceDb |

### 8.8 ComplianceEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/compliance | GET | governance:compliance:read | Global | JWT | GovernanceDb |

### 8.9 ReportsEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/reports | GET | governance:reports:read | data-intensive | JWT | GovernanceDb |

### 8.10 ControlsEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/controls | GET | governance:controls:read | Global | JWT | GovernanceDb |

### 8.11 EvidenceEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/evidence | GET | governance:evidence:read | Global | JWT | GovernanceDb |

### 8.12 ExecutiveEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/executive | GET | governance:reports:read | data-intensive | JWT | GovernanceDb |

### 8.13 IntegrationsEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/integrations | GET | integrations:read | Global | JWT | GovernanceDb |
| /api/governance/integrations | POST | integrations:write | Global | JWT | GovernanceDb |

### 8.14 OnboardingEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/onboarding | GET | governance:teams:read | Global | JWT | GovernanceDb |
| /api/governance/onboarding | POST | governance:teams:write | Global | JWT | GovernanceDb |

### 8.15 PlatformStatusEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/platform-status | GET | platform:admin:read | Global | JWT | GovernanceDb |

### 8.16 ProductAnalyticsEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/analytics | GET | governance:analytics:read | data-intensive | JWT | GovernanceDb |
| /api/governance/analytics | POST | governance:analytics:write | Global | JWT | GovernanceDb |

### 8.17 ScopedContextEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/scoped-context | GET | governance:teams:read | Global | JWT | GovernanceDb |

### 8.18 DelegatedAdminEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/governance/delegated-admin | GET | platform:admin:read | Global | JWT | GovernanceDb |
| /api/governance/delegated-admin | POST | platform:admin:read | auth-sensitive | JWT | GovernanceDb |

---

## 9. IdentityAccess.API — Endpoint Module (1→10 Sub-Módulos)

### 9.1 IdentityEndpointModule → Sub-Módulos

| Sub-Módulo | Rotas Principais (est.) | Permissões |
|------------|------------------------|------------|
| Auth | /api/identity/auth/login, /register, /refresh, /logout | auth (rate limited) |
| User | /api/identity/users, /users/{id} | identity:users:read/write |
| RolePermission | /api/identity/roles, /permissions | identity:roles:read/assign, identity:permissions:read |
| BreakGlass | /api/identity/breakglass | identity:sessions:read/revoke |
| JitAccess | /api/identity/jit-access | identity:sessions:read/revoke |
| Delegation | /api/identity/delegation | identity:users:write |
| Tenant | /api/identity/tenants | platform:admin:read |
| AccessReview | /api/identity/access-review | identity:sessions:read |
| Environment | /api/identity/environments | identity:users:read |
| RuntimeContext | /api/identity/runtime-context | identity:sessions:read |
| CookieSession | /api/identity/session | identity:sessions:read/revoke |

---

## 10. Notifications.API — Endpoint Module

### 10.1 NotificationCenterEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/notifications/inbox | GET | notifications:inbox:read | Global | JWT | NotificationsDb |
| /api/notifications/inbox/{id}/read | PUT | notifications:inbox:write | Global | JWT | NotificationsDb |
| /api/notifications/preferences | GET | notifications:preferences:read | Global | JWT | NotificationsDb |
| /api/notifications/preferences | PUT | notifications:preferences:write | Global | JWT | NotificationsDb |

---

## 11. OperationalIntelligence.API — Endpoint Modules

### 11.1 AutomationEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/operations/automation | GET | operations:automation:read | operations | JWT | AutomationDb |
| /api/operations/automation | POST | operations:automation:write | operations | JWT | AutomationDb |
| /api/operations/automation/{id}/execute | POST | operations:automation:execute | operations | JWT | AutomationDb |
| /api/operations/automation/{id}/approve | POST | operations:automation:approve | auth-sensitive | JWT | AutomationDb |

### 11.2 CostIntelligenceEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/operations/costs | GET | operations:cost:read | data-intensive | JWT | CostIntelDb |
| /api/operations/costs | POST | operations:cost:write | operations | JWT | CostIntelDb |

### 11.3 IncidentEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/operations/incidents | GET | operations:incidents:read | operations | JWT | IncidentDb |
| /api/operations/incidents | POST | operations:incidents:write | operations | JWT | IncidentDb |
| /api/operations/incidents/{id} | GET | operations:incidents:read | operations | JWT | IncidentDb |
| /api/operations/incidents/{id} | PUT | operations:incidents:write | operations | JWT | IncidentDb |

### 11.4 MitigationEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/operations/mitigation | GET | operations:mitigation:read | operations | JWT | IncidentDb |
| /api/operations/mitigation | POST | operations:mitigation:write | operations | JWT | IncidentDb |

### 11.5 RunbookEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/operations/runbooks | GET | operations:runbooks:read | Global | JWT | AutomationDb |

### 11.6 ReliabilityEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/operations/reliability | GET | operations:reliability:read | data-intensive | JWT | ReliabilityDb |

### 11.7 RuntimeIntelligenceEndpointModule

| Rota (estimada) | Método | Permissão | Rate Limit | Auth | Persistência |
|-----------------|--------|-----------|------------|------|-------------|
| /api/operations/runtime | GET | operations:runtime:read | operations | JWT | RuntimeIntelDb |
| /api/operations/runtime | POST | operations:runtime:write | operations | JWT | RuntimeIntelDb |

---

## 12. Análise de Gaps

### 12.1 Endpoints sem Utilização Frontend Confirmada

| Endpoint Module | Observação |
|----------------|-----------|
| SourceOfTruthEndpointModule | Funcionalidade core mas sem rota dedicada no frontend |
| ContractStudioEndpointModule | Frontend tem páginas mas 3 rotas estão quebradas |
| ExecutiveEndpointModule | Pode não ter página dedicada no frontend |
| ProductAnalyticsEndpointModule | Pode não ter página dedicada no frontend |

### 12.2 Endpoints com Problemas Identificados

| Problema | Módulo | Detalhe |
|----------|--------|---------|
| Tamanho excessivo | AiGovernanceEndpointModule | 665 linhas — decomposição necessária |
| Delegação complexa | IdentityEndpointModule | 10 sub-módulos — documentação insuficiente |
| Delegação complexa | ChangeIntelligenceEndpointModule | 6 sub-endpoints — pode dificultar manutenção |

### 12.3 Padrão de Auto-Discovery

O ApiHost descobre automaticamente todos os endpoint modules via reflexão. Isto elimina registos manuais mas pode ocultar endpoints não intencionais. Recomenda-se auditoria periódica dos modules registados.

---

## 13. Mapa de Permissões por API

| API | Permissões Utilizadas |
|-----|----------------------|
| AIKnowledge | ai:assistant:read/write, ai:governance:read/write, ai:ide:read/write, ai:runtime:read/write |
| AuditCompliance | audit:compliance:read/write, audit:events:write, audit:reports:read, audit:trail:read |
| Catalog | catalog:assets:read/write, contracts:import/read/write, developer-portal:read/write |
| ChangeGovernance | change-intelligence:read/write, promotion:*, rulesets:*, workflow:* |
| Configuration | configuration:read/write |
| Governance | governance:* (teams, domains, policies, packs, waivers, finops, risk, compliance, reports, controls, evidence, analytics), integrations:read/write, platform:admin:read |
| IdentityAccess | identity:* (users, roles, permissions, sessions) |
| Notifications | notifications:inbox:read/write, notifications:preferences:read/write |
| OperationalIntelligence | operations:* (automation, cost, incidents, mitigation, runbooks, reliability, runtime) |
