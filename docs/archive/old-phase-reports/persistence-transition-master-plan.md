# Plano Mestre de Transição da Persistência

> **Status:** DRAFT
> **Data:** 2026-03-25
> **Fase:** N15 — Estratégia de Transição de Persistência
> **Versão:** 1.0

---

## 1. Resumo Executivo

### Objetivo

Transitar o NexTraceOne do estado actual de persistência (29 migrations legadas em 20 DbContexts, prefixos inconsistentes, seeds em HasData(), 3 módulos sem backend próprio) para uma nova baseline limpa, previsível e sustentável, com PostgreSQL como banco transacional e ClickHouse como banco analítico complementar.

### Princípios

1. **Um único banco PostgreSQL** com isolamento por prefixos e DbContexts
2. **1 DbContext por módulo** (subdomínios podem ter DbContexts adicionais)
3. **Prefixo obrigatório** em todas as tabelas (`iam_`, `env_`, `cat_`, etc.)
4. **Seeds programáticos** — nunca em HasData() ou migrations
5. **ClickHouse complementar** — apenas para dados analíticos de alto volume
6. **Zero EnsureCreated** — schema sempre via migrations
7. **Ordem de ondas** — fundação primeiro, módulos dependentes depois

### Estratégia Geral

```
Fase 0: Pré-condições (extrações, limpezas, seeders)
    ↓
Fase 1: Remoção das migrations antigas (por onda)
    ↓
Fase 2: Nova baseline PostgreSQL (por onda)
    ↓
Fase 3: Baseline ClickHouse (Product Analytics primeiro)
    ↓
Fase 4: Validação completa
```

---

## 2. Pré-condições

> Referência: `migration-removal-prerequisites.md`

### Pré-condições Obrigatórias (Bloqueantes)

| # | Pré-condição | Responsável | Estado |
|---|-------------|-----------|--------|
| P-01 | Extrair Contracts de Catalog para `src/modules/contracts/` (OI-01) | Backend team | ❌ Pendente |
| P-02 | Extrair Integrations de Governance para `src/modules/integrations/` (OI-02) | Backend team | ❌ Pendente |
| P-03 | Extrair Product Analytics de Governance para `src/modules/productanalytics/` (OI-03) | Backend team | ❌ Pendente |
| P-04 | Criar backend Environment Management em `src/modules/environmentmanagement/` (OI-04) | Backend team | ❌ Pendente |
| P-05 | Corrigir prefixos: `identity_`→`iam_`, `oi_`→`ops_`, `ct_`→`ctr_` em Configurations | Backend team | ❌ Pendente |
| P-06 | Remover 17 permissões licensing de seeds e RolePermissionCatalog (OI-05) | Backend team | ❌ Pendente |
| P-07 | Extrair HasData() seeds para seeders programáticos (Identity, Governance) | Backend team | ❌ Pendente |
| P-08 | Registar permissões `notifications:*` no RolePermissionCatalog | Backend team | ❌ Pendente |
| P-09 | Tag do repositório `pre-migration-reset-v1` | DevOps | ❌ Pendente |
| P-10 | Export DDL snapshot do schema actual | DevOps | ❌ Pendente |

### Pré-condições Recomendadas (Não-Bloqueantes)

| # | Pré-condição | Estado |
|---|-------------|--------|
| P-11 | MFA enforcement implementado no Identity module | ❌ Pendente |
| P-12 | API Key authentication implementada | ❌ Pendente |
| P-13 | RowVersion (xmin) configurado em todas as entidades | ❌ Pendente |
| P-14 | Colunas de auditoria completas em todas as entidades | ⚠️ Parcial |

---

## 3. Ordem de Execução

> Referência: `postgresql-baseline-execution-order.md`

### Onda 0 — Pré-requisitos (não gera migrations)

| Tarefa | Sprints |
|--------|---------|
| Extrações OI-01 a OI-04 | 2-3 |
| Correcção de prefixos | 1 |
| Criação de seeders programáticos | 1 |
| Limpeza de licensing | 0.5 |
| **Total Onda 0** | **3-4** |

### Onda 1 — Fundação

