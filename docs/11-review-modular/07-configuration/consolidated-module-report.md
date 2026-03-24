# Configuration — Consolidated Module Report

> Gerado a partir da consolidação de todos os relatórios de auditoria e revisão modular do NexTraceOne.
> Última atualização: 2026-03-24

---

## 1. Visão Geral do Módulo

O módulo **Configuration** é o módulo transversal que centraliza todas as definições de configuração da plataforma NexTraceOne. Cada feature do produto depende de configurações provenientes deste módulo, tornando-o um pilar silencioso mas crítico para o funcionamento global.

| Atributo | Valor |
|----------|-------|
| Prioridade | P3 (Módulo Transversal) |
| Maturidade global | **77%** |
| Backend | `src/modules/configuration/` |
| Frontend | `src/frontend/src/features/configuration/` |
| Base de dados | `nextraceone_operations` (partilhada com 11 outros DbContexts) |
| DbContext | `ConfigurationDbContext` (3 DbSets) |
| Criticidade | MÉDIA — mas bloqueia todos os módulos (configuração é transversal) |
| Tipo de correção | LOCAL_FIX |
| Alvo de maturidade | 85%+ |

### Maturidade por Dimensão

| Dimensão | Estado | Nota |
|----------|--------|------|
| Backend | 🟢 95% | CQRS completo, handlers funcionais, seeder robusto |
| Frontend | 🟢 90% | Interface robusta, advanced console com 6 tabs |
| Documentação | 🔴 30% | 35 ficheiros fragmentados, sem README, sem doc unificada |
| Testes | 🟢 95% | 251 backend + 82 frontend — melhor cobertura da plataforma |

---

## 2. Estado Atual

### 2.1 Definições de Configuração (~345 definições em 8 fases)

| Fase | Domínio | Definições | Key Prefix | SortOrder Range |
|------|---------|-----------|------------|-----------------|
| Phase 0 | Foundation (instance) | ~5 | `instance.*` | 1–10 |
| Phase 1 | Foundation (feature flags, policies) | ~10 | `instance.*`, `policies.*` | 10–50 |
| Phase 2 | Notifications | 38 | `notifications.*` | 150–201 |
| Phase 3 | Workflow & Promotion | 45 | `workflow.*`, `promotion.*` | 2000–2650 |
| Phase 4 | Governance & Compliance | 44 | `governance.*` | 3000–3540 |
| Phase 5 | Catalog, Contracts & Change | 49 | `catalog.*`, `change.*` | 4000–4690 |
| Phase 6 | Operations, Incidents, FinOps | 53 | `incidents.*`, `operations.*`, `finops.*`, `benchmarking.*` | 5000–5620 |
| Phase 7 | AI & Integrations | 55 | `ai.*`, `integrations.*` | 6000–6670 |

Cada definição possui: `Key`, `DisplayName`, `Description`, `Category` (System/Functional), `DataType`, `EditorType`, `DefaultValue`, `SortOrder`, `Scope` (Instance/Tenant/Environment), `IsInheritable`, `IsMandatory` e `ValidationRules` (JSON).

### 2.2 Pontos Fortes Confirmados

- **Backend CQRS sólido** — 7 features (GetDefinitions, GetEntries, GetEffectiveSettings, SetConfigurationValue, ToggleConfiguration, RemoveOverride, GetAuditHistory)
- **Seeder maduro** — `ConfigurationDefinitionSeeder` com 345+ definições em 8 fases, validado por 251 testes
- **Frontend avançado** — advanced console com 6 tabs (Explorer, Diff, Import/Export, Rollback, History, Health) e 9 filtros
- **Herança de configuração** — suporte a scopes Instance → Tenant → Environment com `IsInheritable`
- **Segurança transversal** — RLS (multi-tenancy), audit interceptor, soft deletes, encriptação de segredos
- **React Query factory** — hook `useConfiguration` para definitions, entries, effective settings e audit logs
- **Testes excelentes** — 251 backend (validam unique keys, unique sortOrders, categorias, prefixos, defaults, editors) + 82 frontend

---

## 3. Problemas Críticos e Bloqueadores

### 🔴 CR-01 — Schema sem migrações (usa `EnsureCreated`)

