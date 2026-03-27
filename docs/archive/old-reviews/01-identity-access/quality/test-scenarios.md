# Identity & Access — Cenários de Teste

> **Módulo:** Identity & Access  
> **Área:** Qualidade — Test Scenarios  
> **Estado:** `NOT_STARTED`  
> **Prioridade:** `HIGH`  
> **Última atualização:** [A PREENCHER]

---

## Instruções

Documentar todos os cenários de teste necessários para o módulo Identity & Access, organizados por categoria.

---

## 1. Happy Path

| # | Cenário | Tipo | Páginas/Endpoints | Descrição | Automatizado? | Estado |
|---|---------|------|------------------|-----------|--------------|--------|
| 1 | Login com credenciais válidas | Funcional | `/login`, `POST /api/identity/auth/login` | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |
| 2 | Criação de utilizador | Funcional | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |
| 3 | Atribuição de role | Funcional | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |
| 4 | Seleção de tenant | Funcional | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |
| 5 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |

<!-- TODO: preencher com todos os cenários happy path -->

---

## 2. Cenários de Erro

| # | Cenário | Tipo de erro | Páginas/Endpoints | Descrição | Automatizado? | Estado |
|---|---------|-------------|------------------|-----------|--------------|--------|
| 1 | Login com password incorreta | Validação | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |
| 2 | Criação de utilizador com email duplicado | Negócio | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |
| 3 | Acesso a recurso sem autenticação | Segurança | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |
| 4 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |

<!-- TODO: preencher -->

---

## 3. Cenários de Permissão

| # | Cenário | Permissão testada | Comportamento esperado | Automatizado? | Estado |
|---|---------|------------------|----------------------|--------------|--------|
| 1 | Utilizador sem permissão tenta aceder gestão de utilizadores | `users.read` | Redirecionado / 403 | [Sim / Não] | `NOT_STARTED` |
| 2 | Admin tenta aceder área de super-admin | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |
| 3 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |

<!-- TODO: preencher -->

---

## 4. Cenários Multi-tenant

| # | Cenário | Descrição | Comportamento esperado | Automatizado? | Estado |
|---|---------|-----------|----------------------|--------------|--------|
| 1 | Utilizador acede dados de outro tenant | [A PREENCHER] | Dados não visíveis / 403 | [Sim / Não] | `NOT_STARTED` |
| 2 | Troca de tenant durante sessão | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |
| 3 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [Sim / Não] | `NOT_STARTED` |

<!-- TODO: preencher -->

---

## 5. Cenários de Ambiente

| # | Cenário | Ambiente | Descrição | Comportamento esperado | Estado |
|---|---------|----------|-----------|----------------------|--------|
| 1 | Operação destrutiva em produção | Produção | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |

<!-- TODO: preencher -->

---

## 6. Cenários de IA

| # | Cenário | Capacidade de IA | Descrição | Comportamento esperado | Estado |
|---|---------|-----------------|-----------|----------------------|--------|
| 1 | IA sugere permissões para role | Assistência na criação de roles | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |

<!-- TODO: preencher -->

---

## 7. Cenários de Agents

| # | Cenário | Agent | Descrição | Comportamento esperado | Estado |
|---|---------|-------|-----------|----------------------|--------|
| 1 | Agent de segurança deteta anomalia | Identity Security Agent | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| 2 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |

<!-- TODO: preencher -->

---

## 8. Cenários de Integração

| # | Cenário | Sistemas envolvidos | Descrição | Comportamento esperado | Estado |
|---|---------|-------------------|-----------|----------------------|--------|
| 1 | Login via OIDC | Identity + IDP externo | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| 2 | Login via SAML | Identity + AD FS | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| 3 | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |

<!-- TODO: preencher -->

---

## Resumo

| Categoria | Total | Automatizados | Manuais | Estado |
|-----------|-------|--------------|---------|--------|
| Happy path | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| Erro | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| Permissão | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| Multi-tenant | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| Ambiente | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| IA | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| Agents | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| Integração | [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| **Total** | **[A PREENCHER]** | **[A PREENCHER]** | **[A PREENCHER]** | `NOT_STARTED` |

<!-- TODO: preencher -->

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
