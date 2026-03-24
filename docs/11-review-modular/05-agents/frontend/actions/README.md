# Agents — Documentação de Ações Frontend

> **Módulo:** Agents  
> **Área:** Frontend — Ações  
> **Estado:** `NOT_STARTED`

---

## Como Documentar Cada Ação

Para cada ação relevante do módulo, criar um ficheiro individual nesta pasta ou documentar abaixo agrupado por página.

Cada ação deve seguir o template abaixo.

---

## Template para Documentação de Ação

```markdown
# [Nome da Ação]

> **Módulo:** Agents  
> **Estado:** `NOT_STARTED`  
> **Prioridade:** [CRITICAL | HIGH | MEDIUM | LOW]

---

## Informações Gerais

| Campo | Valor |
|-------|-------|
| **Nome da ação** | [A PREENCHER] |
| **Página de origem** | [A PREENCHER] |
| **Objetivo** | [A PREENCHER] |
| **Gatilho** | [A PREENCHER] (ex.: Clique no botão, Submit do formulário) |
| **Permissão necessária** | [A PREENCHER] (ex.: `agents.create`) |
| **Persona principal** | [A PREENCHER] |

---

## Payload

| Campo | Tipo | Obrigatório | Validação | Descrição |
|-------|------|-------------|-----------|-----------|
| [A PREENCHER] | [string / number / boolean / object] | [Sim / Não] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher com todos os campos do payload -->

---

## Endpoint Envolvido

| Campo | Valor |
|-------|-------|
| **Método HTTP** | [GET / POST / PUT / PATCH / DELETE] |
| **URL** | [A PREENCHER] (ex.: `/api/agents/[recurso]`) |
| **Content-Type** | [A PREENCHER] |
| **Autenticação** | [Bearer Token / Cookie / Outro] |

---

## Validações

### Validações de Frontend

| Campo | Regra | Mensagem i18n |
|-------|-------|--------------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

### Validações de Backend

| Campo | Regra | Código de erro |
|-------|-------|---------------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## Mensagens

| Tipo | Mensagem | Chave i18n | Estado |
|------|---------|-----------|--------|
| Sucesso | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| Erro genérico | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| Erro de validação | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| Erro de permissão | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## Auditoria Esperada

| Aspecto | Valor |
|---------|-------|
| **Evento de auditoria** | [A PREENCHER] |
| **Dados auditados** | [A PREENCHER] |
| **Nível** | [Info / Warning / Critical] |
| **Implementado?** | [Sim / Não / Parcial] |

<!-- TODO: preencher -->

---

## Problemas Encontrados

| # | Problema | Tipo | Prioridade | Descrição |
|---|----------|------|-----------|-----------|
| 1 | [A PREENCHER] | [Funcional / Validação / Segurança / UX / i18n] | [CRITICAL / HIGH / MEDIUM / LOW] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## Correções Necessárias

| # | Correção | Problema # | Estado | Responsável |
|---|----------|-----------|--------|-------------|
| 1 | [A PREENCHER] | # [A PREENCHER] | `NOT_STARTED` | [A PREENCHER] |

<!-- TODO: preencher -->

---

## Critérios de Aceite

- [ ] Ação executa corretamente para utilizador autorizado
- [ ] Payload validado no frontend e backend
- [ ] Mensagens de sucesso e erro exibidas corretamente
- [ ] i18n aplicado em todas as mensagens
- [ ] Evento de auditoria registado
- [ ] Permissão verificada antes da execução
- [ ] Tratamento de erros de rede/timeout
- [ ] Loading state durante execução

<!-- TODO: complementar -->

---

## Evidências

| Tipo | Descrição | Link/Ficheiro |
|------|-----------|--------------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->
```

---

## Ações a Documentar

<!-- TODO: listar todas as ações do módulo -->

| # | Ação | Página | Ficheiro | Estado |
|---|------|--------|----------|--------|
| 1 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER].md | `NOT_STARTED` |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER].md | `NOT_STARTED` |
| 3 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER].md | `NOT_STARTED` |

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
