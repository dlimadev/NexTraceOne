# Environment Management — Frontend Functional Corrections

> **Módulo:** 02 — Environment Management  
> **Data:** 2026-03-25  
> **Fase:** N4-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Inventário de páginas

### 1.1 Páginas do módulo

| Página | Ficheiro actual | Feature actual | Feature alvo | Linhas |
|--------|----------------|---------------|-------------|--------|
| `EnvironmentsPage` | `src/frontend/src/features/identity-access/pages/EnvironmentsPage.tsx` | `identity-access` | `environment-management` | ~434 |
| `EnvironmentComparisonPage` | `src/frontend/src/features/operations/pages/EnvironmentComparisonPage.tsx` | `operations` | `environment-management` (ou manter em operations) | ~623 |

### 1.2 Componentes de shell (permanecem)

| Componente | Ficheiro | Feature | Linhas | Migra? |
|-----------|---------|---------|--------|--------|
| `EnvironmentContext` | `src/frontend/src/contexts/EnvironmentContext.tsx` | shell (contexts) | ~261 | ❌ — shell concern |
| `EnvironmentBanner` | `src/frontend/src/components/shell/EnvironmentBanner.tsx` | shell (components) | ~46 | ❌ — shell concern |

### 1.3 Páginas ausentes

| Página | Propósito | Prioridade |
|--------|-----------|-----------|
| `EnvironmentDetailPage` | Detalhe completo de um ambiente (overview, acessos, políticas) | ALTA |
| `EnvironmentAccessManagementPage` | Gestão de acessos por ambiente (grant, revoke, list) | MÉDIA |
| `EnvironmentPoliciesPage` | Gestão de políticas por ambiente (Phase 2) | BAIXA |

---

## 2. Revisão de rotas

| Rota | Componente | Registada em `App.tsx` | Sidebar | Permissão | Estado |
|------|-----------|----------------------|---------|-----------|--------|
| `/environments` | `EnvironmentsPage` | ✅ | ❌ **SEM ENTRADA** | `identity:users:read` | ⚠️ Bug de descobribilidade |
| `/operations/runtime-comparison` | `EnvironmentComparisonPage` | ✅ | ✅ (em Operations) | `operations:runtime:read` | ✅ |
| `/environments/{id}` | — | ❌ | — | — | ❌ Rota inexistente |
| `/environments/{id}/accesses` | — | ❌ | — | — | ❌ Rota inexistente |

### 2.1 Problema crítico: Sidebar entry ausente

A `EnvironmentsPage` na rota `/environments` **não tem entrada no sidebar**. Isto significa que:

- A página é **inacessível via navegação normal** — só acessível por URL directo
- Utilizadores não sabem que a funcionalidade existe
- Violação do princípio de descobribilidade

**Correcção necessária:** Adicionar entrada no sidebar, secção "Foundation" ou "Services", com:
- Label i18n: `sidebar.environments` / `sidebar.environmentManagement`
- Icon: appropriate environment/server icon
- Permissão: `env:environments:read` (após migração) ou `identity:users:read` (interim)
- Posição: antes de Services/Catalog

---

## 3. Revisão de formulários

### 3.1 `EnvironmentsPage` — Create Environment Form

| Campo | Tipo | Obrigatório | Validação frontend | i18n label | Estado |
|-------|------|-------------|-------------------|------------|--------|
| Name | text input | ✅ | ⚠️ Verificar max length | ⚠️ Verificar | ⚠️ |
| Slug | text input | ✅ | ⚠️ Verificar format | ⚠️ Verificar | ⚠️ |
| Code | text input | ❌ | ⚠️ Verificar | ⚠️ Verificar | ⚠️ |
| Description | textarea | ❌ | — | ⚠️ Verificar | ⚠️ |
| Region | text input | ❌ | ⚠️ Verificar | ⚠️ Verificar | ⚠️ |
| Profile | select | ✅ | ⚠️ Enum values | ⚠️ Verificar | ⚠️ |
| Criticality | select | ✅ | ⚠️ Enum values | ⚠️ Verificar | ⚠️ |
| SortOrder | number | ✅ | ⚠️ >= 0 | ⚠️ Verificar | ⚠️ |
| IsProductionLike | checkbox | ❌ | — | ⚠️ Verificar | ⚠️ |

### 3.2 `EnvironmentsPage` — Edit Environment Form

| Aspecto | Estado | Notas |
|---------|--------|-------|
| Pre-fill com dados actuais | ⚠️ Verificar | — |
| Slug editável? | ⚠️ | Slug deveria ser imutável após criação |
| Feedback de sucesso/erro | ⚠️ Verificar | — |
| Concurrency conflict handling | ❌ | Sem RowVersion no backend |

### 3.3 Grant Access Form

