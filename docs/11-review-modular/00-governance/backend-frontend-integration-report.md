# Relatório de Integração Backend-Frontend — NexTraceOne

> **Data:** 2025-01  
> **Versão:** 1.0  
> **Tipo:** Auditoria de integração entre backend e frontend  
> **Escopo:** Todos os endpoints backend vs páginas frontend

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| API projects backend | 9 |
| Endpoint modules backend | 44+ |
| Rotas quebradas no frontend | 3 |
| Endpoints sem página frontend confirmada | ~8 |
| Páginas frontend sem endpoint backend | A verificar |

---

## 2. Rotas Quebradas no Frontend

### 2.1 Rotas Identificadas

| Rota Frontend | Módulo Backend | Endpoint Backend | Problema |
|---------------|---------------|-----------------|----------|
| /contracts/governance | catalog | ContractsEndpointModule | Página existe no frontend mas **não está registada no App.tsx** |
| /spectral | catalog | ContractStudioEndpointModule | Página existe no frontend mas **não está registada no App.tsx** |
| /canonical | catalog | ContractsEndpointModule | Página existe no frontend mas **não está registada no App.tsx** |

### 2.2 Impacto

- As 3 rotas correspondem a funcionalidades do pilar **Contract Governance** — pilar central do produto
- O backend expõe os endpoints correspondentes
- As páginas React existem nos ficheiros fonte
- O problema é exclusivamente de **routing no App.tsx** — não é falta de implementação

### 2.3 Resolução Recomendada

Adicionar as 3 rotas ao `App.tsx` do frontend, garantindo:
- Rota correcta
- Componente de página correcto
- Guard de permissão adequado
- Posição no menu de navegação

---

## 3. Mapa Backend → Frontend por Módulo

### 3.1 AIKnowledge

