# Environment Management — Notas de Onboarding para Developers

> **Módulo:** Environment Management  
> **Área:** Documentação — Developer Onboarding Notes  
> **Estado:** `NOT_STARTED`  
> **Prioridade:** `HIGH`  
> **Última atualização:** [A PREENCHER]

---

## Instruções

Este documento destina-se a novos developers que precisam compreender e trabalhar no módulo Environment Management. Deve ser mantido atualizado e servir como ponto de entrada principal.

---

## 1. O que o Módulo Faz

<!-- TODO: preencher com descrição clara e concisa -->

O módulo Environment Management é responsável por:

- [A PREENCHER] — Criação e gestão de ambientes (Development, Staging, Production)
- [A PREENCHER] — Configuração de variáveis e parâmetros por ambiente
- [A PREENCHER] — Definição de criticidade e políticas por ambiente
- [A PREENCHER] — Promoção de configurações e artefactos entre ambientes
- [A PREENCHER] — Isolamento e segurança por ambiente
- [A PREENCHER] — Integração com Change Intelligence para validação de mudanças

---

## 2. Por que Existe

<!-- TODO: preencher -->

Todos os outros módulos do NexTraceOne dependem do Environment Management para:

- Saber em que ambiente estão a operar
- Aplicar configurações corretas por ambiente
- Validar criticidade antes de mudanças
- Garantir isolamento entre ambientes
- Aplicar políticas de segurança por ambiente

---

## 3. Principais Fluxos

<!-- TODO: preencher com os fluxos mais importantes que um novo developer deve conhecer -->

| # | Fluxo | Descrição resumida | Ficheiros chave |
|---|-------|-------------------|----------------|
| 1 | Criação de ambiente | [A PREENCHER] | [A PREENCHER] |
| 2 | Configuração de variáveis | [A PREENCHER] | [A PREENCHER] |
| 3 | Promoção entre ambientes | [A PREENCHER] | [A PREENCHER] |
| 4 | Definição de políticas | [A PREENCHER] | [A PREENCHER] |
| 5 | Verificação de criticidade | [A PREENCHER] | [A PREENCHER] |

---

## 4. Principais Páginas

<!-- TODO: preencher -->

| # | Página | Rota | O que faz | Ficheiro |
|---|--------|------|----------|----------|
| 1 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| 3 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

---

## 5. Principais Classes

<!-- TODO: preencher -->

### Backend

| # | Classe/Interface | Namespace | O que faz |
|---|-----------------|-----------|----------|
| 1 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| 3 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

### Frontend

| # | Componente/Hook | Ficheiro | O que faz |
|---|----------------|----------|----------|
| 1 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

---

## 6. Onde Alterar — Frontend

<!-- TODO: preencher com guia prático -->

| Tarefa | Onde alterar | Notas |
|--------|-------------|-------|
| Adicionar nova página | [A PREENCHER] | [A PREENCHER] |
| Alterar formulário existente | [A PREENCHER] | [A PREENCHER] |
| Adicionar nova ação | [A PREENCHER] | [A PREENCHER] |
| Alterar validação de frontend | [A PREENCHER] | [A PREENCHER] |
| Adicionar nova chave i18n | [A PREENCHER] | [A PREENCHER] |
| Alterar navegação/menu | [A PREENCHER] | [A PREENCHER] |

---

## 7. Onde Alterar — Backend

<!-- TODO: preencher com guia prático -->

| Tarefa | Onde alterar | Notas |
|--------|-------------|-------|
| Adicionar novo endpoint | [A PREENCHER] | [A PREENCHER] |
| Adicionar novo command handler | [A PREENCHER] | [A PREENCHER] |
| Adicionar nova regra de domínio | [A PREENCHER] | [A PREENCHER] |
| Alterar validação | [A PREENCHER] | [A PREENCHER] |
| Adicionar nova permissão | [A PREENCHER] | [A PREENCHER] |
| Adicionar novo evento de domínio | [A PREENCHER] | [A PREENCHER] |
| Alterar configuração de DI | [A PREENCHER] | [A PREENCHER] |

---

## 8. Onde Alterar — Base de Dados

<!-- TODO: preencher com guia prático -->

| Tarefa | Onde alterar | Notas |
|--------|-------------|-------|
| Adicionar nova tabela | [A PREENCHER] | [A PREENCHER] |
| Alterar coluna existente | [A PREENCHER] | [A PREENCHER] |
| Adicionar nova migration | [A PREENCHER] | [A PREENCHER] |
| Adicionar seed data | [A PREENCHER] | [A PREENCHER] |
| Alterar mapeamento EF Core | [A PREENCHER] | [A PREENCHER] |

---

## 9. Riscos Comuns

<!-- TODO: preencher com erros e riscos que novos developers devem evitar -->

| # | Risco | Descrição | Como evitar |
|---|-------|-----------|------------|
| 1 | Esquecer tenant context | [A PREENCHER] | [A PREENCHER] |
| 2 | Não propagar CancellationToken | [A PREENCHER] | [A PREENCHER] |
| 3 | Colocar lógica de domínio no handler | [A PREENCHER] | [A PREENCHER] |
| 4 | Esquecer evento de auditoria | [A PREENCHER] | [A PREENCHER] |
| 5 | Hardcoded strings no frontend | [A PREENCHER] | [A PREENCHER] |
| 6 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

---

## 10. Pontos de Atenção

<!-- TODO: preencher com informações importantes que não se encaixam nas secções anteriores -->

- [ ] [A PREENCHER] — Padrões específicos deste módulo
- [ ] [A PREENCHER] — Convenções de naming
- [ ] [A PREENCHER] — Testes obrigatórios antes de PR
- [ ] [A PREENCHER] — Como executar o módulo localmente
- [ ] [A PREENCHER] — Variáveis de ambiente necessárias
- [ ] [A PREENCHER] — Dependências de outros serviços

---

## Comandos Úteis

```bash
# [A PREENCHER] — Como construir o módulo
# [A PREENCHER] — Como executar os testes
# [A PREENCHER] — Como aplicar migrations
# [A PREENCHER] — Como executar seeds
# [A PREENCHER] — Como executar o frontend localmente
```

<!-- TODO: preencher com comandos reais -->

---

## Contactos

| Responsabilidade | Pessoa | Contacto |
|-----------------|--------|---------|
| Tech Lead | [A PREENCHER] | [A PREENCHER] |
| Domain Expert | [A PREENCHER] | [A PREENCHER] |
| Frontend Lead | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
