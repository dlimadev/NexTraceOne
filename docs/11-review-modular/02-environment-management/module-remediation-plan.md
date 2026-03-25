# Environment Management — Module Remediation Plan

> **Módulo:** 02 — Environment Management  
> **Data:** 2026-03-25  
> **Fase:** N4-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## A. Quick Wins

Alterações pequenas, de alto valor, que podem ser executadas imediatamente sem dependências externas.

| # | Item | Ficheiro(s) | Esforço | Impacto |
|---|------|-------------|---------|---------|
| QW-01 | **Adicionar sidebar entry** para `/environments` | Sidebar config em `App.tsx` ou menu definition | 1h | Página acessível via navegação — elimina bug de descobribilidade |
| QW-02 | **Adicionar guard `Deactivate()` para primary production** — impedir desactivação de ambiente primary | `Environment.cs` — method `Deactivate()` | 30min | Previne corrupção de dados: `if (IsPrimaryProduction) throw new InvalidOperationException()` |
| QW-03 | **Adicionar XML docs** a `Environment.cs`, `EnvironmentAccess.cs`, handlers críticos | Domain entities, CQRS handlers | 2h | Compreensibilidade do código |
| QW-04 | **Adicionar `UseXminAsConcurrencyToken()`** a `EnvironmentConfiguration.cs` e `EnvironmentAccessConfiguration.cs` | EF configs em `Infrastructure/Persistence/Configurations/` | 30min | Optimistic concurrency pronta |
| QW-05 | **Adicionar JSDoc** a `EnvironmentContext.tsx` | `src/frontend/src/contexts/EnvironmentContext.tsx` | 30min | Shell concern global documentado |
| QW-06 | **Criar Module README** provisório (referenciando localização no Identity) | `docs/modules/environment-management/README.md` (novo) | 2h | Onboarding viável |

**Total Quick Wins: ~6.5 horas**

---

## B. Functional Corrections (obrigatórias)

Correcções necessárias para o módulo ser considerado funcionalmente correcto.

### B.1 Backend — Endpoints ausentes

| # | Item | Ficheiro(s) | Esforço | Impacto |
|---|------|-------------|---------|---------|
| FC-01 | **Criar endpoint `GET /api/v1/environments/{id}`** — detalhe de um ambiente | `EnvironmentEndpoints.cs`, novo handler `GetEnvironment` | 2h | Suporta página de detalhe no frontend |
| FC-02 | **Criar endpoint `DELETE /api/v1/environments/{id}`** — soft-delete | `EnvironmentEndpoints.cs`, novo handler, adicionar `IsDeleted` ao domínio | 3h | Actualmente impossível eliminar ambientes |
| FC-03 | **Criar endpoint `POST /api/v1/environments/{id}/revoke-access`** | `EnvironmentEndpoints.cs`, novo handler | 2h | Método `Revoke()` existe no domínio, endpoint não |
| FC-04 | **Criar endpoint `GET /api/v1/environments/{id}/accesses`** — listar acessos | `EnvironmentEndpoints.cs`, novo handler | 2h | Gestão administrativa de acessos |

### B.2 Backend — Validações e error handling

| # | Item | Ficheiro(s) | Esforço | Impacto |
|---|------|-------------|---------|---------|
| FC-05 | **Validação de slug format** — alphanumeric + hyphens only | `CreateEnvironment` validator | 1h | Previne slugs inválidos na DB |
| FC-06 | **Slug uniqueness pre-check** — verificar antes de salvar, retornar `409 Conflict` | `CreateEnvironment` handler | 1h | Evita `DbUpdateException` não tratada |
| FC-07 | **Concurrency conflict handling** — capturar `DbUpdateConcurrencyException` após QW-04 | Todos os write handlers | 1h | Conflitos detectados e comunicados (409) |
| FC-08 | **FluentValidation para max lengths** — Name(100), Code(50), Region(100) | Create/Update validators | 1h | Validação antes de chegar ao DB |
| FC-09 | **Validar existência do utilizador** em `grant-access` | `GrantEnvironmentAccess` handler | 1h | Evita registos fantasma |
| FC-10 | **Validar sem acesso duplicado activo** em `grant-access` | `GrantEnvironmentAccess` handler | 1h | Evita duplicatas |

