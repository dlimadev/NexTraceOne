# Environment Management — Security and Permissions Review

> **Módulo:** 02 — Environment Management  
> **Data:** 2026-03-25  
> **Fase:** N4-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Permissões por página

| Página | Rota | Permissão actual | Permissão alvo | Enforcement | Estado |
|--------|------|-----------------|----------------|-------------|--------|
| EnvironmentsPage | `/environments` | `identity:users:read` | `env:environments:read` | ⚠️ Verificar `<ProtectedRoute>` | ⚠️ Genérica |
| EnvironmentDetailPage | `/environments/{id}` | — (não existe) | `env:environments:read` | — | ❌ Página ausente |
| EnvironmentComparisonPage | `/operations/runtime-comparison` | `operations:runtime:read` | `operations:runtime:read` | ✅ `<ProtectedRoute>` | ✅ OK (módulo Operations) |

---

## 2. Permissões por acção

### 2.1 Acções de leitura

| Acção | Permissão backend actual | Permissão frontend actual | Permissão alvo | Estado |
|-------|-------------------------|--------------------------|----------------|--------|
| Listar ambientes | `identity:users:read` | `identity:users:read` | `env:environments:read` | ⚠️ Demasiado genérica |
| Ver detalhe ambiente | — (sem endpoint) | — | `env:environments:read` | ❌ Não implementado |
| Ver primary production | `identity:users:read` | `identity:users:read` | `env:environments:read` | ⚠️ Demasiado genérica |
| Listar acessos | — (sem endpoint) | — | `env:access:read` | ❌ Não implementado |

### 2.2 Acções de escrita

| Acção | Permissão backend actual | Permissão frontend actual | Permissão alvo | Estado |
|-------|-------------------------|--------------------------|----------------|--------|
| Criar ambiente | `identity:users:write` | `identity:users:write` (implícita) | `env:environments:write` | ⚠️ Demasiado genérica |
| Editar ambiente | `identity:users:write` | `identity:users:write` (implícita) | `env:environments:write` | ⚠️ Demasiado genérica |
| Activar/Desactivar | `identity:users:write` | — | `env:environments:write` | ⚠️ |
| Eliminar ambiente | — (sem endpoint) | — | `env:environments:admin` | ❌ Não implementado |

### 2.3 Acções de alta criticidade

| Acção | Permissão backend actual | Permissão alvo | Criticidade | Estado |
|-------|-------------------------|----------------|------------|--------|
| **Designar primary production** | `identity:users:write` | `env:environments:admin` | **CRÍTICA** | ❌ **SEC-01** — qualquer user com `identity:users:write` pode designar |
| **Conceder acesso** | `identity:users:write` | `env:access:admin` | **ALTA** | ❌ **SEC-02** — sem segregação de privilege |
| **Revogar acesso** | — | `env:access:admin` | **ALTA** | ❌ Não implementado |

---

## 3. Análise de gaps de segurança

### SEC-01: Designação de primary production sem permissão adequada

**Severidade: ALTA**

| Aspecto | Detalhe |
|---------|---------|
| Problema | Qualquer utilizador com `identity:users:write` pode designar um ambiente como primary production |
| Impacto | Operação que altera routing de produção, blast radius e risk scoring de todo o tenant |
| Permissão actual | `identity:users:write` — mesma permissão que editar um utilizador |
| Permissão necessária | `env:environments:admin` com confirmação explícita |
| Recomendação | Criar namespace `env:*`, exigir `env:environments:admin` para acções de alta criticidade |

### SEC-02: Grant access sem segregação de privilégio

**Severidade: ALTA**

| Aspecto | Detalhe |
|---------|---------|
| Problema | `identity:users:write` permite conceder acesso admin a qualquer ambiente |
| Impacto | Escalação de privilégio — user pode conceder-se acesso admin |
| Permissão actual | `identity:users:write` |
| Permissão necessária | `env:access:admin` + validação de que quem concede tem nível >= ao nível concedido |
| Recomendação | Implementar check: `grantor.AccessLevel >= grantee.requestedAccessLevel` |

### SEC-03: Sem audit trail para acções críticas

**Severidade: MÉDIA**

| Aspecto | Detalhe |
|---------|---------|
| Problema | Designar primary production e grant access não geram audit events explícitos |
| Impacto | Acções críticas sem rastreabilidade completa |
| Mitigação parcial | `AuditInterceptor` captura saves genéricos, mas sem semântica de domínio |
| Recomendação | Implementar domain events: `PrimaryProductionDesignated`, `EnvironmentAccessGranted` |

### SEC-04: Sem rate limiting específico para acções sensíveis

**Severidade: BAIXA**

