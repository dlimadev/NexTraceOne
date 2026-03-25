# PARTE 11 — Mapa de Dependências do Módulo Product Analytics

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: DEFINIÇÃO FINAL

---

## 1. Dependência com Governance

| Aspecto | Tipo | Estado Atual | Estado Alvo |
|---------|------|-------------|------------|
| Backend location | Acoplamento físico | 🔴 Dentro de Governance | Módulo próprio |
| DbContext | Acoplamento físico | 🔴 GovernanceDbContext | ProductAnalyticsDbContext |
| Table prefix | Acoplamento lógico | 🔴 `gov_` | `pan_` |
| DI Registration | Acoplamento técnico | 🔴 Governance DI | ProductAnalytics DI |
| Permissions | Naming | 🟠 `governance:analytics:*` | `analytics:*` |
| Conceitual | Nenhum | ✅ Independente | Manter independente |
| Dados partilhados | Nenhum | ✅ Sem dependência de dados | Manter independente |
| Eventos | Nenhum | ✅ Governance não emite para Analytics | Manter independente |

**Resumo**: A dependência é **100% física/técnica** (código vive dentro de Governance). Não há dependência conceitual nem de dados. A extração é limpa.

---

## 2. Dependência com Notifications

| Aspecto | Tipo | Estado Atual | Estado Alvo |
|---------|------|-------------|------------|
| Emissão de notificações | Funcional | ❌ Não existe | ⚠️ Opcional — alertas de anomalia de adoção |
| Consumo de notificações | N/A | ❌ | N/A |

**Alvo futuro**: Product Analytics pode emitir domain events (`FrictionThresholdExceeded`, `AdoptionDropDetected`) que o módulo Notifications consumiria para enviar alertas. Isto é P3 (futuro).

---

## 3. Dependência com Operational Intelligence

| Aspecto | Tipo | Estado Atual | Estado Alvo |
|---------|------|-------------|------------|
| Instrumentação de eventos | Emissor → Analytics | ❌ Não implementado | OI emite eventos de uso (IncidentInvestigated, etc.) |
| Consumo de métricas OI | N/A | ❌ | N/A (dados separados) |
| SLA/SLO correlação | N/A | ❌ | N/A (OI mede SLAs, Analytics mede uso) |

**Nota**: Operational Intelligence é um dos módulos com mais tipos de evento definidos no `AnalyticsEventType` (IncidentInvestigated, MitigationWorkflowStarted, MitigationWorkflowCompleted, RunbookViewed, ReliabilityDashboardViewed, AutomationWorkflowManaged). A instrumentação nestes pontos é prioritária.

---

## 4. Dependência com Audit & Compliance

| Aspecto | Tipo | Estado Atual | Estado Alvo |
|---------|------|-------------|------------|
| Publicação de audit events | Analytics → Audit | ❌ Zero audit events | Publicar para ações administrativas |
| Consumo de audit data | N/A | ❌ | N/A |

**Alvo**: Quando implementadas gestão de definições e exportação, essas ações devem publicar audit events para Audit & Compliance.

---

## 5. Dependência com Identity & Access

| Aspecto | Tipo | Estado Atual | Estado Alvo |
|---------|------|-------------|------------|
| TenantId | Contextual (JWT) | ✅ Automático via interceptor | Manter |
| UserId | Contextual (JWT) | ✅ Automático via interceptor | Manter |
| Persona | Contextual (JWT/claim) | ✅ Capturado no evento | Manter |
| Permissions | Authorization | ⚠️ `governance:analytics:*` | `analytics:*` |
| RLS enforcement | Infraestrutura | ✅ Via TenantRlsInterceptor | Manter |
| Total de utilizadores (para adoption %) | Query | ⚠️ Não implementado | Precisaria de interface cross-module |

**Nota importante**: Para calcular "adoption %" de forma real, Product Analytics precisa saber o número total de utilizadores ativos do tenant. Isso requer uma interface (ex: `IUserCountProvider`) exposta por Identity & Access.

---

## 6. Dependência com Change Governance

| Aspecto | Tipo | Estado Atual | Estado Alvo |
|---------|------|-------------|------------|
| Instrumentação de eventos | Emissor → Analytics | ❌ Não implementado | Change Gov emite ChangeViewed |
| Consumo | N/A | ❌ | N/A |

