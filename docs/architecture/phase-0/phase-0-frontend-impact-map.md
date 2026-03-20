# Phase 0 — Frontend Impact Map

**Data:** 2026-03-20  
**Scope:** Inventário de impacto da refatoração TenantId + EnvironmentId no frontend React/TypeScript

Legenda:  
- ✅ Correto / já implementado  
- ⚠️ Parcial ou frágil  
- ❌ Ausente / incorreto  
- 🔴 Impacto crítico  
- 🟠 Impacto alto  
- 🟡 Impacto médio  
- 🟢 Impacto baixo

---

## 1. App Shell e Roteamento

| Componente | Arquivo | TenantId? | EnvironmentId? | Problema | Fase |
|------------|---------|-----------|----------------|----------|------|
| `App.tsx` (routing) | `src/App.tsx` | ✅ via AuthContext | ❌ | Rotas não consideram ambiente ativo | 6 |
| `AppShell` | `components/shell/AppShell.tsx` | ✅ | ❌ | Shell não propaga EnvironmentContext | 6 |
| `AppSidebar` | `components/shell/AppSidebar.tsx` | ✅ | ❌ | Menu não filtra por ambiente | 6 |
| `WorkspaceSwitcher` | `components/shell/WorkspaceSwitcher.tsx` | ✅ (nome do tenant) | 🔴 Hardcoded `['Production', 'Staging', 'Development']` | `AVAILABLE_ENVIRONMENTS` é constante, não vem do backend | 6 |
| `ContextStrip` | `components/shell/ContextStrip.tsx` | ❌ | ❌ | Apenas container visual, sem contexto funcional | 6 |
| `ProtectedRoute` | `components/ProtectedRoute.tsx` | ✅ | ❌ | Guards não verificam ambiente ativo | 6 |
| `releaseScope.ts` | `src/releaseScope.ts` | — | — | Lista de rotas de produção — sem impacto direto | — |

---

## 2. Contexts e Providers

| Context/Provider | Arquivo | TenantId? | EnvironmentId? | Problema | Fase |
|-----------------|---------|-----------|----------------|----------|------|
| `AuthContext` / `AuthProvider` | `contexts/AuthContext.tsx` | ✅ `tenantId: string | null` | ❌ | AuthState não inclui `environmentId` nem `availableEnvironments` | 6 |
| `PersonaContext` / `PersonaProvider` | `contexts/PersonaContext.tsx` | ✅ via AuthContext | ❌ | Persona não considera ambiente ativo | 6 |
| `EnvironmentContext` | — | — | ❌ **Não existe** | Nenhum provider de ambiente ativo existe no app | 6 |

---

## 3. Token e Session Storage

| Item | Arquivo | TenantId? | EnvironmentId? | Problema | Fase |
|------|---------|-----------|----------------|----------|------|
| `sessionStorage['nxt_tid']` | `utils/tokenStorage.ts` | ✅ persistido | ❌ | `nxt_eid` (environment id) não existe no session storage | 6 |
| `SESSION_KEYS` | `utils/tokenStorage.ts` | ✅ `TENANT_ID` | ❌ `ENVIRONMENT_ID` ausente | Após refresh da página, ambiente ativo não é recuperado | 6 |
| `getTenantId()` | `utils/tokenStorage.ts` | ✅ | ❌ | `getEnvironmentId()` não existe | 6 |

---

## 4. API Client

| Item | Arquivo | TenantId? | EnvironmentId? | Problema | Fase |
|------|---------|-----------|----------------|----------|------|
| Request interceptor | `api/client.ts` | ✅ `X-Tenant-Id` header | ❌ | `X-Environment-Id` header não é injetado | 6 |
| Refresh token flow | `api/client.ts` | ✅ | ❌ | Sem impacto para ambientes | — |

---

## 5. Feature: Identity / Tenant

| Página/Componente | Arquivo | TenantId? | EnvironmentId? | Problema | Fase |
|------------------|---------|-----------|----------------|----------|------|
| `TenantSelectionPage` | `features/identity-access/pages/TenantSelectionPage.tsx` | ✅ | ❌ | Após selecionar tenant, não carrega ambientes disponíveis | 6 |
| `LoginPage` | `features/identity-access/pages/LoginPage.tsx` | ✅ | ❌ | Sem impacto direto | — |
| `identity.ts` (API) | `features/identity-access/api/identity.ts` | ✅ | ❌ | Endpoints de identidade não retornam ambientes disponíveis | 4 |

