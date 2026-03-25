# Ordem Oficial de Recriação da Baseline PostgreSQL

> **Status:** DRAFT
> **Data:** 2026-03-25
> **Fase:** N15 — Estratégia de Transição de Persistência

---

## Objetivo

Definir a sequência oficial em que os módulos devem entrar na nova baseline PostgreSQL, organizada em ondas, com justificação baseada em dependências, maturidade e risco.

---

## Princípios de Ordenação

1. **Módulos fundacionais primeiro** — outros módulos dependem deles
2. **Menor risco de retrabalho** — módulos mais maduros primeiro
3. **Dependências respeitadas** — um módulo só entra após os que consome
4. **Extrações antes de baselines** — módulos dentro de outros devem ser extraídos primeiro
5. **ClickHouse em paralelo** — módulos com ClickHouse REQUIRED entram no fim

---

## Mapa de Dependências

```
Configuration ← (todos os módulos consomem)
Identity & Access ← (todos os módulos consomem autenticação/autorização)
Environment Management ← (Identity consome; outros módulos consomem ambiente)
  └─ Catalog ← Change Governance, Operational Intelligence
      └─ Contracts ← Change Governance
  └─ Governance ← (relatórios, compliance)
  └─ Audit & Compliance ← (todos publicam eventos de auditoria)
  └─ Notifications ← (todos publicam notificações)
  └─ Change Governance ← Operational Intelligence
  └─ Operational Intelligence
  └─ AI & Knowledge
  └─ Integrations
  └─ Product Analytics
```

---

## Sequência em Ondas

### 🟢 Onda 0 — Pré-requisitos (não gera migrations)

| Tarefa | Descrição | Bloqueadores |
|--------|-----------|-------------|
| Extração OI-01 | Mover Contracts para `src/modules/contracts/` com `ContractsDbContext` dedicado | Catalog depende |
| Extração OI-02 | Mover Integrations para `src/modules/integrations/` com `IntegrationsDbContext` dedicado | Governance depende |
| Extração OI-03 | Mover Product Analytics para `src/modules/productanalytics/` com `ProductAnalyticsDbContext` dedicado | Governance depende |
| Extração OI-04 | Criar `src/modules/environmentmanagement/` com `EnvironmentDbContext` dedicado | Identity depende |
| Remoção OI-05 | Limpar 17 permissões licensing de seeds e `RolePermissionCatalog` | Identity depende |
| Prefixos | Corrigir `identity_`→`iam_`, `oi_`→`ops_`, `ct_`→`ctr_` em Configurations | Todos dependem |

**Duração estimada:** 3-4 sprints

---

### 🟢 Onda 1 — Fundação (módulos que todos consomem)

| Ordem | Módulo | DbContext | Prefixo | Tabelas | Maturidade | Justificação |
|-------|--------|-----------|---------|---------|-----------|-------------|
| 1.1 | **Configuration** | `ConfigurationDbContext` | `cfg_` | 4 | ~90% | Sem dependências, seeds já formalizados (~345 defs), prefixo correto |
| 1.2 | **Identity & Access** | `IdentityDbContext` | `iam_` | 17 | 82% | Base de autenticação/autorização, RBAC com 73+ permissões, 7 roles |
| 1.3 | **Environment Management** | `EnvironmentDbContext` | `env_` | 5-7 | 40%→60% | Dimensão de autorização consumida por todos; requer extração prévia (Onda 0) |

**Validação Onda 1:**
- [ ] Aplicação sobe do zero
- [ ] Login funciona
- [ ] Tenant resolução funciona
- [ ] Ambiente resolução funciona
- [ ] Seeds aplicados corretamente
- [ ] Permissões e roles carregados

**Duração estimada:** 2-3 sprints (após Onda 0)

---

### 🟡 Onda 2 — Núcleo de Ativos e Contratos

| Ordem | Módulo | DbContext | Prefixo | Tabelas | Maturidade | Justificação |
|-------|--------|-----------|---------|---------|-----------|-------------|
| 2.1 | **Service Catalog** | `CatalogGraphDbContext` + `DeveloperPortalDbContext` | `cat_`, `dp_` | 9+5 | ~75% | Catálogo de serviços, dependência do Change Governance |
| 2.2 | **Contracts** | `ContractsDbContext` | `ctr_` | 8+5 | ~65% | API/Event contracts; requer extração prévia (Onda 0) |

**Validação Onda 2:**
- [ ] Catálogo de serviços carrega
- [ ] Contratos CRUD funciona
- [ ] Dependências com Catalog resolvidas
- [ ] Prefixo `ctr_` aplicado (não `ct_`)