**Causa raiz:** CR-7 (Dívida técnica no schema de BD)

O `ConfigurationDbContext` utiliza `context.Database.EnsureCreated()` em vez de migrações EF Core formais. Este é o único problema de severidade crítica do módulo.

**Impacto concreto:**
- Impossível evoluir o schema incrementalmente — qualquer alteração requer drop/recreate
- Conflito potencial com migrações dos outros 11 DbContexts na mesma base `nextraceone_operations`
- Rollback controlado é impossível
- Se `ConfigurationEntry` (valores definidos por utilizador) contiver dados user-generated, a perda de dados em evolução de schema é inaceitável

**Entidades afetadas:** `ConfigurationDefinition` (AggregateRoot), `ConfigurationEntry`, `ConfigurationAuditEntry`

**Localização do problema:** `src/modules/configuration/Infrastructure/Persistence/ConfigurationDbContext.cs`

**Ação:** Criar migração `InitialCreate` formal e remover `EnsureCreated`. Referenciado como **W2-1** no plano de ondas de execução.

### 🟠 CR-02 — Documentação fragmentada e não navegável

**Causa raiz:** CR-2 (Documentação não acompanha o código)

Existem 35 ficheiros `execution/CONFIGURATION-*` sem documento unificado. O módulo não tem README. A documentação está a 30% — o pior indicador do módulo, num contraste gritante com backend (95%) e testes (95%).

**Impacto:** Onboarding impossível sem conhecimento tribal; contribuições ao módulo dependem de quem já o conhece.

---

## 4. Problemas por Camada

### 4.1 Frontend

| Severidade | Problema | Detalhe |
|-----------|---------|--------|
| 🟢 Baixo | Módulo pequeno (6 ficheiros) | Adequado ao escopo — não é um problema, apenas contexto |
| 🟢 Baixo | Apenas 2 páginas próprias | `ConfigurationAdminPage` e `AdvancedConfigurationConsolePage`; as 6 páginas distribuídas (Notifications, Workflows, Governance, Catalog/Contracts, Operations/FinOps, AI/Integrations) pertencem a outros módulos |

**Classificação frontend:** `COMPLETE_APPARENT` — sem problemas identificados.

**Rotas registadas e funcionais:**

| Página | Rota | Estado |
|--------|------|--------|
| ConfigurationAdminPage | `/platform/configuration` | ✅ Funcional |
| AdvancedConfigurationConsolePage | `/platform/configuration/advanced` | ✅ Funcional |

**Páginas distribuídas (pertencem a outros módulos mas consomem configuração):**

| Página | Rota | Feature de origem | Secções |
|--------|------|-------------------|---------|
| NotificationConfigurationPage | `/platform/configuration/notifications` | notifications | 6 secções |
| WorkflowConfigurationPage | `/platform/configuration/workflows` | change-governance | 7 secções |
| GovernanceConfigurationPage | `/platform/configuration/governance` | governance | 6 secções |
| CatalogContractsConfigurationPage | `/platform/configuration/catalog-contracts` | catalog | 7 secções |
| OperationsFinOpsConfigurationPage | `/platform/configuration/operations-finops` | operational-intelligence | 6 secções |
| AiIntegrationsConfigurationPage | `/platform/configuration/ai-integrations` | ai-hub | 6 secções |

**API frontend:** `configuration/api/configurationApi.ts` (1 ficheiro)

### 4.2 Backend

| Severidade | Problema | Detalhe |
|-----------|---------|--------|
| 🟡 Médio | Acoplamento com infraestrutura via `EnsureCreated` | Domínio classificado como ⚠️ PARCIAL no relatório de domínio por depender de mecanismo de infraestrutura para criação de schema |
| 🟢 Baixo | Modelo de domínio mínimo (6 entidades) | Adequado ao propósito — módulo de configuração não necessita modelo rico |

**Endpoints (5 via `ConfigurationEndpointModule`):**

| Rota | Método | Permissão | Rate Limit | Auth |
|------|--------|-----------|------------|------|
| `/api/configuration` | GET | `configuration:read` | Global | JWT |
| `/api/configuration` | PUT | `configuration:write` | Global | JWT |
| `/api/configuration/definitions` | GET | `configuration:read` | Global | JWT |
| `/api/configuration/phases` | GET | `configuration:read` | Global | JWT |
| `/api/configuration/reset` | POST | `configuration:write` | auth-sensitive | JWT |

