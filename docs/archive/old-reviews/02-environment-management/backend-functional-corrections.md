# Environment Management — Backend Functional Corrections

> **Módulo:** 02 — Environment Management  
> **Data:** 2026-03-25  
> **Fase:** N4-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Endpoints existentes — Inventário completo

**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/EnvironmentEndpoints.cs`

| # | Verbo | Rota | Permissão actual | Handler CQRS | Estado |
|---|-------|------|-------------------|-------------|--------|
| EP-01 | `GET` | `/api/v1/environments` | `identity:users:read` | `ListEnvironments` | ✅ Funcional — ⚠️ permissão genérica |
| EP-02 | `POST` | `/api/v1/environments` | `identity:users:write` | `CreateEnvironment` | ✅ Funcional — ⚠️ permissão genérica |
| EP-03 | `GET` | `/api/v1/environments/primary-production` | `identity:users:read` | `GetPrimaryProductionEnvironment` | ✅ Funcional — ⚠️ permissão genérica |
| EP-04 | `PUT` | `/api/v1/environments/{id}` | `identity:users:write` | `UpdateEnvironment` | ✅ Funcional — ⚠️ permissão genérica |
| EP-05 | `POST` | `/api/v1/environments/{id}/set-primary-production` | `identity:users:write` | `SetPrimaryProductionEnvironment` | ⚠️ Deveria ter permissão específica de alta criticidade |
| EP-06 | `POST` | `/api/v1/environments/{id}/grant-access` | `identity:users:write` | `GrantEnvironmentAccess` | ✅ Funcional — ⚠️ permissão genérica |

### 1.1 Análise de request/response por endpoint

#### EP-01: `GET /api/v1/environments`

| Aspecto | Estado | Notas |
|---------|--------|-------|
| Paginação | ⚠️ Verificar | Pode não ter `skip`/`take` |
| Filtros | ⚠️ Verificar | Sem filtro por `Profile`, `Criticality`, `IsActive` |
| Response DTO | ✅ | Lista de environments |
| TenantId filtering | ✅ | Via RLS interceptor |

#### EP-02: `POST /api/v1/environments`

| Aspecto | Estado | Notas |
|---------|--------|-------|
| Validação do `Name` | ✅ | Guard clause no factory method |
| Validação do `Slug` | ⚠️ Verificar | Unique constraint na DB, mas validação prévia? |
| Slug duplicado handling | ⚠️ | Pode resultar em `DbUpdateException` não tratada |
| Response | ✅ | Created environment |

#### EP-03: `GET /api/v1/environments/primary-production`

| Aspecto | Estado | Notas |
|---------|--------|-------|
| Not found handling | ⚠️ Verificar | Quando nenhum ambiente é primary production |
| Response | ✅ | Single environment |

#### EP-04: `PUT /api/v1/environments/{id}`

| Aspecto | Estado | Notas |
|---------|--------|-------|
| Not found handling | ⚠️ Verificar | Ambiente inexistente |
| Concurrency conflict | ❌ | Sem `RowVersion` — sem detecção de conflito |
| Partial update support | ⚠️ | PUT substitui tudo — considerar PATCH |

#### EP-05: `POST /api/v1/environments/{id}/set-primary-production`

| Aspecto | Estado | Notas |
|---------|--------|-------|
| Revogação do anterior | ✅ | Handler revoga primary actual antes de designar novo |
| Audit trail | ❌ | Acção de alta criticidade sem audit event explícito |
| Confirmação | ❌ | Sem mecanismo de confirmação/approval |

#### EP-06: `POST /api/v1/environments/{id}/grant-access`

| Aspecto | Estado | Notas |
|---------|--------|-------|
| Validação de UserId | ⚠️ Verificar | Utilizador existe? |
| Acesso duplicado | ⚠️ | Sem unique constraint na DB para user+env activo |
| Expiração | ✅ | Suporta `ExpiresAt` |

---

## 2. Endpoints ausentes

| # | Verbo | Rota sugerida | Propósito | Prioridade |
|---|-------|-------------|-----------|-----------|
| EP-07 | `GET` | `/api/v1/environments/{id}` | Detalhe de um ambiente | ALTA — suporta página de detalhe |
| EP-08 | `DELETE` | `/api/v1/environments/{id}` | Soft-delete de ambiente | ALTA — actualmente impossível eliminar |
| EP-09 | `POST` | `/api/v1/environments/{id}/activate` | Activar ambiente (separado do update genérico) | MÉDIA |
| EP-10 | `POST` | `/api/v1/environments/{id}/deactivate` | Desactivar ambiente | MÉDIA |
| EP-11 | `POST` | `/api/v1/environments/{id}/revoke-access` | Revogar acesso de utilizador | ALTA — método de domínio existe, endpoint não |
| EP-12 | `GET` | `/api/v1/environments/{id}/accesses` | Listar acessos do ambiente | ALTA — gestão administrativa |
| EP-13 | `PUT` | `/api/v1/environments/{id}/accesses/{accessId}/change-level` | Alterar nível de acesso | MÉDIA — método existe, endpoint não |
| EP-14 | `PUT` | `/api/v1/environments/{id}/sort-order` | Reordenar ambiente | BAIXA |
| EP-15 | `GET` | `/api/v1/environments/{id}/policies` | Listar políticas (Phase 2) | BAIXA |
| EP-16 | `POST` | `/api/v1/environments/{id}/policies` | Criar política (Phase 2) | BAIXA |

---

## 3. Dead endpoints / código morto

| Item | Estado | Notas |
|------|--------|-------|
| Endpoints mortos | ✅ Nenhum detectado | Todos os 6 endpoints estão em uso |
| Handlers sem endpoint | ⚠️ Possível | Verificar se existem handlers CQRS não mapeados |
| Entidades sem endpoints | ⚠️ | `EnvironmentPolicy`, `EnvironmentTelemetryPolicy`, `EnvironmentIntegrationBinding` sem API |

---

## 4. Validações existentes vs necessárias

### 4.1 Validações no domínio (guard clauses)

| Método | Validação | Estado |
|--------|----------|--------|
| `Environment.Create()` | `name` not empty, `slug` not empty | ✅ |
| `Environment.UpdateBasicInfo()` | `name` not empty | ✅ |
| `Environment.UpdateProfile()` | Valid enum | ✅ |
| `EnvironmentAccess.Create()` | `userId`, `environmentId` not empty | ✅ |

### 4.2 Validações ausentes

| # | Validação | Onde | Prioridade |
|---|----------|------|-----------|
| VL-01 | Slug format (alphanumeric + hyphens only) | CreateEnvironment validator | ALTA |
| VL-02 | Slug uniqueness check (antes de salvar) | CreateEnvironment handler | ALTA |
| VL-03 | `Name` max length 100 | CreateEnvironment / UpdateEnvironment validators | MÉDIA |
| VL-04 | `Code` max length 50 | CreateEnvironment / UpdateEnvironment validators | MÉDIA |
| VL-05 | `Region` max length 100 | CreateEnvironment / UpdateEnvironment validators | MÉDIA |
| VL-06 | Cannot deactivate primary production | Deactivate handler | ALTA |
| VL-07 | Cannot delete primary production | Delete handler (a criar) | ALTA |
| VL-08 | User exists check on grant-access | GrantEnvironmentAccess handler | MÉDIA |
| VL-09 | No duplicate active access per user+environment | GrantEnvironmentAccess handler | ALTA |
| VL-10 | Profile enum value range (1-9) | FluentValidation | MÉDIA |
| VL-11 | Criticality enum value range (1-4) | FluentValidation | MÉDIA |

---

## 5. Error handling

### 5.1 Padrões de erro actuais

| Cenário | Tratamento actual | Tratamento esperado |
|---------|-------------------|-------------------|
| Ambiente não encontrado | ⚠️ Verificar | `404 Not Found` com `code: "ENV_NOT_FOUND"` |
| Slug duplicado | ❌ `DbUpdateException` não tratada | `409 Conflict` com `code: "ENV_SLUG_DUPLICATE"` |
| Permissão insuficiente | ✅ Via middleware auth | `403 Forbidden` |
| Validação falhou | ⚠️ Verificar FluentValidation | `400 Bad Request` com validation details |
| Concurrency conflict | ❌ Não detectado | `409 Conflict` com `code: "ENV_CONCURRENCY_CONFLICT"` |
| Tentar desactivar primary production | ❌ Não validado | `422 Unprocessable Entity` com `code: "ENV_CANNOT_DEACTIVATE_PRIMARY"` |

### 5.2 Padrão de resposta de erro recomendado

```json
{
  "code": "ENV_SLUG_DUPLICATE",
  "messageKey": "errors.environment.slugDuplicate",
  "params": { "slug": "prod-eu" },
  "correlationId": "abc-123"
}
```

---

## 6. Permissões por acção — actual vs alvo

| Acção | Permissão actual | Permissão alvo | Criticidade da acção |
|-------|-----------------|----------------|---------------------|
| Listar ambientes | `identity:users:read` | `env:environments:read` | BAIXA |
| Criar ambiente | `identity:users:write` | `env:environments:write` | MÉDIA |
| Editar ambiente | `identity:users:write` | `env:environments:write` | MÉDIA |
| Activar/Desactivar | `identity:users:write` | `env:environments:write` | MÉDIA |
| Eliminar ambiente | — (não existe) | `env:environments:admin` | ALTA |
| Designar primary production | `identity:users:write` | `env:environments:admin` | **CRÍTICA** |
| Ver primary production | `identity:users:read` | `env:environments:read` | BAIXA |
| Conceder acesso | `identity:users:write` | `env:access:admin` | ALTA |
| Revogar acesso | — (não existe) | `env:access:admin` | ALTA |
| Alterar nível de acesso | — (não existe) | `env:access:admin` | ALTA |
| Listar acessos | — (não existe) | `env:access:read` | MÉDIA |

---

## 7. Audit trail

| Acção | Audit event actual | Audit event necessário |
|-------|-------------------|----------------------|
| Criar ambiente | ❌ | `EnvironmentCreated` via domain event |
| Editar ambiente | ❌ | `EnvironmentUpdated` via domain event |
| Activar/Desactivar | ❌ | `EnvironmentActivated` / `EnvironmentDeactivated` |
| Designar primary production | ❌ | `PrimaryProductionDesignated` — **CRÍTICO** |
| Conceder acesso | ❌ | `EnvironmentAccessGranted` |
| Revogar acesso | ❌ | `EnvironmentAccessRevoked` |

**Nota:** O `AuditInterceptor` do `NexTraceDbContextBase` pode captar alterações a nível de EF, mas domain events explícitos são necessários para audit trail granular e cross-module notification.

---

## 8. Serviços de infraestrutura — Análise

| Serviço | Ficheiro | Propósito | Migra para Env Mgmt? |
|---------|---------|-----------|---------------------|
| `EnvironmentContextAccessor` | `Identity.Infrastructure/Services/` | Accessor scoped do ambiente resolvido | ✅ |
| `EnvironmentAccessValidator` | `Identity.Infrastructure/Services/` | Valida acesso do utilizador | ⚠️ Permanece no Identity (auth) |
| `EnvironmentProfileResolver` | `Identity.Infrastructure/Services/` | Resolve UI profile | ✅ |
| `TenantEnvironmentContextResolver` | `Identity.Infrastructure/Services/` | Resolve contexto tenant+ambiente | ✅ |
| `EnvironmentResolutionMiddleware` | `Identity.Infrastructure/Middleware/` | Middleware `X-Environment-Id` | ⚠️ Permanece no Identity (pipeline) |
| `EnvironmentRepository` | `Identity.Infrastructure/Persistence/` | Data access | ✅ |
| `CurrentEnvironmentAdapter` | `Identity.Infrastructure/Services/` | Implementa `ICurrentEnvironment` | ✅ |

---

## 9. Backlog de correcções backend

### 9.1 Prioridade ALTA

| # | Correcção | Ficheiro(s) afectado(s) | Esforço |
|---|----------|-------------------------|---------|
| BF-01 | Criar endpoint `GET /api/v1/environments/{id}` (detalhe) | `EnvironmentEndpoints.cs`, novo handler | 2h |
| BF-02 | Criar endpoint `DELETE /api/v1/environments/{id}` (soft-delete) | `EnvironmentEndpoints.cs`, novo handler, domínio | 3h |
| BF-03 | Criar endpoint `POST /api/v1/environments/{id}/revoke-access` | `EnvironmentEndpoints.cs`, novo handler | 2h |
| BF-04 | Criar endpoint `GET /api/v1/environments/{id}/accesses` | `EnvironmentEndpoints.cs`, novo handler | 2h |
| BF-05 | Adicionar validação slug format e uniqueness check | Validators, CreateEnvironment handler | 2h |
| BF-06 | Migrar permissões de `identity:users:*` para `env:*` | `EnvironmentEndpoints.cs`, Permission seeds | 3h |
| BF-07 | Adicionar `UseXminAsConcurrencyToken()` e handling de `DbUpdateConcurrencyException` | EF configs, handlers | 2h |
| BF-08 | Validação: não permitir desactivar primary production | `Deactivate` handler / domínio | 1h |

### 9.2 Prioridade MÉDIA

| # | Correcção | Ficheiro(s) afectado(s) | Esforço |
|---|----------|-------------------------|---------|
| BF-09 | Adicionar domain events (`EnvironmentCreated`, `PrimaryProductionDesignated`, etc.) | `Environment.cs`, `EnvironmentAccess.cs` | 4h |
| BF-10 | Tratar `DbUpdateException` por slug duplicado como `409 Conflict` | CreateEnvironment handler | 1h |
| BF-11 | Adicionar paginação e filtros a `ListEnvironments` | Handler, validator | 2h |
| BF-12 | Criar endpoints dedicados `activate` / `deactivate` | `EnvironmentEndpoints.cs`, handlers | 2h |
| BF-13 | Adicionar FluentValidation para max lengths | Validators | 1h |
| BF-14 | Verificar existência do utilizador em `grant-access` | Handler | 1h |

### 9.3 Prioridade BAIXA

| # | Correcção | Ficheiro(s) afectado(s) | Esforço |
|---|----------|-------------------------|---------|
| BF-15 | Endpoint `PUT .../sort-order` dedicado | `EnvironmentEndpoints.cs`, handler | 1h |
| BF-16 | Endpoint `PUT .../accesses/{id}/change-level` | `EnvironmentEndpoints.cs`, handler | 1h |
| BF-17 | Endpoints de policies (Phase 2) | Novos handlers, validators, endpoints | 8h |

**Total estimado: ~38 horas**
