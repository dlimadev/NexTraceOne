# Módulo: AI Core

> **Revisão modular — NexTraceOne**

---

## Informações Gerais

| Campo | Valor |
|-------|-------|
| **Nome do módulo** | AI Core |
| **Objetivo** | Motor de IA do NexTraceOne — gestão de providers, modelos, governance de IA, model registry, quotas de tokens, políticas de acesso a IA, auditoria de prompts e respostas, integração com IA interna e externa. |
| **Valor de negócio** | Garantir que todas as capacidades de IA da plataforma sejam governadas, auditáveis, seguras e controladas. O AI Core é a fundação de toda a inteligência artificial do NexTraceOne. |
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
| Fuga de dados sensíveis via prompts para IA externa | `CRITICAL` | [A PREENCHER] | [A PREENCHER] |
| Falta de governança em acesso a modelos | `CRITICAL` | [A PREENCHER] | [A PREENCHER] |
| Consumo descontrolado de tokens sem budget | `HIGH` | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher com riscos identificados na revisão -->

---

## Relação com Outros Módulos

| Módulo | Tipo de relação | Descrição |
|--------|----------------|-----------|
| Identity & Access | Dependência | IA requer contexto de identidade e permissões governadas |
| Licensing | Dependência | Capacidades de IA podem ser limitadas por licença |
| Agents | Dependente | Agents consomem o motor de IA do AI Core |
| Audit & Compliance | Dependente | Auditoria de prompts, respostas e tokens origina no AI Core |
| Contracts | Dependente | Geração e validação de contratos assistidas por IA |
| Catalog | Dependente | Enriquecimento de dados do catálogo por IA |
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

- Código backend: `src/modules/aicore/`
- Código frontend: `src/frontend/src/features/ai-core/`

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