| Aspecto | Detalhe |
|---------|---------|
| Problema | Rate limiting é global (100 req/60s), sem proteção específica para operações sensíveis |
| Impacto | Ataque de força bruta ou spam em grant-access/set-primary |
| Recomendação | Considerar rate limit mais restritivo em endpoints de alta criticidade |

### SEC-05: Sem validação de existência do utilizador em grant-access

**Severidade: MÉDIA**

| Aspecto | Detalhe |
|---------|---------|
| Problema | O endpoint `grant-access` pode aceitar `UserId` de utilizador inexistente |
| Impacto | Registos de acesso para utilizadores fantasma |
| Recomendação | Validar existência do utilizador no Identity module antes de criar `EnvironmentAccess` |

---

## 4. Frontend guards

| Guard | Implementação | Estado |
|-------|---------------|--------|
| Route-level protection | ⚠️ Verificar se `/environments` usa `<ProtectedRoute>` | ⚠️ |
| Sidebar visibility | ❌ Sem sidebar entry | ❌ |
| Button-level write guard | ⚠️ Verificar se botões "Create", "Edit", "Set Primary" têm permission check | ⚠️ |
| Confirmation dialog para acções críticas | ❌ Set Primary Production sem confirmação modal | ❌ |

### 4.1 Recomendações de frontend guard

| # | Guard | Onde | Permissão |
|---|-------|------|-----------|
| FG-01 | `<ProtectedRoute permission="env:environments:read">` | Route `/environments` | Leitura |
| FG-02 | `<ProtectedRoute permission="env:environments:read">` | Route `/environments/{id}` | Leitura |
| FG-03 | Botão "Create" visível apenas com `env:environments:write` | `EnvironmentsPage` | Escrita |
| FG-04 | Botão "Edit" visível apenas com `env:environments:write` | `EnvironmentsPage` / detail | Escrita |
| FG-05 | Botão "Delete" visível apenas com `env:environments:admin` | `EnvironmentsPage` / detail | Admin |
| FG-06 | Botão "Set Primary" visível apenas com `env:environments:admin` | `EnvironmentsPage` | Admin |
| FG-07 | Botão "Grant Access" visível apenas com `env:access:admin` | Detail page | Admin |
| FG-08 | Confirmation modal para "Set Primary Production" | `EnvironmentsPage` | UX |
| FG-09 | Confirmation modal para "Delete Environment" | `EnvironmentsPage` | UX |

---

## 5. Backend enforcement

| Camada | Mecanismo | Estado |
|--------|----------|--------|
| Authentication | JWT / API Key / OIDC via global middleware | ✅ Herdado |
| Authorization | `RequirePermission()` per endpoint | ⚠️ Funcional mas com permissões genéricas |
| Multi-tenancy | PostgreSQL RLS via `TenantRlsInterceptor` | ✅ Herdado de `NexTraceDbContextBase` |
| Rate limiting | Global rate limiting | ✅ — ⚠️ sem rate limit específico |
| Input validation | Guard clauses no domínio | ⚠️ FluentValidation ausente em alguns handlers |
| Concurrency | ❌ Sem `RowVersion` / `xmin` | ❌ Conflitos silenciosos |

---

## 6. Tenant scoping

| Aspecto | Implementação | Estado |
|---------|---------------|--------|
| Data isolation | PostgreSQL RLS via `tenant_id` em `identity_environments` e `identity_environment_accesses` | ✅ |
| Scope em queries | `TenantRlsInterceptor` aplica filtro automático | ✅ |
| Cross-tenant access | Prevenido por RLS policy | ✅ |
| Tenant no slug unique | `(tenant_id, slug)` unique constraint | ✅ |
| Tenant no primary production | `(tenant_id, is_primary_production)` partial unique | ✅ |

---

## 7. Environment scoping

| Aspecto | Implementação | Estado |
|---------|---------------|--------|
| Environment-aware requests | Header `X-Environment-Id` resolvido pelo `EnvironmentResolutionMiddleware` | ✅ |
| `ICurrentEnvironment` interface | Implementada por `CurrentEnvironmentAdapter` | ✅ |
| `IsProductionLike` propagação | Disponível via `ICurrentEnvironment.IsProductionLike` | ✅ |
| Access validation per environment | `EnvironmentAccessValidator` verifica acesso do user ao environment | ✅ |
| Temporal access expiry | `EnvironmentAccess.IsActiveAt()` verifica expiração | ✅ |

---

## 8. Audit de acções críticas

