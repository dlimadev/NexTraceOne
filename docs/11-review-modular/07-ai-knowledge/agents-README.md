# Módulo: Agents

> **Revisão modular — NexTraceOne**

---

## Informações Gerais

| Campo | Valor |
|-------|-------|
| **Nome do módulo** | Agents |
| **Objetivo** | Gestão de agentes inteligentes do NexTraceOne — catálogo de agents, configuração, execução, observabilidade, auditoria, segurança, prompts, contexto, scheduling e lifecycle management. |
| **Valor de negócio** | Garantir que agentes inteligentes sejam geridos de forma centralizada, governada e auditável. Os agents são a camada de automação inteligente do NexTraceOne. |
| **Estado atual** | `NOT_STARTED` |

---

## Personas

| Persona | Relação com o módulo | Notas |
|---------|---------------------|-------|
| Engineer | [A PREENCHER] | <!-- TODO: preencher --> |
| Tech Lead | [A PREENCHER] | <!-- TODO: preencher --> |
| Architect | [A PREENCHER] | <!-- TODO: preencher --> |
| Product | [A PREENCHER] | <!-- TODO: preencher --> |
| Executive | [A PREENCHER] | <!-- TODO: preencher --> |
| Platform Admin | [A PREENCHER] | <!-- TODO: preencher --> |
| Auditor | [A PREENCHER] | <!-- TODO: preencher --> |

---

## Páginas Principais

| Página | Rota | Persona principal | Estado |
|--------|------|-------------------|--------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |

<!-- TODO: preencher com todas as páginas do módulo -->

---

## Ações Principais

| Ação | Página de origem | Persona | Estado |
|------|-----------------|---------|--------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |

<!-- TODO: preencher com todas as ações do módulo -->

---

## Integrações

| Sistema/Módulo | Tipo de integração | Descrição | Estado |
|---------------|-------------------|-----------|--------|
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] | `NOT_STARTED` |

<!-- TODO: preencher com integrações internas e externas -->

---

## Principais Riscos

| Risco | Prioridade | Descrição | Mitigação proposta |
|-------|-----------|-----------|-------------------|
| Execução de agent com acesso não autorizado a dados | `CRITICAL` | [A PREENCHER] | [A PREENCHER] |
| Falta de isolamento de tenant em execuções de agents | `CRITICAL` | [A PREENCHER] | [A PREENCHER] |
| Agent sem observabilidade nem auditoria | `HIGH` | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher com riscos identificados na revisão -->

---

## Relação com Outros Módulos

| Módulo | Tipo de relação | Descrição |
|--------|----------------|-----------|
| AI Core | Dependência | Agents consomem o motor de IA do AI Core para execução |
| Identity & Access | Dependência | Agents requerem contexto de identidade e permissões |
| Licensing | Dependência | Agents avançados podem depender de licença enterprise |
| Audit & Compliance | Dependente | Execuções de agents são auditadas |
| Operational Intelligence | Dependente | Agents de operações alimentam inteligência operacional |
| Catalog | Dependente | Agents podem enriquecer dados do catálogo de serviços |
| [A PREENCHER] | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: completar e validar relações -->

---

## Status da Revisão

| Área | Estado | Responsável | Data |
|------|--------|-------------|------|
| Frontend — Páginas | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Frontend — Ações | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Backend — Endpoints | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Backend — Application Services | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Backend — Domain Rules | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Backend — Authorization | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Backend — Validações | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Database — Schema | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Database — Migrations | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Database — Seed Data | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| IA — Capacidades | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| IA — Agents | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Qualidade — Bugs & Gaps | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Qualidade — Dívida Técnica | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Qualidade — Cenários de Teste | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Qualidade — Checklist de Aceite | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Documentação — Comentários | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |
| Documentação — Onboarding | `NOT_STARTED` | [A PREENCHER] | [A PREENCHER] |

---

## Referências

- Código backend: `src/modules/agents/`
- Código frontend: `src/frontend/src/features/agents/`

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