| Módulo | DbContexts | Prefixo | Sprints |
|--------|-----------|---------|---------|
| Configuration | 1 | `cfg_` | 0.5 |
| Identity & Access | 1 | `iam_` | 1.5 |
| Environment Management | 1 | `env_` | 1 |
| **Total Onda 1** | **3** | — | **2-3** |

### Onda 2 — Núcleo de Ativos

| Módulo | DbContexts | Prefixo | Sprints |
|--------|-----------|---------|---------|
| Service Catalog | 2 | `cat_`, `dp_` | 1 |
| Contracts | 1 | `ctr_` | 1 |
| **Total Onda 2** | **3** | — | **2** |

### Onda 3 — Change e Operação

| Módulo | DbContexts | Prefixo | Sprints |
|--------|-----------|---------|---------|
| Change Governance | 4 | `chg_` | 1.5 |
| Notifications | 1 | `ntf_` | 1 |
| Operational Intelligence | 5 | `ops_` | 2 |
| **Total Onda 3** | **10** | — | **3** |

### Onda 4 — Rastreabilidade

| Módulo | DbContexts | Prefixo | Sprints |
|--------|-----------|---------|---------|
| Audit & Compliance | 1 | `aud_` | 1 |
| Governance | 1 | `gov_` | 1 |
| **Total Onda 4** | **2** | — | **2** |

### Onda 5 — Integrações + Analytics

| Módulo | DbContexts | Prefixo | ClickHouse | Sprints |
|--------|-----------|---------|-----------|---------|
| Integrations | 1 | `int_` | RECOMMENDED | 1.5 |
| Product Analytics | 1 | `pan_` | REQUIRED | 2 |
| **Total Onda 5** | **2** | — | — | **3** |

### Onda 6 — IA

| Módulo | DbContexts | Prefixo | Sprints |
|--------|-----------|---------|---------|
| AI & Knowledge | 3 | `aik_` | 3-4 |
| **Total Onda 6** | **3** | — | **3-4** |

### Timeline Total

| Item | Sprints |
|------|---------|
| Onda 0 | 3-4 |
| Onda 1 | 2-3 |
| Onda 2 | 2 |
| Onda 3 | 3 |
| Onda 4 | 2 |
| Onda 5 | 3 |
| Onda 6 | 3-4 |
| **TOTAL** | **18-21** |

> **Nota:** Ondas 2+3 podem executar em paralelo. Onda 6 pode começar em paralelo com Onda 4/5. Timeline mínima com paralelismo: **~14 sprints (~28 semanas)**.

---

## 4. Estratégia PostgreSQL

> Referência: `new-postgresql-baseline-strategy.md`

### Resumo

- **1 banco físico** PostgreSQL
- **23 DbContexts** totais (alguns módulos com subdomínios)
- **~156 tabelas** com prefixo obrigatório
- **23 outbox tables** (1 por DbContext)
- Colunas transversais: `Id` (UUID), `TenantId`, `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`, `xmin`
- Zero FK cross-module (referência lógica por ID)
- RLS via NexTraceDbContextBase

### Procedimento por Módulo

1. Remover pasta Migrations/ + ModelSnapshot
2. Confirmar Configurations com prefixo correcto
3. Configurar xmin, auditoria, TenantId
4. Gerar nova InitialBaseline migration
5. Validar schema gerado
6. Executar seeds
7. Smoke test

---

## 5. Estratégia ClickHouse

> Referência: `clickhouse-baseline-strategy.md`

### Resumo

- **REQUIRED:** Product Analytics (5 tabelas, Fase 1)
- **RECOMMENDED:** OpIntel (3 tabelas), Integrations (2 tabelas), Governance (2 tabelas) — Fase 2
- **OPTIONAL_LATER:** AI Knowledge, Catalog, Change Gov, Audit — Fase 3+
- **NONE:** Identity, Environment, Contracts, Configuration, Notifications

### Fase 1 (com Onda 5)

| Tabela | Engine | Módulo |
|--------|--------|--------|
| `pan_events` | MergeTree | Product Analytics |
| `pan_daily_module_stats` | SummingMergeTree | Product Analytics |
| `pan_daily_persona_stats` | SummingMergeTree | Product Analytics |
| `pan_daily_friction_stats` | SummingMergeTree | Product Analytics |
| `pan_session_summaries` | AggregatingMergeTree | Product Analytics |

