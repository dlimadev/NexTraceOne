# Módulo: Environment Management

> **Revisão modular — NexTraceOne**

---

## Informações Gerais

| Campo | Valor |
|-------|-------|
| **Nome do módulo** | Environment Management |
| **Objetivo** | Gestão de ambientes (Development, Staging, Production, etc.), configuração por ambiente, criticidade, variáveis, promoção entre ambientes e políticas de segurança por ambiente. |
| **Valor de negócio** | Garantir que cada ambiente tenha configuração adequada, isolamento, criticidade definida e políticas de segurança aplicadas. Fundamental para Change Confidence e Operational Reliability. |
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
| Drift de configuração entre ambientes | `CRITICAL` | [A PREENCHER] | [A PREENCHER] |
| Promoção não validada para produção | `CRITICAL` | [A PREENCHER] | [A PREENCHER] |
| Falta de isolamento entre ambientes | `HIGH` | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher com riscos identificados na revisão -->

---

## Relação com Outros Módulos

| Módulo | Tipo de relação | Descrição |
|--------|----------------|-----------|
| Identity & Access | Dependência | Ambientes requerem contexto de identidade e permissões por ambiente |
| Change Governance | Dependente | Mudanças são promovidas entre ambientes com validação |
| Catalog | Dependente | Serviços no catálogo operam em ambientes específicos |
| Operational Intelligence | Dependente | Monitorização contextualizada por ambiente |
| Configuration | Dependente | Configurações variam por ambiente |
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

- Código backend: `src/modules/environments/`
- Código frontend: `src/frontend/src/features/environment-management/`

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