**Features CQRS:** GetDefinitions, GetEntries, GetEffectiveSettings, SetConfigurationValue, ToggleConfiguration, RemoveOverride, GetAuditHistory.

### 4.3 Database

| Severidade | Problema | Detalhe |
|-----------|---------|--------|
| 🔴 Crítico | 0 migrações — `EnsureCreated` | Ver secção 3, CR-01 |
| 🟡 Médio | Sem RowVersion/ConcurrencyToken | Controlo de concorrência apenas via `UpdatedAt` app-level — risco de lost updates sob carga elevada (problema transversal a toda a plataforma, 0 entidades com RowVersion) |
| 🟡 Médio | Sem check constraints | Validação de domínio depende exclusivamente da camada aplicacional (problema transversal, 0 check constraints em toda a plataforma) |
| 🟢 Baixo | 12 DbContexts em `nextraceone_operations` | ConfigurationDb é um dos 12 DbContexts na mesma base — risco de contenção DDL e colisão de nomes outbox (problema transversal) |

**Infraestrutura de persistência (funcional):**

| Capacidade | Estado |
|-----------|--------|
| Multi-tenant RLS | ✅ `TenantRlsInterceptor` |
| Audit interceptor | ✅ CreatedAt/By, UpdatedAt/By automáticos |
| Soft deletes | ✅ `IsDeleted` + global query filter |
| Encriptação | ✅ AES-256-GCM para segredos de configuração |
| Outbox pattern | ✅ Domain events via outbox |
| Strongly-typed IDs | ✅ Via `TypedIdBase` |

**Seed data:**

