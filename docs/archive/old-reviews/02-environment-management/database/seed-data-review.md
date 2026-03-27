# Environment Management — Revisão de Seed Data

> **Módulo:** Environment Management  
> **Área:** Database — Seed Data Review  
> **Estado:** `NOT_STARTED`  
> **Prioridade:** `HIGH`  
> **Última atualização:** [A PREENCHER]

---

## Instruções

Documentar todos os dados obrigatórios de seed para o módulo: permissões, roles, catálogos, tipos, estados. Identificar seeds ausentes e seu impacto.

---

## 1. Dados Obrigatórios

### 1.1 Permissões

| # | Permissão | Código | Módulo | Descrição | Existe no seed? |
|---|-----------|--------|--------|-----------|----------------|
| 1 | [A PREENCHER] | [A PREENCHER] | Environment Management | [A PREENCHER] | [Sim / Não] |
| 2 | [A PREENCHER] | [A PREENCHER] | Environment Management | [A PREENCHER] | [Sim / Não] |

<!-- TODO: preencher com todas as permissões necessárias -->

### 1.2 Roles

| # | Role | Código | Permissões associadas | Descrição | Existe no seed? |
|---|------|--------|----------------------|-----------|----------------|
| 1 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] |

<!-- TODO: preencher -->

---

## 2. Catálogos e Tipos

| # | Catálogo/Tipo | Tabela | Valores obrigatórios | Existe no seed? | Completo? |
|---|--------------|--------|---------------------|----------------|----------|
| 1 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | [Sim / Não] |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | [Sim / Não] |

<!-- TODO: preencher com todos os catálogos de dados -->

---

## 3. Estados e Enumerações

| # | Enumeração | Tabela/Campo | Valores | Existe no seed? |
|---|-----------|-------------|---------|----------------|
| 1 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] |

<!-- TODO: preencher -->

---

## 4. Dados de Configuração Inicial

| # | Configuração | Descrição | Valor padrão | Existe no seed? |
|---|-------------|-----------|-------------|----------------|
| 1 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] |

<!-- TODO: preencher -->

---

## 5. Seeds Ausentes

| # | Seed ausente | Tipo | Prioridade | Impacto | Ação necessária |
|---|-------------|------|-----------|---------|----------------|
| 1 | [A PREENCHER] | [Permissão / Role / Catálogo / Tipo / Estado / Configuração] | [CRITICAL / HIGH / MEDIUM / LOW] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## 6. Impacto dos Seeds

| Aspecto | Avaliação | Observações |
|---------|-----------|-------------|
| Sistema funciona sem seeds? | [Sim / Não / Parcialmente] | <!-- TODO: preencher --> |
| Seeds são idempotentes? | [Sim / Não] | <!-- TODO: preencher --> |
| Seeds suportam multi-tenant? | [Sim / Não] | <!-- TODO: preencher --> |
| Seeds estão versionados? | [Sim / Não] | <!-- TODO: preencher --> |
| Seeds podem ser executados em produção? | [Sim / Não] | <!-- TODO: preencher --> |
| Ordem de execução está definida? | [Sim / Não] | <!-- TODO: preencher --> |

---

## 7. Localização dos Seeds no Código

| # | Ficheiro/Classe | Descrição | Tipo de seed |
|---|----------------|-----------|-------------|
| 1 | [A PREENCHER] | [A PREENCHER] | [Migration seed / Seeder class / Script SQL] |

<!-- TODO: preencher -->

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