### Ingestão

- Via Outbox (domain events → event bus → ClickHouse writer)
- Via Direct Write (runtime metrics)
- Via OpenTelemetry Collector (infra telemetry)

---

## 6. Estratégia de Seeds

> Referência: `module-seed-strategy.md`

### Ordem de Seeds

1. Configuration (~345 definitions)
2. Identity & Access (7 roles, 73+ permissions, 1 tenant)
3. Environment Management (3 ambientes)
4. Restantes módulos conforme necessidade

### Regras

- Seeds programáticos (seeder classes), nunca HasData()
- Idempotentes (re-executáveis)
- Separação PROD vs DEV
- Zero permissões licensing

---

## 7. Estratégia de Validação

> Referência: `new-baseline-validation-strategy.md`

### Checklist por Onda

| Validação | Critério |
|-----------|---------|
| Criação limpa PostgreSQL | 100% pass |
| Prefixos correctos | 100% pass |
| Seeds aplicados | 100% pass |
| Startup sem erros | 100% pass |
| Login funciona | 100% pass |
| CRUD básico funciona | 90% pass |
| Zero EnsureCreated | 100% pass |

---

## 8. Riscos e Mitigação

> Referência: `migration-transition-risks-and-mitigations.md`

### Top 5 Riscos

| # | Risco | Score | Mitigação Principal |
|---|-------|-------|-------------------|
| R-04 | Fronteiras entre módulos quebradas | 🔴 CRÍTICO | Extrair OI-01 a OI-04 na Onda 0 |
| R-10 | Perda de seed data implícito | 🔴 CRÍTICO | Extrair HasData() antes de remover migrations |
| R-03 | Resíduos de Licensing | 🟠 ALTO | Limpar licensing antes da Onda 1 |
| R-12 | CI/CD quebra | 🟠 ALTO | Actualizar pipelines na mesma PR |
| R-01 | Apagar cedo demais | 🟠 ALTO | Checklist de pré-condições formal |

---

## 9. Critérios de Pronto para Execução Real

### Quando sair do papel para implementação

A execução real pode começar quando:

| # | Critério | Estado |
|---|---------|--------|
| 1 | Todas as pré-condições P-01 a P-10 satisfeitas | ❌ |
| 2 | Tag `pre-migration-reset-v1` criada | ❌ |
| 3 | DDL snapshot exportado | ❌ |
| 4 | Seeders programáticos implementados e testados | ❌ |
| 5 | CI/CD pipeline preparada para novo workflow | ❌ |
| 6 | Equipa alinhada com o plano de ondas | ❌ |
| 7 | Ambiente de teste isolado disponível | ❌ |
| 8 | Este plano revisado e aprovado pela equipa | ❌ |

### Quando NÃO iniciar

- Se pré-condições P-01 a P-04 (extrações) não estiverem resolvidas
- Se seeders não estiverem prontos e testados
- Se não houver ambiente de teste isolado
- Se o plano não tiver sido revisado pela equipa

---

## Anexo: Mapa de Ficheiros Produzidos (N15)

| # | Ficheiro | Conteúdo |
|---|---------|---------|
| 1 | `migration-readiness-by-module.md` | Readiness de cada módulo para baseline |
| 2 | `migration-removal-prerequisites.md` | Checklist de pré-condições |
| 3 | `postgresql-baseline-execution-order.md` | Ordem de ondas |
| 4 | `legacy-migrations-removal-strategy.md` | Como remover migrations antigas |
| 5 | `new-postgresql-baseline-strategy.md` | Política da nova baseline |
| 6 | `module-seed-strategy.md` | Seeds por módulo |
| 7 | `clickhouse-baseline-strategy.md` | Estratégia ClickHouse |
| 8 | `final-data-placement-matrix.md` | PostgreSQL vs ClickHouse por módulo |
| 9 | `new-baseline-validation-strategy.md` | Estratégia de validação |
| 10 | `migration-transition-risks-and-mitigations.md` | Riscos e mitigação |
| 11 | `persistence-transition-master-plan.md` | Este documento — plano mestre |

---

## Histórico

| Data | Versão | Alteração |
|------|--------|-----------|
| 2026-03-25 | 1.0 | Versão inicial — N15 completo |
