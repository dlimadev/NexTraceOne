# AI Core — Visão Geral do Módulo

> **Módulo:** AI Core  
> **Estado:** `NOT_STARTED`  
> **Prioridade:** `CRITICAL`  
> **Última atualização:** [A PREENCHER]

---

## 1. Visão Funcional

<!-- TODO: preencher -->

O módulo AI Core é responsável por:

- [A PREENCHER] — Model Registry — registo e gestão de modelos
- [A PREENCHER] — Provider Management — gestão de providers de IA
- [A PREENCHER] — Token Budget Management — gestão de quotas de tokens
- [A PREENCHER] — AI Access Policies — políticas de acesso a IA
- [A PREENCHER] — Prompt Auditing — auditoria de prompts
- [A PREENCHER] — Response Auditing — auditoria de respostas

---

## 2. Visão Técnica

<!-- TODO: preencher com detalhes da arquitetura técnica -->

| Aspecto | Descrição |
|---------|-----------|
| **Módulo backend** | `src/modules/aicore/` |
| **Módulo frontend** | `src/frontend/src/features/ai-core/` |
| **Padrão arquitetural** | [A PREENCHER] (DDD, CQRS, etc.) |
| **Persistência** | [A PREENCHER] (EF Core, DbContext específico, etc.) |
| **Autenticação** | [A PREENCHER] (JWT, cookies, etc.) |
| **Multi-tenancy** | [A PREENCHER] (RLS, tenant header, etc.) |
| **Eventos de domínio** | [A PREENCHER] |
| **Testes** | [A PREENCHER] (número de testes, cobertura) |

---

## 3. Principais Fluxos

### 3.1 Fluxo Principal

```
[A PREENCHER]
1. ...
2. ...
3. ...
```

<!-- TODO: preencher com o fluxo completo -->

### 3.2 Fluxo Secundário

```
[A PREENCHER]
1. ...
2. ...
3. ...
```

<!-- TODO: preencher -->

### 3.3 Outros Fluxos Relevantes

<!-- TODO: listar e descrever outros fluxos -->

- [A PREENCHER]

---

## 4. Entidades Principais

| Entidade | Tipo | Descrição | Estado |
|----------|------|-----------|--------|
| [A PREENCHER] | Aggregate Root | [A PREENCHER] | `NOT_STARTED` |
| [A PREENCHER] | Entity | [A PREENCHER] | `NOT_STARTED` |
| [A PREENCHER] | Value Object | [A PREENCHER] | `NOT_STARTED` |
| [A PREENCHER] | Domain Event | [A PREENCHER] | `NOT_STARTED` |

<!-- TODO: preencher com todas as entidades do domínio -->

---

## 5. Dependências

### 5.1 Dependências Internas (módulos NexTraceOne)

| Módulo | Tipo | Descrição |
|--------|------|-----------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

### 5.2 Dependências Externas (pacotes, serviços)

| Dependência | Versão | Propósito |
|-------------|--------|-----------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## 6. Riscos Arquiteturais

| # | Risco | Prioridade | Impacto | Mitigação |
|---|-------|-----------|---------|-----------|
| 1 | Fuga de dados sensíveis via prompts para IA externa | `CRITICAL` | [A PREENCHER] | [A PREENCHER] |
| 2 | Falta de governança em acesso a modelos | `CRITICAL` | [A PREENCHER] | [A PREENCHER] |
| 3 | Consumo descontrolado de tokens sem budget | `HIGH` | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher com riscos identificados -->

---

## 7. Gaps Percebidos

| # | Gap | Área | Prioridade | Descrição |
|---|-----|------|-----------|-----------|
| 1 | [A PREENCHER] | Frontend | [A PREENCHER] | [A PREENCHER] |
| 2 | [A PREENCHER] | Backend | [A PREENCHER] | [A PREENCHER] |
| 3 | [A PREENCHER] | Database | [A PREENCHER] | [A PREENCHER] |
| 4 | [A PREENCHER] | IA | [A PREENCHER] | [A PREENCHER] |
| 5 | [A PREENCHER] | Documentação | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher -->

---

## 8. Critérios de Pronto

O módulo AI Core será considerado `DONE` quando:

- [ ] Todas as páginas frontend estiverem revistas e aprovadas
- [ ] Todos os endpoints estiverem documentados e validados
- [ ] Todas as regras de domínio estiverem corretas e testadas
- [ ] Autorizações estiverem completas e consistentes
- [ ] Validações estiverem implementadas em todas as camadas
- [ ] Schema de base de dados estiver aderente ao domínio
- [ ] Seeds e migrations estiverem consolidados
- [ ] Capacidades de IA estiverem definidas e governadas
- [ ] Agents estiverem configurados e auditáveis
- [ ] Todos os bugs e gaps identificados estiverem resolvidos
- [ ] Dívida técnica estar mapeada e priorizada
- [ ] Cenários de teste estiverem definidos e executados
- [ ] Checklist de aceite estiver completo
- [ ] Comentários de código estiverem adequados
- [ ] Documentação de onboarding estiver pronta
- [ ] i18n estiver aplicado em todo o frontend
- [ ] Segurança e auditoria estiverem validadas

<!-- TODO: validar e complementar critérios -->

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
