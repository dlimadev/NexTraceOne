# Environment Management — Persistence Model Finalization

> **Módulo:** 02 — Environment Management  
> **Data:** 2026-03-25  
> **Fase:** N4-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Estado actual da persistência

Todas as tabelas do módulo estão actualmente no `IdentityDbContext`, com prefixo `identity_`.

**Ficheiro do DbContext:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/IdentityDbContext.cs`

### 1.1 DbSets actuais

```csharp
public DbSet<Environment> Environments { get; set; }
public DbSet<EnvironmentAccess> EnvironmentAccesses { get; set; }
```

### 1.2 Configurações EF actuais

| Entidade | Ficheiro de configuração | Tabela actual |
|----------|------------------------|---------------|
| `Environment` | `Infrastructure/Persistence/Configurations/EnvironmentConfiguration.cs` | `identity_environments` |
| `EnvironmentAccess` | `Infrastructure/Persistence/Configurations/EnvironmentAccessConfiguration.cs` | `identity_environment_accesses` |
| `EnvironmentPolicy` | ❌ Sem configuração EF | — |
| `EnvironmentTelemetryPolicy` | ❌ Sem configuração EF | — |
| `EnvironmentIntegrationBinding` | ❌ Sem configuração EF | — |

---

## 2. Tabelas actuais — Esquema detalhado

### 2.1 `identity_environments`

| Coluna | Tipo PostgreSQL | Nullable | Default | Notas |
|--------|----------------|----------|---------|-------|
| `id` | `uuid` | NOT NULL | — | PK, EnvironmentId strongly-typed |
| `tenant_id` | `uuid` | NOT NULL | — | FK lógica, RLS |
| `name` | `varchar(100)` | NOT NULL | — | Nome de exibição |
| `slug` | `varchar(50)` | NOT NULL | — | URL-friendly |
| `sort_order` | `integer` | NOT NULL | `0` | Ordenação |
| `is_active` | `boolean` | NOT NULL | `true` | Soft-toggle |
| `created_at` | `timestamptz` | NOT NULL | — | UTC |
| `profile` | `integer` | NOT NULL | — | Enum EnvironmentProfile |
| `criticality` | `integer` | NOT NULL | — | Enum EnvironmentCriticality |
| `code` | `varchar(50)` | NULL | — | Ex: "DEV", "PROD-BR" |
| `description` | `text` | NULL | — | Descrição livre |
| `region` | `varchar(100)` | NULL | — | Ex: "eu-west-1" |
| `is_production_like` | `boolean` | NOT NULL | `false` | — |
| `is_primary_production` | `boolean` | NOT NULL | `false` | Filtro parcial unique |

**Colunas ausentes (necessárias):**

| Coluna | Tipo | Motivo |
|--------|------|--------|
| `is_deleted` | `boolean NOT NULL DEFAULT false` | Soft-delete |
| `updated_at` | `timestamptz` | Tracking de alterações |
| `updated_by` | `varchar(200)` | Quem alterou |
| `xmin` | (system column) | Concurrency token via `UseXminAsConcurrencyToken()` |

### 2.2 `identity_environment_accesses`

| Coluna | Tipo PostgreSQL | Nullable | Default | Notas |
|--------|----------------|----------|---------|-------|
| `id` | `uuid` | NOT NULL | — | PK |
| `user_id` | `uuid` | NOT NULL | — | Referência ao utilizador |
| `tenant_id` | `uuid` | NOT NULL | — | FK lógica, RLS |
| `environment_id` | `uuid` | NOT NULL | — | FK lógica a `identity_environments` |
| `access_level` | `varchar` | NOT NULL | — | "read"/"write"/"admin"/"none" |
| `granted_at` | `timestamptz` | NOT NULL | — | UTC |
| `expires_at` | `timestamptz` | NULL | — | Temporal access |
| `granted_by` | `uuid` | NOT NULL | — | Quem concedeu |
| `is_active` | `boolean` | NOT NULL | `true` | — |
| `revoked_at` | `timestamptz` | NULL | — | Quando revogado |

---

## 3. Índices actuais

| Tabela | Índice | Tipo | Colunas | Filtro |
|--------|--------|------|---------|--------|
| `identity_environments` | PK | Primary Key | `id` | — |
| `identity_environments` | `IX_identity_environments_tenant_slug` | Unique | `(tenant_id, slug)` | — |
| `identity_environments` | `IX_identity_environments_tenant_primary` | Partial Unique | `(tenant_id, is_primary_production)` | `WHERE is_primary_production = true AND is_active = true` |
| `identity_environment_accesses` | PK | Primary Key | `id` | — |

---

## 4. Modelo de persistência alvo — Phase 1

### 4.1 Tabela `env_environments` (renomeada)

| Coluna | Tipo PostgreSQL | Nullable | Default | Migração |
|--------|----------------|----------|---------|----------|
| `id` | `uuid` | NOT NULL | — | ✅ Existente |
| `tenant_id` | `uuid` | NOT NULL | — | ✅ Existente |
| `name` | `varchar(100)` | NOT NULL | — | ✅ Existente |
| `slug` | `varchar(50)` | NOT NULL | — | ✅ Existente |
| `sort_order` | `integer` | NOT NULL | `0` | ✅ Existente |
| `is_active` | `boolean` | NOT NULL | `true` | ✅ Existente |
| `is_deleted` | `boolean` | NOT NULL | `false` | 🆕 Adicionar |
| `created_at` | `timestamptz` | NOT NULL | — | ✅ Existente |
| `updated_at` | `timestamptz` | NULL | — | 🆕 Adicionar |
| `updated_by` | `varchar(200)` | NULL | — | 🆕 Adicionar |
| `profile` | `integer` | NOT NULL | — | ✅ Existente |
| `criticality` | `integer` | NOT NULL | — | ✅ Existente |
| `code` | `varchar(50)` | NULL | — | ✅ Existente |
| `description` | `text` | NULL | — | ✅ Existente |
| `region` | `varchar(100)` | NULL | — | ✅ Existente |
| `is_production_like` | `boolean` | NOT NULL | `false` | ✅ Existente |
| `is_primary_production` | `boolean` | NOT NULL | `false` | ✅ Existente |

**Concurrency:** `UseXminAsConcurrencyToken()` — usa a coluna de sistema `xmin` do PostgreSQL.

### 4.2 Tabela `env_environment_accesses` (renomeada)

| Coluna | Tipo PostgreSQL | Nullable | Default | Migração |
|--------|----------------|----------|---------|----------|
| `id` | `uuid` | NOT NULL | — | ✅ Existente → strongly-typed |
| `user_id` | `uuid` | NOT NULL | — | ✅ Existente |
| `tenant_id` | `uuid` | NOT NULL | — | ✅ Existente |
| `environment_id` | `uuid` | NOT NULL | — | ✅ Existente |
| `access_level` | `varchar(20)` | NOT NULL | — | ⚠️ Existente → considerar enum conversion |
| `granted_at` | `timestamptz` | NOT NULL | — | ✅ Existente |
| `expires_at` | `timestamptz` | NULL | — | ✅ Existente |
| `granted_by` | `uuid` | NOT NULL | — | ✅ Existente |
| `is_active` | `boolean` | NOT NULL | `true` | ✅ Existente |
| `revoked_at` | `timestamptz` | NULL | — | ✅ Existente |

---

## 5. Índices alvo — Phase 1

| Tabela | Índice | Tipo | Colunas | Filtro | Estado |
|--------|--------|------|---------|--------|--------|
| `env_environments` | PK | Primary Key | `id` | — | ✅ Renomear |
| `env_environments` | `IX_env_environments_tenant_slug` | Unique | `(tenant_id, slug)` | — | ✅ Renomear |
| `env_environments` | `IX_env_environments_tenant_primary` | Partial Unique | `(tenant_id, is_primary_production)` | `WHERE is_primary_production = true AND is_active = true AND is_deleted = false` | ⚠️ Actualizar filtro (adicionar `is_deleted`) |
| `env_environments` | `IX_env_environments_tenant_active` | Index | `(tenant_id, is_active)` | `WHERE is_deleted = false` | 🆕 Adicionar |
| `env_environments` | `IX_env_environments_profile` | Index | `(tenant_id, profile)` | — | 🆕 Adicionar |
| `env_environments` | `IX_env_environments_criticality` | Index | `(tenant_id, criticality)` | — | 🆕 Adicionar |
| `env_environment_accesses` | PK | Primary Key | `id` | — | ✅ Renomear |
| `env_environment_accesses` | `IX_env_accesses_env_id` | Index | `(environment_id)` | — | 🆕 Adicionar — FK join |
| `env_environment_accesses` | `IX_env_accesses_user_env` | Unique | `(user_id, environment_id, tenant_id)` | `WHERE is_active = true` | 🆕 Adicionar — evitar duplicados |
| `env_environment_accesses` | `IX_env_accesses_user_active` | Index | `(user_id, is_active)` | — | 🆕 Adicionar — auth lookups |

---

## 6. Constraints alvo

| Tabela | Constraint | Tipo | Definição | Estado |
|--------|-----------|------|----------|--------|
| `env_environments` | `CK_env_environments_profile` | Check | `profile IN (1,2,3,4,5,6,7,8,9)` | 🆕 |
| `env_environments` | `CK_env_environments_criticality` | Check | `criticality IN (1,2,3,4)` | 🆕 |
| `env_environments` | `CK_env_environments_sort_order` | Check | `sort_order >= 0` | 🆕 |
| `env_environments` | `CK_env_environments_name_length` | Check | `length(name) >= 1` | 🆕 |
| `env_environment_accesses` | `FK_env_accesses_env` | Foreign Key | `environment_id → env_environments(id)` | 🆕 — actualmente sem FK real |
| `env_environment_accesses` | `CK_env_accesses_level` | Check | `access_level IN ('read','write','admin','none')` | 🆕 |

---

## 7. Tabelas Phase 2 (após persistência das entidades definidas)

### 7.1 `env_policies`

| Coluna | Tipo | Nullable | Notas |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| `environment_id` | `uuid` | NOT NULL | FK → `env_environments` |
| `policy_type` | `varchar(50)` | NOT NULL | promotion_approval, freeze_window, alert_escalation, deploy_quality_gate |
| `configuration` | `jsonb` | NOT NULL | Detalhes da política |
| `is_active` | `boolean` | NOT NULL | Default true |
| `created_at` | `timestamptz` | NOT NULL | — |
| `updated_at` | `timestamptz` | NULL | — |

**Índices:** PK, `(tenant_id, environment_id, policy_type)` unique, `(environment_id)` FK.

### 7.2 `env_telemetry_policies`

| Coluna | Tipo | Nullable | Notas |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| `environment_id` | `uuid` | NOT NULL | FK → `env_environments` |
| `retention_days` | `integer` | NOT NULL | Default 30 |
| `verbosity_level` | `varchar(20)` | NOT NULL | minimal, standard, verbose |
| `allow_cross_env_comparison` | `boolean` | NOT NULL | Default false |
| `created_at` | `timestamptz` | NOT NULL | — |
| `updated_at` | `timestamptz` | NULL | — |

**Índices:** PK, `(tenant_id, environment_id)` unique (uma policy por ambiente).

### 7.3 `env_integration_bindings`

| Coluna | Tipo | Nullable | Notas |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| `environment_id` | `uuid` | NOT NULL | FK → `env_environments` |
| `binding_type` | `varchar(30)` | NOT NULL | observability, alerting, ci_cd, event_broker |
| `configuration` | `jsonb` | NOT NULL | Endpoint, credentials ref, etc. |
| `is_active` | `boolean` | NOT NULL | Default true |
| `created_at` | `timestamptz` | NOT NULL | — |
| `updated_at` | `timestamptz` | NULL | — |

**Índices:** PK, `(tenant_id, environment_id, binding_type)` unique, `(environment_id)` FK.

---

## 8. Divergências actual vs final

| Aspecto | Estado actual | Estado alvo | Acção necessária |
|---------|-------------|-------------|-----------------|
| **Prefixo de tabela** | `identity_` | `env_` | ⚠️ BLOCKED por OI-04 — requer migração coordenada |
| **DbContext** | `IdentityDbContext` | `EnvironmentDbContext` (dedicado) | Criar novo DbContext na infra do módulo |
| **FK real `environment_accesses` → `environments`** | ❌ Sem FK | ✅ FK real | Adicionar na migração |
| **Concurrency token** | ❌ Ausente em todo o codebase | ✅ `UseXminAsConcurrencyToken()` | Adicionar a ambas as configurations |
| **Soft-delete (`is_deleted`)** | ❌ Ausente | ✅ Em `env_environments` | Adicionar coluna + query filter |
| **`updated_at` / `updated_by`** | ❌ Ausente | ✅ Em `env_environments` | Adicionar colunas |
| **Check constraints** | ❌ Ausente | ✅ Profile, Criticality, AccessLevel | Adicionar 6 check constraints |
| **Índices de performance** | Mínimos (3) | Completos (10+) | Adicionar 7+ índices |
| **Tabelas Phase 2** | ❌ Não existem | 3 novas tabelas | Criar após modelo de domínio Phase 2 |

---

## 9. Migração de tabelas — Estratégia

### 9.1 Pré-condição: OI-04

O item `OI-04` em `docs/architecture/phase-a-open-items.md` é classificado como **BLOCKING**:

> Não é possível aplicar o prefixo `env_` de forma independente enquanto as tabelas residem no `IdentityDbContext`.

### 9.2 Sequência recomendada

1. **Criar `EnvironmentDbContext`** no novo módulo `NexTraceOne.EnvironmentManagement.Infrastructure`
2. **Registar os DbSets** no novo context (Environments, EnvironmentAccesses)
3. **Gerar migração de rename** (`identity_environments` → `env_environments`, etc.)
4. **Remover DbSets** do `IdentityDbContext`
5. **Adicionar colunas novas** (`is_deleted`, `updated_at`, `updated_by`)
6. **Adicionar constraints e índices**
7. **Adicionar `UseXminAsConcurrencyToken()`**
8. **Verificar RLS** — `TenantRlsInterceptor` funciona com o novo DbContext

### 9.3 Interceptores herdados

O novo `EnvironmentDbContext` herda de `NexTraceDbContextBase`, que aplica automaticamente:

| Interceptor | Propósito | Herdado? |
|------------|-----------|----------|
| `TenantRlsInterceptor` | Row-Level Security por tenant_id | ✅ Automático |
| `AuditInterceptor` | Audit trail | ✅ Automático |
| `EncryptionInterceptor` | Encriptação de campos sensíveis | ✅ Automático |
| `OutboxInterceptor` | Outbox pattern para integration events | ✅ Automático |

---

## 10. Backlog de persistência

| # | Item | Prioridade | Bloqueado por |
|---|------|-----------|--------------|
| PM-01 | Criar `EnvironmentDbContext` dedicado | ALTA | Módulo backend dedicado |
| PM-02 | Renomear tabelas para prefixo `env_` | ALTA | OI-04, PM-01 |
| PM-03 | Adicionar `UseXminAsConcurrencyToken()` a ambas as tabelas | ALTA | PM-01 |
| PM-04 | Adicionar `is_deleted`, `updated_at`, `updated_by` a `env_environments` | MÉDIA | PM-02 |
| PM-05 | Adicionar FK real `env_environment_accesses.environment_id` → `env_environments.id` | MÉDIA | PM-02 |
| PM-06 | Adicionar check constraints para enums | MÉDIA | PM-02 |
| PM-07 | Adicionar índices de performance (7+) | MÉDIA | PM-02 |
| PM-08 | Actualizar partial unique index (adicionar `is_deleted = false`) | MÉDIA | PM-04 |
| PM-09 | Criar tabela `env_policies` | BAIXA | Phase 2 |
| PM-10 | Criar tabela `env_telemetry_policies` | BAIXA | Phase 2 |
| PM-11 | Criar tabela `env_integration_bindings` | BAIXA | Phase 2 |
