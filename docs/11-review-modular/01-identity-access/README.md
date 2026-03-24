# Módulo: Identity & Access

> **Revisão modular — NexTraceOne**

---

## Informações Gerais

| Campo | Valor |
|-------|-------|
| **Nome do módulo** | Identity & Access |
| **Objetivo** | Gestão de identidade, autenticação, autorização, multi-tenancy, sessões, MFA e funcionalidades enterprise de acesso seguro. Este módulo é a fundação de todo o NexTraceOne — todos os outros módulos dependem dele. |
| **Valor de negócio** | Garantir acesso seguro, auditável e governado a toda a plataforma. Suportar cenários enterprise como multi-tenancy, MFA, delegações, Break Glass, JIT Access e Access Review Campaigns. |
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

<!-- TODO: preencher com integrações internas e externas (OIDC, SAML, AD, etc.) -->

---

## Principais Riscos

| Risco | Prioridade | Descrição | Mitigação proposta |
|-------|-----------|-----------|-------------------|
| [A PREENCHER] | `CRITICAL` | [A PREENCHER] | [A PREENCHER] |
| [A PREENCHER] | `HIGH` | [A PREENCHER] | [A PREENCHER] |
| [A PREENCHER] | `MEDIUM` | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher com riscos identificados na revisão -->

---

## Relação com Outros Módulos

| Módulo | Tipo de relação | Descrição |
|--------|----------------|-----------|
| Contracts | Dependente | Contratos de API dependem de autenticação e autorização |
| Catalog | Dependente | Serviços no catálogo requerem ownership baseado em identidade |
| Change Governance | Dependente | Mudanças requerem contexto de identidade e permissão |
| Audit & Compliance | Dependente | Eventos de segurança e auditoria originam neste módulo |
| AI & Knowledge | Dependente | Capacidades de IA requerem governança de acesso |
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

- Revisão existente: [`../02-identity-access/module-review.md`](../02-identity-access/module-review.md)
- Código backend: `src/modules/identityaccess/`
- Código frontend: `src/frontend/src/features/identity-access/`

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
