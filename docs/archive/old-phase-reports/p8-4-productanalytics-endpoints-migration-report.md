# P8.4 — ProductAnalytics Endpoints Migration Report

## Objetivo

Completar a migração da superfície de API de Product Analytics do módulo Governance para o módulo ProductAnalytics independente, alinhando permissões, naming e referências cross-layer.

## Estado anterior (pré-P8.4)

P8.3 já havia completado a maioria do trabalho:
- `ProductAnalyticsEndpointModule` criado em `src/modules/productanalytics/NexTraceOne.ProductAnalytics.API/` com 7 endpoints
- 7 CQRS handlers migrados para `ProductAnalytics.Application`
- `ProductAnalyticsDbContext` operacional com `pan_analytics_events`
- Frontend já consumindo `/api/v1/product-analytics/*` (rotas corretas)
- `AddProductAnalyticsModule()` registado em `Program.cs`
- Sem `ProductAnalyticsEndpointModule` residual em Governance

**Porém:** as permissões nos endpoints foram renomeadas de `governance:analytics:*` para `analytics:*`, mas os catálogos de permissões não foram atualizados.

## Permissões residuais corrigidas em P8.4

### RolePermissionCatalog.cs (backend)

| Role | Linha | Antes | Depois |
|------|-------|-------|--------|
| PlatformAdmin | 90 | `governance:analytics:read` | `analytics:read` |
| PlatformAdmin | 91 | `governance:analytics:write` | `analytics:write` |
| TechLead | 171 | `governance:analytics:read` | `analytics:read` |
| Viewer | 229 | `governance:analytics:read` | `analytics:read` |

### Frontend permissions.ts

| Linha | Antes | Depois |
|-------|-------|--------|
| 97 | `governance:analytics:read` | *(removido — `analytics:read` já existia na linha 96)* |
| 98 | `governance:analytics:write` | `analytics:write` |

### Frontend e2e/helpers/auth.ts

| Linha | Antes | Depois |
|-------|-------|--------|
| 57 | `governance:analytics:read` | `analytics:read` |

## Ficheiros alterados

| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/RolePermissionCatalog.cs` | 4 permissões `governance:analytics:*` → `analytics:*` |
| `src/frontend/src/auth/permissions.ts` | Removidas 2 permissões obsoletas, mantida `analytics:write` |
| `src/frontend/e2e/helpers/auth.ts` | 1 permissão `governance:analytics:read` → `analytics:read` |

## Rotas migradas (confirmação P8.3)

Todas as rotas já estavam migradas em P8.3. Confirmação:

| Rota | Método | Permissão |
|------|--------|-----------|
| `/api/v1/product-analytics/events` | POST | `analytics:write` |
| `/api/v1/product-analytics/summary` | GET | `analytics:read` |
| `/api/v1/product-analytics/adoption/modules` | GET | `analytics:read` |
| `/api/v1/product-analytics/adoption/personas` | GET | `analytics:read` |
| `/api/v1/product-analytics/journeys` | GET | `analytics:read` |
| `/api/v1/product-analytics/value-milestones` | GET | `analytics:read` |
| `/api/v1/product-analytics/friction` | GET | `analytics:read` |

## Impacto no frontend

- Rotas já estavam alinhadas (`/product-analytics/*`) — sem alteração
- Tipo `Permission` no TypeScript agora correto (sem referências `governance:analytics:*`)
- E2E test helper usa permissão nova `analytics:read`

## Compatibilidade transitória

Nenhuma compatibilidade transitória necessária. A migração é limpa:
- Nenhum endpoint Governance residual
- Frontend já consumia `/product-analytics/*`
- Permissões completamente migradas

## Validação

- **Build backend**: 0 errors ✅
- **ProductAnalytics.Tests**: 7 passed ✅
- **Governance.Tests**: 139 passed ✅
- **IdentityAccess.Tests**: 290 passed ✅
- **Zero referências `governance:analytics` restantes**: confirmado via grep ✅