| Campo | Tipo | Obrigatório | Estado |
|-------|------|-------------|--------|
| UserId | user picker/input | ✅ | ⚠️ Verificar se existe picker |
| AccessLevel | select (read/write/admin) | ✅ | ⚠️ Verificar |
| ExpiresAt | date picker | ❌ | ⚠️ Verificar |

---

## 4. Página de detalhe — Ausente

A `EnvironmentsPage` lista ambientes mas **não existe página de detalhe individual**.

### 4.1 Conteúdo esperado da `EnvironmentDetailPage`

| Secção | Conteúdo | Prioridade |
|--------|----------|-----------|
| Overview | Nome, slug, code, profile, criticality, region, status, production flags | ALTA |
| Access Control | Lista de utilizadores com acesso, grant/revoke | ALTA |
| Activity | Timeline de alterações (via audit trail) | MÉDIA |
| Policies | Políticas associadas (Phase 2) | BAIXA |
| Telemetry Policy | Configuração de retenção/verbosidade (Phase 2) | BAIXA |
| Integration Bindings | Conectores CI/CD, alerting, observability (Phase 2) | BAIXA |
| Related Incidents | Incidentes neste ambiente (cross-module) | BAIXA |

---

## 5. API Integration — Ficheiros actuais

### 5.1 API client

**Ficheiro:** `src/frontend/src/features/identity-access/api/identity.ts`

| Função | Endpoint | Método | Estado |
|--------|---------|--------|--------|
| `listEnvironments()` | `GET /api/v1/environments` | GET | ✅ |
| `createEnvironment(data)` | `POST /api/v1/environments` | POST | ✅ |
| `updateEnvironment(id, data)` | `PUT /api/v1/environments/{id}` | PUT | ✅ |
| `getPrimaryProductionEnvironment()` | `GET /api/v1/environments/primary-production` | GET | ✅ |
| `setPrimaryProductionEnvironment(id)` | `POST /api/v1/environments/{id}/set-primary-production` | POST | ✅ |

### 5.2 Funções API ausentes

| Função | Endpoint necessário | Prioridade |
|--------|-------------------|-----------|
| `getEnvironment(id)` | `GET /api/v1/environments/{id}` | ALTA |
| `deleteEnvironment(id)` | `DELETE /api/v1/environments/{id}` | ALTA |
| `listEnvironmentAccesses(envId)` | `GET /api/v1/environments/{id}/accesses` | ALTA |
| `revokeEnvironmentAccess(envId, accessId)` | `POST /api/v1/environments/{id}/revoke-access` | ALTA |
| `activateEnvironment(id)` | `POST /api/v1/environments/{id}/activate` | MÉDIA |
| `deactivateEnvironment(id)` | `POST /api/v1/environments/{id}/deactivate` | MÉDIA |

### 5.3 Migração do ficheiro API

**Actual:** `src/frontend/src/features/identity-access/api/identity.ts`  
**Alvo:** `src/frontend/src/features/environment-management/api/environments.ts`

---

## 6. i18n — Análise de chaves

### 6.1 Chaves i18n esperadas (mínimo 50+)

**Namespace sugerido:** `environmentManagement` ou `environments`

| Categoria | Chaves estimadas | Exemplos |
|-----------|-----------------|---------|
| Page titles | 5 | `environments.title`, `environments.detail.title`, `environments.accesses.title` |
| Column headers | 12 | `environments.columns.name`, `environments.columns.slug`, `environments.columns.profile`, `environments.columns.criticality`, `environments.columns.region`, `environments.columns.status`, etc. |
| Form labels | 10 | `environments.form.name`, `environments.form.slug`, `environments.form.code`, `environments.form.description`, `environments.form.region`, `environments.form.profile`, etc. |
| Form placeholders | 8 | `environments.form.namePlaceholder`, `environments.form.slugPlaceholder`, etc. |
| Buttons | 8 | `environments.actions.create`, `environments.actions.edit`, `environments.actions.delete`, `environments.actions.activate`, `environments.actions.deactivate`, `environments.actions.grantAccess`, `environments.actions.revokeAccess`, `environments.actions.setPrimary` |
| Status/badges | 6 | `environments.status.active`, `environments.status.inactive`, `environments.profile.development`, `environments.profile.production`, `environments.criticality.low`, `environments.criticality.critical` |
| Empty states | 3 | `environments.empty.noEnvironments`, `environments.empty.noAccesses`, `environments.empty.noPolicies` |
| Loading | 2 | `environments.loading`, `environments.accesses.loading` |
| Errors | 5 | `environments.errors.createFailed`, `environments.errors.updateFailed`, `environments.errors.slugDuplicate`, `environments.errors.cannotDeactivatePrimary`, `environments.errors.notFound` |
| Success | 4 | `environments.success.created`, `environments.success.updated`, `environments.success.deleted`, `environments.success.primarySet` |
| Tooltips | 4 | `environments.tooltips.primaryProduction`, `environments.tooltips.productionLike`, `environments.tooltips.criticality`, `environments.tooltips.profile` |
| Confirmations | 3 | `environments.confirm.delete`, `environments.confirm.deactivate`, `environments.confirm.setPrimary` |
| **TOTAL** | **~70** | — |

