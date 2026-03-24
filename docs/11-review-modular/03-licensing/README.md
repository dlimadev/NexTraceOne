# Módulo: Licensing

> **Revisão modular — NexTraceOne**

---

## Informações Gerais

| Campo | Valor |
|-------|-------|
| **Nome do módulo** | Licensing |
| **Objetivo** | Licenciamento e controle de funcionalidades por licença, gestão de planos, limites, feature flags controladas por licença, enforcement e auditoria de uso. |
| **Valor de negócio** | Garantir que funcionalidades da plataforma sejam controladas por licença, permitindo monetização, compliance e governance de acesso a features premium e enterprise. |
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
| Bypass de verificação de licença | `CRITICAL` | [A PREENCHER] | [A PREENCHER] |
| Funcionalidades premium acessíveis sem licença | `CRITICAL` | [A PREENCHER] | [A PREENCHER] |
| Falta de enforcement consistente em todas as camadas | `HIGH` | [A PREENCHER] | [A PREENCHER] |

<!-- TODO: preencher com riscos identificados na revisão -->

---

## Relação com Outros Módulos

| Módulo | Tipo de relação | Descrição |
|--------|----------------|-----------|
| Identity & Access | Dependência | Licenças são associadas a tenants e organizações autenticadas |
| Catalog | Dependente | Funcionalidades do catálogo dependem da licença ativa |
| AI & Knowledge | Dependente | Capacidades de IA podem ser limitadas por licença |
| Governance | Dependente | Reports e compliance podem exigir licença enterprise |
| Contracts | Dependente | Funcionalidades avançadas de contratos podem depender de licença |
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

- Código backend: `src/modules/licensing/`
- Código frontend: `src/frontend/src/features/licensing/`

---

> **Valores de estado válidos:** `NOT_STARTED` | `IN_ANALYSIS` | `GAP_IDENTIFIED` | `IN_FIX` | `BLOCKED` | `READY_FOR_RETEST` | `APPROVED` | `DONE`
>
> **Valores de prioridade válidos:** `CRITICAL` | `HIGH` | `MEDIUM` | `LOW`
