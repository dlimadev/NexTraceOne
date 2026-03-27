# Estratégia da Nova Baseline PostgreSQL por Módulo

> **Status:** DRAFT
> **Data:** 2026-03-25
> **Fase:** N15 — Estratégia de Transição de Persistência

---

## Objetivo

Definir como gerar uma nova baseline limpa por módulo, mantendo um único banco físico PostgreSQL, com políticas claras para DbContexts, prefixos, colunas transversais, seeds e referências cruzadas.

---

## Princípios Estruturais

### 1. Banco Físico Único

- **1 banco PostgreSQL** para toda a aplicação
- Schema: `public` (default)
- Isolamento entre módulos via prefixos de tabela e DbContexts separados
- RLS (Row-Level Security) para multi-tenancy

### 2. DbContext por Módulo

| Regra | Detalhe |
|-------|---------|
| 1 DbContext principal por módulo | Obrigatório |
| Subdomínios com DbContext próprio | Permitido quando justificado (ex: AI Knowledge com 3, OpIntel com 5, Change Gov com 4) |
| Cada DbContext = conjunto próprio de migrations | Obrigatório |
| Herança de NexTraceDbContextBase | Obrigatório (fornece RLS, Audit, Outbox, Encryption) |

### 3. Política de Prefixos

| Módulo | Prefixo | Formato |
|--------|---------|---------|
| Identity & Access | `iam_` | `iam_users`, `iam_roles`, `iam_sessions` |
| Environment Management | `env_` | `env_environments`, `env_access_rules` |
| Service Catalog | `cat_` | `cat_services`, `cat_dependencies` |
| Contracts | `ctr_` | `ctr_api_contracts`, `ctr_versions` |
| Change Governance | `chg_` | `chg_change_records`, `chg_promotions` |
| Operational Intelligence | `ops_` | `ops_incidents`, `ops_runbooks` |
| AI & Knowledge | `aik_` | `aik_models`, `aik_agents` |
| Governance | `gov_` | `gov_compliance_reports` |
| Configuration | `cfg_` | `cfg_definitions`, `cfg_entries` |
| Audit & Compliance | `aud_` | `aud_events`, `aud_campaigns` |
| Notifications | `ntf_` | `ntf_notifications`, `ntf_preferences` |
| Integrations | `int_` | `int_connectors`, `int_sources` |
| Product Analytics | `pan_` | `pan_event_definitions` |

**Regra de naming:** `{prefix}_{snake_case_table_name}`

**Outbox tables:** `{prefix}outbox_messages` (ex: `iam_outbox_messages`)

---

## Políticas Transversais

### A. Tabelas Compartilhadas e Referências Cruzadas

**Regra:** Não existem tabelas compartilhadas entre módulos no PostgreSQL.

| Cenário | Solução |
|---------|---------|
| Módulo A precisa de dados do Módulo B | Referência lógica via ID (sem FK física) |
| Consultas cross-module | Via Application layer (CQRS query que chama ambos os DbContexts) |
| Dados de referência (ex: TenantId, UserId) | Armazenar apenas o ID; resolver nome/detalhes via query ao módulo dono |

**Excepção:** Coluna `TenantId` é transversal mas não implica FK cross-module. Cada módulo armazena `TenantId` como `Guid` sem FK para `iam_tenants`.

### B. Foreign Keys entre Módulos

**Regra:** FKs entre módulos são **proibidas** a nível de banco de dados.

| Tipo de FK | Permitido? | Alternativa |
|-----------|-----------|------------|
| FK dentro do mesmo módulo | ✅ Sim | — |
| FK dentro do mesmo DbContext | ✅ Sim | — |
| FK entre DbContexts do mesmo módulo | ⚠️ Caso a caso | Preferir referência lógica |
| FK entre módulos diferentes | ❌ Não | Referência lógica (store ID, sem constraint) |

### C. Colunas Transversais Obrigatórias

Todas as entidades persistidas devem incluir:

| Coluna | Tipo | Obrigatório | Notas |
|--------|------|------------|-------|
| `Id` | `uuid` | ✅ | PK, gerado client-side ou via `gen_random_uuid()` |
| `TenantId` | `uuid` | ✅ | Obrigatório em todas as tabelas com RLS |
| `CreatedAt` | `timestamptz` | ✅ | UTC, set on insert |
| `UpdatedAt` | `timestamptz` | ✅ | UTC, updated on every write |
| `CreatedBy` | `uuid` | ✅ | UserId de quem criou |
| `UpdatedBy` | `uuid` | ✅ | UserId de quem atualizou |
| `xmin` | `xid` | ✅ | RowVersion para concurrency (PostgreSQL system column) |

**Colunas opcionais:**

