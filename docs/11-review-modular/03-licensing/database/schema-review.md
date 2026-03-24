# Licensing — Revisão de Schema da Base de Dados

> **Módulo:** Licensing  
> **Área:** Database — Schema Review  
> **Estado:** `NOT_STARTED`  
> **Prioridade:** `HIGH`  
> **Última atualização:** [A PREENCHER]

---

## Instruções

Documentar todas as tabelas, colunas, relações, constraints, índices e avaliar a aderência ao domínio.

---

## 1. Tabelas

| # | Tabela | Schema | Descrição | Entidade de domínio | Estado |
|---|--------|--------|-----------|-------------------|--------|
| 1 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| 3 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |

<!-- TODO: preencher com todas as tabelas do módulo -->

---

## 2. Colunas (por tabela)

### Tabela: `[A PREENCHER]`

| # | Coluna | Tipo | Nullable | Default | Descrição | Aderente ao domínio? |
|---|--------|------|----------|---------|-----------|---------------------|
| 1 | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] |

<!-- TODO: repetir para cada tabela -->

---

## 3. Relações (Foreign Keys)

| # | Tabela origem | Tabela destino | Tipo | Coluna FK | On Delete | Descrição |
|---|--------------|---------------|------|----------|-----------|-----------|
| 1 | [A PREENCHER] | [A PREENCHER] | [1:N / N:N / 1:1] | [A PREENCHER] | [Cascade / Restrict / SetNull / NoAction] | [A PREENCHER] |

<!-- TODO: preencher com todas as relações -->

---

## 4. Constraints

| # | Tabela | Constraint | Tipo | Colunas | Descrição |
|---|--------|-----------|------|---------|-----------|
| 1 | [A PREENCHER] | [A PREENCHER] | [PK / FK / Unique / Check] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## 5. Índices

| # | Tabela | Índice | Tipo | Colunas | Justificação | Adequado? |
|---|--------|--------|------|---------|-------------|----------|
| 1 | [A PREENCHER] | [A PREENCHER] | [Clustered / Non-clustered / Unique / Filtered] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] |

<!-- TODO: preencher -->

---

## 6. Aderência ao Domínio

| Aspecto | Avaliação | Observações |
|---------|-----------|-------------|
| Nomes de tabelas refletem domínio | [A PREENCHER] | <!-- TODO: preencher --> |
| Nomes de colunas refletem domínio | [A PREENCHER] | <!-- TODO: preencher --> |
| Value objects mapeados corretamente | [A PREENCHER] | <!-- TODO: preencher --> |
| Enums mapeados corretamente | [A PREENCHER] | <!-- TODO: preencher --> |
| Strongly typed IDs | [A PREENCHER] | <!-- TODO: preencher --> |
| Soft delete implementado | [A PREENCHER] | <!-- TODO: preencher --> |
| Audit columns (CreatedAt, UpdatedBy, etc.) | [A PREENCHER] | <!-- TODO: preencher --> |
| Multi-tenancy (TenantId) | [A PREENCHER] | <!-- TODO: preencher --> |
| RLS configurado | [A PREENCHER] | <!-- TODO: preencher --> |

---

## 7. Lacunas

| # | Lacuna | Tipo | Prioridade | Descrição | Risco |
|---|--------|------|-----------|-----------|-------|
| 1 | [A PREENCHER] | [Tabela ausente / Coluna ausente / Índice em falta / FK em falta / Tipo incorreto] | [CRITICAL / HIGH / MEDIUM / LOW] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## 8. Riscos

| # | Risco | Prioridade | Descrição | Mitigação |
|---|-------|-----------|-----------|-----------|
| 1 | [A PREENCHER] | [CRITICAL / HIGH / MEDIUM / LOW] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
