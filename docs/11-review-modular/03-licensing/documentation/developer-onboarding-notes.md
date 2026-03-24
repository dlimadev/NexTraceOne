# Licensing — Notas de Onboarding para Developers

> **Módulo:** Licensing  
> **Área:** Documentação — Developer Onboarding Notes  
> **Estado:** `NOT_STARTED`  
> **Prioridade:** `HIGH`  
> **Última atualização:** [A PREENCHER]

---

## Instruções

Este documento destina-se a novos developers que precisam compreender e trabalhar no módulo Licensing. Deve ser mantido atualizado e servir como ponto de entrada principal.

---

## 1. O que o Módulo Faz

<!-- TODO: preencher com descrição clara e concisa -->

O módulo Licensing é responsável por:

- [A PREENCHER] — Gestão de licenças e planos
- [A PREENCHER] — Controle de funcionalidades por licença (feature gating)
- [A PREENCHER] — Gestão de limites e quotas por licença
- [A PREENCHER] — Enforcement de licença em todas as camadas
- [A PREENCHER] — Auditoria de uso de funcionalidades licenciadas
- [A PREENCHER] — Integração com billing e renovação

---

## 2. Por que Existe

<!-- TODO: preencher -->

Todos os outros módulos do NexTraceOne dependem do Licensing para:

- Verificar se funcionalidades estão habilitadas
- Aplicar limites e quotas
- Controlar acesso a features premium
- Garantir compliance com termos de licença
- Auditar uso de funcionalidades

---

## 3. Principais Fluxos

<!-- TODO: preencher com os fluxos mais importantes que um novo developer deve conhecer -->

| # | Fluxo | Descrição resumida | Ficheiros chave |
|---|-------|-------------------|----------------|
| 1 | Ativação de licença | [A PREENCHER] | [A PREENCHER] |
| 2 | Verificação de feature | [A PREENCHER] | [A PREENCHER] |
| 3 | Gestão de limites | [A PREENCHER] | [A PREENCHER] |
| 4 | Renovação | [A PREENCHER] | [A PREENCHER] |
| 5 | Auditoria de uso | [A PREENCHER] | [A PREENCHER] |

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