| Coluna | Tipo | Quando usar |
|--------|------|------------|
| `EnvironmentId` | `uuid` | Quando a entidade é scoped por ambiente |
| `IsDeleted` | `boolean` | Se soft-delete for necessário |
| `DeletedAt` | `timestamptz` | Se soft-delete com timestamp |

### D. Política de RowVersion

- Usar `xmin` do PostgreSQL como concurrency token
- Configurar em cada EntityTypeConfiguration:
  ```csharp
  builder.UseXminAsConcurrencyToken();
  ```
- Obrigatório para todas as entidades que suportam update/delete

### E. Política de TenantId

- **Todas** as tabelas devem ter `TenantId` (excepto tabelas de referência global como `iam_permissions`)
- RLS policy filtra automaticamente por `TenantId` via `NexTraceDbContextBase`
- Tabelas que NÃO precisam de TenantId:
  - `iam_permissions` (catálogo global)
  - `cfg_definitions` (definições globais do sistema)
  - `env_environments` (ambientes são per-tenant mas a tabela principal é multi-tenant com TenantId)

### F. Política de EnvironmentId

- Tabelas que necessitam scoping por ambiente devem incluir `EnvironmentId`
- EnvironmentId é uma **referência lógica** (sem FK para `env_environments`)
- Módulos que usam EnvironmentId:
  - Identity & Access (iam_sessions pode ter environment context)
  - Operational Intelligence (incidents scoped por ambiente)
  - Change Governance (changes scoped por ambiente)
  - Catalog (services podem ter environment bindings)

---

## Estratégia de Geração da Nova Baseline

### Passo 1 — Preparar o DbContext

1. Confirmar que todas as `EntityTypeConfiguration<T>` usam o prefixo correto
2. Confirmar que `UseXminAsConcurrencyToken()` está configurado
3. Confirmar que colunas de auditoria estão mapeadas
4. Confirmar que TenantId está presente
5. Remover qualquer `HasData()` (seeds passam para seeder explícito)

### Passo 2 — Gerar Migration Baseline

```bash
cd src/modules/{module}/NexTraceOne.{Module}.Infrastructure
dotnet ef migrations add InitialBaseline --context {DbContextName}
```

### Passo 3 — Validar Migration Gerada

1. Inspecionar o ficheiro `.cs` gerado
2. Confirmar nomes de tabelas com prefixo correto
3. Confirmar PKs, FKs, índices
4. Confirmar constraints de unicidade
5. Confirmar ausência de `HasData()`
6. Confirmar outbox table com prefixo correto

### Passo 4 — Aplicar e Validar

```bash
dotnet ef database update --context {DbContextName}
# Comparar schema gerado com modelo esperado
```

### Passo 5 — Executar Seeds

```bash
# Via application startup ou CLI dedicado
dotnet run --seed {module}
```

---

## Política de Seeds na Nova Baseline

| Aspecto | Regra |
|---------|-------|
| Seeds em HasData() | ❌ Proibido na nova baseline |
| Seeds em migrations | ❌ Proibido |
| Seeds em seeders programáticos | ✅ Obrigatório |
| Seeds idempotentes | ✅ Obrigatório (re-executáveis sem duplicação) |
| Seeds por environment (dev/staging/prod) | ✅ Separados por flag |

---

## Política de Índices

| Tipo | Regra |
|------|-------|
| PK | UUID para todas as entidades |
| FK intra-módulo | Com índice automático |
| Índice em TenantId | Obrigatório (RLS performance) |
| Índice composto TenantId + campo | Quando queries frequentes filtram por tenant + outro campo |
| Índice de unicidade | Onde regras de negócio exigem (ex: email único por tenant) |
| Índice full-text | Caso a caso, documentar justificação |

---

## Sumário de Tabelas por Módulo (Nova Baseline)

| Módulo | DbContexts | Tabelas Aprox. | Outbox |
|--------|-----------|---------------|--------|
| Configuration | 1 | 4 | `cfg_outbox_messages` |
| Identity & Access | 1 | 17 | `iam_outbox_messages` |
| Environment Management | 1 | 5-7 | `env_outbox_messages` |
| Service Catalog | 2 | 14 | `cat_outbox_messages`, `dp_outbox_messages` |
| Contracts | 1 | 13 | `ctr_outbox_messages` |
| Change Governance | 4 | 26 | `chg_*_outbox_messages` |
| Notifications | 1 | 3-5 | `ntf_outbox_messages` |
| Operational Intelligence | 5 | 19 | `ops_*_outbox_messages` |
| Audit & Compliance | 1 | 6 | `aud_outbox_messages` |
| Governance | 1 | 12 | `gov_outbox_messages` |
| Integrations | 1 | 3-5 | `int_outbox_messages` |
| Product Analytics | 1 | 2-3 | `pan_outbox_messages` |
| AI & Knowledge | 3 | 27+ | `aik_*_outbox_messages` |
| **TOTAL** | **23** | **~156** | 23 outbox tables |