| Endpoint Module | Endpoint Backend | Página Frontend (est.) | Estado |
|----------------|-----------------|----------------------|--------|
| ExternalAiEndpointModule | /api/ai/external/* | AI Assistant, AI Providers | ✅ INTEGRADO |
| AiGovernanceEndpointModule | /api/ai/governance/* | AI Governance, Model Registry | ✅ INTEGRADO |
| AiIdeEndpointModule | /api/ai/ide/* | IDE Extensions | ✅ INTEGRADO |
| AiOrchestrationEndpointModule | /api/ai/orchestration/* | AI Sessions, Knowledge Sources | ✅ INTEGRADO |
| AiRuntimeEndpointModule | /api/ai/runtime/* | AI Runtime Status | ⚠️ PARCIAL — pode não ter página dedicada |

### 3.2 AuditCompliance

| Endpoint Module | Endpoint Backend | Página Frontend (est.) | Estado |
|----------------|-----------------|----------------------|--------|
| AuditEndpointModule | /api/audit/* | Audit Trail, Compliance Reports | ✅ INTEGRADO |

### 3.3 Catalog

| Endpoint Module | Endpoint Backend | Página Frontend (est.) | Estado |
|----------------|-----------------|----------------------|--------|
| ContractsEndpointModule | /api/contracts/* | Contracts List, Contract Detail | ✅ INTEGRADO |
| ContractStudioEndpointModule | /api/contracts/studio/* | Contract Studio | ⚠️ PARCIAL — spectral route quebrada |
| ServiceCatalogEndpointModule | /api/catalog/services/* | Service Catalog, Service Detail | ✅ INTEGRADO |
| DeveloperPortalEndpointModule | /api/portal/* | Developer Portal | ✅ INTEGRADO |
| SourceOfTruthEndpointModule | /api/source-of-truth/* | Source of Truth Overview | ⚠️ PARCIAL — pode não ter rota dedicada |

### 3.4 ChangeGovernance

| Endpoint Module | Endpoint Backend | Página Frontend (est.) | Estado |
|----------------|-----------------|----------------------|--------|
| ChangeIntelligenceEndpointModule | /api/changes/* | Change Intelligence Dashboard | ✅ INTEGRADO |
| PromotionEndpointModule | /api/promotion/* | Promotion Requests | ✅ INTEGRADO |
| RulesetGovernanceEndpointModule | /api/rulesets/* | Rulesets | ✅ INTEGRADO |
| WorkflowEndpointModule | /api/workflows/* | Workflows | ✅ INTEGRADO |

### 3.5 Configuration

| Endpoint Module | Endpoint Backend | Página Frontend (est.) | Estado |
|----------------|-----------------|----------------------|--------|
| ConfigurationEndpointModule | /api/configuration/* | Platform Configuration | ✅ INTEGRADO |

### 3.6 Governance

| Endpoint Module | Endpoint Backend | Página Frontend (est.) | Estado |
|----------------|-----------------|----------------------|--------|
| TeamsEndpointModule | /api/governance/teams/* | Teams | ✅ INTEGRADO |
| DomainsEndpointModule | /api/governance/domains/* | Domains | ✅ INTEGRADO |
| PoliciesEndpointModule | /api/governance/policies | Policies | ✅ INTEGRADO |
| PacksEndpointModule | /api/governance/packs/* | Governance Packs | ✅ INTEGRADO |
| WaiversEndpointModule | /api/governance/waivers | Waivers | ✅ INTEGRADO |
| FinOpsEndpointModule | /api/governance/finops | FinOps Dashboard | ✅ INTEGRADO |
| RiskEndpointModule | /api/governance/risk | Risk Center | ✅ INTEGRADO |
| ComplianceEndpointModule | /api/governance/compliance | Compliance | ✅ INTEGRADO |
| ReportsEndpointModule | /api/governance/reports | Reports | ✅ INTEGRADO |
| ControlsEndpointModule | /api/governance/controls | Controls | ✅ INTEGRADO |
| EvidenceEndpointModule | /api/governance/evidence | Evidence | ✅ INTEGRADO |
| ExecutiveEndpointModule | /api/governance/executive | Executive View | ⚠️ PARCIAL — pode não ter página dedicada |
| IntegrationsEndpointModule | /api/governance/integrations | Integrations | ✅ INTEGRADO |
| OnboardingEndpointModule | /api/governance/onboarding | Onboarding | ✅ INTEGRADO |
| PlatformStatusEndpointModule | /api/governance/platform-status | Platform Status | ✅ INTEGRADO |
| ProductAnalyticsEndpointModule | /api/governance/analytics | Product Analytics | ⚠️ PARCIAL — pode não ter página dedicada |
| ScopedContextEndpointModule | /api/governance/scoped-context | (interno) | ✅ API interna — não precisa de página |
| DelegatedAdminEndpointModule | /api/governance/delegated-admin | Delegated Admin | ✅ INTEGRADO |

### 3.7 IdentityAccess

| Sub-Módulo | Endpoint Backend | Página Frontend (est.) | Estado |
|------------|-----------------|----------------------|--------|
| Auth | /api/identity/auth/* | Login, Register | ✅ INTEGRADO |
| User | /api/identity/users/* | Users Management | ✅ INTEGRADO |
| RolePermission | /api/identity/roles/* | Roles & Permissions | ✅ INTEGRADO |
| BreakGlass | /api/identity/breakglass | BreakGlass Access | ✅ INTEGRADO |
| JitAccess | /api/identity/jit-access | JIT Access | ✅ INTEGRADO |
| Delegation | /api/identity/delegation | Delegation | ✅ INTEGRADO |
| Tenant | /api/identity/tenants | Tenant Management | ✅ INTEGRADO |
| AccessReview | /api/identity/access-review | Access Review | ✅ INTEGRADO |
| Environment | /api/identity/environments | Environments | ✅ INTEGRADO |
| RuntimeContext | /api/identity/runtime-context | (interno) | ✅ API interna |
| CookieSession | /api/identity/session | (interno) | ✅ API interna |

### 3.8 Notifications

| Endpoint Module | Endpoint Backend | Página Frontend (est.) | Estado |
|----------------|-----------------|----------------------|--------|
| NotificationCenterEndpointModule | /api/notifications/* | Notification Center | ✅ INTEGRADO |

### 3.9 OperationalIntelligence

| Endpoint Module | Endpoint Backend | Página Frontend (est.) | Estado |
|----------------|-----------------|----------------------|--------|
| AutomationEndpointModule | /api/operations/automation | Automation | ✅ INTEGRADO |
| CostIntelligenceEndpointModule | /api/operations/costs | Cost Intelligence | ✅ INTEGRADO |
| IncidentEndpointModule | /api/operations/incidents/* | Incidents | ✅ INTEGRADO |
| MitigationEndpointModule | /api/operations/mitigation | Mitigation | ✅ INTEGRADO |
| RunbookEndpointModule | /api/operations/runbooks | Runbooks | ✅ INTEGRADO |
| ReliabilityEndpointModule | /api/operations/reliability | Reliability | ✅ INTEGRADO |
| RuntimeIntelligenceEndpointModule | /api/operations/runtime | Runtime Intelligence | ✅ INTEGRADO |

---

## 4. Análise de Gaps

### 4.1 Endpoints Backend sem Página Frontend Dedicada

| Endpoint Module | Razão |
|----------------|-------|
| AiRuntimeEndpointModule | Pode estar integrado na página AI Assistant |
| SourceOfTruthEndpointModule | Funcionalidade core mas sem rota dedicada confirmada |
| ExecutiveEndpointModule | Pode estar integrado na página Reports |
| ProductAnalyticsEndpointModule | Pode estar integrado na página Reports ou Dashboard |

### 4.2 Consistência de Contratos API

| Aspecto | Estado |
|---------|--------|
| DTOs backend vs tipos frontend | ⚠️ Verificação manual necessária |
| Tipagem TypeScript do frontend | ⚠️ Pode não estar sincronizada |
| Geração automática de tipos | ❌ Não identificada |

### 4.3 Recomendações de Geração de Tipos

| Abordagem | Benefício |
|-----------|----------|
| OpenAPI → TypeScript | Garante consistência automática |
| NSwag / Kiota | Geração de clientes tipados |
| Contract testing | Detecta breaking changes |

---

## 5. Análise por Pilar do Produto

### 5.1 Service Governance

| Backend | Frontend | Integração |
|---------|----------|-----------|
| ServiceCatalogEndpointModule | Service Catalog page | ✅ |
| TeamsEndpointModule | Teams page | ✅ |
| DomainsEndpointModule | Domains page | ✅ |
| OnboardingEndpointModule | Onboarding flow | ✅ |

**Classificação:** ✅ INTEGRADO

### 5.2 Contract Governance

| Backend | Frontend | Integração |
|---------|----------|-----------|
| ContractsEndpointModule | Contracts page | ✅ |
| ContractStudioEndpointModule | Contract Studio | ⚠️ spectral route quebrada |
| — | /contracts/governance | ❌ Rota quebrada |
| — | /canonical | ❌ Rota quebrada |

**Classificação:** ⚠️ PARCIAL — 3 rotas quebradas no pilar central

### 5.3 Change Confidence

| Backend | Frontend | Integração |
|---------|----------|-----------|
| ChangeIntelligenceEndpointModule | Change Intelligence | ✅ |
| PromotionEndpointModule | Promotions | ✅ |
| WorkflowEndpointModule | Workflows | ✅ |
| RulesetGovernanceEndpointModule | Rulesets | ✅ |

**Classificação:** ✅ INTEGRADO

### 5.4 Operational Reliability

| Backend | Frontend | Integração |
|---------|----------|-----------|
| IncidentEndpointModule | Incidents | ✅ |
| MitigationEndpointModule | Mitigation | ✅ |
| AutomationEndpointModule | Automation | ✅ |
| RunbookEndpointModule | Runbooks | ✅ |
| ReliabilityEndpointModule | Reliability | ✅ |

**Classificação:** ✅ INTEGRADO

### 5.5 AI-assisted Operations

| Backend | Frontend | Integração |
|---------|----------|-----------|
| ExternalAiEndpointModule | AI Assistant | ✅ |
| AiGovernanceEndpointModule | AI Governance | ✅ |
| AiOrchestrationEndpointModule | AI Sessions | ✅ |
| AiIdeEndpointModule | IDE Extensions | ✅ |

**Classificação:** ✅ INTEGRADO

---

## 6. Recomendações

### Prioridade ALTA

| # | Recomendação | Justificação |
|---|-------------|-------------|
| 1 | Corrigir 3 rotas quebradas no App.tsx | Afectam pilar central (Contract Governance) |
| 2 | Verificar consistência de tipos TS com DTOs backend | Prevenir runtime errors |

### Prioridade MÉDIA

| # | Recomendação | Justificação |
|---|-------------|-------------|
| 3 | Implementar geração automática de tipos TypeScript | Garante sincronização backend-frontend |
| 4 | Criar página dedicada para Source of Truth | Funcionalidade core sem rota dedicada |
| 5 | Verificar se Executive e ProductAnalytics precisam de página | Endpoints sem frontend confirmado |

### Prioridade BAIXA

| # | Recomendação | Justificação |
|---|-------------|-------------|
| 6 | Implementar contract testing entre backend e frontend | Detecta breaking changes automaticamente |
| 7 | Documentar mapa completo de rotas frontend ↔ endpoints backend | Facilita manutenção |
