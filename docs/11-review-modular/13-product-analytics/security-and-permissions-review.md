# PARTE 10 — Segurança e Permissões do Módulo Product Analytics

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: REVISÃO COMPLETA

---

## 1. Permissões por página

| Página | Rota | Permissão Frontend | Status |
|--------|------|-------------------|--------|
| Overview Dashboard | `/analytics` | `analytics:read` | ✅ Definida em App.tsx |
| Module Adoption | `/analytics/adoption` | `analytics:read` | ✅ Definida |
| Persona Usage | `/analytics/personas` | `analytics:read` | ✅ Definida |
| Journey Funnel | `/analytics/journeys` | `analytics:read` | ✅ Definida |
| Value Tracking | `/analytics/value` | `analytics:read` | ✅ Definida |

---

## 2. Permissões por ação

| Ação | Permissão Backend | Permissão Frontend | Status |
|------|-------------------|-------------------|--------|
| Ler analytics summary | `governance:analytics:read` | `analytics:read` | ⚠️ Prefixo errado no backend |
| Ler module adoption | `governance:analytics:read` | `analytics:read` | ⚠️ Prefixo errado no backend |
| Ler persona usage | `governance:analytics:read` | `analytics:read` | ⚠️ Prefixo errado no backend |
| Ler journeys | `governance:analytics:read` | `analytics:read` | ⚠️ Prefixo errado no backend |
| Ler milestones | `governance:analytics:read` | `analytics:read` | ⚠️ Prefixo errado no backend |
| Ler friction | `governance:analytics:read` | `analytics:read` | ⚠️ Prefixo errado no backend |
| Gravar evento | `governance:analytics:write` | N/A (auto) | ⚠️ Prefixo errado |
| Exportar dados | N/A (não existe) | N/A | 🔴 Criar `analytics:export` |
| Gerir definições | N/A (não existe) | N/A | 🔴 Criar `analytics:manage` |

### Problema de naming

As permissões backend usam `governance:analytics:*` porque o módulo está dentro de Governance. Após extração, devem ser:

| Atual | Alvo |
|-------|------|
| `governance:analytics:read` | `analytics:read` |
| `governance:analytics:write` | `analytics:write` |
| N/A | `analytics:export` (NOVO) |
| N/A | `analytics:manage` (NOVO) |

---

## 3. Revisão de guards do frontend

| Guard | Ficheiro | Status |
|-------|---------|--------|
| Route guard para `/analytics/*` | `App.tsx` (RequirePermission) | ✅ Protegido com `analytics:read` |
| Sidebar visibility | `AppSidebar.tsx` | ✅ Protegido com `analytics:read` |
| Command Palette visibility | `CommandPalette.tsx` | ✅ Protegido com `analytics:read` |
| AnalyticsEventTracker | `AnalyticsEventTracker.tsx` | ⚠️ Sem guard — grava eventos sem verificar permissão explícita |

---

## 4. Revisão de enforcement no backend

| Endpoint | Authorization | Ficheiro | Status |
|----------|-------------|---------|--------|
| POST /events | `RequireAuthorization("governance:analytics:write")` | ProductAnalyticsEndpointModule.cs | ✅ Protegido |
| GET /summary | `RequireAuthorization("governance:analytics:read")` | ProductAnalyticsEndpointModule.cs | ✅ Protegido |
| GET /adoption/modules | `RequireAuthorization("governance:analytics:read")` | ProductAnalyticsEndpointModule.cs | ✅ Protegido |
| GET /adoption/personas | `RequireAuthorization("governance:analytics:read")` | ProductAnalyticsEndpointModule.cs | ✅ Protegido |
| GET /journeys | `RequireAuthorization("governance:analytics:read")` | ProductAnalyticsEndpointModule.cs | ✅ Protegido |
| GET /value-milestones | `RequireAuthorization("governance:analytics:read")` | ProductAnalyticsEndpointModule.cs | ✅ Protegido |
| GET /friction | `RequireAuthorization("governance:analytics:read")` | ProductAnalyticsEndpointModule.cs | ✅ Protegido |

---

## 5. Revisão de ações sensíveis

### Consulta de métricas por tenant

| Aspecto | Status | Detalhe |
|---------|--------|---------|
| TenantId filtering | ✅ | Via TenantRlsInterceptor (RLS no PostgreSQL) |
| Cross-tenant access | ✅ Bloqueado | RLS impede acesso cross-tenant |
| Admin override | ⚠️ | Não há mecanismo de super-admin analytics |