### B.3 Backend — Permissões

| # | Item | Ficheiro(s) | Esforço | Impacto |
|---|------|-------------|---------|---------|
| FC-11 | **Criar namespace `env:*`** — seed de permissões | Permission seeds, seeder | 2h | Namespace dedicado para o módulo |
| FC-12 | **Migrar endpoints para `env:*`** — substituir `identity:users:*` | `EnvironmentEndpoints.cs` | 1h | Permissões correctas por acção |
| FC-13 | **Escalar `set-primary-production` para `env:environments:admin`** | `EnvironmentEndpoints.cs` | 30min | Acção crítica com permissão adequada |
| FC-14 | **Escalar `grant-access` para `env:access:admin`** | `EnvironmentEndpoints.cs` | 30min | Segregação de privilégio |

### B.4 Backend — Audit

| # | Item | Ficheiro(s) | Esforço | Impacto |
|---|------|-------------|---------|---------|
| FC-15 | **Implementar domain events** — `EnvironmentCreated`, `PrimaryProductionDesignated`, `EnvironmentAccessGranted`, `EnvironmentAccessRevoked`, `EnvironmentDeactivated` | `Environment.cs`, `EnvironmentAccess.cs` | 4h | Audit trail granular e cross-module notification |
| FC-16 | **Publicar integration events** via outbox | Handlers, event definitions | 2h | Notificação a Audit, Notifications |

### B.5 Frontend

| # | Item | Ficheiro(s) | Esforço | Impacto |
|---|------|-------------|---------|---------|
| FC-17 | **Criar `EnvironmentDetailPage`** — overview, access control, actions | Nova página em `features/environment-management/pages/` | 8h | Funcionalidade core ausente |
| FC-18 | **Audit i18n** — extrair strings hardcoded de `EnvironmentsPage.tsx` | `EnvironmentsPage.tsx`, locales | 3h | i18n obrigatório por convenção |
| FC-19 | **Adicionar ~70 chaves i18n** em en, pt-BR, es | Ficheiros de locales | 3h | Completude i18n |
| FC-20 | **Mover API client** de `identity-access/api/identity.ts` para `environment-management/api/environments.ts` | Frontend API files | 2h | Separação de concerns |
| FC-21 | **Adicionar confirmation modal** para "Set Primary Production" | `EnvironmentsPage.tsx` | 1h | UX para acção crítica |
| FC-22 | **Adicionar confirmation modal** para "Delete Environment" | `EnvironmentsPage.tsx` | 1h | UX para acção destrutiva |
| FC-23 | **Tornar slug readonly** no edit form | Edit form component | 30min | Slug é imutável após criação |

**Total Functional Corrections: ~43.5 horas**

---

## C. Structural Adjustments

Alterações estruturais para alinhar com a arquitectura alvo.

| # | Item | Ficheiro(s) | Esforço | Impacto |
|---|------|-------------|---------|---------|
| SA-01 | **Mover `EnvironmentsPage`** de `features/identity-access/` para `features/environment-management/` | Frontend directory + imports | 2h | Feature no directório correcto |
| SA-02 | **Criar `EnvironmentAccessLevel` enum** — substituir `string` por enum tipado | `EnvironmentAccess.cs`, EF config | 2h | Type safety |
| SA-03 | **Criar `EnvironmentAccessId` strongly-typed** | `EnvironmentAccess.cs`, EF config | 1h | Consistência com padrão codebase |
| SA-04 | **Adicionar `IsDeleted`, `UpdatedAt`, `UpdatedBy`** ao aggregate `Environment` | `Environment.cs`, EF config | 2h | Tracking completo |
| SA-05 | **Adicionar FK real** `env_environment_accesses.environment_id → env_environments.id` | EF config | 30min | Referential integrity |
| SA-06 | **Adicionar check constraints** para Profile, Criticality, AccessLevel | EF configs | 1h | Data integrity na DB |
| SA-07 | **Adicionar índices de performance** (7+ novos) | EF configs | 1h | Query performance |
| SA-08 | **Actualizar partial unique index** — adicionar `is_deleted = false` ao filtro | EF config | 30min | Correctude com soft-delete |
| SA-09 | **Validar privilege escalation** em grant-access (grantor.level >= grantee.level) | `GrantEnvironmentAccess` handler | 1h | Segurança |

