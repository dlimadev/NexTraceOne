# Identity & Access — Mapeamento de Endpoints

> **Módulo:** Identity & Access  
> **Área:** Backend — Endpoints  
> **Estado:** `NOT_STARTED`  
> **Prioridade:** `CRITICAL`  
> **Última atualização:** [A PREENCHER]

---

## Instruções

Documentar todos os endpoints do módulo Identity & Access. Para cada endpoint, preencher a secção completa conforme o template abaixo.

---

## Template de Endpoint

### `[MÉTODO] /api/identity/[recurso]`

| Campo | Valor |
|-------|-------|
| **Endpoint** | [A PREENCHER] (ex.: `/api/identity/users`) |
| **Método HTTP** | [GET / POST / PUT / PATCH / DELETE] |
| **Objetivo** | [A PREENCHER] |
| **Controller/Module** | [A PREENCHER] |
| **Handler/Service** | [A PREENCHER] |

#### Request

| Campo | Tipo | Obrigatório | Validação | Descrição |
|-------|------|-------------|-----------|-----------|
| [A PREENCHER] | [A PREENCHER] | [Sim / Não] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

**Exemplo de request:**
```json
{
  // [A PREENCHER]
}
```

#### Response

| Campo | Tipo | Descrição |
|-------|------|-----------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

**Exemplo de response (sucesso):**
```json
{
  // [A PREENCHER]
}
```

#### Autenticação e Autorização

| Aspecto | Valor |
|---------|-------|
| **Requer autenticação** | [Sim / Não] |
| **Permissão necessária** | [A PREENCHER] |
| **Restrição de tenant** | [Sim / Não] |
| **Restrição de ambiente** | [A PREENCHER] |

#### Validações

| Validação | Camada | Descrição | Código de erro |
|-----------|--------|-----------|---------------|
| [A PREENCHER] | [API / Application / Domain] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

#### Auditoria

| Aspecto | Valor |
|---------|-------|
| **Evento registado** | [A PREENCHER] |
| **Dados auditados** | [A PREENCHER] |
| **Implementado?** | [Sim / Não / Parcial] |

#### Erros Possíveis

| Código HTTP | Código interno | Descrição | Mensagem |
|-------------|---------------|-----------|---------|
| 400 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| 401 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| 403 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| 404 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| 409 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| 422 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| 500 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher apenas os aplicáveis -->

#### Relação com Frontend

| Página | Ação | Componente |
|--------|------|-----------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

#### Gaps Identificados

| # | Gap | Prioridade | Descrição |
|---|-----|-----------|-----------|
| 1 | [A PREENCHER] | [CRITICAL / HIGH / MEDIUM / LOW] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## Inventário de Endpoints

<!-- TODO: preencher com todos os endpoints do módulo -->

| # | Método | Endpoint | Objetivo | Estado |
|---|--------|----------|----------|--------|
| 1 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| 3 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |

---

## Resumo de Gaps

| Área | Quantidade | Prioridade mais alta |
|------|-----------|---------------------|
| Validações em falta | [A PREENCHER] | [A PREENCHER] |
| Autorizações em falta | [A PREENCHER] | [A PREENCHER] |
| Auditoria em falta | [A PREENCHER] | [A PREENCHER] |
| Documentação em falta | [A PREENCHER] | [A PREENCHER] |
| Erros mal tratados | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