---

## 7. Dependência com Catalog

| Aspecto | Tipo | Estado Atual | Estado Alvo |
|---------|------|-------------|------------|
| Instrumentação de eventos | Emissor → Analytics | ❌ Não implementado | Catalog emite EntityViewed, SearchExecuted |
| Consumo | N/A | ❌ | N/A |

---

## 8. Dependência com Contracts

| Aspecto | Tipo | Estado Atual | Estado Alvo |
|---------|------|-------------|------------|
| Instrumentação de eventos | Emissor → Analytics | ❌ Não implementado | Contracts emite ContractDraftCreated, ContractPublished |
| Consumo | N/A | ❌ | N/A |

---

## 9. Dependência com AI & Knowledge

| Aspecto | Tipo | Estado Atual | Estado Alvo |
|---------|------|-------------|------------|
| Instrumentação de eventos | Emissor → Analytics | ❌ Não implementado | AI emite AssistantPromptSubmitted, AssistantResponseUsed |
| Consumo | N/A | ❌ | N/A |

---

## 10. O que o módulo expõe para outros

| Exposição | Consumidor | Tipo | Status |
|-----------|-----------|------|--------|
| GET /summary | Qualquer módulo que precise de KPIs de produto | API REST | ✅ Implementado |
| GET /adoption/modules | Dashboard de módulos | API REST | ✅ Implementado |
| GET /friction | Alertas de qualidade do produto | API REST | ✅ Implementado |
| Domain event: `FrictionThresholdExceeded` | Notifications | Event | ❌ Não implementado |
| Domain event: `AdoptionDropDetected` | Notifications | Event | ❌ Não implementado |

---

## 11. O que o módulo consome de outros

| Consumo | Fornecedor | Tipo | Status |
|---------|-----------|------|--------|
| TenantId, UserId, Persona | Identity & Access | JWT context | ✅ Automático |
| RLS enforcement | Building Blocks | Interceptor | ✅ Automático |
| NexTraceDbContextBase | Building Blocks | Herança | ✅ Via GovernanceDbContext (alvo: ProductAnalyticsDbContext) |
| Total de utilizadores ativos | Identity & Access | Interface | ❌ Não implementado |
| Eventos de uso (page views) | Frontend (todos os módulos) | POST /events | ⚠️ Apenas ModuleViewed |
| Eventos de ação (feature usage) | Módulos individuais | POST /events | ❌ Não implementado |

---

## 12. Diagrama de dependências

```
┌─────────────────────────────────────────────────────┐
│                    Identity & Access                  │
│  (TenantId, UserId, Persona, Permissions, RLS)       │
└──────────────────────┬──────────────────────────────┘
                       │ JWT context + RLS
                       ▼
┌─────────────────────────────────────────────────────┐
│               Product Analytics                       │
│  ┌─────────────┐  ┌───────────┐  ┌─────────────┐   │
│  │ Event Store │  │ Dashboards│  │ Definitions │   │
│  │ (PG buffer) │  │ (API/UI)  │  │ (PG config) │   │
│  └──────┬──────┘  └─────┬─────┘  └─────────────┘   │
│         │               │                            │
│         ▼               │                            │
│  ┌─────────────┐       │                            │
│  │ ClickHouse  │◀──────┘ (queries)                  │
│  │ (permanent) │                                     │
│  └─────────────┘                                     │
└──────────────────────┬──────────────────────────────┘
                       │ Domain events (futuro)
                       ▼
            ┌──────────────────┐
            │   Notifications   │ (alertas de anomalia)
            └──────────────────┘

Events emitidos por:
┌───────────┐ ┌──────────┐ ┌─────────────┐ ┌──────┐ ┌─────┐
│  Catalog  │ │Contracts │ │Change Gov   │ │  OI  │ │ AI  │
│(EntityV.) │ │(Draft/Pub)│ │(ChangeV.)  │ │(Inc.)│ │(Pmt)│
└─────┬─────┘ └────┬─────┘ └──────┬──────┘ └──┬───┘ └──┬──┘
      │            │              │            │        │
      └────────────┴──────────────┴────────────┴────────┘
                              │
                    POST /events (instrumentation)
                              │
                              ▼
                    Product Analytics
```