---

## 6. Feature: Change Governance

| Página/Componente | Arquivo | TenantId? | EnvironmentId? | Problema | Fase |
|------------------|---------|-----------|----------------|----------|------|
| `ChangeCatalogPage` | `features/change-governance/pages/ChangeCatalogPage.tsx` | ✅ | ⚠️ | Filtros de ambiente usam string livre | 6 |
| `ChangeDetailPage` | `features/change-governance/pages/ChangeDetailPage.tsx` | ✅ | ⚠️ | Exibe ambiente como string | 6 |
| `ReleasesPage` | `features/change-governance/pages/ReleasesPage.tsx` | ✅ | 🔴 `environment: 'production'` default hardcoded | Formulário de notificação de release usa string `'production'` default | 6 |
| `PromotionPage` | `features/change-governance/pages/PromotionPage.tsx` | ✅ | ✅ IDs | Usa IDs de DeploymentEnvironment — relativamente seguro | — |
| `ReleasesIntelligenceTab` | `features/change-governance/components/ReleasesIntelligenceTab.tsx` | ✅ | ⚠️ `intel.release.environment` como string | Exibe string de ambiente sem tipagem | 6 |
| `changeIntelligence.ts` API | `features/change-governance/api/changeIntelligence.ts` | ✅ | ⚠️ `environment?: string` | Parâmetros de ambiente como string livre | 6 |
| `changeConfidence.ts` API | `features/change-governance/api/changeConfidence.ts` | ✅ | ⚠️ `environment?: string` | Parâmetros de ambiente como string livre | 6 |
| `promotion.ts` API | `features/change-governance/api/promotion.ts` | ✅ | ✅ `sourceEnvironment: string`, `targetEnvironment: string` | Usa nome de ambiente, não ID — inconsistente | 6 |

---

## 7. Feature: Operations / Incidents

| Página/Componente | Arquivo | TenantId? | EnvironmentId? | Problema | Fase |
|------------------|---------|-----------|----------------|----------|------|
| `IncidentsPage` | `features/operations/pages/IncidentsPage.tsx` | ✅ | 🔴 Default `'Production'` hardcoded | Formulário de criação tem `environment: 'Production'` como default | 6 |
| `IncidentDetailPage` | `features/operations/pages/IncidentDetailPage.tsx` | ✅ | ⚠️ `impactedEnvironment: string` | Ambiente exibido como string, não como entidade | 6 |
| `AutomationAdminPage` | `features/operations/pages/AutomationAdminPage.tsx` | ✅ | 🔴 `'Production'`, `'Staging'`, `'Dev'` hardcoded | Dados mock com ambientes fixos — não substituíveis sem EnvironmentContext | 6 |
| `incidents.ts` API | `features/operations/api/incidents.ts` | ✅ | ⚠️ `environment: string` | Tipo de ambiente como string nos DTOs | 6 |
| `reliability.ts` API | `features/operations/api/reliability.ts` | ✅ | ⚠️ `environment?: string` | Filtro de ambiente como string | 6 |

---

## 8. Feature: Catalog / Services

| Página/Componente | Arquivo | TenantId? | EnvironmentId? | Problema | Fase |
|------------------|---------|-----------|----------------|----------|------|
| `DeveloperPortalPage` | `features/catalog/pages/DeveloperPortalPage.tsx` | ✅ | ⚠️ `environment: string` opcional | Playground aceita ambiente como string livre | 6 |
| `serviceCatalog.ts` API | `features/catalog/api/serviceCatalog.ts` | ✅ | ⚠️ `consumerEnvironment: string` | Ambiente do consumidor como string | 6 |
| `developerPortal.ts` API | `features/catalog/api/developerPortal.ts` | ✅ | ⚠️ `environment?: string` | Ambiente como string opcional | 6 |

---

## 9. Feature: Integrations