**Total Structural Adjustments: ~11 horas**

---

## D. Pre-conditions for Module Migration

Itens que devem ser completados antes de migrar Environment Management para módulo independente.

| # | Pré-condição | Dependências | Estado |
|---|-------------|-------------|--------|
| D-01 | **Domain model finalizado** — gaps DM-01 a DM-10 resolvidos | QW-02, QW-04, SA-02, SA-03, SA-04 | ⬜ Pendente |
| D-02 | **Persistence model finalizado** — colunas, constraints, índices | SA-05, SA-06, SA-07, SA-08 | ⬜ Pendente |
| D-03 | **Permissões `env:*` criadas** e atribuídas a roles | FC-11, FC-12, FC-13, FC-14 | ⬜ Pendente |
| D-04 | **Todos os endpoints existentes corrigidos** — validações, error handling | FC-05 a FC-10 | ⬜ Pendente |
| D-05 | **Domain events implementados** | FC-15, FC-16 | ⬜ Pendente |
| D-06 | **OI-04 resolvido** — capacidade de renomear tabelas para `env_` | Decisão arquitectural sobre migração de prefixo | ⬜ **BLOQUEADO** |
| D-07 | **`IEnvironmentAccessReader` interface definida** — para Identity consumir | Decisão sobre fronteira partilhada | ⬜ Pendente |
| D-08 | **Frontend feature directory criado** — `features/environment-management/` | FC-20, SA-01 | ⬜ Pendente |
| D-09 | **i18n completo** — 70+ chaves em en, pt-BR, es | FC-18, FC-19 | ⬜ Pendente |
| D-10 | **Testes adequados** — domain, application, integration | Após correcções | ⬜ Pendente |

### Sequência de desbloqueio

```
QW-01..QW-06 (paralelo)
    ↓
FC-05..FC-10 (validações, pode ser paralelo)
    ↓
FC-11..FC-14 (permissões, sequencial)
    ↓
FC-15..FC-16 (domain events)
    ↓
SA-01..SA-09 (estrutural, após functional)
    ↓
FC-01..FC-04 (novos endpoints)
    ↓
FC-17..FC-23 (frontend)
    ↓
D-01..D-10 (pré-condições verificadas)
    ↓
[MIGRAÇÃO PARA MÓDULO INDEPENDENTE]
    ↓
Resolver OI-04 (renomear tabelas para env_)
    ↓
Criar EnvironmentDbContext dedicado
    ↓
Remover DbSets do IdentityDbContext
```

---

## E. Acceptance Criteria

### E.1 Módulo mínimo funcional (Phase 1)

| # | Critério | Métrica | Estado |
|---|---------|---------|--------|
| AC-01 | CRUD completo de ambientes (criar, listar, detalhe, editar, eliminar) | 5 endpoints funcionais | ⬜ |
| AC-02 | Designação de primary production com permissão adequada | `env:environments:admin` enforced | ⬜ |
| AC-03 | Gestão de acessos completa (grant, revoke, list) | 3 endpoints + UI | ⬜ |
| AC-04 | Sidebar entry funcional | Página descobrível via navegação | ⬜ |
| AC-05 | Página de detalhe do ambiente | `EnvironmentDetailPage` implementada | ⬜ |
| AC-06 | Permissões `env:*` implementadas e enforced | Backend + frontend | ⬜ |
| AC-07 | Concurrency tokens activos | `UseXminAsConcurrencyToken()` + conflict handling | ⬜ |
| AC-08 | Domain events para acções críticas | 5+ events publicados | ⬜ |
| AC-09 | i18n completo (70+ chaves × 3 locales) | Sem strings hardcoded | ⬜ |
| AC-10 | Validações FluentValidation em todos os handlers | Slug format, max lengths, enum ranges | ⬜ |
| AC-11 | Error handling consistente | Error codes `ENV_*`, HTTP status correcto | ⬜ |
| AC-12 | Module README operacional | Developer pode começar a trabalhar com o README | ⬜ |

