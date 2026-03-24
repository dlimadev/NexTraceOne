# Agents — Revisão de Migrations

> **Módulo:** Agents  
> **Área:** Database — Migrations Review  
> **Estado:** `NOT_STARTED`  
> **Prioridade:** `HIGH`  
> **Última atualização:** [A PREENCHER]

---

## Instruções

Documentar todas as migrations relevantes do módulo, identificar inconsistências, redundâncias e riscos de manutenção.

---

## 1. Migrations Relevantes

| # | Migration | Data | Descrição | Reversível? | Estado |
|---|-----------|------|-----------|------------|--------|
| 1 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |
| 3 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |

<!-- TODO: preencher com todas as migrations do módulo -->

---

## 2. Inconsistências

| # | Migration | Tipo | Prioridade | Descrição |
|---|-----------|------|-----------|-----------|
| 1 | [A PREENCHER] | [Naming / Tipo de dado / Constraint em falta / Default errado / Ordem incorreta] | [CRITICAL / HIGH / MEDIUM / LOW] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## 3. Redundâncias

| # | Migrations envolvidas | Tipo | Descrição | Pode ser consolidado? |
|---|----------------------|------|-----------|---------------------|
| 1 | [A PREENCHER] | [Coluna adicionada e removida / Índice duplicado / Alteração revertida] | [A PREENCHER] | [Sim / Não] |

<!-- TODO: preencher -->

---

## 4. Riscos de Manutenção

| # | Risco | Prioridade | Descrição | Mitigação |
|---|-------|-----------|-----------|-----------|
| 1 | [A PREENCHER] | [CRITICAL / HIGH / MEDIUM / LOW] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## 5. Necessidade de Consolidação

| Aspecto | Avaliação | Observações |
|---------|-----------|-------------|
| Número total de migrations | [A PREENCHER] | <!-- TODO: preencher --> |
| Migrations que podem ser unificadas | [A PREENCHER] | <!-- TODO: preencher --> |
| Migrations com operações destrutivas | [A PREENCHER] | <!-- TODO: preencher --> |
| Migrations sem Down() | [A PREENCHER] | <!-- TODO: preencher --> |
| Migrations com dados hardcoded | [A PREENCHER] | <!-- TODO: preencher --> |
| Squash recomendado? | [Sim / Não] | <!-- TODO: preencher --> |

---

## 6. Compatibilidade com Ambientes

| Ambiente | Migrations aplicadas com sucesso? | Problemas conhecidos |
|----------|--------------------------------|---------------------|
| Development | [A PREENCHER] | [A PREENCHER] |
| Staging | [A PREENCHER] | [A PREENCHER] |
| Production | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