| Página/Componente | Arquivo | TenantId? | EnvironmentId? | Problema | Fase |
|------------------|---------|-----------|----------------|----------|------|
| `ConnectorDetailPage` | `features/integrations/pages/ConnectorDetailPage.tsx` | ✅ | 🔴 `environment: 'Production'` hardcoded em mock data | Mock com ambientes fixos — duas instâncias de `environment: 'Production'` | 6 |
| `integrations.ts` API | `features/integrations/api/integrations.ts` | ✅ | ⚠️ `environment?: string` | Ambiente como string opcional | 6 |

---

## 10. Feature: Governance

| Página/Componente | Arquivo | TenantId? | EnvironmentId? | Problema | Fase |
|------------------|---------|-----------|----------------|----------|------|
| `PolicyCatalogPage` | `features/governance/pages/PolicyCatalogPage.tsx` | ✅ | ⚠️ `pol.effectiveEnvironments.join(', ')` | Ambientes de políticas como array de strings | 6 |
| `FinOpsPage` | `features/governance/pages/FinOpsPage.tsx` | ✅ | 🔴 `'idle-staging'` hardcoded em mock | Mock com padrão de ambiente no nome | 6 |

---

## 11. Feature: AI Hub

| Página/Componente | Arquivo | TenantId? | EnvironmentId? | Problema | Fase |
|------------------|---------|-----------|----------------|----------|------|
| `AiAssistantPage` | `features/ai-hub/pages/AiAssistantPage.tsx` | ✅ | ❌ | Chat não inclui ambiente no contexto | 7 |
| `AssistantPanel` | `features/ai-hub/components/AssistantPanel.tsx` | ✅ | ❌ | Painel integrado em detail pages não passa EnvironmentId | 7 |
| `aiGovernance.ts` API | `features/ai-hub/api/aiGovernance.ts` | ✅ | ❌ | Endpoints de IA sem parâmetro de ambiente | 7 |

---

## 12. Problemas Transversais no Frontend

### 12.1 Ausência de EnvironmentContext

Nenhum `EnvironmentContext` existe no React app. O ambiente ativo é:
- Um valor hardcoded `'Production'` em `WorkspaceSwitcher`
- Uma constante `AVAILABLE_ENVIRONMENTS = ['Production', 'Staging', 'Development']`
- Nunca carregado do backend
- Nunca propagado para queries de API
- Nunca injetado no header `X-Environment-Id`

### 12.2 Formulários com defaults hardcoded

- `IncidentsPage`: `environment: 'Production'` no estado inicial do formulário
- `ReleasesPage`: `environment: 'production'` no estado inicial do formulário de notificação de release

### 12.3 Mock data com ambientes fixos

- `AutomationAdminPage`: arrays de `environments: ['Production', 'Staging', 'Dev']` em dados mockados
- `ConnectorDetailPage`: objetos com `environment: 'Production'` hardcoded
- `FinOpsPage`: `pattern: 'idle-staging'` em mock data

### 12.4 API calls sem EnvironmentId

Nenhuma chamada de API injeta `EnvironmentId` como header ou query parameter. Todas as chamadas que precisam de contexto de ambiente usam `string?` opcional no corpo da query.

---

## 13. Refatoração Frontend Recomendada (Fase 6)

### Prioridade 1 — `EnvironmentContext` provider
Criar `contexts/EnvironmentContext.tsx` com:
- `availableEnvironments: Environment[]` carregados de `GET /api/v1/environments`
- `activeEnvironment: Environment | null`
- `setActiveEnvironment(env: Environment): void`
- Persistência em `sessionStorage['nxt_eid']`

### Prioridade 2 — Injetar `X-Environment-Id` no API client
Atualizar `api/client.ts` para injetar `X-Environment-Id` nos request headers, análogo ao `X-Tenant-Id`.

### Prioridade 3 — `WorkspaceSwitcher` dinâmico
Remover `AVAILABLE_ENVIRONMENTS` hardcoded. Usar `useEnvironment()` para listar ambientes do tenant autenticado.

### Prioridade 4 — Atualizar formulários
Remover defaults hardcoded `'Production'` dos formulários. Usar `activeEnvironment.id` como default.

### Prioridade 5 — Atualizar API types
Substituir `environment?: string` por `environmentId?: string` nos DTOs TypeScript, alinhando com o backend refatorado.

### Prioridade 6 — Guards de rota
Considerar adicionar `requiresEnvironment?: boolean` às rotas protegidas para módulos operacionais.