### 6.2 Estado actual do i18n

| Aspecto | Estado | Notas |
|---------|--------|-------|
| Chaves existentes | ⚠️ Verificar em locales | Possivelmente sob namespace `identity` |
| Localização pt-BR | ⚠️ Verificar completude | — |
| Localização es | ⚠️ Verificar completude | — |
| Localização en | ⚠️ Verificar completude | — |
| Hardcoded strings na `EnvironmentsPage` | ⚠️ Provável | Página com 434 linhas — audit necessário |
| Hardcoded strings na `EnvironmentComparisonPage` | ⚠️ Provável | Página com 623 linhas — audit necessário |

---

## 7. `EnvironmentContext.tsx` — Análise

**Ficheiro:** `src/frontend/src/contexts/EnvironmentContext.tsx` (~261 linhas)

| Aspecto | Estado | Notas |
|---------|--------|-------|
| Propósito | ✅ | Resolve ambiente activo globalmente |
| Persiste selecção | ⚠️ Verificar | LocalStorage / SessionStorage? |
| Sincroniza com header `X-Environment-Id` | ⚠️ Verificar | Necessário para middleware backend |
| Expõe `isProductionLike` | ⚠️ Verificar | Para warnings de UI |
| Migra para Env Mgmt? | ❌ | Permanece como shell concern |

---

## 8. `EnvironmentBanner.tsx` — Análise

**Ficheiro:** `src/frontend/src/components/shell/EnvironmentBanner.tsx` (~46 linhas)

| Aspecto | Estado | Notas |
|---------|--------|-------|
| Propósito | ✅ | Banner visual indicando ambiente activo |
| Usa `EnvironmentUiProfile`? | ⚠️ Verificar | Badge color, protection warning |
| i18n | ⚠️ Verificar | — |
| Visível em produção? | ⚠️ Verificar | Deveria mostrar warning especial |

---

## 9. Backlog de correcções frontend

### 9.1 Prioridade ALTA

| # | Correcção | Ficheiro(s) | Esforço |
|---|----------|-------------|---------|
| FF-01 | **Adicionar sidebar entry para `/environments`** | `App.tsx` ou sidebar config | 1h |
| FF-02 | **Mover `EnvironmentsPage` de `identity-access` para `environment-management`** | Move + actualizar imports | 2h |
| FF-03 | **Criar `EnvironmentDetailPage`** com overview e access control | Nova página + rota `/environments/{id}` | 8h |
| FF-04 | **Criar ficheiro API dedicado** `features/environment-management/api/environments.ts` | Move + novas funções | 2h |
| FF-05 | **Audit i18n** — identificar e extrair todas as strings hardcoded | `EnvironmentsPage.tsx` | 3h |

### 9.2 Prioridade MÉDIA

| # | Correcção | Ficheiro(s) | Esforço |
|---|----------|-------------|---------|
| FF-06 | Adicionar ~70 chaves i18n em `en.json`, `pt-BR.json`, `es.json` | Locales | 3h |
| FF-07 | Adicionar loading/error/empty states consistentes | `EnvironmentsPage.tsx` | 2h |
| FF-08 | Adicionar formulário de revoke access | `EnvironmentDetailPage` (nova) | 2h |
| FF-09 | Adicionar confirmação modal para "Set Primary Production" | `EnvironmentsPage.tsx` | 1h |
| FF-10 | Adicionar confirmação modal para delete | `EnvironmentsPage.tsx` | 1h |
| FF-11 | Validação frontend de slug format (alphanumeric + hyphens) | Create form | 1h |
| FF-12 | Tornar slug readonly no edit form | Edit form | 30min |
| FF-13 | Mover `EnvironmentComparisonPage` ou manter em operations com referência cruzada | Avaliar | 1h |

### 9.3 Prioridade BAIXA

| # | Correcção | Ficheiro(s) | Esforço |
|---|----------|-------------|---------|
| FF-14 | Adicionar filtros à lista (profile, criticality, active/inactive) | `EnvironmentsPage.tsx` | 2h |
| FF-15 | Adicionar paginação à lista | `EnvironmentsPage.tsx` | 1h |
| FF-16 | Ambiente detail — secção de policies (Phase 2) | Nova secção | 4h |
| FF-17 | Ambiente detail — timeline de actividade | Nova secção | 4h |

**Total estimado: ~38.5 horas**