### Consulta de métricas sensíveis

| Dado | Sensibilidade | Protecção | Status |
|------|--------------|-----------|--------|
| Quem usa o produto | MÉDIA | Apenas agregados expostos | ✅ OK |
| UserId em eventos raw | ALTA | Não exposto via API (apenas interno) | ✅ OK |
| SessionId | MÉDIA | Não exposto diretamente | ✅ OK |
| Persona usage breakdown | MÉDIA | Protegido por `analytics:read` | ✅ OK |
| MetadataJson | VARIÁVEL | Não exposto diretamente | ✅ OK |

### Exportação

| Aspecto | Status |
|---------|--------|
| Endpoint de exportação | ❌ Não existe |
| Permissão dedicada | ❌ Não existe |
| Rate limiting em export | N/A |

### Reprocessamento

Não aplicável — Product Analytics é read-only + event ingestion.

---

## 6. Revisão de escopo por tenant

| Aspecto | Status | Detalhe |
|---------|--------|---------|
| AnalyticsEvent.TenantId | ✅ | Obrigatório, preenchido automaticamente |
| RLS enforcement | ✅ | Via TenantRlsInterceptor |
| Queries filtradas por tenant | ✅ | Repository queries incluem tenant filter |
| Cross-tenant analytics | ❌ Não existe | Correto — cada tenant vê apenas os seus dados |

---

## 7. Revisão de escopo por environment

| Aspecto | Status | Detalhe |
|---------|--------|---------|
| EnvironmentId no evento | ❌ Não existe (campo ausente) | Deve ser adicionado como nullable |
| Filtro por environment nos dashboards | ❌ Não existe | Deve ser adicionado |
| Necessidade | BAIXA | Analytics de produto é cross-environment na maioria dos casos |

---

## 8. Revisão de auditoria das ações críticas

| Ação | Auditada | Status |
|------|----------|--------|
| Gravar evento de uso | ❌ | Não é necessário auditar cada evento de telemetria |
| Consultar dashboards | ❌ | ⚠️ Pode ser desejável para data governance |
| Exportar dados | N/A | 🔴 Quando implementado, DEVE ser auditado |
| Gerir definições | N/A | 🔴 Quando implementado, DEVE ser auditado |

---

## 9. Backlog de correções de segurança

| # | ID | Correção | Prioridade | Esforço | Ficheiro(s) |
|---|-----|---------|-----------|---------|-------------|
| 1 | S-01 | Renomear permissões de `governance:analytics:*` para `analytics:*` | P1_CRITICAL | 2h | `ProductAnalyticsEndpointModule.cs`, `RolePermissionCatalog.cs` |
| 2 | S-02 | Adicionar `analytics:read` e `analytics:write` ao `RolePermissionCatalog.cs` como permissões próprias | P1_CRITICAL | 1h | `RolePermissionCatalog.cs` |
| 3 | S-03 | Criar permissão `analytics:export` para futura exportação | P2_HIGH | 0.5h | `RolePermissionCatalog.cs` |
| 4 | S-04 | Criar permissão `analytics:manage` para gestão de definições | P2_HIGH | 0.5h | `RolePermissionCatalog.cs` |
| 5 | S-05 | Adicionar rate limiting no POST /events (prevenir flood de eventos) | P2_HIGH | 3h | Endpoint + middleware |
| 6 | S-06 | Validar que AnalyticsEventTracker não envia dados sensíveis no MetadataJson | P2_HIGH | 1h | `AnalyticsEventTracker.tsx` |
| 7 | S-07 | Adicionar auditoria na futura exportação de dados | P3_MEDIUM | 1h | Quando endpoint criado |
| 8 | S-08 | Adicionar auditoria na futura gestão de definições | P3_MEDIUM | 1h | Quando CRUD criado |
| 9 | S-09 | Revisar role assignments para novas permissões analytics:* | P1_CRITICAL | 1h | `RolePermissionCatalog.cs` |

**Roles alvo para permissões analytics:*:**

| Permissão | PlatformAdmin | TechLead | Developer | Product | Executive | Auditor |
|-----------|:---:|:---:|:---:|:---:|:---:|:---:|
| analytics:read | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| analytics:write | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| analytics:export | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ |
| analytics:manage | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |

**Total segurança**: 9 itens, ~11h estimadas