### E.2 Módulo independente (Phase 2 — após resolução OI-04)

| # | Critério | Métrica | Estado |
|---|---------|---------|--------|
| AC-13 | `EnvironmentDbContext` dedicado | DbContext no novo módulo | ⬜ |
| AC-14 | Tabelas com prefixo `env_` | `env_environments`, `env_environment_accesses` | ⬜ |
| AC-15 | Frontend em `features/environment-management/` | Directório dedicado | ⬜ |
| AC-16 | `EnvironmentAccess` lifecycle no Env Mgmt | Grant/Revoke no novo módulo | ⬜ |
| AC-17 | `IEnvironmentAccessReader` exposta | Identity consome via interface | ⬜ |
| AC-18 | Zero dependências directas de Identity.Domain | Apenas BuildingBlocks | ⬜ |
| AC-19 | FK real em `env_environment_accesses` | Referential integrity | ⬜ |
| AC-20 | Check constraints em todas as enums | Profile, Criticality, AccessLevel | ⬜ |

### E.3 Módulo com governança (Phase 3)

| # | Critério | Métrica | Estado |
|---|---------|---------|--------|
| AC-21 | `EnvironmentPolicy` persistida | Tabela `env_policies` + endpoints | ⬜ |
| AC-22 | `EnvironmentTelemetryPolicy` persistida | Tabela `env_telemetry_policies` + endpoints | ⬜ |
| AC-23 | `EnvironmentIntegrationBinding` persistida | Tabela `env_integration_bindings` + endpoints | ⬜ |
| AC-24 | Promotion paths definidos | UI + backend | ⬜ |
| AC-25 | Baseline management | Snapshot + drift detection | ⬜ |

---

## F. Execution Priority — Roadmap

### Sprint 1 — Quick Wins + Validações (1 semana)

| Dia | Itens | Esforço |
|-----|-------|---------|
| 1 | QW-01 (sidebar), QW-02 (deactivate guard), QW-04 (xmin) | 2h |
| 1 | FC-05 (slug format), FC-06 (slug uniqueness), FC-08 (max lengths) | 3h |
| 2 | FC-07 (concurrency handling), FC-09 (user exists), FC-10 (no duplicate access) | 3h |
| 2 | QW-03 (XML docs), QW-05 (JSDoc), QW-06 (README) | 5h |

### Sprint 2 — Permissões + Endpoints (1 semana)

| Dia | Itens | Esforço |
|-----|-------|---------|
| 1-2 | FC-11 (namespace env:*), FC-12 (migrar endpoints), FC-13, FC-14 | 4h |
| 2-3 | FC-01 (GET detail), FC-02 (DELETE), FC-03 (revoke), FC-04 (list accesses) | 9h |
| 4 | FC-15 (domain events) | 4h |
| 5 | FC-16 (integration events) | 2h |

### Sprint 3 — Frontend + Structural (1-2 semanas)

| Dia | Itens | Esforço |
|-----|-------|---------|
| 1-3 | FC-17 (EnvironmentDetailPage) | 8h |
| 3-4 | FC-18 (i18n audit), FC-19 (i18n keys), FC-20 (API client) | 8h |
| 4-5 | FC-21 (confirm primary), FC-22 (confirm delete), FC-23 (slug readonly) | 2.5h |
| 5-6 | SA-01 through SA-09 | 11h |

### Sprint 4 — Migration prep (dependente de OI-04)

| Itens | Esforço |
|-------|---------|
| D-01 through D-10 verificação | 4h |
| Criar `EnvironmentDbContext` | 4h |
| Migration de tabelas | 4h |
| Testes e verificação | 4h |

**Total geral estimado: ~77 horas (±10%)**
