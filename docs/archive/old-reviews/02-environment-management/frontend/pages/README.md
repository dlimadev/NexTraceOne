# Environment Management — Documentação de Páginas Frontend

> **Módulo:** Environment Management  
> **Área:** Frontend — Páginas  
> **Estado:** `NOT_STARTED`

---

## Como Documentar Cada Página

Para cada página do módulo Environment Management, criar um ficheiro individual nesta pasta com o nome da página (ex.: `environment-list-page.md`, `environment-detail-page.md`).

Cada ficheiro de página deve seguir o template abaixo.

---

## Template para Documentação de Página

```markdown
# [Nome da Página]

> **Módulo:** Environment Management  
> **Estado:** `NOT_STARTED`  
> **Prioridade:** [CRITICAL | HIGH | MEDIUM | LOW]  
> **Última atualização:** [A PREENCHER]

---

## Informações Gerais

| Campo | Valor |
|-------|-------|
| **Nome** | [A PREENCHER] |
| **Rota** | [A PREENCHER] (ex.: `/admin/environments`) |
| **Objetivo** | [A PREENCHER] |
| **Persona principal** | [A PREENCHER] (Engineer, Tech Lead, Platform Admin, etc.) |
| **Personas secundárias** | [A PREENCHER] |
| **Autenticação obrigatória** | [Sim / Não] |
| **Componente React** | [A PREENCHER] |
| **Ficheiro de rota** | [A PREENCHER] |

---

## Componentes Principais

| Componente | Tipo | Descrição | Reutilizado? |
|------------|------|-----------|-------------|
| [A PREENCHER] | Layout / Form / Table / Modal / Card | [A PREENCHER] | [Sim / Não] |

<!-- TODO: preencher com todos os componentes da página -->

---

## Dados Exibidos

| Dado | Fonte | Endpoint/Hook | Atualização |
|------|-------|--------------|-------------|
| [A PREENCHER] | [API / Cache / Local] | [A PREENCHER] | [Real-time / Polling / Manual] |

<!-- TODO: preencher -->

---

## Ações Disponíveis

| Ação | Componente | Permissão | Endpoint | Descrição |
|------|-----------|-----------|----------|-----------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher com todas as ações possíveis na página -->

---

## Permissões

| Permissão | Descrição | Efeito na página |
|-----------|-----------|-----------------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## Estados da Página

| Estado | Condição | Comportamento |
|--------|----------|---------------|
| Loading | [A PREENCHER] | [A PREENCHER] |
| Empty | [A PREENCHER] | [A PREENCHER] |
| Error | [A PREENCHER] | [A PREENCHER] |
| Success | [A PREENCHER] | [A PREENCHER] |
| Forbidden | [A PREENCHER] | [A PREENCHER] |
| Not Found | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## i18n

| Aspecto | Estado | Observações |
|---------|--------|-------------|
| Títulos | [A PREENCHER] | <!-- TODO: preencher --> |
| Labels | [A PREENCHER] | <!-- TODO: preencher --> |
| Placeholders | [A PREENCHER] | <!-- TODO: preencher --> |
| Botões | [A PREENCHER] | <!-- TODO: preencher --> |
| Mensagens de erro | [A PREENCHER] | <!-- TODO: preencher --> |
| Mensagens de sucesso | [A PREENCHER] | <!-- TODO: preencher --> |
| Tooltips | [A PREENCHER] | <!-- TODO: preencher --> |
| Estados vazios | [A PREENCHER] | <!-- TODO: preencher --> |

---

## Problemas Encontrados

| # | Problema | Tipo | Prioridade | Descrição |
|---|----------|------|-----------|-----------|
| 1 | [A PREENCHER] | [Funcional / Visual / UX / i18n / Segurança] | [CRITICAL / HIGH / MEDIUM / LOW] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## Correções Necessárias

| # | Correção | Relacionado ao problema # | Estado | Responsável |
|---|----------|--------------------------|--------|-------------|
| 1 | [A PREENCHER] | # [A PREENCHER] | `NOT_STARTED` | [A PREENCHER] |

<!-- TODO: preencher -->

---

## Critérios de Aceite

- [ ] Página carrega corretamente para persona autorizada
- [ ] Dados exibidos estão corretos e completos
- [ ] Todas as ações funcionam conforme esperado
- [ ] Permissões aplicadas corretamente
- [ ] Todos os estados da página estão tratados
- [ ] i18n aplicado em todos os textos visíveis
- [ ] Sem erros de consola
- [ ] Responsivo e acessível
- [ ] Observabilidade adequada (logs, telemetria)

<!-- TODO: complementar critérios específicos da página -->

---

## Evidências

| Tipo | Descrição | Link/Ficheiro |
|------|-----------|--------------|
| Screenshot | [A PREENCHER] | [A PREENCHER] |
| Vídeo | [A PREENCHER] | [A PREENCHER] |
| Log | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher com evidências da revisão -->
```

---

## Páginas a Documentar

<!-- TODO: criar um ficheiro .md para cada página listada abaixo -->

| # | Página | Ficheiro | Estado |
|---|--------|----------|--------|
| 1 | [A PREENCHER] | [A PREENCHER].md | `NOT_STARTED` |
| 2 | [A PREENCHER] | [A PREENCHER].md | `NOT_STARTED` |
| 3 | [A PREENCHER] | [A PREENCHER].md | `NOT_STARTED` |

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