**Duração estimada:** 2 sprints

---

### 🟡 Onda 3 — Change, Operação e Notificações

| Ordem | Módulo | DbContext | Prefixo | Tabelas | Maturidade | Justificação |
|-------|--------|-----------|---------|---------|-----------|-------------|
| 3.1 | **Change Governance** | 4 DbContexts (`chg_`) | `chg_` | 26 | 81% | Alta maturidade, 4 subdomínios bem separados |
| 3.2 | **Notifications** | `NotificationsDbContext` | `ntf_` | 3-5 | 65% | 0 migrations atuais; necessita baseline from scratch |
| 3.3 | **Operational Intelligence** | 5 DbContexts (`ops_`) | `ops_` | 19 | 55% | 5 subdomínios; requer correção prefixo `oi_`→`ops_` |

**Validação Onda 3:**
- [ ] Incidentes CRUD funciona
- [ ] Notificações persistem (baseline nova, não 0 migrations)
- [ ] Change intelligence funcional
- [ ] Prefixo `ops_` aplicado (não `oi_`)

**Duração estimada:** 3 sprints

---

### 🟠 Onda 4 — Rastreabilidade e Governance

| Ordem | Módulo | DbContext | Prefixo | Tabelas | Maturidade | Justificação |
|-------|--------|-----------|---------|---------|-----------|-------------|
| 4.1 | **Audit & Compliance** | `AuditDbContext` | `aud_` | 6 | 53% | Trilha de auditoria necessária para compliance |
| 4.2 | **Governance** | `GovernanceDbContext` | `gov_` | 12 | ~70% | Governance central; requer extrações prévias (Onda 0) |

**Validação Onda 4:**
- [ ] Eventos de auditoria persistem
- [ ] Governance reports funcional
- [ ] GovernanceDbContext limpo (sem Integrations/Product Analytics)

**Duração estimada:** 2 sprints

---

### 🟠 Onda 5 — Integrações e Analytics

| Ordem | Módulo | DbContext | Prefixo | Tabelas (PG) | ClickHouse | Maturidade | Justificação |
|-------|--------|-----------|---------|-------------|-----------|-----------|-------------|
| 5.1 | **Integrations** | `IntegrationsDbContext` | `int_` | 3-5 | RECOMMENDED | 45% | Requer extração prévia; ClickHouse opcional nesta fase |
| 5.2 | **Product Analytics** | `ProductAnalyticsDbContext` | `pan_` | 2-3 (config) | REQUIRED | 30% | Requer extração + ClickHouse; menor maturidade |

**Validação Onda 5:**
- [ ] Integrações CRUD funciona
- [ ] Product Analytics persiste no PostgreSQL (config tables)
- [ ] ClickHouse tables criadas (Product Analytics)

**Duração estimada:** 3 sprints

---

### 🔴 Onda 6 — IA (maior complexidade e menor maturidade)

| Ordem | Módulo | DbContext | Prefixo | Tabelas | Maturidade | Justificação |
|-------|--------|-----------|---------|---------|-----------|-------------|
| 6.1 | **AI & Knowledge** | 3 DbContexts (`aik_`) | `aik_` | 27+ | 25% | Menor maturidade, 3 DbContexts, tools não executam (CR-2), 9 migrations existentes |

**Validação Onda 6:**
- [ ] AI Governance persiste
- [ ] Chat/Assistant funcional
- [ ] Orchestration persiste
- [ ] External AI config persiste

**Duração estimada:** 3-4 sprints

---

## Timeline Consolidada

| Onda | Módulos | Sprints | Dependências |
|------|---------|---------|-------------|
| 0 | Extrações + correções prefixo | 3-4 | Nenhuma |
| 1 | Configuration, Identity, Environment | 2-3 | Onda 0 |
| 2 | Catalog, Contracts | 2 | Onda 1 |
| 3 | Change Gov, Notifications, OpIntel | 3 | Onda 1 |
| 4 | Audit, Governance | 2 | Onda 3 |
| 5 | Integrations, Product Analytics | 3 | Onda 0, 4 |
| 6 | AI & Knowledge | 3-4 | Onda 1 |
| **TOTAL** | **13 módulos** | **18-21 sprints** | — |

---

## Notas

1. Ondas 2 e 3 podem executar em **paralelo** pois não possuem dependências entre si.
2. Onda 6 pode começar em paralelo com Onda 4/5 se a equipa de IA estiver disponível.
3. A timeline assume 1 sprint = 2 semanas, equipa dedicada.
4. Cada onda inclui: remoção migrations antigas → nova baseline → validação → seeds → smoke test.