| Acção crítica | Audit event | Inclui user | Inclui timestamp | Inclui detalhes | Estado |
|--------------|-------------|-------------|-----------------|-----------------|--------|
| Criar ambiente | ❌ Sem event explícito | (interceptor) | (interceptor) | ❌ | ⚠️ Apenas interceptor genérico |
| Editar ambiente | ❌ Sem event explícito | (interceptor) | (interceptor) | ❌ | ⚠️ |
| Activar/Desactivar | ❌ Sem event explícito | (interceptor) | (interceptor) | ❌ | ⚠️ |
| **Designar primary production** | ❌ Sem event explícito | (interceptor) | (interceptor) | ❌ | ❌ **Inaceitável** para operação desta criticidade |
| **Conceder acesso** | ❌ Sem event explícito | (interceptor) | (interceptor) | ❌ | ❌ **Inaceitável** |
| **Revogar acesso** | — (sem endpoint) | — | — | — | ❌ Não implementado |

### 8.1 Audit events necessários (por prioridade)

| # | Event | Dados mínimos | Prioridade |
|---|-------|--------------|-----------|
| AE-01 | `PrimaryProductionDesignated` | environmentId, previousPrimaryId, userId, timestamp | **CRÍTICA** |
| AE-02 | `EnvironmentAccessGranted` | environmentId, userId, accessLevel, grantedBy, expiresAt | **ALTA** |
| AE-03 | `EnvironmentAccessRevoked` | environmentId, userId, revokedBy, reason | **ALTA** |
| AE-04 | `EnvironmentCreated` | environmentId, name, profile, criticality, createdBy | MÉDIA |
| AE-05 | `EnvironmentDeactivated` | environmentId, deactivatedBy, reason | MÉDIA |
| AE-06 | `EnvironmentUpdated` | environmentId, changedFields, updatedBy | MÉDIA |
| AE-07 | `EnvironmentDeleted` | environmentId, deletedBy, reason | MÉDIA |

---

## 9. Namespace de permissões alvo

### 9.1 Definição do namespace `env:`

| Permissão | Scope | Acções cobertas |
|-----------|-------|----------------|
| `env:environments:read` | Leitura | Listar, ver detalhe, ver primary production |
| `env:environments:write` | Escrita | Criar, editar, activar/desactivar |
| `env:environments:admin` | Admin | Eliminar, designar primary production |
| `env:access:read` | Leitura acessos | Listar acessos de um ambiente |
| `env:access:admin` | Admin acessos | Conceder, revogar, alterar nível de acesso |
| `env:policies:read` | Leitura políticas | Listar políticas (Phase 2) |
| `env:policies:write` | Escrita políticas | Criar/editar políticas (Phase 2) |

### 9.2 Migração de permissões

| Endpoint | Permissão actual | Permissão alvo | Breaking change? |
|---------|-----------------|----------------|-----------------|
| `GET /environments` | `identity:users:read` | `env:environments:read` | ✅ Sim — requer seed + migration |
| `POST /environments` | `identity:users:write` | `env:environments:write` | ✅ Sim |
| `GET /environments/primary-production` | `identity:users:read` | `env:environments:read` | ✅ Sim |
| `PUT /environments/{id}` | `identity:users:write` | `env:environments:write` | ✅ Sim |
| `POST /environments/{id}/set-primary-production` | `identity:users:write` | `env:environments:admin` | ✅ Sim — **escalação de requisito** |
| `POST /environments/{id}/grant-access` | `identity:users:write` | `env:access:admin` | ✅ Sim — **escalação de requisito** |

**Nota:** A migração de permissões é uma breaking change coordenada. Requer:
1. Criar permissões `env:*` no seed
2. Atribuir `env:*` a roles que actualmente têm `identity:users:*`
3. Actualizar endpoints para usar `env:*`
4. Actualizar frontend guards

---

## 10. Backlog de segurança

| # | Item | Severidade | Esforço | Prioridade |
|---|------|-----------|---------|-----------|
| SEC-01 | Criar namespace `env:*` e migrar permissões | ALTA | 4h | ALTA |
| SEC-02 | Segregar `set-primary-production` com `env:environments:admin` | ALTA | 1h | ALTA |
| SEC-03 | Implementar audit events para acções críticas | MÉDIA | 4h | ALTA |
| SEC-04 | Validar privilege level em grant-access (grantor >= grantee) | ALTA | 2h | ALTA |
| SEC-05 | Validar existência de utilizador em grant-access | MÉDIA | 1h | MÉDIA |
| SEC-06 | Adicionar confirmation modal para primary production no frontend | MÉDIA | 1h | MÉDIA |
| SEC-07 | Adicionar `<ProtectedRoute>` com permissão correcta | MÉDIA | 1h | MÉDIA |
| SEC-08 | Adicionar button-level permission guards no frontend | BAIXA | 2h | MÉDIA |
| SEC-09 | Rate limit específico para set-primary e grant-access | BAIXA | 1h | BAIXA |
| SEC-10 | Adicionar `UseXminAsConcurrencyToken()` para prevenir conflitos | MÉDIA | 1h | ALTA |

**Total estimado: ~18 horas**