| Seeder | Tipo | Estado |
|--------|------|--------|
| `ConfigurationDefinitionSeeder` | C# programático | ✅ 345+ definições em 8 fases, idempotente |
| SQL seed para Configuration | SQL | ❌ Não existe (seeder C# é suficiente) |

O `ConfigurationDefinitionSeeder` é executado via `DevelopmentSeedDataExtensions` apenas em ambiente Development. Para produção, o seeder precisa de ser adaptado para executar igualmente (as 345+ definições são necessárias em todos os ambientes).

### 4.4 Segurança

| Severidade | Problema | Detalhe |
|-----------|---------|--------|
| 🟡 Médio | Granularidade de permissões | Apenas 2 permissões (`configuration:read`, `configuration:write`). Não há separação entre leitura/escrita por domínio de configuração (ex: `configuration:ai:write` vs `configuration:governance:write`) |
| 🟢 Baixo | Endpoint `/api/configuration/reset` marcado como `auth-sensitive` | Correctamente protegido — apenas observação |

**Capacidades de segurança confirmadas:** JWT auth, rate limiting global, RLS por tenant, encriptação de segredos, audit trail automático, soft deletes.

### 4.5 IA e Agentes

Não aplicável. O módulo Configuration não tem componentes de IA. Contudo, a Phase 7 do seeder fornece 55 definições de configuração para o domínio AI (`ai.*`, `integrations.*`), o que é essencial para a governança de IA do produto.

### 4.6 Documentação

| Severidade | Problema | Detalhe |
|-----------|---------|--------|
| 🟠 Alto | 35 ficheiros fragmentados sem estrutura | Ficheiros `execution/CONFIGURATION-*` sem índice, sem navegação, sem consolidação |
| 🟠 Alto | README do módulo inexistente | Impossível entender o módulo sem ler código |
| 🟡 Médio | Sem documentação das ~345 definições | Cada definição tem `DisplayName` e `Description` no seeder, mas não há catálogo navegável externo |
| 🟡 Médio | Sem documentação do modelo de herança | Lógica Instance → Tenant → Environment com `IsInheritable` não está documentada |

---

## 5. Dependências

### Dependências de entrada (Configuration depende de)

| Módulo | Tipo | Detalhe |
|--------|------|--------|
| Identity & Access | Fundacional | Autenticação (JWT), permissões (`configuration:read/write`), tenant context (RLS) |

### Dependências de saída (módulos que dependem de Configuration)

| Módulo | Tipo | Detalhe |
|--------|------|--------|
| **Todos os 11 módulos** | Transversal | Todas as features do NexTraceOne consomem configuração deste módulo |

**Natureza transversal:** Este módulo é consumido por toda a plataforma. Qualquer instabilidade no schema, no seeder ou nos endpoints afeta diretamente todos os módulos. A ausência de migrações (CR-01) é particularmente arriscada num módulo com este nível de dependência.

```
Identity & Access  ──► Configuration  ──► Todos os módulos
   (auth, RLS)         (345+ defs)        (consumo transversal)
```

---

## 6. Quick Wins

| # | Ação | Esforço | Impacto | Ref. |
|---|------|---------|---------|------|
| QW-1 | Criar README mínimo do módulo Configuration (propósito, setup, endpoints, definições por fase) | 2–3h | Docs 30% → 40% | W1-4 |
| QW-2 | Gerar catálogo navegável das ~345 definições a partir do seeder (script automático ou export Markdown) | 3–4h | Documentação das definições acessível sem ler código C# | — |
| QW-3 | Documentar modelo de herança (Instance → Tenant → Environment) com diagrama | 1–2h | Compreensão rápida do mecanismo central do módulo | — |
| QW-4 | Validar que as 6 tabs do advanced console funcionam end-to-end (Explorer, Diff, Import/Export, Rollback, History, Health) | 2h | Confiança na funcionalidade mais complexa do módulo | Acção P1 #3 |

---

## 7. Refactors Estruturais

| # | Refactor | Esforço | Risco | Ref. |
|---|---------|---------|-------|------|
| SR-1 | **Criar migração `InitialCreate` para `ConfigurationDbContext` e remover `EnsureCreated`** | 1–2 dias | Médio — testar com BD limpa e BD com dados existentes | W2-1 |
| SR-2 | **Consolidar 35 ficheiros de documentação fragmentados** num README + sub-docs organizados por fase | 3–5 dias | Baixo | W2-5 |
| SR-3 | Adaptar `ConfigurationDefinitionSeeder` para execução em produção (não apenas Development) | 1–2 dias | Baixo-Médio — garantir idempotência em prod | — |
| SR-4 | Adicionar RowVersion/ConcurrencyToken em `ConfigurationDefinition` e `ConfigurationEntry` | 1–2 dias | Médio — pode causar conflitos em handlers existentes | W2-3 (parcial) |
| SR-5 | Avaliar granularização de permissões (`configuration:ai:write`, etc.) versus manter `configuration:read/write` simples | 1 dia (análise) | Baixo | — |

---

## 8. Critérios de Fecho

Critérios mínimos para considerar o módulo Configuration como "fechado" (maturidade ≥85%):

| # | Critério | Estado Atual | Alvo |
|---|---------|-------------|------|
| 1 | Migrações EF Core criadas e aplicáveis via `dotnet ef database update` | ❌ Usa `EnsureCreated` | ✅ Migração `InitialCreate` |
| 2 | Documentação consolidada e navegável | ❌ 35 ficheiros fragmentados | ✅ README + doc unificada por fase |
| 3 | README do módulo existente | ❌ Inexistente | ✅ README com setup, endpoints, modelo |
| 4 | Feature flags funcionais end-to-end | ⚠️ Não validado | ✅ Validado com teste |
| 5 | Herança de configuração validada (Instance → Tenant → Environment) | ⚠️ Não validado | ✅ Validado com teste |
| 6 | Advanced console validado (6 tabs, export masking, rollback) | ⚠️ Não validado | ✅ Validado com teste |
| 7 | Seeder funcional em produção (não apenas Development) | ⚠️ Apenas dev | ✅ Adaptado para prod |
| 8 | Testes ≥95% (manter) | ✅ 95% | ✅ Manter |
| 9 | Maturidade ≥85% | 🟡 77% | ✅ ≥85% |

---

## 9. Plano de Ação Priorizado

### Fase 1 — Imediato (Wave 1, Semana 1–2)

| # | Ação | Responsável | Esforço | Prioridade |
|---|------|------------|---------|-----------|
| 1 | Criar README do módulo Configuration | Doc writer | 2–3h | 🟠 P1 |

### Fase 2 — Backend/DB Críticos (Wave 2, Semana 3–5)

| # | Ação | Responsável | Esforço | Prioridade |
|---|------|------------|---------|-----------|
| 2 | **Criar migração `InitialCreate` e remover `EnsureCreated`** | Backend dev | 1–2 dias | 🔴 P2 (criticidade funcional alta) |
| 3 | **Consolidar 35 ficheiros de documentação** | Doc writer | 3–5 dias | 🟠 P2 |
| 4 | Adaptar seeder para produção | Backend dev | 1–2 dias | 🟠 P2 |
| 5 | Validar herança Instance → Tenant → Environment | QA / Backend dev | 2h | 🟡 P2 |
| 6 | Validar advanced console (6 tabs) | QA / Frontend dev | 2h | 🟡 P2 |

### Fase 3 — Melhoria Contínua (Wave 3+, Semana 6+)

| # | Ação | Responsável | Esforço | Prioridade |
|---|------|------------|---------|-----------|
| 7 | Gerar catálogo navegável das ~345 definições | Backend dev | 3–4h | 🟡 P3 |
| 8 | Documentar modelo de herança com diagrama | Doc writer | 1–2h | 🟡 P3 |
| 9 | Adicionar RowVersion em entidades de configuração | Backend dev | 1–2 dias | 🟢 P3 |
| 10 | Avaliar granularização de permissões | Architect | 1 dia | 🟢 P4 |
| 11 | Verificar cobertura de testes para todas as 8 fases | QA | 2h | 🟢 P3 |
| 12 | Avaliar unificação vs separação das 6 páginas distribuídas | Product / Architect | 1h | 🟢 P4 |

---

## 10. Inconsistências entre Relatórios

| # | Inconsistência | Relatório A | Relatório B | Análise |
|---|---------------|-------------|-------------|---------|
| 1 | **Número de definições de configuração** | module-review.md indica **~345 definições** | backend-migrations-and-seeds-report.md indica **600+ definições** e database-seeds-report.md indica **345+ definitions** | O valor ~345 refere-se às definições no seeder validadas por testes. O valor 600+ poderá incluir estimativas de fases futuras ou contabilizar definições + overrides. **Valor consolidado: ~345 definições confirmadas, organizadas em 8 fases.** |
| 2 | **Número de fases do seeder** | module-review.md descreve **8 fases (Phase 0–7)** com nomes específicos | database-seeds-report.md descreve **8 fases (1–8)** com nomes diferentes (Phase 1 = Notifications, etc.) | Os nomes e numeração diferem entre relatórios. O module-review.md (análise mais recente e detalhada) é a fonte mais fiável. |
| 3 | **Número de entidades** | backend-domain-report.md indica **6 entidades** (`ConfigurationDefinition`, `ConfigurationValue`, `ConfigurationPhase`) | module-review.md indica **3 entidades** (`ConfigurationDefinition`, `ConfigurationEntry`, `ConfigurationAuditEntry`) | Os nomes divergem entre relatórios. O module-review.md utiliza nomenclatura mais recente (Entry vs Value, AuditEntry). A contagem de 6 no domain-report pode incluir value objects. **O ConfigurationDbContext tem 3 DbSets confirmados.** |
| 4 | **Classificação de maturidade** | modular-review-summary.md classifica Configuration como **✅ Funcional** | module-consolidation-report.md classifica como **LOCAL_FIX** com criticidade **MÉDIA** | Não são contraditórios — o módulo é funcional mas necessita correções locais (migrações, docs). |
| 5 | **Mecanismo de schema** | database-structural-audit.md e database-migrations-report.md indicam `EnsureCreated` | Nenhum relatório menciona se `EnsureCreated` causa problemas em runtime actual | Pode funcionar sem problemas enquanto o schema não mudar, mas é um risco latente que se materializa na primeira evolução de schema. |

---

*Documento consolidado como parte da revisão modular do NexTraceOne. Todos os dados provêm de análise estática do código do repositório e dos relatórios de auditoria em `docs/11-review-modular/00-governance/`.*
