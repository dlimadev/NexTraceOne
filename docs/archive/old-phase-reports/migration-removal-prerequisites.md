# Pré-condições para Remoção das Migrations Antigas

> **Status:** DRAFT
> **Data:** 2026-03-25
> **Fase:** N15 — Estratégia de Transição de Persistência

---

## Objetivo

Definir a checklist formal de pré-condições que devem estar satisfeitas antes de apagar qualquer migration antiga do repositório.

---

## Princípio Fundamental

> **Nenhuma migration antiga deve ser removida enquanto o modelo final do módulo não estiver completamente fechado, validado e documentado.**

---

## Checklist de Pré-condições

### A. Modelo de Domínio

| # | Pré-condição | Ficheiro de Referência | Estado |
|---|-------------|----------------------|--------|
| A-01 | Todas as entidades, aggregates, VOs e enums do módulo estão finalizados | `domain-model-finalization.md` por módulo | ⚠️ 13/13 finalizados em docs, código precisa validação |
| A-02 | Nenhuma entidade anémica sem justificação | `domain-model-finalization.md` | ⚠️ A validar |
| A-03 | Relações internas e externas mapeadas | `module-dependency-map.md` | ✅ Feito para todos os módulos |
| A-04 | Campos ausentes e indevidos identificados e corrigidos | `domain-model-finalization.md` | ⚠️ Identificados, correção pendente |

### B. Mapeamentos EF Core

| # | Pré-condição | Ficheiro de Referência | Estado |
|---|-------------|----------------------|--------|
| B-01 | Todas as entidades possuem Configuration EF correspondente | Configurations/ por módulo | ⚠️ Environment entities: 3/5 sem mapping |
| B-02 | PKs, FKs, índices e constraints definidos no modelo final | `persistence-model-finalization.md` | ✅ Desenhado para 13 módulos |
| B-03 | RowVersion (xmin) configurado em todas as entidades relevantes | Configurations/ | ⚠️ Ausente em vários módulos |
| B-04 | Colunas de auditoria (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) presentes | Configurations/ | ⚠️ Parcial |
| B-05 | TenantId configurado com RLS onde aplicável | DbContext base | ✅ NexTraceDbContextBase fornece |
| B-06 | EnvironmentId configurado onde aplicável | Configurations/ | ⚠️ Parcial |

### C. Prefixos de Tabela

| # | Pré-condição | Ficheiro de Referência | Estado |
|---|-------------|----------------------|--------|
| C-01 | Prefixo oficial confirmado e documentado para cada módulo | `database-table-prefixes.md` | ✅ 13 prefixos definidos |
| C-02 | Código dos Configurations usa prefixo correto | `.ToTable()` calls | ❌ 4+ módulos com prefixo errado |
| C-03 | Nomes finais de tabelas definidos no formato `prefix_snake_case` | `persistence-model-finalization.md` | ✅ Desenhado |

### D. Seeds

| # | Pré-condição | Ficheiro de Referência | Estado |
|---|-------------|----------------------|--------|
| D-01 | Seeds obrigatórios definidos por módulo | `module-seed-strategy.md` | ❌ A criar (este documento) |
| D-02 | Seeds implementados como idempotentes | Seeders/ ou HasData() | ⚠️ 3/13 módulos com seeds |
| D-03 | Ordem de aplicação de seeds entre módulos definida | `module-seed-strategy.md` | ❌ A definir |
| D-04 | Seeds de teste separados dos seeds de produção | Seeders/ | ⚠️ Parcial |

### E. Fronteiras entre Módulos

| # | Pré-condição | Ficheiro de Referência | Estado |
|---|-------------|----------------------|--------|
| E-01 | OI-01 resolvido: Contracts extraído de Catalog | `phase-a-open-items.md` | ❌ Pendente |
| E-02 | OI-02 resolvido: Integrations extraído de Governance | `phase-a-open-items.md` | ❌ Pendente |
| E-03 | OI-03 resolvido: Product Analytics extraído de Governance | `phase-a-open-items.md` | ❌ Pendente |
| E-04 | OI-04 resolvido: Environment Management com backend dedicado | `phase-a-open-items.md` | ❌ Pendente |
| E-05 | Cada módulo possui DbContext dedicado e isolado | Infrastructure/ | ⚠️ 4 módulos pendentes |

### F. EnsureCreated e Resíduos

| # | Pré-condição | Ficheiro de Referência | Estado |
|---|-------------|----------------------|--------|
| F-01 | Zero chamadas EnsureCreated no codebase | Global search | ✅ Confirmado: nenhuma encontrada |
| F-02 | Resíduos de Licensing removidos do modelo de persistência | `licensing-residue-cleanup-review.md` | ❌ 17 permissões licensing, seeds HasData com licensing |
| F-03 | Nenhum schema antigo remanescente em code ou config | Global review | ⚠️ A validar |

### G. Data Placement

| # | Pré-condição | Ficheiro de Referência | Estado |
|---|-------------|----------------------|--------|
| G-01 | Decisão PostgreSQL vs ClickHouse fechada para cada módulo | `module-data-placement-matrix.md` | ✅ Fechado para 13 módulos |
| G-02 | Nenhum dado analítico obrigatório em PostgreSQL sem justificação | `clickhouse-data-placement-review.md` | ✅ Revisto nos módulos RECOMMENDED/REQUIRED |
| G-03 | Esquema ClickHouse desenhado para módulos REQUIRED | `clickhouse-data-placement-review.md` | ⚠️ Product Analytics desenhado, outros parciais |

### H. Backlog Crítico

| # | Pré-condição | Ficheiro de Referência | Estado |
|---|-------------|----------------------|--------|
| H-01 | Bloqueadores P0 dos módulos fundacionais resolvidos | `module-remediation-plan.md` | ❌ MFA enforcement (Identity), permissions (Notifications) |
| H-02 | Módulos fundacionais com maturidade >= 80% | Readiness matrix | ⚠️ Identity 82%, Configuration OK, Environment 40% |
| H-03 | Documentação de persistência completa para cada módulo | `persistence-model-finalization.md` | ✅ Feito para 13 módulos |

---

## Resumo de Estado

| Categoria | Total | ✅ OK | ⚠️ Parcial | ❌ Pendente |
|-----------|-------|-------|-----------|-----------|
| A. Domínio | 4 | 1 | 3 | 0 |
| B. EF Core | 6 | 2 | 3 | 1 |
| C. Prefixos | 3 | 2 | 0 | 1 |
| D. Seeds | 4 | 0 | 1 | 3 |
| E. Fronteiras | 5 | 0 | 1 | 4 |
| F. Resíduos | 3 | 1 | 1 | 1 |
| G. Data Placement | 3 | 2 | 1 | 0 |
| H. Backlog | 3 | 1 | 1 | 1 |
| **TOTAL** | **31** | **9** (29%) | **11** (35%) | **11** (35%) |

---

## Conclusão

**29% das pré-condições estão satisfeitas**, 35% parcialmente e 35% pendentes.

**Bloqueadores críticos antes da remoção:**
1. Extração dos módulos Contracts, Integrations, Product Analytics, Environment Management (E-01 a E-04)
2. Correção de prefixos de tabela em código (C-02)
3. Formalização de seeds por módulo (D-01 a D-04)
4. Remoção de resíduos de Licensing dos seeds e permissões (F-02)
5. Resolução de bloqueadores P0 nos módulos fundacionais (H-01)

**A remoção das migrations não deve começar antes de pelo menos 80% das pré-condições estarem satisfeitas, com zero itens ❌ nas categorias E (Fronteiras) e F (Resíduos).**
